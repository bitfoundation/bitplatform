﻿using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitIconButton
{
    private BitButtonSize buttonSize = BitButtonSize.Medium;
    private int? _tabIndex;

    /// <summary>
    /// Whether the icon button can have focus in disabled mode
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the icon button for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The size of button, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitButtonSize ButtonSize
    {
        get => buttonSize;
        set
        {
            if (buttonSize == value) return;

            buttonSize = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The type of the button
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for different parts of the BitIconButton component.
    /// </summary>
    [Parameter] public BitIconButtonClassStyles? ClassStyles { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// The icon name for the icon shown in the button
    /// </summary>
    [Parameter] public BitIconName IconName { get; set; }

    /// <summary>
    /// Callback for when the button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the icon button
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    protected override string RootElementClass => "bit-icb";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => ButtonSize switch
        {
            BitButtonSize.Small => $"{RootElementClass}-sm",
            BitButtonSize.Large => $"{RootElementClass}-lg",
            _ => $"{RootElementClass}-md"
        });
    }

    protected override async Task OnInitializedAsync()
    {
        if (IsEnabled is false)
        {
            _tabIndex = AllowDisabledFocus ? null : -1;
        }
        
        ButtonType ??= EditContext is null ? BitButtonType.Button : BitButtonType.Submit;

        await base.OnInitializedAsync();
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
