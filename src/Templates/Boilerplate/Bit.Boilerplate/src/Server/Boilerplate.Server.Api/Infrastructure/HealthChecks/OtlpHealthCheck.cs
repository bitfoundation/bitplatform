using System.Net;
using System.Net.Http.Headers;
using Bit.Belemetry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

public partial class OtlpHealthCheck : IHealthCheck
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var logsEndpoint = configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]?.Trim();
        var protocol = "http/protobuf";
        if (string.IsNullOrWhiteSpace(endpoint) && configuration.GetBelemetryOtlpEndpoint() is { } belemetryEndpoint)
        {
            endpoint = belemetryEndpoint.ToString();
        }
        else if (configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.Trim().ToLowerInvariant() is "grpc" or "http/json")
        {
            protocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]!.Trim().ToLowerInvariant();
        }

        var grpc = protocol is "grpc";
        var url = string.IsNullOrEmpty(logsEndpoint) is false && grpc is false
            ? logsEndpoint
            : $"{(string.IsNullOrEmpty(logsEndpoint) ? endpoint : logsEndpoint)?.Trim().TrimEnd('/')}/{(grpc ? "opentelemetry.proto.collector.logs.v1.LogsService/Export" : "v1/logs")}";

        if (Uri.TryCreate(url, UriKind.Absolute, out var logsUri) is false || logsUri.Scheme is not ("http" or "https"))
            return new HealthCheckResult(context.Registration.FailureStatus, $"{(string.IsNullOrEmpty(logsEndpoint) ? "OTEL_EXPORTER_OTLP_ENDPOINT" : "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT")} is not an http or https address");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, logsUri)
            {
                Content = protocol switch
                {
                    "grpc" => new ByteArrayContent(new byte[5]),
                    "http/json" => new ByteArrayContent("{}"u8.ToArray()),
                    _ => new ByteArrayContent([])
                }
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(protocol switch { "grpc" => "application/grpc", "http/json" => "application/json", _ => "application/x-protobuf" });

            if (grpc)
            {
                request.Version = HttpVersion.Version20;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                request.Headers.TryAddWithoutValidation("te", "trailers");
            }

            foreach (var header in (configuration["OTEL_EXPORTER_OTLP_HEADERS"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = header.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length is 2 && parts[0].Length > 0)
                {
                    request.Headers.TryAddWithoutValidation(parts[0], Uri.UnescapeDataString(parts[1]));
                }
            }

            using var response = await httpClientFactory.CreateClient("Otlp").SendAsync(request, cancellationToken);
            await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (response.IsSuccessStatusCode is false)
                return new HealthCheckResult(context.Registration.FailureStatus, $"The OTLP endpoint answered {(int)response.StatusCode} {response.StatusCode}");

            if (grpc && GrpcStatusOf(response) is var grpcStatus && grpcStatus is not "0")
                return new HealthCheckResult(context.Registration.FailureStatus, grpcStatus is null
                    ? "The OTLP endpoint answered without a grpc-status"
                    : $"The OTLP endpoint answered grpc-status {grpcStatus} ({GrpcMessageOf(response)})");

            return HealthCheckResult.Healthy($"The OTLP endpoint accepts the app's telemetry over {protocol}");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "The OTLP endpoint is unreachable", exp);
        }
    }

    private static string? GrpcStatusOf(HttpResponseMessage response)
        => response.TrailingHeaders.TryGetValues("grpc-status", out var trailer) ? trailer.FirstOrDefault()
            : response.Headers.TryGetValues("grpc-status", out var header) ? header.FirstOrDefault()
            : null;

    private static string GrpcMessageOf(HttpResponseMessage response)
        => Uri.UnescapeDataString((response.TrailingHeaders.TryGetValues("grpc-message", out var trailer) ? trailer.FirstOrDefault()
            : response.Headers.TryGetValues("grpc-message", out var header) ? header.FirstOrDefault()
            : null) ?? string.Empty);
}
