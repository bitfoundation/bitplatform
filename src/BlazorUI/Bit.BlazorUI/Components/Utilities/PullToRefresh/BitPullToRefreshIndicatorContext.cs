namespace Bit.BlazorUI;

/// <summary>
/// The context passed to the IndicatorTemplate of the BitPullToRefresh, describing the gesture the indicator is drawn for.
/// </summary>
public class BitPullToRefreshIndicatorContext
{
    /// <summary>
    /// Creates a new instance of <see cref="BitPullToRefreshIndicatorContext"/>.
    /// </summary>
    /// <param name="state">
    /// The stage of the gesture the component is at.
    /// </param>
    /// <param name="progress">
    /// How far the pull has come as a fraction of the trigger, from 0 to 1.
    /// </param>
    public BitPullToRefreshIndicatorContext(BitPullToRefreshState state, decimal progress)
    {
        State = state;
        Progress = progress;
    }

    /// <summary>
    /// The stage of the gesture the component is at: idle, pulling, past the trigger, refreshing or complete.
    /// </summary>
    public BitPullToRefreshState State { get; }

    /// <summary>
    /// How far the pull has come as a fraction of the trigger: 0 while nothing is being pulled, and 1 once releasing
    /// would start a refresh, for the whole of the refresh and in the complete state.
    /// </summary>
    public decimal Progress { get; }
}
