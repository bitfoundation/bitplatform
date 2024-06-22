namespace Bit.BlazorUI;

public partial class BitLabel
{
    private bool required;

    /// <summary>
    /// The content of label, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// This attribute specifies which form element a label is bound to
    /// </summary>
    [Parameter] public string? For { get; set; }

    /// <summary>
    /// Whether the associated field is required or not, it shows a star above of it
    /// </summary>
    [Parameter]
    public bool Required
    {
        get => required;
        set
        {
            if (required == value) return;

            required = value;
            ClassBuilder.Reset();
        }
    }


    protected override string RootElementClass => "bit-lbl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Required ? "bit-lbl-req" : string.Empty);
    }
}
