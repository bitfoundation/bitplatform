using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitActionButton
{
    private int? _tabIndex;
    private BitButtonType _buttonType;


    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }


    /// <summary>
    /// Whether the action button can have focus in disabled mode
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the action button for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The type of the button
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of action button, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitActionButton.
    /// </summary>
    [Parameter] public BitActionButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// The icon name for the icon shown in the action button
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Callback for when the button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitActionButton.
    /// </summary>
    [Parameter] public BitActionButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button
    /// </summary>
    [Parameter] public string? Title { get; set; }


    protected override string RootElementClass => "bit-acb";

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
