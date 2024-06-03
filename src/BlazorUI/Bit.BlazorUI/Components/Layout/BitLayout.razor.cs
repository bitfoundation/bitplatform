namespace Bit.BlazorUI;

public partial class BitLayout
{
    private bool isRequired;

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


    /// <summary>
    /// Whether the associated field is required or not, it shows a star above of it
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }


    protected override string RootElementClass => "bit-lbl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsRequired ? $"{RootElementClass}-req" : string.Empty);
    }
}
