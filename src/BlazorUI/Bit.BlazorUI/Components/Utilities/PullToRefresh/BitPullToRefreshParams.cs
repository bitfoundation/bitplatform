namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitPullToRefresh"/> component.
/// </summary>
public class BitPullToRefreshParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitPullToRefresh cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPullToRefresh value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPullToRefresh)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the pull to refresh.
    /// </summary>
    public BitPullToRefreshClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the pull indicator.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom template to replace the default checkmark shown while the complete state is visible.
    /// </summary>
    public RenderFragment? Complete { get; set; }

    /// <summary>
    /// The duration in milliseconds to keep the complete indicator visible after a successful refresh (0 disables the complete state).
    /// </summary>
    public int? CompleteDelay { get; set; }

    /// <summary>
    /// The text that gets announced to screen readers while the complete state is visible.
    /// </summary>
    public string? CompleteLabel { get; set; }

    /// <summary>
    /// The custom css color of the pull indicator, which only applies while Color is left unset.
    /// </summary>
    public string? CustomColor { get; set; }

    /// <summary>
    /// The direction the pull travels in to refresh: down from the top of the scroller, or up from its bottom.
    /// </summary>
    public BitPullToRefreshDirection? Direction { get; set; }

    /// <summary>
    /// The factor the pull-down distance gets divided by; higher values make the pull feel heavier.
    /// </summary>
    public decimal? Factor { get; set; }

    /// <summary>
    /// Whether the component takes the whole width of its container.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The custom template to replace the whole indicator, handed the state and the progress of the gesture.
    /// </summary>
    public RenderFragment<BitPullToRefreshIndicatorContext>? IndicatorTemplate { get; set; }

    /// <summary>
    /// The custom template to replace the default loading glyph.
    /// </summary>
    public RenderFragment? Loading { get; set; }

    /// <summary>
    /// The value in pixels added to the pull height as a margin above the indicator.
    /// </summary>
    public int? Margin { get; set; }

    /// <summary>
    /// The furthest the pull can travel, in pixels, past which it stops following the finger (0 stops it at the trigger).
    /// </summary>
    public int? MaxPull { get; set; }

    /// <summary>
    /// Leaves the mouse out of the gesture, so that only touch and pen pull to refresh.
    /// </summary>
    public bool? NoMouse { get; set; }

    /// <summary>
    /// The text that gets announced to screen readers while the refresh is in progress.
    /// </summary>
    public string? RefreshingLabel { get; set; }

    /// <summary>
    /// The custom template to replace the glyph while the pull has passed the trigger and releasing starts the refresh.
    /// </summary>
    public RenderFragment? Release { get; set; }

    /// <summary>
    /// The text that gets announced to screen readers while the pull has passed the trigger.
    /// </summary>
    public string? ReleaseLabel { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the pull to refresh.
    /// </summary>
    public BitPullToRefreshClassStyles? Styles { get; set; }

    /// <summary>
    /// The dead-zone distance in pixels that the pull-down must travel before the indicator appears.
    /// </summary>
    public int? Threshold { get; set; }

    /// <summary>
    /// The pulling height in pixels that triggers the refresh.
    /// </summary>
    public int? Trigger { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPullToRefresh"/> instance with any values that have been
    /// set on this object, if those properties have not already been set on the <see cref="BitPullToRefresh"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPullToRefresh"/>
    /// will be updated. This method does not overwrite existing values on <paramref name="bitPullToRefresh"/>.
    /// </remarks>
    /// <param name="bitPullToRefresh">
    /// The <see cref="BitPullToRefresh"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPullToRefresh bitPullToRefresh)
    {
        if (bitPullToRefresh is null) return;

        UpdateBaseParameters(bitPullToRefresh);

        if (Classes is not null && bitPullToRefresh.HasNotBeenSet(nameof(Classes)))
        {
            bitPullToRefresh.Classes = Classes;

            bitPullToRefresh.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(Color)))
        {
            bitPullToRefresh.Color = Color.Value;

            bitPullToRefresh.StyleBuilder.Reset();
        }

        if (Complete is not null && bitPullToRefresh.HasNotBeenSet(nameof(Complete)))
        {
            bitPullToRefresh.Complete = Complete;
        }

        if (CompleteDelay.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(CompleteDelay)))
        {
            bitPullToRefresh.CompleteDelay = CompleteDelay.Value;
        }

        if (CompleteLabel is not null && bitPullToRefresh.HasNotBeenSet(nameof(CompleteLabel)))
        {
            bitPullToRefresh.CompleteLabel = CompleteLabel;
        }

        if (CustomColor.HasValue() && bitPullToRefresh.HasNotBeenSet(nameof(CustomColor)))
        {
            bitPullToRefresh.CustomColor = CustomColor;

            bitPullToRefresh.StyleBuilder.Reset();
        }

        if (Direction.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(Direction)))
        {
            bitPullToRefresh.Direction = Direction.Value;

            bitPullToRefresh.ClassBuilder.Reset();
        }

        if (Factor.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(Factor)))
        {
            bitPullToRefresh.Factor = Factor.Value;
        }

        if (FullWidth.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(FullWidth)))
        {
            bitPullToRefresh.FullWidth = FullWidth.Value;

            bitPullToRefresh.ClassBuilder.Reset();
        }

        if (IndicatorTemplate is not null && bitPullToRefresh.HasNotBeenSet(nameof(IndicatorTemplate)))
        {
            bitPullToRefresh.IndicatorTemplate = IndicatorTemplate;
        }

        if (Loading is not null && bitPullToRefresh.HasNotBeenSet(nameof(Loading)))
        {
            bitPullToRefresh.Loading = Loading;
        }

        if (Margin.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(Margin)))
        {
            bitPullToRefresh.Margin = Margin.Value;
        }

        if (MaxPull.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(MaxPull)))
        {
            bitPullToRefresh.MaxPull = MaxPull.Value;
        }

        if (NoMouse.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(NoMouse)))
        {
            bitPullToRefresh.NoMouse = NoMouse.Value;
        }

        if (RefreshingLabel is not null && bitPullToRefresh.HasNotBeenSet(nameof(RefreshingLabel)))
        {
            bitPullToRefresh.RefreshingLabel = RefreshingLabel;
        }

        if (Release is not null && bitPullToRefresh.HasNotBeenSet(nameof(Release)))
        {
            bitPullToRefresh.Release = Release;
        }

        // An empty label is a value of its own here - it is what leaves the release state unannounced - so only a
        // label left unset on the params object stands down.
        if (ReleaseLabel is not null && bitPullToRefresh.HasNotBeenSet(nameof(ReleaseLabel)))
        {
            bitPullToRefresh.ReleaseLabel = ReleaseLabel;
        }

        if (Styles is not null && bitPullToRefresh.HasNotBeenSet(nameof(Styles)))
        {
            bitPullToRefresh.Styles = Styles;

            bitPullToRefresh.StyleBuilder.Reset();
        }

        if (Threshold.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(Threshold)))
        {
            bitPullToRefresh.Threshold = Threshold.Value;
        }

        if (Trigger.HasValue && bitPullToRefresh.HasNotBeenSet(nameof(Trigger)))
        {
            bitPullToRefresh.Trigger = Trigger.Value;
        }
    }
}
