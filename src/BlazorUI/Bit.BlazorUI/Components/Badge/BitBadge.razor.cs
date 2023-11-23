﻿using System.Text;

namespace Bit.BlazorUI;

public partial class BitBadge
{
    private BitBadgeColor? color;
    private BitBadgeStyle badgeStyle = BitBadgeStyle.Primary;
    private BitBadgePosition badgePosition = BitBadgePosition.TopRight;
    
    private string? _content;


    /// <summary>
    /// The style of badge, Possible values: Primary | Standard | Text
    /// </summary>
    [Parameter]
    public BitBadgeStyle BadgeStyle
    {
        get => badgeStyle;
        set
        {
            if (badgeStyle == value) return;

            badgeStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Applies a border around the badge.
    /// </summary>
    [Parameter] public bool Bordered { get; set; }

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
    public BitBadgeColor? Color
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
    [Parameter] public string? Icon { get; set; }

    /// <summary>
    /// Max value to show when content is integer type.
    /// </summary>
    [Parameter] public int Max { get; set; } = 99;

    /// <summary>
    /// Button click event if set.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Overlaps the childcontent on top of the content.
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
        className.Append(Bordered ? " bit-bdg-brd" : string.Empty);

        if (Icon.HasValue() && Dot is false)
        {
            className.Append(" bit-bdg-icn");
        }

        className.Append(' ').Append(BadgeStyle switch
        {
            BitBadgeStyle.Primary => "bit-bdg-pri",
            BitBadgeStyle.Standard => "bit-bdg-std",
            BitBadgeStyle.Text => "bit-bdg-txt",
            _ => "bit-bdg-pri"
        });

        className.Append(' ').Append(Color switch
        {
            BitBadgeColor.Info => "bit-bdg-inf",
            BitBadgeColor.Success => "bit-bdg-suc",
            BitBadgeColor.Warning => "bit-bdg-wrn",
            BitBadgeColor.SevereWarning => "bit-bdg-swr",
            BitBadgeColor.Error => "bit-bdg-err",
            _ => string.Empty
        });

        className.Append(' ').Append(GetBadgePositionClasses());

        return className.ToString();
    }
}
