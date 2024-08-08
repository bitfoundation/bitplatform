﻿using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitActionButton : BitComponentBase
{
    private int? _tabIndex;
    private BitButtonType _buttonType;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }



    /// <summary>
    /// Whether the button can have focus in disabled mode.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; }

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The type html attribute of the button element.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of the button.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the button.
    /// </summary>
    [Parameter] public BitActionButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The value of the href attribute of the link rendered by the button.
    /// If provided, the component will be rendered as an anchor tag instead of button.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// The icon name of the icon to render inside the button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The callback for the click event of the button.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the button.
    /// </summary>
    [Parameter] public BitActionButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the content of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReversedIcon { get; set; }

    /// <summary>
    /// The size of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Specifies target attribute of the link when the button renders as an anchor (by providing the Href parameter).
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-acb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-acb-pri",
            BitColor.Secondary => "bit-acb-sec",
            BitColor.Tertiary => "bit-acb-ter",
            BitColor.Info => "bit-acb-inf",
            BitColor.Success => "bit-acb-suc",
            BitColor.Warning => "bit-acb-wrn",
            BitColor.SevereWarning => "bit-acb-swr",
            BitColor.Error => "bit-acb-err",
            _ => "bit-acb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-acb-sm",
            BitSize.Medium => "bit-acb-md",
            BitSize.Large => "bit-acb-lg",
            _ => "bit-acb-md"
        });

        ClassBuilder.Register(() => ReversedIcon ? "bit-acb-rvi" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        if (IsEnabled is false)
        {
            _tabIndex = AllowDisabledFocus ? null : -1;
        }

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        base.OnParametersSet();
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
