using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// A lightweight and special type of button/link with icon-first styling, sized presets, and colorized text/icon support.
/// </summary>
public partial class BitActionButton : BitComponentBase
{
    private string? _rel;
    private string? _tabIndex;
    private BitButtonType _buttonType;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>.
    /// The value is coming from the cascading value provided by the EditForm.
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }



    /// <summary>
    /// Keeps the disabled action button focusable by not forcing a negative tabindex when <see cref="BitComponentBase.IsEnabled"/> is false.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; }

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an <c>aria-hidden</c> attribute instructing screen readers to ignore the button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The type of the button element; defaults to <c>submit</c> inside an <see cref="EditForm"/> otherwise <c>button</c>.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The custom body of the action button (text and/or any render fragment).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the root, icon, and content sections of the action button.
    /// </summary>
    [Parameter] public BitActionButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button that applies to the icon and text of the action button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Renders the action button in full width of its container with flex-start.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The value of the href attribute of the link rendered by the button.
    /// If provided, the component will be rendered as an anchor tag instead of button.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

    /// <summary>
    /// The Fluent UI icon name to render inside the action button (e.g., <c>BitIconName.AddFriend</c>).
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery: <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Removes the text container and renders only the icon.
    /// </summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>
    /// Raised when the action button is clicked (only when <see cref="BitComponentBase.IsEnabled"/> is true); receives <see cref="MouseEventArgs"/>.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Custom inline styles for the root, icon, and content sections of the action button.
    /// </summary>
    [Parameter] public BitActionButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Swaps the order of the icon and content so the icon appears after the text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReversedIcon { get; set; }

    /// <summary>
    /// Sets the <c>rel</c> attribute for link-rendered buttons when <see cref="Href"/> is a non-anchor URL; ignored for empty or hash-only hrefs.
    /// The <c>rel</c> attribute specifies the relationship between the current document and the linked document.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRel? Rel { get; set; }

    /// <summary>
    /// Sets the preset size for typography and padding of the action button.
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
            BitColor.PrimaryBackground => "bit-acb-pbg",
            BitColor.SecondaryBackground => "bit-acb-sbg",
            BitColor.TertiaryBackground => "bit-acb-tbg",
            BitColor.PrimaryForeground => "bit-acb-pfg",
            BitColor.SecondaryForeground => "bit-acb-sfg",
            BitColor.TertiaryForeground => "bit-acb-tfg",
            BitColor.PrimaryBorder => "bit-acb-pbr",
            BitColor.SecondaryBorder => "bit-acb-sbr",
            BitColor.TertiaryBorder => "bit-acb-tbr",
            _ => "bit-acb-pri"
        });

        ClassBuilder.Register(() => FullWidth ? "bit-acb-fwi" : string.Empty);

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
            _tabIndex = AllowDisabledFocus ? null : "-1";
        }
        else
        {
            _tabIndex = TabIndex ?? _tabIndex;
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



    private void OnSetHrefAndRel()
    {
        if (Rel.HasValue is false || Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        _rel = BitLinkRelUtils.GetRels(Rel.Value);
    }
}
