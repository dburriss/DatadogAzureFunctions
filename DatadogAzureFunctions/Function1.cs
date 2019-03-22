using System;
using System.Threading.Tasks;
using AzureFunctions.Monitoring.Datadog;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DatadogAzureFunctions
{
    public static class Function1
    {
        [FunctionName("MetricsAfterFunction")]
        public static void Run1(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, 
            [DatadogMetric] out Metric metric, 
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            metric = Metric.Gauge("metric.name.test", 10, "test", "datadog", "azure-functions");
            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");

        }

        [FunctionName("SyncronousMetrics")]
        public static void Run2(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [DatadogMetric] ICollector<Metric> metrics,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            metrics.Add(Metric.Gauge("metric.name.test", 10, "test", "datadog", "azure-functions"));
            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");

        }


        [FunctionName("MultiMetricsAfterFunction")]
        public static void Run3(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
            [DatadogMetrics] out Metrics metrics,
            ILogger log)
        {
            metrics = new Metrics();
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            metrics.Gauge("metric.name.testgauge", 10, "test", "datadog", "azure-functions");
            metrics.Rate("metric.name.testrate", 20, 5, "test", "datadog", "azure-functions");
            metrics.Count("metric.name.testcount", 20, "test", "datadog", "azure-functions");
            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
        }

        [FunctionName("Other")]
        public static void Run4(
            [TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
            [DatadogMetrics] out Metrics metrics,
            ILogger log)
        {
            metrics = new Metrics();
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            metrics.Gauge("metric.name.testgauge", 5, "test", "datadog", "azure-functions");
            metrics.Rate("metric.name.testrate", 10, 5, "test", "datadog", "azure-functions");
            metrics.Count("metric.name.testcount", 10, "test", "datadog", "azure-functions");
            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");
        }
    }
}
