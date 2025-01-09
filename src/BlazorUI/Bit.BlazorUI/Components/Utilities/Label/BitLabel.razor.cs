namespace Bit.BlazorUI;

/// <summary>
/// Labels give a name or title to a control or group of controls, including text fields, check boxes, combo boxes, radio buttons, and drop-down menus.
/// </summary>
public partial class BitLabel : BitComponentBase
{
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
    [Parameter, ResetClassBuilder]
    public bool Required { get; set; }



    protected override string RootElementClass => "bit-lbl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Required ? "bit-lbl-req" : string.Empty);
    }
}
