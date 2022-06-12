using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI;

public partial class BitCompoundButton
{
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;

    private int? tabIndex;

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
    /// The text of compound button
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Description of the action compound button takes
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// URL the link points to, if provided, button renders as an anchor
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// If Href provided, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the button
    /// </summary>
    [Parameter] public string? Title { get; set; }

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
    [Parameter] public BitButtonType ButtonType { get; set; } = BitButtonType.Button;

    /// <summary>
    /// Callback for when the compound button clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    protected override string RootElementClass => "bit-cmp-btn";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
                                    ? ButtonStyle == BitButtonStyle.Primary
                                        ? $"{RootElementClass}-primary-disabled-{VisualClassRegistrar()}"
                                        : $"{RootElementClass}-standard-disabled-{VisualClassRegistrar()}"
                                    : ButtonStyle == BitButtonStyle.Primary
                                        ? $"{RootElementClass}-primary-{VisualClassRegistrar()}"
                                        : $"{RootElementClass}-standard-{VisualClassRegistrar()}");
    }

    protected override async Task OnInitializedAsync()
    {
        if (!IsEnabled)
        {
            tabIndex = AllowDisabledFocus ? null : -1;
        }

        await base.OnInitializedAsync().ConfigureAwait(true);
    }

    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e).ConfigureAwait(true);
        }
    }
}
