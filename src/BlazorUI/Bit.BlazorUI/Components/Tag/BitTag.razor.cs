namespace Bit.BlazorUI;

public partial class BitTag
{
    private BitColor? color;
    private BitAppearance appearance = BitAppearance.Primary;



    /// <summary>
    /// The appearance of tag, Possible values: Primary | Standard | Text
    /// </summary>
    [Parameter]
    public BitAppearance Appearance
    {
        get => appearance;
        set
        {
            if (appearance == value) return;

            appearance = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Child content of component, the content that the tag will apply to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the tag.
    /// </summary>
    [Parameter]
    public BitColor? Color
    {
        get => color;
        set
        {
            if (color == value) return;

            color = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The icon to show inside the tag.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Click event handler of the tag.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Dismiss button click event, if set the dismiss icon will show up.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the tag.
    /// </summary>
    [Parameter] public string? Text { get; set; }


    protected override string RootElementClass => "bit-tag";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Appearance switch
        {
            BitAppearance.Primary => "bit-tag-pri",
            BitAppearance.Standard => "bit-tag-std",
            BitAppearance.Text => "bit-tag-txt",
            _ => "bit-tag-pri"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Info => "bit-tag-inf",
            BitColor.Success => "bit-tag-suc",
            BitColor.Warning => "bit-tag-wrn",
            BitColor.SevereWarning => "bit-tag-swr",
            BitColor.Error => "bit-tag-err",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    private async Task HandleOnDismissClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnDismiss.InvokeAsync(e);
        }
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
