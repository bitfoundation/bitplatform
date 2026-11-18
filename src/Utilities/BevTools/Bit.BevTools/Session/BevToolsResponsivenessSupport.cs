namespace Bit.BevTools;

/// <summary>Which of the browser's timing entries the page's browser has.</summary>
public sealed class BevToolsResponsivenessSupport
{
    public bool LongAnimationFrame { get; set; }

    public bool LongTask { get; set; }

    public bool EventTiming { get; set; }
}
