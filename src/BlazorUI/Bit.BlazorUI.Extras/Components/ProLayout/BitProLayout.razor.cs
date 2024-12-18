namespace Bit.BlazorUI;

public partial class BitProLayout : BitComponentBase
{
    /// <summary>
    /// The content of the layout.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the layout.
    /// </summary>
    [Parameter] public BitProLayoutClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the layout.
    /// </summary>
    [Parameter] public BitProLayoutClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-ply";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }
}
