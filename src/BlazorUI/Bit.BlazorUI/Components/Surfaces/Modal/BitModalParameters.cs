namespace Bit.BlazorUI;

/// <summary>
/// The set of parameters used to customize a <see cref="BitModal"/> that is shown using the <see cref="BitModalService"/>.
/// </summary>
/// <remarks>
/// BREAKING CHANGE: the boolean members (<see cref="IsEnabled"/>, <see cref="AriaModal"/>, <see cref="Blocking"/>,
/// <see cref="FullHeight"/>, <see cref="FullWidth"/>, <see cref="ShowOverlay"/>) are now nullable (<c>bool?</c>)
/// instead of <c>bool</c>. A <c>null</c> value means "not set" and the corresponding <see cref="BitModal"/> default
/// is used (or the cascaded value, when merged). Code that read these members as non-nullable <c>bool</c> must be updated.
/// </remarks>
public class BitModalParameters
{
    /// <summary>
    /// Whether or not the Modal is enabled. <c>null</c> means not set (defaults to enabled).
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the Modal's parameters.
    /// </summary>
    public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    /// <summary>
    /// The general directionality of the Modal.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies. <c>null</c> means not set (defaults to <c>true</c>).
    /// </summary>
    public bool? AriaModal { get; set; }

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay). <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    public BitModalClassStyles? Classes { get; set; }

    /// <summary>
    /// Makes the Modal height 100% of its parent container. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    public bool? IsAlert { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Whether the overlay should be rendered. <c>null</c> means not set (defaults to <c>true</c>).
    /// </summary>
    public bool? ShowOverlay { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitModal component.
    /// </summary>
    public BitModalClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    public string? TitleAriaId { get; set; }


    /// <summary>
    /// Merges two sets of <see cref="BitModalParameters"/> giving precedence to the values of the first one.
    /// </summary>
    public static BitModalParameters? Merge(BitModalParameters? params1, BitModalParameters? params2)
    {
        if (params1 is null && params2 is null) return null;

        if (params2 is null) return params1;
        if (params1 is null) return params2;


        return new BitModalParameters
        {
            IsEnabled = params1.IsEnabled ?? params2.IsEnabled,
            HtmlAttributes = (params2.HtmlAttributes ?? []).Concat(params1.HtmlAttributes ?? []).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value),
            Dir = params1.Dir ?? params2.Dir,
            AriaModal = params1.AriaModal ?? params2.AriaModal,
            Blocking = params1.Blocking ?? params2.Blocking,
            Classes = BitModalClassStyles.Merge(params1.Classes, params2.Classes),
            FullHeight = params1.FullHeight ?? params2.FullHeight,
            FullWidth = params1.FullWidth ?? params2.FullWidth,
            IsAlert = params1.IsAlert ?? params2.IsAlert,
            OnDismiss = MergeCallbacks(params1.OnDismiss, params2.OnDismiss),
            OnOverlayClick = MergeCallbacks(params1.OnOverlayClick, params2.OnOverlayClick),
            ShowOverlay = params1.ShowOverlay ?? params2.ShowOverlay,
            Styles = BitModalClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
        };
    }

    /// <summary>
    /// Composes two <see cref="EventCallback{MouseEventArgs}"/> into one that invokes both (first then second).
    /// Returns an empty callback when neither source has a delegate, so the merged result preserves the
    /// "no delegate" semantics (<see cref="EventCallback.HasDelegate"/> stays <c>false</c>) instead of
    /// reporting a handler that does nothing.
    /// </summary>
    private static EventCallback<MouseEventArgs> MergeCallbacks(EventCallback<MouseEventArgs> callback1, EventCallback<MouseEventArgs> callback2)
    {
        if (callback1.HasDelegate is false && callback2.HasDelegate is false) return default;

        // These callbacks are invoked manually (never bound to a child component), so the
        // EventCallback receiver only needs to be non-null to be considered "has delegate".
        // A throwaway object() is sufficient here; there's no component to associate for re-render.
        return EventCallback.Factory.Create<MouseEventArgs>(new object(), async (MouseEventArgs e) =>
        {
            await callback1.InvokeAsync(e);
            await callback2.InvokeAsync(e);
        });
    }
}
