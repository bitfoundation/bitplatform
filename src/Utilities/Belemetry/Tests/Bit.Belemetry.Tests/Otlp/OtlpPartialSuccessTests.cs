using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Bit.Belemetry.Tests.Otlp;

/// <summary>
/// OTLP <c>partial_success</c>: never retried (the specification forbids it), but reported - otherwise rejected data
/// vanishes behind a 200. The protobuf responses come from the types generated from the official .proto files.
/// </summary>
[TestClass]
public class OtlpPartialSuccessTests
{
    [TestMethod]
    public void ReadsTheProtobufResponseOfEverySignal()
    {
        var traces = new ExportTraceServiceResponse { PartialSuccess = new ExportTracePartialSuccess { RejectedSpans = 5, ErrorMessage = "span too old" } }.ToByteArray();
        var logs = new ExportLogsServiceResponse { PartialSuccess = new ExportLogsPartialSuccess { RejectedLogRecords = 2 } }.ToByteArray();
        var metrics = new ExportMetricsServiceResponse { PartialSuccess = new ExportMetricsPartialSuccess { ErrorMessage = "points coalesced" } }.ToByteArray();

        Assert.AreEqual("5 rejected: span too old", OtlpPartialSuccess.Describe(traces, json: false));
        Assert.AreEqual("2 rejected", OtlpPartialSuccess.Describe(logs, json: false));
        Assert.AreEqual("warning: points coalesced", OtlpPartialSuccess.Describe(metrics, json: false), "A message with nothing rejected is a warning.");
    }

    [TestMethod]
    public void ReadsTheJsonResponse_Int64AsAStringOrANumber()
    {
        Assert.AreEqual("7 rejected: bad attribute", OtlpPartialSuccess.Describe("""{"partialSuccess":{"rejectedLogRecords":"7","errorMessage":"bad attribute"}}"""u8, json: true));
        Assert.AreEqual("3 rejected", OtlpPartialSuccess.Describe("""{"partialSuccess":{"rejectedSpans":3}}"""u8, json: true));
    }

    [TestMethod]
    public void AFullSuccess_OrAnUnreadableBody_ReportsNothing()
    {
        Assert.IsNull(OtlpPartialSuccess.Describe(new ExportTraceServiceResponse().ToByteArray(), json: false));
        Assert.IsNull(OtlpPartialSuccess.Describe(new ExportTraceServiceResponse { PartialSuccess = new ExportTracePartialSuccess() }.ToByteArray(), json: false));
        Assert.IsNull(OtlpPartialSuccess.Describe("{}"u8, json: true));
        Assert.IsNull(OtlpPartialSuccess.Describe("""{"partialSuccess":{}}"""u8, json: true));
        Assert.IsNull(OtlpPartialSuccess.Describe("not json"u8, json: true));
        Assert.IsNull(OtlpPartialSuccess.Describe([0x0A, 0xFF], json: false), "A field running past the end.");
        Assert.IsNull(OtlpPartialSuccess.Describe([0x0F], json: false), "A wire type that cannot be skipped.");
    }

    [TestMethod]
    [Timeout(5000, CooperativeCancellation = true)]
    public void AHostileBody_NeverHangsOrThrows()
    {
        // Field 2, length-delimited, with a length that is negative as an int: it once moved the reader backwards.
        Assert.IsNull(OtlpPartialSuccess.Describe([0x12, 0xFA, 0xFF, 0xFF, 0xFF, 0x0F], json: false));
        Assert.IsNull(OtlpPartialSuccess.Describe([0x0A, 0xFA, 0xFF, 0xFF, 0xFF, 0x0F], json: false));
        Assert.IsNull(OtlpPartialSuccess.Describe([0x0A, 0x03, 0x12, 0xFA, 0xFF], json: false), "A message's own field running past its end.");
        Assert.IsNull(OtlpPartialSuccess.Describe([0x0A, 0x02, 0x08, 0x80], json: false), "A truncated varint inside the message.");
        Assert.IsNull(OtlpPartialSuccess.Describe("""{"partialSuccess":{"rejectedSpans":"99999999999999999999"}}"""u8, json: true), "An overflowing count.");
    }

    [TestMethod]
    public void UnframesAnUncompressedGrpcMessage()
    {
        var message = new ExportTraceServiceResponse { PartialSuccess = new ExportTracePartialSuccess { RejectedSpans = 1 } }.ToByteArray();
        CollectionAssert.AreEqual(message, BelemetryHttpSender.Unframe(BelemetryHttpSender.Frame(message, compressed: false)));
        Assert.IsNull(BelemetryHttpSender.Unframe(BelemetryHttpSender.Frame(message, compressed: true)), "A compressed frame is not decoded.");
        Assert.IsNull(BelemetryHttpSender.Unframe([0, 0, 0, 0, 9, 1]), "A length past the end.");
        Assert.IsNull(BelemetryHttpSender.Unframe(null));
    }

    [TestMethod]
    [DataRow(BelemetryOtlpProtocol.HttpProtobuf)]
    [DataRow(BelemetryOtlpProtocol.HttpJson)]
    [DataRow(BelemetryOtlpProtocol.Grpc)]
    public async Task TheSender_ReportsAPartialSuccessAsAWarning_WithoutRetrying(BelemetryOtlpProtocol protocol)
    {
        var protobuf = new ExportLogsServiceResponse { PartialSuccess = new ExportLogsPartialSuccess { RejectedLogRecords = 3, ErrorMessage = "too old" } }.ToByteArray();
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, r =>
        {
            r.Content = protocol switch
            {
                BelemetryOtlpProtocol.HttpJson => new StringContent("""{"partialSuccess":{"rejectedLogRecords":"3","errorMessage":"too old"}}""", null, "application/json"),
                BelemetryOtlpProtocol.Grpc => new ByteArrayContent(BelemetryHttpSender.Frame(protobuf, compressed: false)),
                _ => new ByteArrayContent(protobuf),
            };
            if (protocol == BelemetryOtlpProtocol.HttpProtobuf) r.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            if (protocol == BelemetryOtlpProtocol.Grpc) r.TrailingHeaders.Add("grpc-status", "0");
        });
        var warnings = new List<string>();
        using var sender = new BelemetryHttpSender(handler, (_, _) => { }, (_, m) => warnings.Add(m)) { Backoff = [TimeSpan.Zero, TimeSpan.Zero] };
        var exporter = new BelemetryOtlpExporter(new BelemetryOtlpExporterOptions { Endpoint = new Uri("http://collector.test:4318"), Protocol = protocol },
                                                 BelemetryMetricTemporality.Cumulative, "otlp");
        var request = exporter.CreateRequests(new BelemetryBatch(Records.Resource(), logs: [Records.Log()])).Single();

        var outcome = await sender.SendAsync(exporter, request, retry: true, CancellationToken.None);

        Assert.AreEqual(BelemetrySendOutcome.Delivered, outcome);
        Assert.HasCount(1, handler.Requests, "A partial success must not be retried.");
        StringAssert.Contains(warnings.Single(), "accepted the request but 3 rejected: too old");
    }
}
