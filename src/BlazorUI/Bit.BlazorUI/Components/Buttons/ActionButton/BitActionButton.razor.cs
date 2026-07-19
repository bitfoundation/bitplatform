using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// A lightweight and special type of button/link with icon-first styling, sized presets, and colorized text/icon support.
/// </summary>
public partial class BitActionButton : BitComponentBase
{
    private string? _rel;
    private string? _tabIndex;
    private bool _showLoading;
    private BitButtonType _buttonType;
    private CancellationTokenSource? _loadingDelayCts;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>.
    /// The value is coming from the cascading value provided by the EditForm.
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// Gets or sets the cascading parameters for the action button component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple action button components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitActionButtonParams.ParamName)]
    public BitActionButtonParams? CascadingParameters { get; set; }



    /// <summary>
    /// Keeps the disabled action button focusable and discoverable by assistive technologies.
    /// When enabled, the disabled state is conveyed using the <c>aria-disabled</c> attribute instead of the
    /// native <c>disabled</c> attribute, so the button remains in the tab order while its action is suppressed.
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
    /// If true, enters the loading state automatically while awaiting the OnClick event and prevents subsequent clicks by default.
    /// </summary>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// The type of the button element; defaults to <c>submit</c> inside an <see cref="EditForm"/> otherwise <c>button</c>.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// Alias for <see cref="ChildContent"/>, the custom body of the action button (text and/or any render fragment).
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

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
    /// The value of the download attribute of the link rendered by the button when the Href parameter is provided.
    /// Instructs the browser to download the linked resource instead of navigating to it, using the provided value
    /// (if any) as the suggested file name (only works for same-origin, blob: and data: URLs).
    /// </summary>
    [Parameter] public string? Download { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component should expand to occupy the full available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The value of the href attribute of the link rendered by the button.
    /// If provided, the component will be rendered as an anchor tag instead of button.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.AddFriend</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// The value is case-sensitive and must match a valid icon identifier.
    /// If not set or set to <c>null</c>, no icon will be rendered.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the icon is displayed, without accompanying text.
    /// </summary>
    /// <remarks>
    /// Set this property to <see langword="true"/> to render the component with only its icon visible.
    /// When <see langword="false"/>, both icon and text are shown if available.
    /// </remarks>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the component's content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Determines whether the action button is in loading mode or not.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsLoading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the loading indicator appears after entering the loading state.
    /// Useful to avoid a spinner flash for fast operations. The click-guard of the loading state applies immediately regardless of this delay.
    /// </summary>
    [Parameter] public int LoadingDelay { get; set; }

    /// <summary>
    /// The text to show next to the spinner while the action button is in the loading state, replacing the button body.
    /// It is also announced by screen readers through a status live region when the loading state starts.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The custom template used to replace the default loading indicator inside the action button in the loading state.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when the component is clicked.
    /// </summary>
    /// <remarks>
    /// The callback receives a <see cref="MouseEventArgs"/> instance containing details about the mouse event.
    /// Assign this property to handle click interactions, such as responding to user input or triggering actions.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Enables re-clicking the action button while it is in the loading state.
    /// By default, clicks are ignored while the button is loading to protect against double submissions.
    /// </summary>
    [Parameter] public bool Reclickable { get; set; }

    /// <summary>
    /// Gets or sets the relationship type between the current element and the linked resource, as defined by the link's rel attribute.
    /// </summary>
    /// <remarks>
    /// Sets the <c>rel</c> attribute for link-rendered buttons when <see cref="Href"/> is a non-anchor URL; ignored for empty or hash-only hrefs.
    /// The <c>rel</c> attribute specifies the relationship between the current document and the linked document.
    /// <br />
    /// Set this property to specify how the linked resource is related to the current context.
    /// Common values include "stylesheet", "noopener", or "nofollow". The value determines how browsers and search
    /// engines interpret the link.
    /// <br />
    /// When <see cref="Target"/> is set to <c>_blank</c> and no opener-related rel is provided, <c>noopener</c> is added automatically.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Sets the preset size (Small, Medium, Large) for typography and padding of the action button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the propagation of the click event to the parent elements.
    /// Useful when the action button is placed inside clickable containers like rows or cards.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS inline styles to apply to the action button component.
    /// </summary>
    /// <remarks>
    /// Use this property to override the default styles of the action button.
    /// If not set, the component uses its built-in styling.
    /// This property is typically used to provide additional visual customization.
    /// </remarks>
    [Parameter] public BitActionButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the name of the target frame or window for the navigation action when the action button renders as an anchor (by providing the Href parameter).
    /// </summary>
    /// <remarks>
    /// Specify a value to control where the linked content will be displayed. Common values include
    /// "_blank" to open in a new window or tab, "_self" for the same frame, "_parent" for the parent frame, and "_top"
    /// for the full body of the window. If not set, the default browser behavior is used.
    /// <br />
    /// When set to <c>_blank</c> and no opener-related <see cref="Rel"/> is provided, <c>noopener</c> is added to the rel attribute automatically.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Adds an underline to the action button text, useful for link-style buttons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



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

        ClassBuilder.Register(() => IsLoading ? "bit-acb-lod" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-acb-sm",
            BitSize.Medium => "bit-acb-md",
            BitSize.Large => "bit-acb-lg",
            _ => "bit-acb-md"
        });

        ClassBuilder.Register(() => Underlined ? "bit-acb-und" : string.Empty);

        ClassBuilder.Register(() => IconPosition is BitIconPosition.End ? "bit-acb-eni" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitActionButtonParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _tabIndex = IsEnabled
            ? TabIndex
            : AllowDisabledFocus ? TabIndex : "-1";

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        UpdateLoadingVisuals();

        base.OnParametersSet();
    }



    protected virtual async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsLoading && Reclickable is false) return;

        if (AutoLoading)
        {
            if (await AssignIsLoading(true) is false) return;

            UpdateLoadingVisuals();
        }

        try
        {
            await OnClick.InvokeAsync(e);
        }
        finally
        {
            if (AutoLoading)
            {
                await AssignIsLoading(false);

                UpdateLoadingVisuals();
            }
        }
    }



    internal void OnSetHrefRelAndTarget()
    {
        if (Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        var rel = Rel.HasValue ? BitLinkRelUtils.GetRels(Rel.Value) : null;

        if (Target is "_blank" && (rel is null || (rel.Contains("noopener") is false && rel.Contains("noreferrer") is false)))
        {
            rel = rel.HasValue() ? $"{rel} noopener" : "noopener";
        }

        _rel = rel;
    }



    private void UpdateLoadingVisuals()
    {
        if (IsLoading)
        {
            if (_showLoading || _loadingDelayCts is not null) return;

            if (LoadingDelay < 1)
            {
                _showLoading = true;
                return;
            }

            _loadingDelayCts = new();
            _ = ShowLoadingAfterDelay(_loadingDelayCts.Token);
        }
        else
        {
            _showLoading = false;
            CancelLoadingDelay();
        }
    }

    private async Task ShowLoadingAfterDelay(CancellationToken token)
    {
        try
        {
            await Task.Delay(LoadingDelay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (IsDisposed || IsLoading is false || token.IsCancellationRequested) return;

        _showLoading = true;

        await InvokeAsync(StateHasChanged);
    }

    private void CancelLoadingDelay()
    {
        if (_loadingDelayCts is null) return;

        _loadingDelayCts.Cancel();
        _loadingDelayCts.Dispose();
        _loadingDelayCts = null;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        CancelLoadingDelay();

        await base.DisposeAsync(disposing);
    }
}
