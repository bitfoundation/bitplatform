using System.Text.RegularExpressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitImage
{
    private string? _internalSrc;
    private bool _imageIsVisible;
    private BitImageLoadingState _loadState;



    /// <summary>
    /// Specifies an alternate text for the image.
    /// </summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitImage.
    /// </summary>
    [Parameter] public BitImageClassStyles? Classes { get; set; }

    /// <summary>
    /// Specifies the cover style to be used for this image.
    /// </summary>
    [Parameter] public BitImageCoverStyle CoverStyle { get; set; }

    /// <summary>
    /// The image height value in px.
    /// </summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the image's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> ImageAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Used to determine how the image is scaled and cropped to fit the frame.
    /// </summary>
    [Parameter] public BitImageFit ImageFit { get; set; }

    /// <summary>
    /// Allows for browser-level image loading (lazy or eager).
    /// </summary>
    [Parameter] public string? Loading { get; set; }

    /// <summary>
    /// If true, the image frame will expand to fill its parent container.
    /// </summary>
    [Parameter] public bool MaximizeFrame { get; set; }

    /// <summary>
    /// Callback for when the image clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Optional callback method for when the image load state has changed.
    /// The 'loadState' parameter indicates the current state of the Image.
    /// </summary>
    [Parameter] public EventCallback<BitImageLoadingState> OnLoadingStateChange { get; set; }

    /// <summary>
    /// If true, fades the image in when loaded.
    /// </summary>
    [Parameter] public bool ShouldFadeIn { get; set; } = true;

    /// <summary>
    /// If true, the image starts as visible and is hidden on error. Otherwise, the image is hidden until it is successfully loaded.
    /// </summary>
    [Parameter] public bool ShouldStartVisible { get; set; } = true;

    /// <summary>
    /// Specifies the src of image.
    /// </summary>
    [Parameter] public string? Src { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitImage.
    /// </summary>
    [Parameter] public BitImageClassStyles? Styles { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the image.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The image width value in px.
    /// </summary>
    [Parameter] public double? Width { get; set; }



    protected override string RootElementClass => "bit-img";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ShouldFadeIn ? $"{RootElementClass}-fde" : string.Empty);
        ClassBuilder.Register(() => MaximizeFrame ? $"{RootElementClass}-max" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Width > 0 ? $"width:{Width}px" : string.Empty);
        StyleBuilder.Register(() => Height > 0 ? $"height:{Height}px" : string.Empty);
    }

    private string GetImageClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append(RootElementClass).Append("-elm");

        className.Append(' ').Append(ImageFit switch
        {
            BitImageFit.Center => $"{RootElementClass}-ctr",
            BitImageFit.Contain => $"{RootElementClass}-cnt",
            BitImageFit.Cover => $"{RootElementClass}-cvr",
            BitImageFit.CenterCover => $"{RootElementClass}-ccv",
            BitImageFit.CenterContain => $"{RootElementClass}-cct",
            _ => $"{RootElementClass}-non"
        });

        className.Append(' ').Append(CoverStyle switch
        {
            BitImageCoverStyle.Portrait => $"{RootElementClass}-prt",
            _ => $"{RootElementClass}-lnd"
        });

        className.Append(' ').Append(_loadState switch
        {
            BitImageLoadingState.NotLoaded => $"{RootElementClass}-nld",
            BitImageLoadingState.Error => $"{RootElementClass}-err",
            _ => $"{RootElementClass}-ldd"
        });

        if (string.IsNullOrEmpty(Classes?.Image) is false)
        {
            className.Append(' ').Append(Classes?.Image);
        }

        return className.ToString();
    }

    protected override void OnInitialized()
    {
        _imageIsVisible = ShouldStartVisible;

        OnLoadingStateChange.InvokeAsync(_loadState);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        _internalSrc = Src;

        base.OnParametersSet();
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }

    protected virtual void HandleOnError()
    {
        _loadState = BitImageLoadingState.Error;

        OnLoadingStateChange.InvokeAsync(_loadState);
    }

    protected void HandleOnLoad()
    {
        _loadState = BitImageLoadingState.Loaded;

        OnLoadingStateChange.InvokeAsync(_loadState);
    }

    protected void HandleOnLoadStart()
    {
        _loadState = BitImageLoadingState.NotLoaded;

        OnLoadingStateChange.InvokeAsync(_loadState);
    }
}
