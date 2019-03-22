using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json;

namespace AzureFunctions.Monitoring.Datadog
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class DatadogAttribute : Attribute
    {
        [AppSetting(Default = "DatadogApiKey")] public string DatadogApiKey { get; set; }
    }

    public abstract class Metric
    {
        public abstract string Name { get; }
        public abstract string Type { get; }
        public abstract object Value { get; }
        public abstract string[] Tags { get; }

        public static Gauge Gauge(string name, long value, params string[] tags)
        {
            Console.WriteLine($"Got metric {name}");
            return new Gauge(name, value, tags);
        }
    }

    public class Gauge : Metric
    {
        public Gauge(string name, long value, string [] tags)
        {
            Name = name;
            Value = value;
            Tags = tags;
        }
        public override string Type => "gauge";
        public override string Name { get; }
        public override object Value { get; }
        public override string[] Tags { get; }
    }

    public class DatadogConfiguration : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            Console.WriteLine("Initializing Datadog");
            context
                .AddBindingRule<DatadogAttribute>()
                .BindToCollector<Gauge>(attr => new DatadogSink(this, attr));

            //maybe need - check queue example or something
            //context
            //    .AddBindingRule<DatadogAttribute>()
            //    .BindToCollector<DatadogSink>(attr => new DatadogSink(this, attr));
        }
    }

    public class MetricDto
    {
        public string metric { get; set; }
        public long[][] points { get; set; }
        public string type { get; set; }
        public string host { get; set; }
        //public string[] tags { get; set; }

        static long[] Point(DateTime dt, long value)
        {
            var posix = dt.Subtract(new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerSecond;
            return new long[] { posix, value };
        }

        public static MetricDto From(Metric o)
        {
            var p = Point(DateTime.UtcNow, (long)o.Value);
            return new MetricDto
            {
                metric = o.Name,
                points = new long[][] { p },
                type = o.Type,
                host = "test",
                //tags = o.Tags
            };
        }

        public override string ToString()
        {
            var f = @"{ series: { metric:'{0}',points:'{1}',type:'{2}',host:'{3}',tags:[] } }";
            var s = string.Format(f, metric, points, type, host);
            return s;
        }
    }

    public class SeriesDto
    {
        public MetricDto[] series { get; set; }
    }



    public class DatadogSink : IAsyncCollector<Gauge>
    {
        private DatadogConfiguration config;
        private DatadogAttribute attr;
        private static HttpClient client = new HttpClient();

        public DatadogSink(DatadogConfiguration config, DatadogAttribute attr)
        {
            this.config = config;
            this.attr = attr;
        }

        static DatadogSink()
        {
            client.BaseAddress = new Uri("https://api.datadoghq.com/api/v1/series/");
        }

        public async Task AddAsync(Gauge item, CancellationToken cancellationToken = default(CancellationToken))
        {
            var dto = new SeriesDto { series = new[]{ MetricDto.From(item) } };
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync($"?api_key={attr.DatadogApiKey}", content, cancellationToken);
            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine("Sent");
            }
            else
            {
                Console.WriteLine(result.ReasonPhrase);
            }
            return;
        }
        
        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
