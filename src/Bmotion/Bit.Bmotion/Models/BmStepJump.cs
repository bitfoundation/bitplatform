namespace Bit.Bmotion;

/// <summary>
/// Where the jumps of a stepped easing occur, mirroring the CSS <c>steps()</c> jump-terms. Use via
/// <c>Bm.Tween(steps: 5, stepJump: BmStepJump.End)</c>.
/// </summary>
public enum BmStepJump
{
    /// <summary>Jump at the end of each interval (CSS <c>jump-end</c>, the default).</summary>
    End = 0,

    /// <summary>Jump at the start of each interval (CSS <c>jump-start</c>).</summary>
    Start = 1,

    /// <summary>No jump at either end (CSS <c>jump-none</c>); reaches both 0 and 1.</summary>
    None = 2,

    /// <summary>Jump at both ends (CSS <c>jump-both</c>).</summary>
    Both = 3,
}
