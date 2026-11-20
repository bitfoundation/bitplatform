using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The BitContainer component centers the content of a page in a column with a maximum width and side gutters.
/// </summary>
/// <remarks>
/// It is the "centered content column" every site writes by hand: it takes the whole width it is given up to
/// <see cref="MaxWidth"/>, centers itself in the room that is left, and keeps its content off the edges of the screen
/// with an inline padding (the gutter) that grows with the width of the window - narrow on a phone, wider on a tablet
/// and a desktop - unless <see cref="NoGutter"/> removes it.
/// <br />
/// The maximum width includes the gutters (the root is a border-box), so two containers of the same maximum width
/// line their content up whatever they are nested in. A full-width band of the page (a colored hero, a footer) is
/// usually an element of its own with a container inside it, which keeps the band edge to edge while its content
/// lines up with the rest of the page - the same thing the <c>MaxWidth</c> of <see cref="BitHeader"/> and
/// <see cref="BitFooter"/> does for their own content.
/// </remarks>
public partial class BitContainer : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the container component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple container components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitContainerParams.ParamName)]
    public BitContainerParams? CascadingParameters { get; set; }



    /// <summary>
    /// Gets or sets the content to be rendered inside the BitContainer.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// A container that holds the main content of the page is more meaningful as a <c>main</c>, one that holds a region of
    /// it as a <c>section</c> or an <c>article</c>. The layout itself is unaffected: only the tag name changes, and with it
    /// the semantics reported to assistive technologies - so a landmark element given here is worth pairing with an
    /// <see cref="BitComponentBase.AriaLabel"/> that names it.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the maximum width of the BitContainer, in which its content is then centered.
    /// </summary>
    /// <remarks>
    /// Takes any CSS length (for example <c>1200px</c> or <c>75rem</c>), or <c>none</c> for a container that spans the
    /// whole width it is given and only keeps its gutters. The width includes the gutters.
    /// <br />
    /// An explicit value wins over the <c>--bit-Container-max-width</c> variable; left unset, the container is 80rem
    /// (1280px, the large breakpoint) wide at most unless that variable says otherwise.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Removes the inline paddings (the gutters) of the BitContainer, so its content can span its full width.
    /// </summary>
    /// <remarks>
    /// It wins over the <c>--bit-Container-padding-inline</c> variable. The block padding
    /// (<c>--bit-Container-padding-block</c>) is not a gutter, so it is left alone.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoGutter { get; set; }



    protected override string RootElementClass => "bit-cnt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => NoGutter ? "bit-cnt-ngt" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => MaxWidth.HasValue() ? $"--bit-cnt-max-width:{MaxWidth}" : string.Empty);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? "div");
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        // A null value here is not the same as nothing at all: the builder still records the name and drops the
        // attribute of the same name that came out of HtmlAttributes, so an unset parameter is only added when the
        // parameter itself carries a value and the splatted one is left alone otherwise.
        if (AriaLabel is not null)
        {
            builder.AddAttribute(3, "aria-label", AriaLabel);
        }
        if (TabIndex is not null)
        {
            builder.AddAttribute(4, "tabindex", TabIndex);
        }
        builder.AddAttribute(5, "style", StyleBuilder.Value);
        builder.AddAttribute(6, "class", ClassBuilder.Value);
        if (Dir is not null)
        {
            builder.AddAttribute(7, "dir", Dir.Value.ToString().ToLowerInvariant());
        }
        builder.AddElementReferenceCapture(8, v => RootElement = v);
        builder.AddContent(9, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
