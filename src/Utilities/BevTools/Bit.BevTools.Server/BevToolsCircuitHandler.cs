using System.Diagnostics;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Bit.BevTools;

/// <summary>
/// Records circuit lifecycle, connection state and inbound message processing for the session that owns the circuit.
/// Scoped: one instance per circuit, sharing the circuit's BevTools session.
/// </summary>
internal sealed class BevToolsCircuitHandler(BevToolsSession session, BevCircuitRegistry registry, BitBevToolsOptions options) : CircuitHandler
{
    private BevCircuitInfo? _info;

    public override int Order => int.MinValue; // observe before application handlers so their failures are attributed correctly

    public BevCircuitInfo? Info => _info;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (!session.IsEnabled)
        {
            return Task.CompletedTask;
        }

        _info = registry.Open(circuit.Id, session.Id);
        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Circuit,
            Category = "circuit",
            Title = "Circuit opened " + _info.ShortId,
            Data = new Dictionary<string, object?> { ["activeCircuits"] = registry.ActiveCount },
        });
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_info is null)
        {
            return Task.CompletedTask;
        }

        var wasDown = _info.LastConnectionChange is not null && !_info.IsConnected;
        _info.IsConnected = true;
        _info.LastConnectionChange = DateTimeOffset.UtcNow;
        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Circuit,
            Category = "circuit",
            Title = wasDown ? "Connection restored" : "Connection up",
            Detail = wasDown ? $"reconnected after {_info.Disconnects} disconnect(s)" : null,
        });
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_info is null)
        {
            return Task.CompletedTask;
        }

        _info.IsConnected = false;
        _info.Disconnects++;
        _info.LastConnectionChange = DateTimeOffset.UtcNow;
        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Circuit,
            Category = "circuit",
            Title = "Connection down",
            Detail = "The browser lost its SignalR connection; the circuit is retained until the disconnect timeout elapses.",
            Severity = BevToolsSeverity.Warning,
        });
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_info is null)
        {
            return Task.CompletedTask;
        }

        registry.Close(circuit.Id);
        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Circuit,
            Category = "circuit",
            Title = "Circuit closed " + _info.ShortId,
            Detail = $"lived {_info.Duration.TotalSeconds:0} s, {_info.InboundMessages} inbound messages, {_info.Disconnects} disconnects",
        });
        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
    {
        if (!session.IsEnabled)
        {
            return next;
        }

        return async context =>
        {
            var start = Stopwatch.GetTimestamp();
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                if (_info is not null)
                {
                    _info.Errors++;
                }

                session.Errors.Record(ex, "circuit", "Inbound circuit activity failed.");
                throw;
            }
            finally
            {
                var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if (_info is not null)
                {
                    Interlocked.Increment(ref _info.InboundMessages);
                    _info.InboundProcessingMs += ms;
                    if (ms > _info.MaxInboundMs)
                    {
                        _info.MaxInboundMs = ms;
                    }

                    _info.LastActivityAt = DateTimeOffset.UtcNow;
                }

                if (ms >= options.Diagnostics.SlowEventHandlerMs)
                {
                    session.Timeline.Record(new BevToolsEvent
                    {
                        Kind = BevToolsEventKind.Circuit,
                        Category = "circuit-inbound",
                        Title = "Slow inbound message",
                        Detail = $"Processing one inbound SignalR message (event, JS→.NET call or render acknowledgement) took {ms:0.#} ms; the circuit handled nothing else meanwhile.",
                        DurationMs = ms,
                        Severity = BevToolsSeverity.Warning,
                    });
                }
            }
        };
    }
}
