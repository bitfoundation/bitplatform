namespace Bit.Belemetry;

/// <summary>How much a statement of an issue report is worth.</summary>
public enum BelemetryIssueFindingKind
{
    /// <summary>Observed: read directly from recorded evidence (an exception, a request's status, the framework's route).</summary>
    Observed,

    /// <summary>Inferred: Belemetry's explanation of the evidence (closeness in time, a component named by a stack frame). It may be wrong.</summary>
    Inferred,

    /// <summary>What the user said happened; not verified.</summary>
    UserProvided,
}
