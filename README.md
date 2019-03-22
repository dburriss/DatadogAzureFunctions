# Datadog with Azure Functions
Experiments with Metrics to Datadog from Azure Functions

> This is just experimentation and is not for production use!


## Usage

There are 2 attributes `DatadogMetric` and `DatadogMetrics`.

Use `DatadogMetric` for a single metric from the function, sent once the code in the function has executed.

```csharp
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
```

Or you can use `ICollector<Metric>` to send multiple metrics but note they are sent as the `Add` method is called.

```csharp
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
```

OR using `DatadogMetrics` you can add metrics to an instance of `Metrics` that assigned to `out` and they will be sent after the code in the function is done executing.

```csharp
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
```