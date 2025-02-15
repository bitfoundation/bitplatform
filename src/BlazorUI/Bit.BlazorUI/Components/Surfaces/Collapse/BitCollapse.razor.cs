namespace Bit.BlazorUI;

/// <summary>
/// The Collapse component allows the user to show and hide sections of related content on a page.
/// </summary>
public partial class BitCollapse : BitComponentBase
{
    /// <summary>
    /// Custom CSS classes for different parts of the collapse.
    /// </summary>
    [Parameter] public BitCollapseClassStyles? Classes { get; set; }

    /// <summary>
    /// The content of the collapse.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Determines whether the BitCollapse is expanded or collapsed.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Expanded { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the collapse.
    /// </summary>
    [Parameter] public BitCollapseClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-col";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Expanded ? Classes?.Expanded : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Expanded ? Styles?.Expanded : string.Empty);
    }
}
