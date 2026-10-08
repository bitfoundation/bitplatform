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

        if (AnnounceViewChanges.HasValue)
        {
            bitMap.TakeFromCascade(nameof(AnnounceViewChanges), AnnounceViewChanges.Value, static m => m.AnnounceViewChanges, static (m, v) => m.AnnounceViewChanges = v);
        }

        if (AutoResize.HasValue)
        {
            bitMap.TakeFromCascade(nameof(AutoResize), AutoResize.Value, static m => m.AutoResize, static (m, v) => m.AutoResize = v);
        }

        if (Classes is not null)
        {
            bitMap.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (CooperativeGestures.HasValue)
        {
            bitMap.TakeFromCascade(nameof(CooperativeGestures), CooperativeGestures.Value, static m => m.CooperativeGestures, static (m, v) => m.CooperativeGestures = v);
        }

        if (CooperativeGesturesTouchHint.HasValue())
        {
            bitMap.TakeFromCascade(nameof(CooperativeGesturesTouchHint), CooperativeGesturesTouchHint!, static m => m.CooperativeGesturesTouchHint, static (m, v) => m.CooperativeGesturesTouchHint = v);
        }

        if (CooperativeGesturesWheelHint.HasValue())
        {
            bitMap.TakeFromCascade(nameof(CooperativeGesturesWheelHint), CooperativeGesturesWheelHint!, static m => m.CooperativeGesturesWheelHint, static (m, v) => m.CooperativeGesturesWheelHint = v);
        }

        if (ErrorLabel.HasValue())
        {
            bitMap.TakeFromCascade(nameof(ErrorLabel), ErrorLabel!, static m => m.ErrorLabel, static (m, v) => m.ErrorLabel = v);
        }

        if (ErrorTemplate is not null)
        {
            bitMap.TakeFromCascade(nameof(ErrorTemplate), ErrorTemplate, static m => m.ErrorTemplate, static (m, v) => m.ErrorTemplate = v);
        }

        if (EscapeToExit.HasValue)
        {
            bitMap.TakeFromCascade(nameof(EscapeToExit), EscapeToExit.Value, static m => m.EscapeToExit, static (m, v) => m.EscapeToExit = v);
        }

        if (KeyboardInstructions.HasValue())
        {
            bitMap.TakeFromCascade(nameof(KeyboardInstructions), KeyboardInstructions!, static m => m.KeyboardInstructions, static (m, v) => m.KeyboardInstructions = v);
        }

        if (LazyLoad.HasValue)
        {
            bitMap.TakeFromCascade(nameof(LazyLoad), LazyLoad.Value, static m => m.LazyLoad, static (m, v) => m.LazyLoad = v);
        }

        if (LazyLoadRootMargin.HasValue())
        {
            bitMap.TakeFromCascade(nameof(LazyLoadRootMargin), LazyLoadRootMargin!, static m => m.LazyLoadRootMargin, static (m, v) => m.LazyLoadRootMargin = v);
        }

        if (LoadingLabel.HasValue())
        {
            bitMap.TakeFromCascade(nameof(LoadingLabel), LoadingLabel!, static m => m.LoadingLabel, static (m, v) => m.LoadingLabel = v);
        }

        if (LoadingTemplate is not null)
        {
            bitMap.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static m => m.LoadingTemplate, static (m, v) => m.LoadingTemplate = v);
        }

        if (MarkerListActionHeader.HasValue())
        {
            bitMap.TakeFromCascade(nameof(MarkerListActionHeader), MarkerListActionHeader!, static m => m.MarkerListActionHeader, static (m, v) => m.MarkerListActionHeader = v);
        }

        if (MarkerListCaption.HasValue())
        {
            bitMap.TakeFromCascade(nameof(MarkerListCaption), MarkerListCaption!, static m => m.MarkerListCaption, static (m, v) => m.MarkerListCaption = v);
        }

        if (MarkerListLatitudeHeader.HasValue())
        {
            bitMap.TakeFromCascade(nameof(MarkerListLatitudeHeader), MarkerListLatitudeHeader!, static m => m.MarkerListLatitudeHeader, static (m, v) => m.MarkerListLatitudeHeader = v);
        }

        if (MarkerListLongitudeHeader.HasValue())
        {
            bitMap.TakeFromCascade(nameof(MarkerListLongitudeHeader), MarkerListLongitudeHeader!, static m => m.MarkerListLongitudeHeader, static (m, v) => m.MarkerListLongitudeHeader = v);
        }

        if (MarkerListMode.HasValue)
        {
            bitMap.TakeFromCascade(nameof(MarkerListMode), MarkerListMode.Value, static m => m.MarkerListMode, static (m, v) => m.MarkerListMode = v);
        }

        if (MarkerListNameHeader.HasValue())
        {
            bitMap.TakeFromCascade(nameof(MarkerListNameHeader), MarkerListNameHeader!, static m => m.MarkerListNameHeader, static (m, v) => m.MarkerListNameHeader = v);
        }

        if (MarkerListZoom.HasValue)
        {
            bitMap.TakeFromCascade(nameof(MarkerListZoom), MarkerListZoom, static m => m.MarkerListZoom, static (m, v) => m.MarkerListZoom = v);
        }

        if (PopupAutoPan.HasValue)
        {
            bitMap.TakeFromCascade(nameof(PopupAutoPan), PopupAutoPan.Value, static m => m.PopupAutoPan, static (m, v) => m.PopupAutoPan = v);
        }

        if (PopupCloseLabel.HasValue())
        {
            bitMap.TakeFromCascade(nameof(PopupCloseLabel), PopupCloseLabel!, static m => m.PopupCloseLabel, static (m, v) => m.PopupCloseLabel = v);
        }

        if (PopupLabel.HasValue())
        {
            bitMap.TakeFromCascade(nameof(PopupLabel), PopupLabel!, static m => m.PopupLabel, static (m, v) => m.PopupLabel = v);
        }

        if (ReplayStateOnProviderSwap.HasValue)
        {
            bitMap.TakeFromCascade(nameof(ReplayStateOnProviderSwap), ReplayStateOnProviderSwap.Value, static m => m.ReplayStateOnProviderSwap, static (m, v) => m.ReplayStateOnProviderSwap = v);
        }

        if (RespectReducedMotion.HasValue)
        {
            bitMap.TakeFromCascade(nameof(RespectReducedMotion), RespectReducedMotion.Value, static m => m.RespectReducedMotion, static (m, v) => m.RespectReducedMotion = v);
        }

        if (RoleDescription.HasValue())
        {
            bitMap.TakeFromCascade(nameof(RoleDescription), RoleDescription!, static m => m.RoleDescription, static (m, v) => m.RoleDescription = v);
        }

        if (ShowLoading.HasValue)
        {
            bitMap.TakeFromCascade(nameof(ShowLoading), ShowLoading.Value, static m => m.ShowLoading, static (m, v) => m.ShowLoading = v);
        }

        if (Styles is not null)
        {
            bitMap.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }

        if (UnsupportedLabel.HasValue())
        {
            bitMap.TakeFromCascade(nameof(UnsupportedLabel), UnsupportedLabel!, static m => m.UnsupportedLabel, static (m, v) => m.UnsupportedLabel = v);
        }

        if (UnsupportedTemplate is not null)
        {
            bitMap.TakeFromCascade(nameof(UnsupportedTemplate), UnsupportedTemplate, static m => m.UnsupportedTemplate, static (m, v) => m.UnsupportedTemplate = v);
        }

        if (ViewAnnouncementFormatter is not null)
        {
            bitMap.TakeFromCascade(nameof(ViewAnnouncementFormatter), ViewAnnouncementFormatter, static m => m.ViewAnnouncementFormatter, static (m, v) => m.ViewAnnouncementFormatter = v);
        }

        if (ViewAnnouncementThrottle.HasValue)
        {
            bitMap.TakeFromCascade(nameof(ViewAnnouncementThrottle), ViewAnnouncementThrottle.Value, static m => m.ViewAnnouncementThrottle, static (m, v) => m.ViewAnnouncementThrottle = v);
        }
    }
}
