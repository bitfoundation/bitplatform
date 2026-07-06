namespace Bit.Bmotion;

/// <summary>Which element a stagger radiates from.</summary>
public enum BmStaggerFrom { First, Last, Center }

/// <summary>
/// A delay generator for multi-element animations, motion.dev's <c>stagger()</c>:
/// <code>
/// await Motion.AnimateAsync(".item", Bm.To(opacity: 1, y: 0),
///     Bm.Spring(), stagger: Bm.Stagger(0.08, from: BmStaggerFrom.Center));
/// </code>
/// </summary>
public sealed class BmStagger
{
    public BmStagger(double each, BmStaggerFrom from = BmStaggerFrom.First, double startDelay = 0)
    {
        if (!double.IsFinite(each) || each < 0)
            throw new ArgumentException("Stagger interval must be a finite, non-negative number of seconds.", nameof(each));
        if (!double.IsFinite(startDelay) || startDelay < 0)
            throw new ArgumentException("Stagger start delay must be a finite, non-negative number of seconds.", nameof(startDelay));
        Each = each;
        From = from;
        StartDelay = startDelay;
    }

    /// <summary>Seconds between each element's start.</summary>
    public double Each { get; }

    /// <summary>The element the stagger radiates from.</summary>
    public BmStaggerFrom From { get; }

    /// <summary>Extra delay before the first element starts.</summary>
    public double StartDelay { get; }

    /// <summary>The delay (seconds) for the element at <paramref name="index"/> of <paramref name="total"/>.</summary>
    public double DelayFor(int index, int total)
    {
        if (total <= 0) return StartDelay;
        double origin = From switch
        {
            BmStaggerFrom.Last => total - 1,
            BmStaggerFrom.Center => (total - 1) / 2.0,
            _ => 0,
        };
        return StartDelay + Math.Abs(index - origin) * Each;
    }
}
