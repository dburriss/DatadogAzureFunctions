using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AzureFunctions.Monitoring.Datadog
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class DatadogMetricAttribute : Attribute
    {
        [AppSetting(Default = "DatadogApiKey")]
        public string DatadogApiKey { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class DatadogMetricsAttribute : Attribute
    {
        [AppSetting(Default = "DatadogApiKey")]
        public string DatadogApiKey { get; set; }
    }

    public class Metric
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public long[][] Value { get; set; }
        public string[] Tags { get; set; }
        public long? Interval { get; set; }

        static long[] Point(DateTime dt, long value)
        {
            var posix = dt.Subtract(new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerSecond;
            return new long[] { posix, value };
        }


        public static Metric Gauge(string name, long value, params string[] tags)
        {
            Console.WriteLine($"Got metric {name}");
            return new Metric(name, value, tags, "gauge");
        }

        public static Metric Rate(string name, long value, long interval, params string[] tags)
        {
            Console.WriteLine($"Got metric {name}");
            return new Metric(name, value, tags, "rate")
            {
                Interval = interval
            };
        }

        public static Metric Count(string name, long value, params string[] tags)
        {
            Console.WriteLine($"Got metric {name}");
            return new Metric(name, value, tags, "count");
        }

        public Metric(string name, long value, string[] tags, string type)
        {
            var p = Point(DateTime.UtcNow, value);

            Name = name;
            Value = new long[][] { p };
            Tags = tags;
            Type = type;
        }
    }

    public class Metrics
    {
        public Queue<Metric> Queue { get; }

        public Metrics()
        {
            Queue = new Queue<Metric>();
        }

        private void Add(Metric metric) => Queue.Enqueue(metric);

        public void Gauge(string name, long value, params string[] tags) => Add(Metric.Gauge(name, value, tags));
        public void Rate(string name, long value, long interval, params string[] tags) => Add(Metric.Gauge(name, value, tags));
        public void Count(string name, long value, params string[] tags) => Add(Metric.Gauge(name, value, tags));
    }

    public class DatadogConfiguration : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            Console.WriteLine("Initializing Datadog");
            context
                .AddBindingRule<DatadogMetricAttribute>()
                .BindToCollector<Metric>(attr => new DatadogSinkSingle(this, attr));

            context
                .AddBindingRule<DatadogMetricsAttribute>()
                .BindToCollector<Metrics>(attr => new DatadogSinkMulti(this, attr));
        }
    }

    public class MetricDto
    {
        public string metric { get; set; }
        public long[][] points { get; set; }
        public string type { get; set; }
        public string host { get; set; }
        public string[] tags { get; set; }

        
        public static MetricDto From(Metric o)
        {
            return new MetricDto
            {
                metric = o.Name,
                points = o.Value,
                type = o.Type,
                host = "function",
                tags = o.Tags
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



    public class DatadogSinkSingle : IAsyncCollector<Metric>
    {
        private DatadogConfiguration config;
        private DatadogMetricAttribute attr;
        private static readonly DatadogHttpClient client;

        public DatadogSinkSingle(DatadogConfiguration config, DatadogMetricAttribute attr)
        {
            this.config = config;
            this.attr = attr;
        }

        static DatadogSinkSingle()
        {
            client = new DatadogHttpClient();
        }

        public async Task AddAsync(Metric item, CancellationToken cancellationToken = default(CancellationToken))
        {
            await client.Send(new Metric[1] { item }, attr.DatadogApiKey, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

    }

    public class DatadogSinkMulti : IAsyncCollector<Metrics>
    {
        private DatadogConfiguration config;
        private DatadogMetricsAttribute attr;
        private static readonly DatadogHttpClient client;

        public DatadogSinkMulti(DatadogConfiguration config, DatadogMetricsAttribute attr)
        {
            this.config = config;
            this.attr = attr;
        }

        static DatadogSinkMulti()
        {
            client = new DatadogHttpClient();
        }

        public async Task AddAsync(Metrics item, CancellationToken cancellationToken = default(CancellationToken))
        {

            await client.Send(item.Queue, attr.DatadogApiKey, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }

    // Quick impl. Make testable later ince laready indirection with attribute bindings.
    public class DatadogHttpClient
    {
        private static HttpClient client = new HttpClient();
        static DatadogHttpClient()
        {
            client.BaseAddress = new Uri("https://api.datadoghq.com/api/v1/series/");
        }

        public async Task Send(IEnumerable<Metric> metrics, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var items = metrics.Select(MetricDto.From).ToArray();
            var dto = new SeriesDto { series = items };

            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync($"?api_key={key}", content, cancellationToken);
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
    }
}
