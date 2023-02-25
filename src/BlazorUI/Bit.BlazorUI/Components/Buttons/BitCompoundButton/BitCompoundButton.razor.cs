using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitCompoundButton
{
    protected override bool UseVisual => false;
    private BitButtonSize buttonSize = BitButtonSize.Medium;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private int? _tabIndex;

    /// <summary>
    /// Whether the compound button can have focus in disabled mode
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the compound button for the benefit of screen readers
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
    /// The style of compound button, Possible values: Primary | Standard
    /// </summary>
    [Parameter]
    public BitButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The type of the button
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Callback for when the compound button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Description of the action compound button takes
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// The text of compound button
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button
    /// </summary>
    [Parameter] public string? Title { get; set; }

    protected override string RootElementClass => "bit-cmpb";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => ButtonStyle == BitButtonStyle.Primary
                                        ? "primary"
                                        : "standard");

        ClassBuilder.Register(() =>
         ButtonSize switch
         {
             BitButtonSize.Small => "small",
             BitButtonSize.Medium => "medium",
             BitButtonSize.Large => "large",
             _ => "small"
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
