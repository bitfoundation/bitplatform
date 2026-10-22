using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiErrorView shows an error the user can act on: what went wrong, a retry when retrying could help, and the
/// technical detail behind a disclosure rather than in their face.
/// </summary>
/// <remarks>
/// The error is drawn as an Error <see cref="BitMessage"/>, which is what announces it to a screen reader as an
/// alert, and restyled through the <c>--bit-Message-*</c> variables (or <see cref="BitAiErrorViewClassStyles.Message"/>).
/// The retry is offered only when the error says retrying could help and a handler is there to do it.
/// </remarks>
public partial class BitAiErrorView : BitAiComponentBase
{
    private bool _showDetail;
    private BitAiIconClassesCache<BitButtonClassStyles> _retryButtonClasses;



    /// <summary>
    /// Gets or sets the cascading parameters for the error view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple error view components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiErrorViewParams.ParamName)]
    public BitAiErrorViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the error view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiErrorViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Renders the error inside another card, titled by its own message instead of a generic heading.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool Compact { get; set; }

    /// <summary>
    /// The failure to show.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiError Error { get; set; } = default!;

    /// <summary>
    /// Raised when the user asks to retry. The retry button is hidden while it is unset, and while the error says
    /// retrying would not help.
    /// </summary>
    [Parameter] public EventCallback OnRetry { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the error view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiErrorViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aierv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiErrorViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    private string _DetailId => $"{_Id}-dtl";

    private async Task HandleOnRetry()
    {
        if (Disabled) return;

        await OnRetry.InvokeAsync();
    }

    // Refresh is not one of the glyphs of the core font, so the retry button draws it from the subset font of this
    // package (see BitAiIconClasses), and is handed the same classes while the app's own stay the same.
    private BitButtonClassStyles _RetryButtonClasses =>
        _retryButtonClasses.Get("bit-icon-ai bit-icon-ai--Refresh", Classes?.RetryButton, BitAiIconClasses.With);

    private void ToggleDetail()
    {
        if (Disabled) return;

        _showDetail = _showDetail is false;
    }
}
