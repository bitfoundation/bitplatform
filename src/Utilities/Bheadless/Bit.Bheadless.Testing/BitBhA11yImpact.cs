namespace Bit.Bheadless;

/// <summary>Severity of an accessibility violation (mirrors axe-core impact levels).</summary>
public enum BitBhA11yImpact
{
    /// <summary>Best-practice issue.</summary>
    Minor,

    /// <summary>Degrades the experience for some users.</summary>
    Moderate,

    /// <summary>Severely degrades the experience.</summary>
    Serious,

    /// <summary>Blocks access for some users.</summary>
    Critical,
}
