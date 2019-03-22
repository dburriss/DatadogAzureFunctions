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
        [FunctionName("Function1")]
        public static void Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, 
            [Datadog] out Gauge metrics, 
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            metrics = Metric.Gauge("metric.name.test", 10, "test", "datadog", "azure-functions");
            log.LogInformation($"C# Timer trigger function finished at: {DateTime.Now}");

        }
    }
}
