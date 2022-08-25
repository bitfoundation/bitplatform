using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI;

public partial class BitButton
{
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;
    private int? _tabIndex;

    /// <summary>
    /// Whether the button can have focus in disabled mode
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The style of button, Possible values: Primary | Standard
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
    /// The content of button, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Callback for when the button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button
    /// </summary>
    [Parameter] public string? Title { get; set; }

    protected override string RootElementClass => "bit-btn";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                       ? string.Empty
                                       : ButtonStyle == BitButtonStyle.Primary
                                           ? "primary"
                                           : "standard");
    }

    protected override async Task OnInitializedAsync()
    {
        if (!IsEnabled)
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
