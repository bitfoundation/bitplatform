namespace Bit.Belemetry;

/// <summary>
/// Optional: register an instance in DI to route Belemetry's export traffic through a custom
/// <see cref="HttpMessageHandler"/> - a proxy, a test double, or an in-process server's handler.
/// </summary>
/// <param name="handler">The handler. Belemetry does not dispose it.</param>
public sealed class BelemetryHttpMessageHandlerProvider(HttpMessageHandler handler)
{
    /// <summary>The handler export requests are sent through.</summary>
    public HttpMessageHandler Handler { get; } = handler;
}
