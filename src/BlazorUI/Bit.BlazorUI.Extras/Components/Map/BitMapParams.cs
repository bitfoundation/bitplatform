namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMap{TMapProvider}"/> component.
/// </summary>
/// <remarks>
/// What a <see cref="BitParams"/> carries is a default and never an override: a map takes each of these only where its
/// own markup leaves that parameter unset, so one wrapped around a layout sets the behavior, the look and the texts of
/// every map in it once - whichever provider each of them uses.
/// <br />
/// The provider, the camera (Center and Zoom), the markers, the clustering, the content templates of the markers and
/// the event callbacks are left out on purpose: they describe the data and the state of a single map rather than a
/// default for many.
/// </remarks>
public class BitMapParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMap{TMapProvider}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMap value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// Every map reads the same object whatever its provider type argument is, so the name does not name one, and
    /// neither does the type: it is not generic.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMap<BitLeafletMapProvider>)}";



    public string Name => ParamName;



    /// <summary>
    /// Announces the new centre and zoom through a polite live region after the user pans or zooms.
    /// </summary>
    public bool? AnnounceViewChanges { get; set; }

    /// <summary>
    /// Keeps the map sized to its container automatically.
    /// </summary>
    public bool? AutoResize { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the map.
    /// </summary>
    public BitMapClassStyles? Classes { get; set; }

    /// <summary>
    /// Requires a modifier before the map takes over the gesture: ctrl/⌘ + wheel to zoom, two fingers to pan.
    /// </summary>
    public bool? CooperativeGestures { get; set; }

    /// <summary>
    /// The hint shown when a one-finger drag is blocked by the cooperative gestures.
    /// </summary>
    public string? CooperativeGesturesTouchHint { get; set; }

    /// <summary>
    /// The hint shown when a bare wheel gesture is blocked by the cooperative gestures.
    /// </summary>
    public string? CooperativeGesturesWheelHint { get; set; }

    /// <summary>
    /// The text of the built-in failure message.
    /// </summary>
    public string? ErrorLabel { get; set; }

    /// <summary>
    /// Replaces the built-in failure message.
    /// </summary>
    public RenderFragment<Exception?>? ErrorTemplate { get; set; }

    /// <summary>
    /// Moves the focus out of the map canvas when Escape is pressed.
    /// </summary>
    public bool? EscapeToExit { get; set; }

    /// <summary>
    /// The description of the keyboard model of the map canvas.
    /// </summary>
    public string? KeyboardInstructions { get; set; }

    /// <summary>
    /// Defers creating the map until its container is at (or near) the viewport.
    /// </summary>
    public bool? LazyLoad { get; set; }

    /// <summary>
    /// How far outside the viewport the container may be and still count as visible for LazyLoad.
    /// </summary>
    public string? LazyLoadRootMargin { get; set; }

    /// <summary>
    /// The text of the built-in loading indicator.
    /// </summary>
    public string? LoadingLabel { get; set; }

    /// <summary>
    /// Replaces the built-in loading indicator.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The label of the button that brings a marker into view, also the hidden column heading of the marker table.
    /// </summary>
    public string? MarkerListActionHeader { get; set; }

    /// <summary>
    /// The caption of the built-in marker table.
    /// </summary>
    public string? MarkerListCaption { get; set; }

    /// <summary>
    /// The column heading for the latitude of the markers.
    /// </summary>
    public string? MarkerListLatitudeHeader { get; set; }

    /// <summary>
    /// The column heading for the longitude of the markers.
    /// </summary>
    public string? MarkerListLongitudeHeader { get; set; }

    /// <summary>
    /// How the text alternative to the markers is rendered.
    /// </summary>
    public BitMapMarkerListMode? MarkerListMode { get; set; }

    /// <summary>
    /// The column heading for the name of the markers.
    /// </summary>
    public string? MarkerListNameHeader { get; set; }

    /// <summary>
    /// The zoom applied when a marker-list row brings its marker into view.
    /// </summary>
    public double? MarkerListZoom { get; set; }

    /// <summary>
    /// Pans the map as a popup opens, just enough to bring the whole popup inside the map.
    /// </summary>
    public bool? PopupAutoPan { get; set; }

    /// <summary>
    /// The accessible name of the close button of the popup.
    /// </summary>
    public string? PopupCloseLabel { get; set; }

    /// <summary>
    /// The accessible name of the popup when its marker has neither an Alt nor a Title.
    /// </summary>
    public string? PopupLabel { get; set; }

    /// <summary>
    /// Replays the imperatively added markers, vector layers and tile overlays after a destructive provider swap.
    /// </summary>
    public bool? ReplayStateOnProviderSwap { get; set; }

    /// <summary>
    /// Honours the reduced-motion preference of the operating system for the camera moves.
    /// </summary>
    public bool? RespectReducedMotion { get; set; }

    /// <summary>
    /// What a screen reader calls the map canvas in place of "region".
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// Shows the built-in loading indicator while the map is being created.
    /// </summary>
    public bool? ShowLoading { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the map.
    /// </summary>
    public BitMapClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the built-in unsupported-browser message.
    /// </summary>
    public string? UnsupportedLabel { get; set; }

    /// <summary>
    /// Replaces the built-in unsupported-browser message.
    /// </summary>
    public RenderFragment? UnsupportedTemplate { get; set; }

    /// <summary>
    /// Builds the text announced when AnnounceViewChanges is on.
    /// </summary>
    public Func<BitMapViewState, string>? ViewAnnouncementFormatter { get; set; }

    /// <summary>
    /// The minimum interval between two view announcements.
    /// </summary>
    public TimeSpan? ViewAnnouncementThrottle { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMap{TMapProvider}"/> instance with any values that have been
    /// set on this object, if those properties have not already been set on the <see cref="BitMap{TMapProvider}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMap"/> will be
    /// updated. This method does not overwrite existing values on <paramref name="bitMap"/>.
    /// </remarks>
    /// <typeparam name="TMapProvider">The provider type argument of the map.</typeparam>
    /// <param name="bitMap">
    /// The <see cref="BitMap{TMapProvider}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TMapProvider>(BitMap<TMapProvider> bitMap) where TMapProvider : class, IBitMapProvider, new()
    {
        if (bitMap is null) return;

        UpdateBaseParameters(bitMap);

        // This runs on every render of every map under the BitParams, so a value that drives the class or
        // the style of its root only resets the builder when it differs from the one it already holds: an
        // unchanged one would rebuild both strings on every render for nothing.
        if (AnnounceViewChanges.HasValue && bitMap.HasNotBeenSet(nameof(AnnounceViewChanges)))
        {
            bitMap.AnnounceViewChanges = AnnounceViewChanges.Value;
        }

        if (AutoResize.HasValue && bitMap.HasNotBeenSet(nameof(AutoResize)))
        {
            bitMap.AutoResize = AutoResize.Value;
        }

        if (Classes is not null && bitMap.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitMap.Classes, Classes) is false)
        {
            bitMap.Classes = Classes;

            bitMap.ClassBuilder.Reset();
        }

        if (CooperativeGestures.HasValue && bitMap.HasNotBeenSet(nameof(CooperativeGestures)))
        {
            bitMap.CooperativeGestures = CooperativeGestures.Value;
        }

        if (CooperativeGesturesTouchHint.HasValue() && bitMap.HasNotBeenSet(nameof(CooperativeGesturesTouchHint)))
        {
            bitMap.CooperativeGesturesTouchHint = CooperativeGesturesTouchHint!;
        }

        if (CooperativeGesturesWheelHint.HasValue() && bitMap.HasNotBeenSet(nameof(CooperativeGesturesWheelHint)))
        {
            bitMap.CooperativeGesturesWheelHint = CooperativeGesturesWheelHint!;
        }

        if (ErrorLabel.HasValue() && bitMap.HasNotBeenSet(nameof(ErrorLabel)))
        {
            bitMap.ErrorLabel = ErrorLabel!;
        }

        if (ErrorTemplate is not null && bitMap.HasNotBeenSet(nameof(ErrorTemplate)))
        {
            bitMap.ErrorTemplate = ErrorTemplate;
        }

        if (EscapeToExit.HasValue && bitMap.HasNotBeenSet(nameof(EscapeToExit)))
        {
            bitMap.EscapeToExit = EscapeToExit.Value;
        }

        if (KeyboardInstructions.HasValue() && bitMap.HasNotBeenSet(nameof(KeyboardInstructions)))
        {
            bitMap.KeyboardInstructions = KeyboardInstructions!;
        }

        if (LazyLoad.HasValue && bitMap.HasNotBeenSet(nameof(LazyLoad)))
        {
            bitMap.LazyLoad = LazyLoad.Value;
        }

        if (LazyLoadRootMargin.HasValue() && bitMap.HasNotBeenSet(nameof(LazyLoadRootMargin)))
        {
            bitMap.LazyLoadRootMargin = LazyLoadRootMargin!;
        }

        if (LoadingLabel.HasValue() && bitMap.HasNotBeenSet(nameof(LoadingLabel)))
        {
            bitMap.LoadingLabel = LoadingLabel!;
        }

        if (LoadingTemplate is not null && bitMap.HasNotBeenSet(nameof(LoadingTemplate)))
        {
            bitMap.LoadingTemplate = LoadingTemplate;
        }

        if (MarkerListActionHeader.HasValue() && bitMap.HasNotBeenSet(nameof(MarkerListActionHeader)))
        {
            bitMap.MarkerListActionHeader = MarkerListActionHeader!;
        }

        if (MarkerListCaption.HasValue() && bitMap.HasNotBeenSet(nameof(MarkerListCaption)))
        {
            bitMap.MarkerListCaption = MarkerListCaption!;
        }

        if (MarkerListLatitudeHeader.HasValue() && bitMap.HasNotBeenSet(nameof(MarkerListLatitudeHeader)))
        {
            bitMap.MarkerListLatitudeHeader = MarkerListLatitudeHeader!;
        }

        if (MarkerListLongitudeHeader.HasValue() && bitMap.HasNotBeenSet(nameof(MarkerListLongitudeHeader)))
        {
            bitMap.MarkerListLongitudeHeader = MarkerListLongitudeHeader!;
        }

        if (MarkerListMode.HasValue && bitMap.HasNotBeenSet(nameof(MarkerListMode)))
        {
            bitMap.MarkerListMode = MarkerListMode.Value;
        }

        if (MarkerListNameHeader.HasValue() && bitMap.HasNotBeenSet(nameof(MarkerListNameHeader)))
        {
            bitMap.MarkerListNameHeader = MarkerListNameHeader!;
        }

        if (MarkerListZoom.HasValue && bitMap.HasNotBeenSet(nameof(MarkerListZoom)))
        {
            bitMap.MarkerListZoom = MarkerListZoom;
        }

        if (PopupAutoPan.HasValue && bitMap.HasNotBeenSet(nameof(PopupAutoPan)))
        {
            bitMap.PopupAutoPan = PopupAutoPan.Value;
        }

        if (PopupCloseLabel.HasValue() && bitMap.HasNotBeenSet(nameof(PopupCloseLabel)))
        {
            bitMap.PopupCloseLabel = PopupCloseLabel!;
        }

        if (PopupLabel.HasValue() && bitMap.HasNotBeenSet(nameof(PopupLabel)))
        {
            bitMap.PopupLabel = PopupLabel!;
        }

        if (ReplayStateOnProviderSwap.HasValue && bitMap.HasNotBeenSet(nameof(ReplayStateOnProviderSwap)))
        {
            bitMap.ReplayStateOnProviderSwap = ReplayStateOnProviderSwap.Value;
        }

        if (RespectReducedMotion.HasValue && bitMap.HasNotBeenSet(nameof(RespectReducedMotion)))
        {
            bitMap.RespectReducedMotion = RespectReducedMotion.Value;
        }

        if (RoleDescription.HasValue() && bitMap.HasNotBeenSet(nameof(RoleDescription)))
        {
            bitMap.RoleDescription = RoleDescription!;
        }

        if (ShowLoading.HasValue && bitMap.HasNotBeenSet(nameof(ShowLoading)))
        {
            bitMap.ShowLoading = ShowLoading.Value;
        }

        if (Styles is not null && bitMap.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitMap.Styles, Styles) is false)
        {
            bitMap.Styles = Styles;

            bitMap.StyleBuilder.Reset();
        }

        if (UnsupportedLabel.HasValue() && bitMap.HasNotBeenSet(nameof(UnsupportedLabel)))
        {
            bitMap.UnsupportedLabel = UnsupportedLabel!;
        }

        if (UnsupportedTemplate is not null && bitMap.HasNotBeenSet(nameof(UnsupportedTemplate)))
        {
            bitMap.UnsupportedTemplate = UnsupportedTemplate;
        }

        if (ViewAnnouncementFormatter is not null && bitMap.HasNotBeenSet(nameof(ViewAnnouncementFormatter)))
        {
            bitMap.ViewAnnouncementFormatter = ViewAnnouncementFormatter;
        }

        if (ViewAnnouncementThrottle.HasValue && bitMap.HasNotBeenSet(nameof(ViewAnnouncementThrottle)))
        {
            bitMap.ViewAnnouncementThrottle = ViewAnnouncementThrottle.Value;
        }
    }
}
