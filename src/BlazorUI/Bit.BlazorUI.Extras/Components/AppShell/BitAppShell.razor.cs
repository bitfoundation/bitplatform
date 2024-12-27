namespace Bit.BlazorUI;

public partial class BitAppShell : BitComponentBase
{
    /// <summary>
    /// The cascading values to be provided for the children of the layout.
    /// </summary>
    [Parameter] public IEnumerable<BitCascadingValue>? CascadingValues { get; set; }

    /// <summary>
    /// The content of the layout.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the layout.
    /// </summary>
    [Parameter] public BitAppShellClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the layout.
    /// </summary>
    [Parameter] public BitAppShellClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-ash";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }
}
