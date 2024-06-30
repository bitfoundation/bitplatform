using System.Text;

namespace Bit.BlazorUI;

public partial class BitBadge : BitComponentBase
{
    private BitSize? size;
    private BitColor? color;
    private BitAppearance appearance = BitAppearance.Primary;
    private BitBadgePosition badgePosition = BitBadgePosition.TopRight;

    private string? _content;



    /// <summary>
    /// The appearance of badge, Possible values: Primary | Standard | Text
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
    /// Child content of component, the content that the badge will apply to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitBadge.
    /// </summary>
    [Parameter] public BitBadgeClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the badge.
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
    /// Content you want inside the badge. Supported types are string and integer.
    /// </summary>
    [Parameter] public object? Content { get; set; }

    /// <summary>
    /// Reduces the size of the badge and hide any of its content.
    /// </summary>
    [Parameter] public bool Dot { get; set; }

    /// <summary>
    /// The visibility of the badge.
    /// </summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>
    /// Sets the Icon to use in the badge.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Max value to display when content is integer type.
    /// </summary>
    [Parameter] public int Max { get; set; } = 99;

    /// <summary>
    /// Button click event if set.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Overlaps the badge on top of the child content.
    /// </summary>
    [Parameter] public bool Overlap { get; set; }

    /// <summary>
    /// The position of the badge.
    /// </summary>
    [Parameter]
    public BitBadgePosition Position
    {
        get => badgePosition;
        set
        {
            if (badgePosition == value) return;

            badgePosition = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The size of badge, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitSize? Size
    {
        get => size;
        set
        {
            if (size == value) return;

            size = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom CSS styles for different parts of the BitBadge.
    /// </summary>
    [Parameter] public BitBadgeClassStyles? Styles { get; set; }


    protected override string RootElementClass => "bit-bdg";

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
        if (Content is string stringContent)
        {
            _content = stringContent;
        }
        else if (Content is int numberContent)
        {
            if (numberContent > Max)
            {
                _content = Max + "+";
            }
            else
            {
                _content = numberContent.ToString();
            }
        }
        else
        {
            _content = null;
        }
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }


    private string GetBadgePositionClasses()
    {
        return Position switch
        {
            BitBadgePosition.TopRight => "bit-bdg-trg",
            BitBadgePosition.TopCenter => "bit-bdg-tcr",
            BitBadgePosition.TopLeft => "bit-bdg-tlf",
            BitBadgePosition.CenterLeft => "bit-bdg-clf",
            BitBadgePosition.BottomLeft => "bit-bdg-blf",
            BitBadgePosition.BottomCenter => "bit-bdg-bcr",
            BitBadgePosition.BottomRight => "bit-bdg-brg",
            BitBadgePosition.CenterRight => "bit-bdg-crg",
            BitBadgePosition.Center => "bit-bdg-ctr",
            _ => "bit-bdg-trg"
        };
    }

    private string GetBadgeClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append(Dot ? "bit-bdg-dot" : string.Empty);

        className.Append(Overlap ? " bit-bdg-orp" : string.Empty);

        if (IconName.HasValue() && Dot is false)
        {
            className.Append(" bit-bdg-icn");
        }

        className.Append(' ').Append(Appearance switch
        {
            BitAppearance.Primary => "bit-bdg-pri",
            BitAppearance.Standard => "bit-bdg-std",
            BitAppearance.Text => "bit-bdg-txt",
            _ => "bit-bdg-pri"
        });

        className.Append(' ').Append(Color switch
        {
            BitColor.Info => "bit-bdg-inf",
            BitColor.Success => "bit-bdg-suc",
            BitColor.Warning => "bit-bdg-wrn",
            BitColor.SevereWarning => "bit-bdg-swr",
            BitColor.Error => "bit-bdg-err",
            _ => string.Empty
        });

        className.Append(' ').Append(Size switch
        {
            BitSize.Small => "bit-bdg-sm",
            BitSize.Medium => "bit-bdg-md",
            BitSize.Large => "bit-bdg-lg",
            _ => string.Empty
        });

        className.Append(' ').Append(GetBadgePositionClasses());

        return className.ToString();
    }
}
