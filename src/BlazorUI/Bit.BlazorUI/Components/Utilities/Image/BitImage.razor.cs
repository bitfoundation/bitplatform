﻿using System.Text;

namespace Bit.BlazorUI;

public partial class BitImage : BitComponentBase
{
    private BitImageLoadingState _loadingState;



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
    /// The image height value.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the image's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> ImageAttributes { get; set; } = [];

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
    [Parameter, ResetClassBuilder]
    public bool MaximizeFrame { get; set; }

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
    [Parameter, ResetClassBuilder]
    public bool ShouldFadeIn { get; set; } = true;

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
    /// The image width value.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    protected override string RootElementClass => "bit-img";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ShouldFadeIn ? "bit-img-fde" : string.Empty);

        ClassBuilder.Register(() => MaximizeFrame ? "bit-img-max" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);

        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);
    }



    private string GetImageClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append("bit-img-img");

        className.Append(' ').Append(ImageFit switch
        {
            BitImageFit.Center => "bit-img-ctr",
            BitImageFit.Contain => "bit-img-cnt",
            BitImageFit.Cover => "bit-img-cvr",
            BitImageFit.CenterCover => "bit-img-ccv",
            BitImageFit.CenterContain => "bit-img-cct",
            _ => "bit-img-non"
        });

        className.Append(' ').Append(CoverStyle switch
        {
            BitImageCoverStyle.Portrait => "bit-img-por",
            _ => "bit-img-lan"
        });

        if (_loadingState == BitImageLoadingState.Loaded || (_loadingState == BitImageLoadingState.NotLoaded && ShouldStartVisible))
        {
            className.Append(" bit-img-vis");
        }

        if (Classes?.Image.HasValue() ?? false)
        {
            className.Append(' ').Append(Classes?.Image);
        }

        return className.ToString();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private void HandleOnError()
    {
        _loadingState = BitImageLoadingState.Error;

        OnLoadingStateChange.InvokeAsync(_loadingState);
    }

    private void HandleOnLoad()
    {
        _loadingState = BitImageLoadingState.Loaded;

        OnLoadingStateChange.InvokeAsync(_loadingState);
    }

    private void HandleOnLoadStart()
    {
        _loadingState = BitImageLoadingState.NotLoaded;

        OnLoadingStateChange.InvokeAsync(_loadingState);
    }
}
