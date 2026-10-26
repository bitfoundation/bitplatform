using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiImagePartView shows an image part of a message - one a model generated or a tool returned - with its caption.
/// </summary>
/// <remarks>
/// The image declares its intrinsic size where the part knows it, so the message list does not jump as images decode
/// during streaming, and it is loaded lazily, so a long transcript only fetches the images that scroll into view. The
/// alternative text of the part (<see cref="BitAiImagePart.Alt"/>) is its accessible name: a model will not always
/// provide one, so ask for it in the prompt or supply a fallback.
/// <br />
/// The image follows the <see cref="BitAiUiOptions.Images"/> policy, as the images of Markdown do: one the policy loads
/// only on request is a button that loads it - and hands the focus on to the image it loads - and one it blocks is
/// replaced by its alternative text. A source of a scheme that can run script is never loaded.
/// </remarks>
public partial class BitAiImagePartView : BitAiComponentBase
{
    // The source the user asked to load under a policy that asks first.
    private string? _loaded;

    private bool _focusImage;
    private ElementReference _image;
    private BitAiIconClassesCache<BitActionButtonClassStyles> _imagePlaceholderClasses;



    /// <summary>
    /// Gets or sets the cascading parameters for the image part view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the classes and styles of its parts) to be
    /// applied to multiple image part view components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiImagePartViewParams.ParamName)]
    public BitAiImagePartViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the image part view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiImagePartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The image to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiImagePart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the image part view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiImagePartViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiipv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiImagePartViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // An image the user loaded takes the focus once it has replaced the button that loaded it (see LoadImage).
        if (_focusImage)
        {
            _focusImage = false;

            await FocusImageAsync();
        }
    }



    // The button that loaded the image is gone once the image is shown, and the focus it held with it - to the body of
    // the document, from where the keyboard and a screen reader start over at the top of the page. So the image takes it,
    // programmatically focusable for that (tabindex="-1") and named by its alternative text.
    private void LoadImage(string source)
    {
        _loaded = source;
        _focusImage = true;
    }

    private async Task FocusImageAsync()
    {
        // No image was rendered for the source loaded - the part was handed another one in the meantime.
        if (string.IsNullOrEmpty(_image.Id) || IsDisposing) return;

        try
        {
            await _image.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // an image taken off the page in the meantime has nothing to focus
    }

    // The classes handed to the image placeholder: the app's own for that part with the glyph's added - it ships with
    // this package's own icon subset rather than with the icon font of the core library (see BitAiIconClasses) - built
    // again only when the app's change, so the button is not handed a new instance on every render.
    private BitActionButtonClassStyles ImagePlaceholderClasses =>
        _imagePlaceholderClasses.Get("bit-icon-ai bit-icon-ai--Photo2", Classes?.ImagePlaceholder, BitAiIconClasses.With);
}
