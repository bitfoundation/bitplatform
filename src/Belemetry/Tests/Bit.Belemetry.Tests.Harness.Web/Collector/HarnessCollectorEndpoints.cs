using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Belemetry.Tests.Harness.Web.Collector;

public static class HarnessCollectorEndpoints
{
    /// <summary>
    /// Maps the capturing receiver under <see cref="HarnessCollectorFilter.CollectorPath"/>:
    /// <list type="bullet">
    /// <item><c>POST v1/{traces|logs|metrics}</c> - OTLP/HTTP, as an OpenTelemetry Collector answers it;</item>
    /// <item><c>POST v2.1/track</c> - the Azure Monitor ingestion endpoint;</item>
    /// <item><c>GET received[?session=&amp;marker=]</c> - everything decoded so far, as JSON;</item>
    /// <item><c>POST reset</c> - forgets everything.</item>
    /// </list>
    /// Plus <c>GET /test-api/echo</c>, which answers with the request's headers, so a test can see the
    /// <c>traceparent</c> the fetch instrumentation added, and <c>GET /test-api/server-telemetry</c>, which writes a
    /// server-side log and span for the server pipeline to pick up.
    /// </summary>
    /// <summary>The CORS policy of the echo endpoint: any origin, with the W3C trace-context headers allowed.</summary>
    public const string CrossOriginPolicy = "harness-cross-origin";

    public static IEndpointRouteBuilder MapHarnessCollector(this IEndpointRouteBuilder endpoints)
    {
        var collector = endpoints.MapGroup(HarnessCollectorFilter.CollectorPath);

        collector.MapPost("v1/{signal}", async (string signal, HttpRequest request, HarnessCollector store) =>
        {
            if (signal is not ("traces" or "logs" or "metrics")) return Results.NotFound();

            store.Add(signal, request.Path, request.ContentType, request.Headers.ContentEncoding, request.Headers.UserAgent, await ReadBodyAsync(request));
            // An empty ExportTraceServiceResponse: full success, in the encoding the client sent.
            return request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) is false
                ? Results.Bytes([], "application/x-protobuf")
                : Results.Text("{}", "application/json");
        });

        collector.MapPost("v2.1/track", async (HttpRequest request, HarnessCollector store) =>
        {
            var items = store.Add("azure", request.Path, request.ContentType, request.Headers.ContentEncoding, request.Headers.UserAgent, await ReadBodyAsync(request));
            return Results.Text($"{{\"itemsReceived\":{items},\"itemsAccepted\":{items},\"errors\":[]}}", "application/json");
        });

        collector.MapGet("received", ([FromQuery] string? session, [FromQuery] string? marker, HarnessCollector store) =>
            Results.Json(store.Snapshot(session, marker)));

        collector.MapPost("reset", (HarnessCollector store) =>
        {
            store.Clear();
            return Results.NoContent();
        });

        endpoints.MapGet("/test-api/echo", (HttpRequest request, HarnessCollector store) =>
        {
            var echo = new CollectedEcho
            {
                Time = DateTimeOffset.UtcNow,
                Query = request.QueryString.Value,
                Headers = request.Headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value.ToString()),
            };
            store.AddEcho(echo);
            return Results.Json(new { headers = echo.Headers });
        }).RequireCors(CrossOriginPolicy);

        endpoints.MapGet("/test-api/server-telemetry", ([FromQuery] string? marker, ILoggerFactory loggers) =>
        {
            marker ??= string.Empty;

            using (var activity = HarnessTelemetry.ActivitySource.StartActivity("harness.server-activity"))
            {
                activity?.SetTag(HarnessTelemetry.MarkerAttribute, marker);
                loggers.CreateLogger(HarnessTelemetry.Name).LogInformation("Harness server log {harness.marker}", marker);
            }
            HarnessTelemetry.Clicks.Add(1, new KeyValuePair<string, object?>(HarnessTelemetry.MarkerAttribute, marker));

            return Results.Json(new { marker, traceId = Activity.Current?.TraceId.ToHexString() });
        });

        return endpoints;
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request)
    {
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
