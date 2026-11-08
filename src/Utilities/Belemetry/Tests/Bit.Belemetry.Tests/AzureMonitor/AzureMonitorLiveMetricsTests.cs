using System.Net;
using System.Text;

namespace Bit.Belemetry.Tests.AzureMonitor;

[TestClass]
public class AzureMonitorLiveMetricsTests
{
    private const string ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;LiveEndpoint=https://live.test/";

    [TestMethod]
    public async Task Exceptions_CountTheOnesRecordedOnSpans_AsTheyAreSent()
    {
        await using var live = Create();

        static BelemetrySpanEvent Exception() => new() { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.type", "System.TimeoutException") };

        var span = Records.Span(BelemetrySpanKind.Server);
        span.Events.Add(Exception());
        span.Events.Add(Exception());
        span.Events.Add(new BelemetrySpanEvent { Name = "exception" });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.stacktrace", "at Y()") });
        span.Events.Add(new BelemetrySpanEvent { Name = "retry" });
        var limited = Exception();
        limited.Limited = true;
        span.Events.Add(limited);
        var logged = Exception();
        logged.LoggedAs = new BelemetryLoggedException("System.TimeoutException", null, null, BelemetrySeverity.Error, false);
        span.Events.Add(logged);

        var log = Records.Log(severity: BelemetrySeverity.Error);
        log.Attributes.Set("exception.type", "System.TimeoutException");

        live.OnRecord(span);
        live.OnRecord(log);

        Assert.AreEqual(3, live.Exceptions, "The 2 exceptions recorded on the span and the logged one - counted once, as its log record. A stack trace alone is no item.");
    }

    [TestMethod]
    public async Task WithoutAConfiguration_NothingIsSampled()
    {
        await using var live = Create();

        live.OnRecord(Request(500));

        var point = Post(live);
        Assert.AreEqual(0, point.GetProperty("Documents").GetArrayLength());
        Assert.AreEqual(0, point.GetProperty("CollectionConfigurationErrors").GetArrayLength());
        Assert.AreEqual(11, point.GetProperty("Metrics").GetArrayLength(), "The counters only.");
    }

    [TestMethod]
    public async Task TheStreams_SampleWhatTheirFiltersMatch_WithTheirIds()
    {
        await using var live = Create();
        Apply(live, """
            {
              "ETag": "e1", "Metrics": [],
              "DocumentStreams": [
                { "Id": "failed", "DocumentFilterGroups": [
                    { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "Success", "Predicate": "Equal", "Comparand": "false" } ] } },
                    { "TelemetryType": "Dependency", "Filters": { "Filters": [ { "FieldName": "Success", "Predicate": "Equal", "Comparand": "false" } ] } } ] },
                { "Id": "all-requests", "DocumentFilterGroups": [
                    { "TelemetryType": "Request", "Filters": { "Filters": [] } } ] },
                { "Id": "orders", "DocumentFilterGroups": [
                    { "TelemetryType": "Trace", "Filters": { "Filters": [ { "FieldName": "Message", "Predicate": "Contains", "Comparand": "ORDER" } ] } } ] }
              ]
            }
            """);

        live.OnRecord(Request(200));
        live.OnRecord(Request(503));
        live.OnRecord(Records.Log("Order 7 placed"));
        live.OnRecord(Records.Log("Cart emptied"));
        var dependency = Records.Span();
        dependency.Status = BelemetryStatusCode.Error;
        live.OnRecord(dependency);

        var documents = Post(live).GetProperty("Documents").EnumerateArray().ToList();

        Assert.HasCount(4, documents);
        var ok = documents[0];
        Assert.AreEqual("Request", ok.GetProperty("DocumentType").GetString());
        Assert.AreEqual("DocumentType", ok.EnumerateObject().First().Name, "The discriminator comes first.");
        CollectionAssert.AreEqual(new[] { "all-requests" }, StreamIds(ok));
        Assert.AreEqual("GET /orders/{id}", ok.GetProperty("Name").GetString());
        Assert.AreEqual("https://shop.test/orders/7", ok.GetProperty("Url").GetString());
        Assert.AreEqual("200", ok.GetProperty("ResponseCode").GetString());
        Assert.AreEqual("PT1.5S", ok.GetProperty("Duration").GetString(), "An ISO 8601 duration.");

        CollectionAssert.AreEqual(new[] { "failed", "all-requests" }, StreamIds(documents[1]));
        Assert.AreEqual("503", documents[1].GetProperty("ResponseCode").GetString());

        Assert.AreEqual("Trace", documents[2].GetProperty("DocumentType").GetString());
        Assert.AreEqual("Order 7 placed", documents[2].GetProperty("Message").GetString());
        CollectionAssert.AreEqual(new[] { "orders" }, StreamIds(documents[2]));

        Assert.AreEqual("RemoteDependency", documents[3].GetProperty("DocumentType").GetString());
        Assert.AreEqual("GET /api/orders", documents[3].GetProperty("Name").GetString());
        Assert.AreEqual("PT1.5S", documents[3].GetProperty("Duration").GetString());
        CollectionAssert.AreEqual(new[] { "failed" }, StreamIds(documents[3]));

        Assert.AreEqual(0, Post(live).GetProperty("Documents").GetArrayLength(), "A sample is sent once.");
    }

    [TestMethod]
    public async Task ASample_CarriesTheAttributes_ButNotTheIds()
    {
        await using var live = Create();
        Apply(live, Streams("""{ "TelemetryType": "Request", "Filters": { "Filters": [] } }"""));

        var span = Request(200);
        span.Attributes.Set("tenant", "contoso").Set(BelemetrySemanticConventions.SessionId, "s").Set(BelemetrySemanticConventions.UserId, "u")
                       .Set(BelemetrySemanticConventions.EndUserPseudoId, "p");
        live.OnRecord(span);

        var properties = Post(live).GetProperty("Documents")[0].GetProperty("Properties").EnumerateArray()
                                   .ToDictionary(p => p.GetProperty("key").GetString()!, p => p.GetProperty("value").GetString());

        Assert.AreEqual("contoso", properties["tenant"]);
        Assert.AreEqual("200", properties[BelemetrySemanticConventions.HttpResponseStatusCode]);
        Assert.IsFalse(properties.ContainsKey(BelemetrySemanticConventions.SessionId));
        Assert.IsFalse(properties.ContainsKey(BelemetrySemanticConventions.UserId));
        Assert.IsFalse(properties.ContainsKey(BelemetrySemanticConventions.EndUserPseudoId));
    }

    [TestMethod]
    public async Task Exceptions_AreSampled_FromLogsAndSpans_OnceEach()
    {
        await using var live = Create();
        Apply(live, Streams("""
            { "TelemetryType": "Exception", "Filters": { "Filters": [ { "FieldName": "Exception.Message", "Predicate": "Contains", "Comparand": "timed out" } ] } }
            """));

        var log = Records.Log("Payment failed", severity: BelemetrySeverity.Error);
        log.Attributes.Set("exception.type", "System.TimeoutException").Set("exception.message", "The call timed out").Set("exception.stacktrace", "at X()").Set("order", "7");
        live.OnRecord(log);

        var span = Records.Span(BelemetrySpanKind.Server);
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.type", "IOException").Set("exception.message", "Read timed out") });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.type", "IOException").Set("exception.message", "Disk full") });
        var logged = new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.type", "System.TimeoutException").Set("exception.message", "The call timed out") };
        logged.LoggedAs = new BelemetryLoggedException("System.TimeoutException", "The call timed out", null, BelemetrySeverity.Error, false);
        span.Events.Add(logged);
        live.OnRecord(span);

        var documents = Post(live).GetProperty("Documents").EnumerateArray().ToList();

        Assert.HasCount(2, documents, "The logged exception once - as its log record - and the one only on the span; not the one the filter leaves out.");
        Assert.AreEqual("Exception", documents[0].GetProperty("DocumentType").GetString());
        Assert.AreEqual("System.TimeoutException", documents[0].GetProperty("ExceptionType").GetString());
        Assert.AreEqual("The call timed out", documents[0].GetProperty("ExceptionMessage").GetString());
        CollectionAssert.AreEqual(new[] { "order" }, documents[0].GetProperty("Properties").EnumerateArray().Select(p => p.GetProperty("key").GetString()).ToArray(),
                                  "Its exception.* attributes are its fields.");
        Assert.AreEqual("Read timed out", documents[1].GetProperty("ExceptionMessage").GetString());
    }

    [TestMethod]
    public async Task Events_AreSampled_ByName()
    {
        await using var live = Create();
        Apply(live, Streams("""{ "TelemetryType": "Event", "Filters": { "Filters": [ { "FieldName": "Name", "Predicate": "Equal", "Comparand": "checkout" } ] } }"""));

        live.OnRecord(Records.Log(eventName: "checkout"));
        live.OnRecord(Records.Log(eventName: "browse"));
        live.OnRecord(Records.Log(eventName: BelemetrySemanticConventions.PageViewEvent));

        var document = Post(live).GetProperty("Documents").EnumerateArray().Single();
        Assert.AreEqual("Event", document.GetProperty("DocumentType").GetString());
        Assert.AreEqual("checkout", document.GetProperty("Name").GetString());
    }

    [TestMethod]
    public async Task TheSamples_FollowTheExportersSignalsAndLogLevel()
    {
        var all = Streams("""
            { "TelemetryType": "Request", "Filters": { "Filters": [] } },
            { "TelemetryType": "Trace", "Filters": { "Filters": [] } }
            """);

        await using (var traces = Create(o => o.Signals = BelemetrySignals.Traces))
        {
            Apply(traces, all);
            traces.OnRecord(Request(200));
            traces.OnRecord(Records.Log("hello", severity: BelemetrySeverity.Warn));
            CollectionAssert.AreEqual(new[] { "Request" }, DocumentTypes(Post(traces)), "Application Insights gets no logs from this exporter.");
        }

        await using var warnings = Create(o => o.MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning);
        Apply(warnings, all);
        warnings.OnRecord(Records.Log("info", severity: BelemetrySeverity.Info));
        warnings.OnRecord(Records.Log("warn", severity: BelemetrySeverity.Warn));
        var document = Post(warnings).GetProperty("Documents").EnumerateArray().Single();
        Assert.AreEqual("warn", document.GetProperty("Message").GetString());
    }

    [TestMethod]
    public async Task AStreamsQuota_LimitsItsSamples()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1", "Metrics": [], "QuotaInfo": { "InitialQuota": 2, "MaxQuota": 2, "QuotaAccrualRatePerSec": 0 },
              "DocumentStreams": [
                { "Id": "a", "DocumentFilterGroups": [ { "TelemetryType": "Request", "Filters": { "Filters": [] } } ] },
                { "Id": "b", "DocumentFilterGroups": [ { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "ResponseCode", "Predicate": "GreaterThanOrEqual", "Comparand": "500" } ] } } ] } ] }
            """);

        for (var i = 0; i < 5; i++) live.OnRecord(Request(200));
        live.OnRecord(Request(500));
        live.OnRecord(Request(500));
        live.OnRecord(Request(500));

        var documents = Post(live).GetProperty("Documents").EnumerateArray().Select(StreamIds).Select(ids => string.Join(",", ids)).ToArray();

        CollectionAssert.AreEqual(new[] { "a", "a", "b", "b" }, documents, "Each stream takes two; a sample no stream has quota for is not sent.");
    }

    [TestMethod]
    public async Task TheDefaultQuota_LetsABurstThrough_NotAFlood_AndEarnsSamplesBack()
    {
        var time = new ManualTime();
        await using var live = Create(time: time);
        Apply(live, Streams("""{ "TelemetryType": "Request", "Filters": { "Filters": [] } }"""));

        for (var i = 0; i < 50; i++) live.OnRecord(Request(200));
        Assert.AreEqual((int)AzureMonitorLiveConfiguration.DefaultInitialQuota, Post(live).GetProperty("Documents").GetArrayLength(), "The clock stands still: nothing is earned back.");

        time.Advance(TimeSpan.FromSeconds(3));
        for (var i = 0; i < 50; i++) live.OnRecord(Request(200));
        Assert.AreEqual(1, Post(live).GetProperty("Documents").GetArrayLength(), "One sample every 2 seconds.");
    }

    [TestMethod]
    public async Task CustomCharts_AreMetricsOfTheDataPoint()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1", "DocumentStreams": [],
              "Metrics": [
                { "Id": "ok-duration", "TelemetryType": "Request", "Projection": "Duration", "Aggregation": "Avg", "BackEndAggregation": "Avg",
                  "FilterGroups": [ { "Filters": [ { "FieldName": "Success", "Predicate": "Equal", "Comparand": "True" } ] } ] },
                { "Id": "calls", "TelemetryType": "Dependency", "Projection": "Count()", "Aggregation": "Sum", "BackEndAggregation": "Sum", "FilterGroups": [] },
                { "Id": "max-items", "TelemetryType": "Event", "Projection": "CustomDimensions.items", "Aggregation": "Max", "BackEndAggregation": "Max", "FilterGroups": [] },
                { "Id": "min-items", "TelemetryType": "Event", "Projection": "CustomDimensions.items", "Aggregation": "Min", "BackEndAggregation": "Min", "FilterGroups": [] }
              ] }
            """);

        live.OnRecord(Request(200, seconds: 1));
        live.OnRecord(Request(201, seconds: 3));
        live.OnRecord(Request(500, seconds: 10));
        for (var i = 0; i < 3; i++) live.OnRecord(Records.Span());
        foreach (var items in new[] { "4", "-2", "9" })
        {
            var checkout = Records.Log(eventName: "checkout");
            checkout.Attributes.Set("items", items);
            live.OnRecord(checkout);
        }

        var point = Post(live);
        var metrics = Metrics(point);
        Assert.AreEqual(2000, metrics["ok-duration"], "The average duration of the successful requests, in milliseconds - not of the failed one.");
        Assert.AreEqual(3, metrics["calls"]);
        Assert.AreEqual(9, metrics["max-items"]);
        Assert.AreEqual(-2, metrics["min-items"]);
        var weights = Weights(point);
        Assert.AreEqual(2, weights["ok-duration"], "Weighed by the items aggregated.");
        Assert.AreEqual(3, weights["calls"]);
        Assert.AreEqual(1, weights[@"\ApplicationInsights\Requests/Sec"], "A built-in metric weighs 1.");

        var next = Post(live);
        Assert.AreEqual(0, Metrics(next)["ok-duration"], "Each data point starts over; nothing is 0.");
        Assert.AreEqual(0, Metrics(next)["calls"]);
        Assert.AreEqual(0, Metrics(next)["max-items"]);
        Assert.AreEqual(0, Weights(next)["ok-duration"], "A data point without items weighs nothing.");
    }

    [TestMethod]
    public async Task ACustomChart_ThatFailsOnAnItem_IsReportedOncePerDataPoint()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1", "DocumentStreams": [],
              "Metrics": [ { "Id": "items", "TelemetryType": "Event", "Projection": "CustomDimensions.items", "Aggregation": "Sum", "BackEndAggregation": "Sum", "FilterGroups": [] } ] }
            """);

        live.OnRecord(Records.Log(eventName: "checkout"));
        var bad = Records.Log(eventName: "checkout");
        bad.Attributes.Set("items", "many");
        live.OnRecord(bad);

        var errors = Post(live).GetProperty("CollectionConfigurationErrors").EnumerateArray().ToList();
        var error = errors.Single();
        Assert.AreEqual("MetricFailureToCreate", error.GetProperty("CollectionConfigurationErrorType").GetString());
        Assert.AreEqual("items", Data(error)["MetricId"]);
        Assert.AreEqual("e1", Data(error)["ETag"]);

        Assert.AreEqual(0, Post(live).GetProperty("CollectionConfigurationErrors").GetArrayLength());
    }

    [TestMethod]
    public async Task WhatCannotBeApplied_IsReported_AndTheRestApplies()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e2",
              "DocumentStreams": [
                { "Id": "s", "DocumentFilterGroups": [
                    { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "Colour", "Predicate": "Equal", "Comparand": "red" } ] } },
                    { "TelemetryType": "PerformanceCounter", "Filters": { "Filters": [] } },
                    { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "Url", "Predicate": "Contains", "Comparand": "orders" } ] } } ] },
                { "Id": "s", "DocumentFilterGroups": [] }
              ],
              "Metrics": [
                { "Id": "m", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "BackEndAggregation": "Sum", "FilterGroups": [] },
                { "Id": "m", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "BackEndAggregation": "Sum", "FilterGroups": [] },
                { "Id": "counter", "TelemetryType": "PerformanceCounter", "Projection": "Count()", "Aggregation": "Sum", "BackEndAggregation": "Sum", "FilterGroups": [] },
                { "Id": "custom", "TelemetryType": "Request", "Projection": "CustomMetrics.x", "Aggregation": "Sum", "BackEndAggregation": "Sum", "FilterGroups": [] },
                { "Id": "ordered", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "BackEndAggregation": "Sum",
                  "FilterGroups": [ { "Filters": [ { "FieldName": "Url", "Predicate": "GreaterThan", "Comparand": "a" } ] } ] }
              ] }
            """);

        var configuration = live.Configuration!;
        CollectionAssert.AreEqual(new[] { "s" }, configuration.StreamIds.ToArray());
        CollectionAssert.AreEqual(new[] { "m" }, configuration.MetricIds.ToArray());

        live.OnRecord(Request(200));
        var point = Post(live);
        Assert.HasCount(1, point.GetProperty("Documents").EnumerateArray().ToList(), "The stream's valid group still samples.");
        Assert.AreEqual(1, Metrics(point)["m"]);

        var errors = point.GetProperty("CollectionConfigurationErrors").EnumerateArray()
                          .Select(e => $"{e.GetProperty("CollectionConfigurationErrorType").GetString()}:{Data(e).GetValueOrDefault("DocumentStreamInfoId") ?? Data(e)["MetricId"]}")
                          .ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "DocumentStreamFailureToCreateFilterUnexpected:s",
            "DocumentStreamFailureToCreate:s",
            "DocumentStreamDuplicateIds:s",
            "MetricDuplicateIds:m",
            "MetricTelemetryTypeUnsupported:counter",
            "MetricFailureToCreate:custom",
            "MetricFailureToCreateFilterUnexpected:ordered",
        }, errors);

        Assert.HasCount(7, Post(live).GetProperty("CollectionConfigurationErrors").EnumerateArray().ToList(), "Reported with every data point while the configuration lasts.");
    }

    [TestMethod]
    public async Task FiltersThatAreNotAList_AreReported_NotTakenAsNone()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1",
              "DocumentStreams": [
                { "Id": "s", "DocumentFilterGroups": [
                    { "TelemetryType": "Request", "Filters": { "Filters": "Success == false" } },
                    { "TelemetryType": "Request", "Filters": [ { "FieldName": "Success", "Predicate": "Equal", "Comparand": "false" } ] },
                    { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "Url", "Predicate": "Contains", "Comparand": "cart" } ] } } ] },
                { "Id": "groups", "DocumentFilterGroups": { "TelemetryType": "Request" } }
              ],
              "Metrics": [
                { "Id": "a", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "FilterGroups": "all" },
                { "Id": "b", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum",
                  "FilterGroups": [ { "Filters": [] }, { "Filters": { "FieldName": "Success", "Predicate": "Equal", "Comparand": "false" } } ] },
                { "Id": "c", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "FilterGroups": [ "Success == false" ] },
                { "Id": "ok", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "FilterGroups": [ { "Filters": [] }, { "Filters": null } ] }
              ] }
            """);

        CollectionAssert.AreEqual(new[] { "s" }, live.Configuration!.StreamIds.ToArray());
        CollectionAssert.AreEqual(new[] { "ok" }, live.Configuration.MetricIds.ToArray(), "An absent or null list is an empty one.");

        live.OnRecord(Request(200));
        var point = Post(live);
        Assert.AreEqual(0, point.GetProperty("Documents").GetArrayLength(), "The groups left out match nothing - not every item.");
        Assert.AreEqual(1, Metrics(point)["ok"]);

        var errors = point.GetProperty("CollectionConfigurationErrors").EnumerateArray()
                          .Select(e => (Type: e.GetProperty("CollectionConfigurationErrorType").GetString(), Message: e.GetProperty("Message").GetString()!,
                                        Id: Data(e).GetValueOrDefault("DocumentStreamInfoId") ?? Data(e)["MetricId"]))
                          .ToList();
        CollectionAssert.AreEqual(new[]
        {
            "DocumentStreamFailureToCreateFilterUnexpected:s",
            "DocumentStreamFailureToCreateFilterUnexpected:s",
            "DocumentStreamFailureToCreate:groups",
            "MetricFailureToCreateFilterUnexpected:a",
            "MetricFailureToCreateFilterUnexpected:b",
            "MetricFailureToCreateFilterUnexpected:c",
        }, errors.Select(e => $"{e.Type}:{e.Id}").ToArray());
        Assert.StartsWith("Filter group 0:", errors[0].Message);
        Assert.StartsWith("Filter group 1:", errors[1].Message);
        Assert.StartsWith("Filter group 1:", errors[4].Message, "The group of the metric that holds it.");
    }

    [TestMethod]
    public async Task ThePropertiesTheEnvelopeAdds_AreDimensionsToo_UnlessAnAttributeHasTheirKey()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1",
              "DocumentStreams": [ { "Id": "all", "DocumentFilterGroups": [
                { "TelemetryType": "Event", "Filters": { "Filters": [ { "FieldName": "CustomDimensions.message", "Predicate": "Contains", "Comparand": "paid" } ] } },
                { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "CustomDimensions.message", "Predicate": "Equal", "Comparand": "Payment declined" } ] } },
                { "TelemetryType": "Trace", "Filters": { "Filters": [] } } ] } ],
              "Metrics": [ { "Id": "orders", "TelemetryType": "Trace", "Projection": "Count()", "Aggregation": "Sum",
                             "FilterGroups": [ { "Filters": [ { "FieldName": "CustomDimensions.CategoryName", "Predicate": "Equal", "Comparand": "MyApp.Orders" } ] } ] } ] }
            """);

        live.OnRecord(Records.Log("Order paid", eventName: "checkout"));
        var explicitMessage = Records.Log("Order paid", eventName: "checkout");
        explicitMessage.Attributes.Set("message", "kept");
        live.OnRecord(explicitMessage);
        var failed = Request(500);
        failed.Status = BelemetryStatusCode.Error;
        failed.StatusMessage = "Payment declined";
        live.OnRecord(failed);
        live.OnRecord(Records.Log("Cart emptied"));
        var categorized = Records.Log("Cart restored");
        categorized.Attributes.Set("CategoryName", "Custom");
        live.OnRecord(categorized);

        var point = Post(live);
        var documents = point.GetProperty("Documents").EnumerateArray().ToList();
        Assert.HasCount(4, documents, "Not the event whose own message attribute the filter does not match.");
        Assert.AreEqual("Order paid", Properties(documents[0])["message"], "An event's body.");
        Assert.AreEqual("Payment declined", Properties(documents[1])["message"], "A failed span's status message.");
        Assert.AreEqual("MyApp.Orders", Properties(documents[2])["CategoryName"], "A trace's category.");
        Assert.AreEqual("Custom", Properties(documents[3])["CategoryName"]);
        Assert.HasCount(1, documents[3].GetProperty("Properties").EnumerateArray().ToList(), "The attribute only.");
        Assert.AreEqual(1, Metrics(point)["orders"]);
    }

    [TestMethod]
    public async Task ASpansNamedEvents_AreSampledAsTraces()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1",
              "DocumentStreams": [ { "Id": "all", "DocumentFilterGroups": [ { "TelemetryType": "Trace", "Filters": { "Filters": [] } } ] } ],
              "Metrics": [ { "Id": "traces", "TelemetryType": "Trace", "Projection": "Count()", "Aggregation": "Sum", "FilterGroups": [] } ] }
            """);

        var span = Records.Span(BelemetrySpanKind.Server);
        span.Events.Add(new BelemetrySpanEvent { Name = "cache.miss", Attributes = new BelemetryAttributes().Set("cache.key", "orders:7") });
        span.Events.Add(new BelemetrySpanEvent { Name = string.Empty });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.type", "IOException") });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.stacktrace", "at X()") });
        live.OnRecord(span);

        var point = Post(live);
        var document = point.GetProperty("Documents").EnumerateArray().Single();
        Assert.AreEqual("Trace", document.GetProperty("DocumentType").GetString());
        Assert.AreEqual("cache.miss", document.GetProperty("Message").GetString());
        Assert.AreEqual("orders:7", Properties(document)["cache.key"]);
        Assert.AreEqual(1, Metrics(point)["traces"], "Not the unnamed event, nor the exceptions.");
    }

    [TestMethod]
    public async Task ASampleOfAFeatureTheUserRevokes_IsNotSent_EvenWhenGrantedAgain()
    {
        await using var host = TestHost.Create();
        await using var live = Create(settings: host.Settings);
        live.Attach(host.Pipeline);
        Apply(live, Streams("""
            { "TelemetryType": "Exception", "Filters": { "Filters": [] } },
            { "TelemetryType": "Trace", "Filters": { "Filters": [] } }
            """));

        var error = Records.Log("Payment failed", severity: BelemetrySeverity.Error);
        error.Attributes.Set("exception.type", "System.TimeoutException");
        error.Feature = BelemetryFeatures.Errors;
        live.OnRecord(error);
        live.OnRecord(Records.Log("Cart emptied"));

        host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors);
        host.Consent.Decide(BelemetryFeatures.All);

        CollectionAssert.AreEqual(new[] { "Trace" }, DocumentTypes(Post(live)),
                                  "The exception goes - the pipeline purged its record at the revoke; the trace, whose feature was not revoked, stays.");
    }

    [TestMethod]
    public async Task ASampleWithoutAPlaceInTheQueue_SpendsNoQuota()
    {
        await using var live = Create();
        Apply(live, """
            { "ETag": "e1", "Metrics": [], "QuotaInfo": { "InitialQuota": 150, "MaxQuota": 150, "QuotaAccrualRatePerSec": 0 },
              "DocumentStreams": [ { "Id": "all", "DocumentFilterGroups": [ { "TelemetryType": "Request", "Filters": { "Filters": [] } } ] } ] }
            """);

        for (var i = 0; i < 90; i++) live.OnRecord(Request(200));
        Parallel.For(0, 64, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ => live.OnRecord(Request(200)));
        Assert.AreEqual(100, Post(live).GetProperty("Documents").GetArrayLength(), "The queue holds 100 samples; the threads racing for its last places took no more.");

        for (var i = 0; i < 80; i++) live.OnRecord(Request(200));
        Assert.AreEqual(50, Post(live).GetProperty("Documents").GetArrayLength(), "150 - 100: the samples that found no place spent none of the quota.");
    }

    [TestMethod]
    public async Task OnceTheStreamStops_ItNeitherSamplesNorCounts()
    {
        await using var host = TestHost.Create();
        using var stopping = new CancellationTokenSource();
        await using var live = Create(settings: host.Settings, handler: new FakeHttpMessageHandler());
        live.Start(host.Pipeline, stopping.Token);
        Apply(live, Streams("""{ "TelemetryType": "Exception", "Filters": { "Filters": [] } }"""));

        stopping.Cancel();
        await WaitUntil(() => live.Configuration is null);
        host.Pipeline.Enqueue(Error());
        Assert.AreEqual(0, live.Exceptions, "The loop's end detaches it from the pipeline.");

        // Disposed without a loop that ran: the same.
        var attached = Create(settings: host.Settings);
        attached.Attach(host.Pipeline);
        Apply(attached, Streams("""{ "TelemetryType": "Exception", "Filters": { "Filters": [] } }"""));
        await attached.DisposeAsync();
        host.Pipeline.Enqueue(Error());
        Assert.IsNull(attached.Configuration);
        Assert.AreEqual(0, attached.Exceptions);

        static BelemetryLogRecord Error()
        {
            var error = Records.Log("Payment failed", severity: BelemetrySeverity.Error);
            error.Attributes.Set("exception.type", "System.TimeoutException");
            return error;
        }
    }

    [TestMethod]
    public async Task TheStream_SendsThroughTheAppsHandler()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, r => Answer(r, subscribed: false, etag: null, string.Empty));
        await using var host = TestHost.Create(o => o.AddAzureMonitorExporter(ConnectionString, e => e.EnableLiveMetrics = true),
                                               configureServices: s => s.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler)));

        await host.Pipeline.Exporters.LiveMetrics.Single().PingAsync(CancellationToken.None);

        Assert.AreEqual("https://live.test/QuickPulseService.svc/ping?api-version=2024-04-01-preview&ikey=11111111-2222-3333-4444-555555555555",
                        handler.Requests.Single().Message.RequestUri!.ToString(), "As the rest of the export traffic: through a proxy, an in-process server, ...");
    }

    [TestMethod]
    public async Task APageVisitTimeSample_HasTheEnvelopesProperties()
    {
        await using var live = Create();
        Apply(live, Streams("""{ "TelemetryType": "Event", "Filters": { "Filters": [ { "FieldName": "CustomDimensions.PageName", "Predicate": "Equal", "Comparand": "Orders" } ] } }"""));

        var visit = Records.Log("/orders", eventName: BelemetrySemanticConventions.PageVisitTimeEvent);
        visit.Attributes.Set(BelemetrySemanticConventions.UrlFull, "https://shop.test/orders").Set(BelemetrySemanticConventions.UrlPath, "/orders")
                        .Set(BelemetrySemanticConventions.PageTitle, "Orders").Set(BelemetrySemanticConventions.PageVisitDuration, 1200L)
                        .Set("PageName", "an attribute").Set("tenant", "contoso");
        live.OnRecord(visit);

        var document = Post(live).GetProperty("Documents").EnumerateArray().Single();
        Assert.AreEqual("PageVisitTime", document.GetProperty("Name").GetString());
        var properties = Properties(document);
        CollectionAssert.AreEqual(new Dictionary<string, string?> { ["tenant"] = "contoso", ["PageName"] = "Orders", ["PageUrl"] = "https://shop.test/orders" }, properties,
                                  "Its fields' attributes are no properties, and its PageName takes the place of the attribute's.");

        using var envelope = JsonDocument.Parse(AzureMonitorEnvelopeSerializer.Serialize(new BelemetryBatch(Records.Resource(), logs: [visit]), new("ikey", MaskIp: false), out _));
        var written = envelope.RootElement[0].GetProperty("data").GetProperty("baseData").GetProperty("properties").EnumerateObject()
                              .Select(p => KeyValuePair.Create(p.Name, (string?)p.Value.GetString())).ToList();
        CollectionAssert.AreEqual(properties.ToList(), written, "The properties of its item in Application Insights, once each.");
    }

    [TestMethod]
    public async Task AConfigurationChange_KeepsWhatItsUnchangedStreamsAndChartsHold()
    {
        await using var live = Create();
        const string requests = """{ "Id": "requests", "DocumentFilterGroups": [ { "TelemetryType": "Request", "Filters": { "Filters": [] } } ] }""";
        const string succeeded = """{ "Id": "requests", "DocumentFilterGroups": [ { "TelemetryType": "Request", "Filters": { "Filters": [ { "FieldName": "Success", "Predicate": "Equal", "Comparand": "true" } ] } } ] }""";
        const string traces = """{ "Id": "traces", "DocumentFilterGroups": [ { "TelemetryType": "Trace", "Filters": { "Filters": [] } } ] }""";
        static string Configuration(string streams) => $$"""
            { "QuotaInfo": { "InitialQuota": 2, "MaxQuota": 2, "QuotaAccrualRatePerSec": 0 }, "DocumentStreams": [ {{streams}} ],
              "Metrics": [ { "Id": "count", "TelemetryType": "Request", "Projection": "Count()", "Aggregation": "Sum", "FilterGroups": [] } ] }
            """;

        Apply(live, Configuration(requests), "e1");
        live.OnRecord(Request(200));

        // Someone opens another stream: the one of requests and the chart are as they were.
        Apply(live, Configuration($"{requests}, {traces}"), "e2");
        live.OnRecord(Request(200));
        live.OnRecord(Request(200));

        var point = Post(live);
        Assert.AreEqual(2, point.GetProperty("Documents").GetArrayLength(), "The sample taken before the change stays, and the stream had 1 of its 2 samples left.");
        Assert.AreEqual(3, Metrics(point)["count"], "The chart kept what it counted before the change.");

        // Someone edits the stream's filters: it is a new one, whose quota starts over and whose waiting samples go at the next edit.
        Apply(live, Configuration(succeeded), "e3");
        live.OnRecord(Request(200));
        Apply(live, Configuration(requests), "e4");
        Assert.AreEqual(0, Post(live).GetProperty("Documents").GetArrayLength(), "The sample the edited filters took went.");
        live.OnRecord(Request(200));
        live.OnRecord(Request(200));
        live.OnRecord(Request(200));
        Assert.AreEqual(2, Post(live).GetProperty("Documents").GetArrayLength());
    }

    [TestMethod]
    public async Task TheOlderProtocolsNumberedEnums_AreRead()
    {
        await using var live = Create();
        // TelemetryType 0 = Request, 4 = Metric, 6 = Trace; Predicate 3 = GreaterThan, 6 = Contains; Aggregation 1 = Sum, 2 = Min, 3 = Max.
        Apply(live, """
            { "ETag": "e1",
              "DocumentStreams": [
                { "Id": "slow", "DocumentFilterGroups": [ { "TelemetryType": 0, "Filters": { "Filters": [ { "FieldName": "Duration", "Predicate": 3, "Comparand": "0.0:0:1" } ] } } ] },
                { "Id": "orders", "DocumentFilterGroups": [ { "TelemetryType": 6, "Filters": { "Filters": [ { "FieldName": "Message", "Predicate": 6, "Comparand": "ORDER" } ] } } ] } ],
              "Metrics": [
                { "Id": "n", "TelemetryType": 0, "Projection": "Count()", "Aggregation": 1, "BackEndAggregation": 1, "FilterGroups": [] },
                { "Id": "fastest", "TelemetryType": 0, "Projection": "Duration", "Aggregation": 2, "FilterGroups": [] },
                { "Id": "slowest", "TelemetryType": 0, "Projection": "Duration", "Aggregation": 3, "FilterGroups": [] },
                { "Id": "metric", "TelemetryType": 4, "Projection": "Count()", "Aggregation": 1, "FilterGroups": [] },
                { "Id": "median", "TelemetryType": 0, "Projection": "Count()", "Aggregation": 9, "FilterGroups": [] } ] }
            """);

        live.OnRecord(Request(200));
        live.OnRecord(Request(200, seconds: 0.5));
        live.OnRecord(Records.Log("Order 7 placed"));

        var point = Post(live);
        var documents = point.GetProperty("Documents").EnumerateArray().ToList();
        Assert.HasCount(2, documents);
        CollectionAssert.AreEqual(new[] { "slow" }, StreamIds(documents[0]), "1.5 s is over 1 s; 0.5 s is not.");
        CollectionAssert.AreEqual(new[] { "orders" }, StreamIds(documents[1]));
        Assert.AreEqual(2, Metrics(point)["n"]);
        Assert.AreEqual(500, Metrics(point)["fastest"]);
        Assert.AreEqual(1500, Metrics(point)["slowest"]);
        CollectionAssert.AreEqual(new[] { "The telemetry type 'Metric' is not supported: Belemetry has no items of it.", "The aggregation '' is not supported." },
                                  point.GetProperty("CollectionConfigurationErrors").EnumerateArray().Select(e => e.GetProperty("Message").GetString()).ToArray());
    }

    [TestMethod]
    public async Task AnUnreadableConfiguration_IsReportedToThePortal_UnderItsEtag()
    {
        var diagnostics = new List<string>();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, r => Answer(r, subscribed: true, etag: "e2", "not json"))
            .Enqueue(HttpStatusCode.OK, r => Answer(r, subscribed: true, etag: "e2", "not json"));
        await using var live = Create(diagnostic: diagnostics.Add, handler: handler);
        Apply(live, Streams("""{ "TelemetryType": "Request", "Filters": { "Filters": [] } }"""));
        live.OnRecord(Request(200));

        await live.PingAsync(CancellationToken.None);
        live.OnRecord(Request(200));
        await live.PostAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        live.ApplyConfiguration([], "e3");

        Assert.AreEqual("e2", handler.Requests[1].Message.Headers.GetValues("x-ms-qps-configuration-etag").Single(),
                        "Its etag is carried back: the service does not send the body again with every response.");
        Assert.AreEqual("e2", live.Configuration!.ETag, "An empty body changes nothing.");
        Assert.HasCount(1, diagnostics, "Read once.");

        using var post = JsonDocument.Parse(handler.Requests[1].Body);
        var point = post.RootElement[0];
        Assert.AreEqual(0, point.GetProperty("Documents").GetArrayLength(), "Nothing is sampled - and what the configuration it replaced took went.");
        var error = point.GetProperty("CollectionConfigurationErrors").EnumerateArray().Single();
        Assert.AreEqual("CollectionConfigurationFailureToCreateUnexpected", error.GetProperty("CollectionConfigurationErrorType").GetString());
        CollectionAssert.AreEqual(new Dictionary<string, string?> { ["ETag"] = "e2" }, Data(error));
        Assert.HasCount(1, Post(live).GetProperty("CollectionConfigurationErrors").EnumerateArray().ToList(), "Reported with every data point while it lasts.");

        Apply(live, Streams("""{ "TelemetryType": "Request", "Filters": { "Filters": [] } }"""), "e4");
        live.OnRecord(Request(200));
        var readable = Post(live);
        Assert.AreEqual(1, readable.GetProperty("Documents").GetArrayLength(), "The next one the portal makes applies.");
        Assert.AreEqual(0, readable.GetProperty("CollectionConfigurationErrors").GetArrayLength());
    }

    [TestMethod]
    [DataRow("0.0:0:0.2", 200d)]
    [DataRow("14.6:56:7.89", 1234567890d)]
    [DataRow("00:00:01", 1000d)]
    [DataRow("1.00:00:00", 86400000d)]
    public void ADurationComparand_IsReadAsTheServiceWritesIt(string comparand, double milliseconds)
    {
        Assert.IsTrue(AzureMonitorLiveFilter.TryParseDuration(comparand, out var parsed));
        Assert.AreEqual(milliseconds, parsed, 0.001);
    }

    [TestMethod]
    [DataRow("Url", "Contains", "ORDERS", true)]
    [DataRow("Url", "Equal", "https://shop.test/orders/7", true)]
    [DataRow("Url", "Equal", "HTTPS://shop.test/orders/7", false)]
    [DataRow("Url", "DoesNotContain", "cart", true)]
    [DataRow("Name", "NotEqual", "GET /orders/{id}", false)]
    [DataRow("ResponseCode", "Equal", "404", true)]
    [DataRow("ResponseCode", "LessThan", "500", true)]
    [DataRow("ResponseCode", "GreaterThanOrEqual", "500", false)]
    [DataRow("Success", "Equal", "true", true)]
    [DataRow("Success", "NotEqual", "TRUE", false)]
    [DataRow("Duration", "GreaterThan", "0.0:0:1.4", true)]
    [DataRow("Duration", "LessThanOrEqual", "0.0:0:1.5", true)]
    [DataRow("Duration", "LessThan", "0.0:0:1.5", false)]
    [DataRow("CustomDimensions.tenant", "Equal", "contoso", true)]
    [DataRow("CustomDimensions.tenant", "Contains", "TOS", true)]
    [DataRow("CustomDimensions.missing", "NotEqual", "x", false)]
    [DataRow("*", "Contains", "contoso", true)]
    [DataRow("*", "Contains", "orders/7", true)]
    [DataRow("*", "DoesNotContain", "contoso", false)]
    [DataRow("*", "DoesNotContain", "fabrikam", true)]
    public void AFilter_MatchesARequest(string field, string predicate, string comparand, bool matches)
    {
        var span = Request(404);
        span.Attributes.Set("tenant", "contoso");
        var item = AzureMonitorLiveItem.FromSpan(span);

        Assert.AreEqual(matches, AzureMonitorLiveFilter.Create(AzureMonitorLiveTelemetryType.Request, field, predicate, comparand).Matches(item));
    }

    [TestMethod]
    [DataRow("Target", "Equal", "db.test:5432", true)]
    [DataRow("Type", "Equal", "postgresql", true)]
    [DataRow("Data", "Contains", "select", true)]
    [DataRow("ResultCode", "Equal", "0", false)]
    [DataRow("ResultCode", "NotEqual", "0", true)]
    public void AFilter_MatchesADependency(string field, string predicate, string comparand, bool matches)
    {
        var span = Records.Span(name: "SELECT orders");
        span.Attributes.Set(BelemetrySemanticConventions.DbSystemName, "postgresql").Set(BelemetrySemanticConventions.DbQueryText, "SELECT * FROM orders")
                       .Set(BelemetrySemanticConventions.ServerAddress, "db.test").Set(BelemetrySemanticConventions.ServerPort, 5432);
        var item = AzureMonitorLiveItem.FromSpan(span);

        Assert.AreEqual(matches, AzureMonitorLiveFilter.Create(AzureMonitorLiveTelemetryType.Dependency, field, predicate, comparand).Matches(item),
                        "A dependency without a result code has none to equal.");
    }

    [TestMethod]
    [DataRow("Request", "", "Equal", "x")]
    [DataRow("Request", "Url", "Like", "x")]
    [DataRow("Request", "Url", "Equal", "")]
    [DataRow("Request", "Url", "GreaterThan", "x")]
    [DataRow("Request", "Message", "Equal", "x")]
    [DataRow("Request", "*", "Equal", "x")]
    [DataRow("Request", "Success", "Equal", "yes")]
    [DataRow("Request", "Success", "LessThan", "true")]
    [DataRow("Request", "Duration", "Contains", "0.0:0:1")]
    [DataRow("Request", "Duration", "GreaterThan", "slow")]
    [DataRow("Request", "ResponseCode", "Equal", "OK")]
    [DataRow("Request", "CustomDimensions.", "Equal", "x")]
    [DataRow("Request", "CustomDimensions.size", "GreaterThan", "1")]
    [DataRow("Request", "CustomMetrics.size", "Equal", "1")]
    [DataRow("Exception", "Message", "Equal", "x")]
    [DataRow("Trace", "Name", "Equal", "x")]
    public void AFilterThatCannotApply_IsRefused(string type, string field, string predicate, string comparand)
    {
        Assert.ThrowsExactly<FormatException>(() => AzureMonitorLiveFilter.Create(Enum.Parse<AzureMonitorLiveTelemetryType>(type), field, predicate, comparand));
    }

    [TestMethod]
    public async Task ThePingAndPost_CarryTheApiVersionAndTheConfigurationsEtag_AndStopSamplingWhenNobodyWatches()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, r => Answer(r, subscribed: true, etag: "e1", Streams("""{ "TelemetryType": "Request", "Filters": { "Filters": [] } }""")))
            .Enqueue(HttpStatusCode.OK, r => Answer(r, subscribed: true, etag: "e1", "{}"))
            .Enqueue(HttpStatusCode.OK, r => Answer(r, subscribed: false, etag: null, string.Empty));
        await using var live = Create(handler: handler);

        var (subscribed, _) = await live.PingAsync(CancellationToken.None);
        Assert.IsTrue(subscribed);
        Assert.IsNotNull(live.Configuration);

        live.OnRecord(Request(200));
        Assert.IsTrue(await live.PostAsync(TimeSpan.FromSeconds(1), CancellationToken.None));
        Assert.AreEqual("e1", live.Configuration!.ETag, "The same etag: the body is not read again.");

        Assert.IsFalse(await live.PostAsync(TimeSpan.FromSeconds(1), CancellationToken.None));
        Assert.IsNull(live.Configuration, "Nobody watches: nothing is sampled.");
        live.OnRecord(Request(200));

        var requests = handler.Requests;
        Assert.AreEqual("https://live.test/QuickPulseService.svc/ping?api-version=2024-04-01-preview&ikey=11111111-2222-3333-4444-555555555555",
                        requests[0].Message.RequestUri!.ToString());
        Assert.IsFalse(requests[0].Message.Headers.Contains("x-ms-qps-configuration-etag"));
        Assert.AreEqual("https://live.test/QuickPulseService.svc/post?api-version=2024-04-01-preview&ikey=11111111-2222-3333-4444-555555555555",
                        requests[1].Message.RequestUri!.ToString());
        Assert.AreEqual("e1", requests[1].Message.Headers.GetValues("x-ms-qps-configuration-etag").Single());

        using var post = JsonDocument.Parse(requests[1].Body);
        Assert.AreEqual(1, post.RootElement[0].GetProperty("Documents").GetArrayLength());
        Assert.AreEqual(0, Post(live).GetProperty("Documents").GetArrayLength());
    }

    private static AzureMonitorLiveMetrics Create(Action<BelemetryAzureMonitorExporterOptions>? configure = null, Action<string>? diagnostic = null, HttpMessageHandler? handler = null,
                                                  BelemetrySettings? settings = null, TimeProvider? time = null)
    {
        var options = new BelemetryAzureMonitorExporterOptions { ConnectionString = ConnectionString };
        configure?.Invoke(options);
        var exporter = new BelemetryAzureMonitorExporter(options, "azuremonitor");
        return new AzureMonitorLiveMetrics(exporter, settings ?? TestHost.CreateSettings(), (_, message) => diagnostic?.Invoke(message), handler, time);
    }

    private static void Apply(AzureMonitorLiveMetrics live, string configuration, string etag = "e1")
    {
        live.ApplyConfiguration(Encoding.UTF8.GetBytes(configuration), etag);
        Assert.AreEqual(etag, live.Configuration?.ETag);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var started = Environment.TickCount64;
        while (condition() is false)
        {
            if (Environment.TickCount64 - started > 5000) Assert.Fail("The condition was not met in time.");
            await Task.Delay(10);
        }
    }

    private static string Streams(string groups) => $$"""{ "ETag": "e1", "Metrics": [], "DocumentStreams": [ { "Id": "all", "DocumentFilterGroups": [ {{groups}} ] } ] }""";

    private static void Answer(HttpResponseMessage response, bool subscribed, string? etag, string body)
    {
        response.Headers.Add("x-ms-qps-subscribed", subscribed ? "true" : "false");
        if (etag is not null) response.Headers.Add("x-ms-qps-configuration-etag", etag);
        response.Content = new StringContent(body, Encoding.UTF8, "application/json");
    }

    private static BelemetrySpan Request(int statusCode, double seconds = 1.5)
    {
        var span = Records.Span(BelemetrySpanKind.Server);
        span.EndTimeUnixNano = span.StartTimeUnixNano + (long)(seconds * 1_000_000_000);
        span.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "GET").Set(BelemetrySemanticConventions.HttpRoute, "/orders/{id}")
                       .Set(BelemetrySemanticConventions.UrlFull, "https://shop.test/orders/7").Set(BelemetrySemanticConventions.HttpResponseStatusCode, statusCode);
        return span;
    }

    private static JsonElement Post(AzureMonitorLiveMetrics live)
    {
        using var document = JsonDocument.Parse(live.CreatePostBody(TimeSpan.FromSeconds(1)));
        return document.RootElement.EnumerateArray().Single().Clone();
    }

    private static string?[] StreamIds(JsonElement document) => document.GetProperty("DocumentStreamIds").EnumerateArray().Select(e => e.GetString()).ToArray();

    private static string?[] DocumentTypes(JsonElement point) => point.GetProperty("Documents").EnumerateArray().Select(d => d.GetProperty("DocumentType").GetString()).ToArray();

    private static Dictionary<string, double> Metrics(JsonElement point) =>
        point.GetProperty("Metrics").EnumerateArray().ToDictionary(m => m.GetProperty("Name").GetString()!, m => m.GetProperty("Value").GetDouble());

    private static Dictionary<string, int> Weights(JsonElement point) =>
        point.GetProperty("Metrics").EnumerateArray().ToDictionary(m => m.GetProperty("Name").GetString()!, m => m.GetProperty("Weight").GetInt32());

    private static Dictionary<string, string?> Properties(JsonElement document) =>
        document.GetProperty("Properties").EnumerateArray().ToDictionary(p => p.GetProperty("key").GetString()!, p => p.GetProperty("value").GetString());

    private static Dictionary<string, string?> Data(JsonElement error) =>
        error.GetProperty("Data").EnumerateArray().ToDictionary(d => d.GetProperty("key").GetString()!, d => d.GetProperty("value").GetString());

    /// <summary>A clock that moves only when told to.</summary>
    private sealed class ManualTime : TimeProvider
    {
        private long _timestamp;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan by) => _timestamp += (long)(by.TotalSeconds * TimestampFrequency);
    }
}
