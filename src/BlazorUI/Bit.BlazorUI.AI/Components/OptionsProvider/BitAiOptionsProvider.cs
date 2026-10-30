namespace Bit.BlazorUI;

/// <summary>
/// Cascades the <see cref="BitAiUiOptions"/> - the string table, the render interval, what a message shows - to the AI
/// components inside it, so a page or a region of one sets its preferences once rather than on every component.
/// </summary>
/// <remarks>
/// It is plumbing, like <see cref="BitCascadingValueProvider"/>: it renders no element of its own, only the content it
/// is given, with the options handed down to every AI component in it (they read them through
/// <see cref="BitAiComponentBase"/>). Nesting works: an inner provider overrides the outer one for its own content.
/// <br />
/// Color scheme, accent, density and direction are not options of the AI components: they come from the bit theme
/// (a <see cref="BitThemeProvider"/>, a preset, <see cref="BitThemeLayout.DensityScale"/>) and from the <c>Dir</c> of
/// each component, exactly as for every other component of the library.
/// </remarks>
public class BitAiOptionsProvider : ComponentBase
{
    private BitAiUiOptions _effective = BitAiUiOptions.Default;



    [CascadingParameter] private BitAiUiOptions? OuterOptions { get; set; }

    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// The content the options are provided to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The options to cascade. Left unset, the options of an enclosing provider are cascaded, else the ones registered
    /// with <c>AddBitBlazorUIAIServices</c> (<see cref="BitAiBuilder.ConfigureUi"/>), else <see cref="BitAiUiOptions.Default"/>.
    /// </summary>
    [Parameter] public BitAiUiOptions? UiOptions { get; set; }



    protected override void OnParametersSet()
    {
        _effective = UiOptions
                  ?? OuterOptions
                  ?? _serviceProvider?.GetService(typeof(BitAiUiOptions)) as BitAiUiOptions
                  ?? BitAiUiOptions.Default;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Not fixed: a new options record handed in re-renders the components that read it.
        builder.OpenComponent<CascadingValue<BitAiUiOptions>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<BitAiUiOptions>.Value), _effective);
        builder.AddComponentParameter(2, nameof(CascadingValue<BitAiUiOptions>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
