namespace Bit.BevTools;

public enum BevToolsProfileChangeKind
{
    /// <summary>Renders as often as in the baseline, and its time changed less than the thresholds.</summary>
    Unchanged,

    /// <summary>Renders more often, or takes clearly longer, than in the baseline.</summary>
    Regressed,

    /// <summary>Renders less often, or takes clearly less time, without rendering more often or taking clearly longer.</summary>
    Improved,

    /// <summary>Rendered in the current recording only.</summary>
    New,

    /// <summary>Rendered in the baseline only.</summary>
    Gone,
}
