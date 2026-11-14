using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitResourceTimeline"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the timelines of an application agree on: their options (dimensions, fonts, colors, time
/// zone, zoom limits, editing), their texts, how they stream their data and show its loading, and how they are styled.
/// The data, the windowed provider, the templates of one timeline's rows, tooltips and corner, and the events and
/// callbacks are left out on purpose: they are what makes one timeline the one it is.
/// </remarks>
public partial class BitResourceTimelineParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitResourceTimeline"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitResourceTimeline value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitResourceTimeline)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineParams))]
    public BitResourceTimelineParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the resource timeline.
    /// </summary>
    public BitResourceTimelineClassStyles? Classes { get; set; }

    /// <summary>
    /// The maximum number of allocations sent to the browser per interop call.
    /// </summary>
    public int? LoadBatchSize { get; set; }

    /// <summary>
    /// The custom content of the overlay shown while the engine draws the bars.
    /// </summary>
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// The minimum time in milliseconds the loading overlay stays visible.
    /// </summary>
    public int? LoadingMinDurationMs { get; set; }

    /// <summary>
    /// The visual configuration of the timeline.
    /// </summary>
    public BitResourceTimelineOptions? Options { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the resource timeline.
    /// </summary>
    public BitResourceTimelineClassStyles? Styles { get; set; }

    /// <summary>
    /// The texts of the timeline.
    /// </summary>
    public BitResourceTimelineTexts? Texts { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitResourceTimeline"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitResourceTimeline"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitResourceTimeline"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitResourceTimeline"/>.
    /// </remarks>
    /// <param name="bitResourceTimeline">
    /// The <see cref="BitResourceTimeline"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitResourceTimeline bitResourceTimeline)
    {
        if (bitResourceTimeline is null) return;

        UpdateBaseParameters(bitResourceTimeline);

        if (Classes is not null)
        {
            bitResourceTimeline.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (LoadBatchSize.HasValue)
        {
            bitResourceTimeline.TakeFromCascade(nameof(LoadBatchSize), LoadBatchSize.Value, static t => t.LoadBatchSize, static (t, v) => t.LoadBatchSize = v);
        }

        if (LoadingContent is not null)
        {
            bitResourceTimeline.TakeFromCascade(nameof(LoadingContent), LoadingContent, static t => t.LoadingContent, static (t, v) => t.LoadingContent = v);
        }

        if (LoadingMinDurationMs.HasValue)
        {
            bitResourceTimeline.TakeFromCascade(nameof(LoadingMinDurationMs), LoadingMinDurationMs.Value, static t => t.LoadingMinDurationMs, static (t, v) => t.LoadingMinDurationMs = v);
        }

        if (Options is not null)
        {
            bitResourceTimeline.TakeFromCascade(nameof(Options), Options, static t => t.Options, static (t, v) => t.Options = v);
        }

        if (Styles is not null)
        {
            bitResourceTimeline.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Texts is not null)
        {
            bitResourceTimeline.TakeFromCascade(nameof(Texts), Texts, static t => t.Texts, static (t, v) => t.Texts = v);
        }
    }
}
