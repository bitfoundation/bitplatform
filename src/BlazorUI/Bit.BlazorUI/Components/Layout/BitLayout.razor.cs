namespace Bit.BlazorUI;

public partial class BitLayout
{
    /// <summary>
    /// The content of label, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The content of label, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? Main { get; set; }

    /// <summary>
    /// The content of label, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }


    protected override string RootElementClass => "bit-lyt";

    protected override void RegisterCssClasses()
    {
        
    }
}
