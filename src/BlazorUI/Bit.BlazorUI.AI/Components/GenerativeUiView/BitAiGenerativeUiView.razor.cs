namespace Bit.BlazorUI;

/// <summary>
/// BitAiGenerativeUiView renders a component the model chose, with the properties it produced.
/// </summary>
/// <remarks>
/// Resolution goes through the <see cref="IBitAiGenerativeUiRegistry"/>, never through a type name in the payload, so a
/// model can only ever instantiate a component the app deliberately registered, and only set the properties listed on
/// it. A name that is not registered renders a quiet placeholder rather than an exception, because a made-up component
/// should not take down the response around it, and the component itself runs inside a
/// <see cref="BitAiRendererBoundary"/> for the same reason. Properties that are still streaming bind as far as they
/// have arrived, so a card fills in as the model writes it.
/// </remarks>
public partial class BitAiGenerativeUiView : BitAiComponentBase
{
    private BitAiGenerativeComponent? _component;
    private BitAiGenerativeBindResult? _binding;
    private BitAiGenerativeUiPart? _boundPart;
    private BitAiGenerativeComponent? _boundComponent;
    private int _boundVersion = -1;
    private readonly RenderFragment _renderComponent;



    public BitAiGenerativeUiView()
    {
        // Held in a field rather than created per render, so the diff keeps being handed the same delegate.
        _renderComponent = RenderComponent;
    }



    [Inject] private IBitAiGenerativeUiRegistry _registry { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the generative UI view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple generative UI view
    /// components through the <see cref="BitParams"/> component - <see cref="ShowBindingErrors"/> above all, which is
    /// one place to turn the diagnostics on while a prompt is being developed.
    /// </remarks>
    [CascadingParameter(Name = BitAiGenerativeUiViewParams.ParamName)]
    public BitAiGenerativeUiViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the generative UI view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiGenerativeUiViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The generative UI part to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiGenerativeUiPart Part { get; set; } = default!;

    /// <summary>
    /// Lists, under the component, the properties that failed to bind and the ones ignored because the component does
    /// not declare them, once the part has finished streaming. Useful while developing the prompt that produces them,
    /// noise in production.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool ShowBindingErrors { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the generative UI view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiGenerativeUiViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiguv";

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

        _component = _registry.Find(Part.ComponentName);

        // The props are bound again only when there is something new to bind: another part, another registration, or
        // a change of this part since the last binding - a re-render of the message around it changes none of them.
        if (_component is not null
            && (ReferenceEquals(_boundPart, Part) is false || ReferenceEquals(_boundComponent, _component) is false || _boundVersion != Part.Version))
        {
            _boundPart = Part;
            _boundComponent = _component;
            _boundVersion = Part.Version;
            _binding = BitAiGenerativePropsBinder.Bind(_component, Part.Props);
        }

        base.OnParametersSet();
    }



    private bool _ShowsBindingErrors => ShowBindingErrors
                                     && _binding is { } binding
                                     && Part.IsStreaming is false
                                     && (binding.Failures.Count > 0 || binding.IgnoredProperties.Count > 0);

    private string FailureText(BitAiGenerativeBindError failure) => failure.Kind switch
    {
        BitAiGenerativeBindErrorKind.Missing => Strings.MissingProperty(failure.PropertyName, failure.ComponentName ?? Part.ComponentName),
        _ => Strings.UnreadableProperty(failure.PropertyName, TypeName(failure.ExpectedType)),
    };

    // The name a person reads a type by: an int? is an Int32, a List<T> a List rather than a List`1.
    private static string TypeName(Type? type)
    {
        if (type is null) return string.Empty;

        type = Nullable.GetUnderlyingType(type) ?? type;

        var name = type.Name;
        var arity = name.IndexOf('`');

        return arity < 0 ? name : name[..arity];
    }

    // The registered type is the only component the payload can open, and only with the parameters the binder made of
    // it. Every parameter is written at the one sequence number: they are the attributes of the one component frame,
    // which the diff matches by name, the way the attributes Razor writes in a loop are.
    // The frame is keyed by the part it was bound from, so another part gets an instance of its own: handed to the one
    // the previous part bound, a part still streaming would pass only the parameters it has so far, and the instance
    // would go on showing the previous part's values of all the others.
    private void RenderComponent(RenderTreeBuilder builder)
    {
        if (_component is null || _binding is null) return;

        builder.OpenComponent(0, _component.ComponentType);
        builder.SetKey(_boundPart);

        foreach (var (name, value) in _binding.Parameters)
        {
            builder.AddComponentParameter(1, name, value);
        }

        builder.CloseComponent();
    }
}
