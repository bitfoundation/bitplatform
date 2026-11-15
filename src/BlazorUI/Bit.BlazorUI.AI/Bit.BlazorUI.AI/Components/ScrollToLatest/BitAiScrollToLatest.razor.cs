namespace Bit.BlazorUI;

/// <summary>
/// BitAiScrollToLatest is the button that appears when the reader has scrolled away from the newest message of a
/// transcript, and takes them back to it.
/// </summary>
/// <remarks>
/// It is shown only while there is somewhere to go (<see cref="Visible"/>), so it never sits over the transcript doing
/// nothing. It floats centered above the nearest positioned ancestor - the footer of a <see cref="BitAiChat"/> - so it
/// sits just above the composer whatever height the composer has.
/// </remarks>
public partial class BitAiScrollToLatest : BitAiComponentBase
{
    private BitAiIconClassesCache<BitButtonClassStyles> _buttonClasses;



    /// <summary>
    /// Gets or sets the cascading parameters for the scroll to latest component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple scroll to latest
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiScrollToLatestParams.ParamName)]
    public BitAiScrollToLatestParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the scroll to latest.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiScrollToLatestClassStyles? Classes { get; set; }

    /// <summary>
    /// The text of the button, in place of the one of the string table (<see cref="BitAiStrings.ScrollToLatest"/>).
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The callback for when the button is pressed.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the scroll to latest.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiScrollToLatestClassStyles? Styles { get; set; }

    /// <summary>
    /// Shows the button. Left unset, nothing is rendered.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool Visible { get; set; }



    protected override string RootElementClass => "bit-aistl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    // The glyph is not one of the core font's, so it is drawn from this package's own subset (see BitAiIconClasses), and
    // the button is handed the same classes while the app's own stay the same.
    private BitButtonClassStyles _ButtonClasses =>
        _buttonClasses.Get("bit-icon-ai bit-icon-ai--Down", Classes?.Button, BitAiIconClasses.With);

    private async Task HandleOnClick()
    {
        if (Disabled) return;

        await OnClick.InvokeAsync();
    }
}
