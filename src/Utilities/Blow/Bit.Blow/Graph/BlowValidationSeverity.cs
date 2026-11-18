namespace Bit.Blow;

/// <summary>How serious a validation issue is.</summary>
public enum BlowValidationSeverity
{
    /// <summary>Worth knowing; nothing is wrong.</summary>
    Info,

    /// <summary>Probably a mistake, but the graph can still be used.</summary>
    Warning,

    /// <summary>The graph is not valid: it cannot be saved, run or published as it is.</summary>
    Error,
}
