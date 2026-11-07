using Bit.Belemetry;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Bit.Belemetry.Samples.Server;

/// <summary>
/// Puts the circuit's connection history on its diagnostic timeline. A circuit handler is scoped to its circuit, so the
/// IBelemetryDiagnostics it receives is that circuit's own - nothing here can reach another user's timeline. The
/// circuit id is never recorded: it is a bearer secret.
/// </summary>
public sealed class SampleCircuitDiagnostics(IBelemetryDiagnostics diagnostics) : CircuitHandler
{
    /// <inheritdoc />
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) => Record("circuit.opened");

    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken) => Record("circuit.connection_up");

    /// <inheritdoc />
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken) => Record("circuit.connection_down", BelemetrySeverity.Warn);

    /// <inheritdoc />
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken) => Record("circuit.closed");

    private Task Record(string name, BelemetrySeverity severity = BelemetrySeverity.Info)
    {
        diagnostics.Record(name, severity: severity, category: BelemetryDiagnosticCategories.Circuit);
        return Task.CompletedTask;
    }
}
