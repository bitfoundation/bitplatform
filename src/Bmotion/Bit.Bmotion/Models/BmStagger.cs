namespace Bit.Bmotion;

/// <summary>Which element a stagger radiates from.</summary>
public enum BmStaggerFrom { First, Last, Center }

/// <summary>
/// A delay generator for multi-element animations, motion.dev's <c>stagger()</c>:
/// <code>
/// await Motion.AnimateAsync(".item", Bm.To(opacity: 1, y: 0),
///     Bm.Spring(), stagger: Bm.Stagger(0.08, from: BmStaggerFrom.Center));
/// </code>
/// <para>
/// Pass <c>grid: (cols, rows)</c> to stagger radially across a 2-D grid (Euclidean distance from the
/// <see cref="From"/> origin), or construct with a <c>Func&lt;int, int, double&gt;</c> for a fully
/// custom per-index delay.
/// </para>
/// </summary>
public sealed class BmStagger
{
    private readonly Func<int, int, double>? _custom;

    public BmStagger(double each, BmStaggerFrom from = BmStaggerFrom.First, double startDelay = 0,
        (int Cols, int Rows)? grid = null)
    {
        if (!double.IsFinite(each) || each < 0)
            throw new ArgumentException("Stagger interval must be a finite, non-negative number of seconds.", nameof(each));
        if (!double.IsFinite(startDelay) || startDelay < 0)
            throw new ArgumentException("Stagger start delay must be a finite, non-negative number of seconds.", nameof(startDelay));
        if (grid is (int cols, int rows) && (cols <= 0 || rows <= 0))
            throw new ArgumentException("Stagger grid dimensions must be positive.", nameof(grid));
        Each = each;
        From = from;
        StartDelay = startDelay;
        Grid = grid;
    }

    /// <summary>Creates a stagger driven entirely by a custom <c>(index, total) =&gt; delaySeconds</c> function.</summary>
    public BmStagger(Func<int, int, double> custom)
        => _custom = custom ?? throw new ArgumentNullException(nameof(custom));

    /// <summary>Seconds between each element's start.</summary>
    public double Each { get; }

    /// <summary>The element the stagger radiates from.</summary>
    public BmStaggerFrom From { get; }

    /// <summary>Extra delay before the first element starts.</summary>
    public double StartDelay { get; }

    /// <summary>Optional grid dimensions for 2-D radial staggering.</summary>
    public (int Cols, int Rows)? Grid { get; }

    /// <summary>The delay (seconds) for the element at <paramref name="index"/> of <paramref name="total"/>.</summary>
    public double DelayFor(int index, int total)
    {
        if (_custom is not null) return _custom(index, total);
        if (total <= 0) return StartDelay;

        if (Grid is (int cols, int rows))
            return StartDelay + GridDistance(index, cols, rows) * Each;

        double origin = From switch
        {
            BmStaggerFrom.Last => total - 1,
            BmStaggerFrom.Center => (total - 1) / 2.0,
            _ => 0,
        };
        return StartDelay + Math.Abs(index - origin) * Each;
    }

    // Euclidean distance (in cells) from the From-origin cell to element `index`'s cell.
    private double GridDistance(int index, int cols, int rows)
    {
        int row = index / cols;
        int col = index % cols;
        (double originRow, double originCol) = From switch
        {
            BmStaggerFrom.Last => (rows - 1.0, cols - 1.0),
            BmStaggerFrom.Center => ((rows - 1) / 2.0, (cols - 1) / 2.0),
            _ => (0.0, 0.0),
        };
        double dr = row - originRow, dc = col - originCol;
        return Math.Sqrt(dr * dr + dc * dc);
    }
}
