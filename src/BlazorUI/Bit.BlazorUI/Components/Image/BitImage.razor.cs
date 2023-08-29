namespace Bit.BlazorUI;

public partial class BitImage
{
    private string? _internalSrc;
    private bool _isLoaded;



    /// <summary>
    /// Specifies an alternate text for the image.
    /// </summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// Detailed description of the image for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// If true, fades the image in when loaded.
    /// </summary>
    [Parameter] public bool ShouldFadeIn { get; set; } = true;

    /// <summary>
    /// If true, the image starts as visible and is hidden on error. Otherwise, the image is hidden until it is successfully loaded.
    /// This disables ShouldFadeIn.
    /// </summary>
    [Parameter] public bool ShouldStartVisible { get; set; } = true;

    /// <summary>
    /// Used to determine how the image is scaled and cropped to fit the frame.
    /// </summary>
    [Parameter] public BitImageFit ImageFit { get; set; }

    /// <summary>
    /// If true, the image frame will expand to fill its parent container.
    /// </summary>
    [Parameter] public bool MaximizeFrame { get; set; }

    /// <summary>
    /// Optional callback method for when the image load state has changed.
    /// The 'loadState' parameter indicates the current state of the Image.
    /// </summary>
    [Parameter] public EventCallback<BitImageLoadingState> OnLoadingStateChange { get; set; }

    /// <summary>
    /// Specifies the cover style to be used for this image.
    /// </summary>
    [Parameter] public BitImageCoverStyle CoverStyle { get; set; }

    /// <summary>
    /// Allows for browser-level image loading (lazy or eager).
    /// </summary>
    [Parameter] public string? Loading { get; set; }

    /// <summary>
    /// Specifies the src of image.
    /// </summary>
    [Parameter] public string? Src { get; set; }

    /// <summary>
    /// Specifies the error src of image.
    /// </summary>
    [Parameter] public string? ErrorSrc { get; set; }

    /// <summary>
    /// Callback for when the image clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the image.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The image height value in px.
    /// </summary>
    [Parameter] public double? Height { get; set; }

    /// <summary>
    /// The image width value in px.
    /// </summary>
    [Parameter] public double? Width { get; set; }



    protected override string RootElementClass => "bit-img";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ShouldFadeIn ? $"{RootElementClass}-fade" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Width > 0 ? $"width:{Width}px" : string.Empty);
        StyleBuilder.Register(() => Height > 0 ? $"height:{Height}px" : string.Empty);
    }

    private string GetImageFitClass() => ImageFit switch
    {
        BitImageFit.Center => $"{RootElementClass}-center",
        BitImageFit.Contain => $"{RootElementClass}-contain",
        BitImageFit.Cover => $"{RootElementClass}-cover",
        BitImageFit.None => $"{RootElementClass}-none",
        BitImageFit.CenterCover => $"{RootElementClass}-center-cover",
        BitImageFit.CenterContain => $"{RootElementClass}-center-contain",
        _ => $"{RootElementClass}-none"
    };

    private string GetCoverStyleClass() => CoverStyle switch
    {
        BitImageCoverStyle.Landscape => $"{RootElementClass}-landscape",
        BitImageCoverStyle.Portrait => $"{RootElementClass}-portrait",
        _ => $"{RootElementClass}-landscape"
    };

    protected override Task OnParametersSetAsync()
    {
        _internalSrc = Src;

        return base.OnParametersSetAsync();
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
        _internalSrc = ErrorSrc;
        ShouldStartVisible = true;
    }

    protected void HandleOnLoad()
    {
        ShouldStartVisible = true;
        StateHasChanged();
    }
}
