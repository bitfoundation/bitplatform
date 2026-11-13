using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitImageViewer"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the image viewers of an app agree on: how far and in what steps they zoom, how they are
/// operated, their size, look and words. The image and its zoom are the state of a single viewer.
/// </remarks>
public class BitImageViewerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitImageViewer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitImageViewer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitImageViewer";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitImageViewerParams))]
    public BitImageViewerParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the image viewer.
    /// </summary>
    public BitImageViewerClassStyles? Classes { get; set; }

    /// <summary>
    /// The zoom a double-click goes to.
    /// </summary>
    public double? DoubleClickZoom { get; set; }

    /// <summary>
    /// The height of the frame the image is shown in.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// The largest zoom.
    /// </summary>
    public double? MaxZoom { get; set; }

    /// <summary>
    /// The smallest zoom.
    /// </summary>
    public double? MinZoom { get; set; }

    /// <summary>
    /// Leaves the zoom buttons out.
    /// </summary>
    public bool? NoToolbar { get; set; }

    /// <summary>
    /// Adds a button to the toolbar that shows the viewer full screen.
    /// </summary>
    public bool? ShowFullScreenButton { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the image viewer.
    /// </summary>
    public BitImageViewerClassStyles? Styles { get; set; }

    /// <summary>
    /// The words of the viewer.
    /// </summary>
    public BitImageViewerTexts? Texts { get; set; }

    /// <summary>
    /// Zooms with the plain wheel too.
    /// </summary>
    public bool? WheelZoom { get; set; }

    /// <summary>
    /// The factor a step of zoom multiplies or divides the zoom by.
    /// </summary>
    public double? ZoomFactor { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitImageViewer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitImageViewer"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitImageViewer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitImageViewer"/>.
    /// </remarks>
    /// <param name="bitImageViewer">
    /// The <see cref="BitImageViewer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitImageViewer bitImageViewer)
    {
        if (bitImageViewer is null) return;

        UpdateBaseParameters(bitImageViewer);

        if (Classes is not null)
        {
            bitImageViewer.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (DoubleClickZoom.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(DoubleClickZoom), DoubleClickZoom.Value, static c => c.DoubleClickZoom, static (c, v) => c.DoubleClickZoom = v);
        }

        if (Height is not null)
        {
            bitImageViewer.TakeFromCascade(nameof(Height), Height, static c => c.Height, static (c, v) => c.Height = v);
        }

        if (MaxZoom.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(MaxZoom), MaxZoom.Value, static c => c.MaxZoom, static (c, v) => c.MaxZoom = v);
        }

        if (MinZoom.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(MinZoom), MinZoom.Value, static c => c.MinZoom, static (c, v) => c.MinZoom = v);
        }

        if (NoToolbar.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(NoToolbar), NoToolbar.Value, static c => c.NoToolbar, static (c, v) => c.NoToolbar = v);
        }

        if (ShowFullScreenButton.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(ShowFullScreenButton), ShowFullScreenButton.Value, static c => c.ShowFullScreenButton, static (c, v) => c.ShowFullScreenButton = v);
        }

        if (Styles is not null)
        {
            bitImageViewer.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (Texts is not null)
        {
            bitImageViewer.TakeFromCascade(nameof(Texts), Texts, static c => c.Texts, static (c, v) => c.Texts = v);
        }

        if (WheelZoom.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(WheelZoom), WheelZoom.Value, static c => c.WheelZoom, static (c, v) => c.WheelZoom = v);
        }

        if (ZoomFactor.HasValue)
        {
            bitImageViewer.TakeFromCascade(nameof(ZoomFactor), ZoomFactor.Value, static c => c.ZoomFactor, static (c, v) => c.ZoomFactor = v);
        }
    }
}
