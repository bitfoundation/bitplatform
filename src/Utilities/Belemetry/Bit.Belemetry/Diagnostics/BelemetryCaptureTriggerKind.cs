namespace Bit.Belemetry;

/// <summary>Why a capture was made.</summary>
public enum BelemetryCaptureTriggerKind
{
    /// <summary>The app asked for it (<see cref="IBelemetryDiagnostics.CaptureAsync"/>), usually on a user's or a support agent's request.</summary>
    Manual,

    /// <summary>A configured trigger fired (<see cref="BelemetryDiagnosticsOptions.AutoCaptureTriggers"/>).</summary>
    Automatic,
}
