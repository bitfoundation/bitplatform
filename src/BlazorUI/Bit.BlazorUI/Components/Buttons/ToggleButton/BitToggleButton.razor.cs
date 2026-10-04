using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// ToggleButton is a type of button that stores and shows a status representing the toggle state of the component.
/// It supports distinct content, icons and appearance per state, a loading state, cancellable changes,
/// and the ARIA semantics of the toggle button pattern.
/// </summary>
public partial class BitToggleButton : BitComponentBase
{
    /// <summary>
    /// The roles a toggle button can be given by hand whose state is read from <c>aria-checked</c> rather than
    /// from <c>aria-pressed</c>. Naming one of them describes the pattern; keeping the state in step with it is
    /// then the component's job, not one more attribute for the page to write and forget to update.
    /// </summary>
    private static readonly string[] _ariaCheckedRoles =
        ["checkbox", "menuitemcheckbox", "menuitemradio", "option", "radio", "switch", "treeitem"];



    private bool _showLoading;
    private int _pendingChanges;
    private CancellationTokenSource? _loadingDelayCts;



    /// <summary>
    /// Gets or sets the cascading parameters for the toggle button component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple toggle button components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitToggleButtonParams.ParamName)]
    public BitToggleButtonParams? CascadingParameters { get; set; }



    /// <summary>
    /// Keeps the disabled toggle button focusable and discoverable by screen readers, rendering <c>aria-disabled</c> instead of the
    /// native <c>disabled</c> attribute when <see cref="BitComponentBase.IsEnabled"/> is false, preserving a consistent tab order.
    /// Set it to false to render the native <c>disabled</c> attribute and remove the toggle button from the tab order.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// The id of the element that the toggle button controls (rendered into <c>aria-controls</c>).
    /// </summary>
    /// <remarks>
    /// Where what it controls is a part of the page the checked state shows or hides, pair it with
    /// <see cref="BitToggleButtonAriaMode.Expanded"/>, which announces the toggle button as collapsed or
    /// expanded rather than as pressed.
    /// </remarks>
    [Parameter] public string? AriaControls { get; set; }

    /// <summary>
    /// Detailed description of the toggle button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    /// <remarks>
    /// It is rendered as visually hidden text beside the toggle button and read after its name, not as part of it.
    /// An <c>aria-describedby</c> written on the component by hand is kept and this description is added to it,
    /// since the attribute is a list of ids.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an <c>aria-hidden</c> attribute instructing screen readers to ignore the toggle button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// The id of the element that labels the toggle button (rendered into <c>aria-labelledby</c>).
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Determines which ARIA state attribute the toggle button exposes to assistive technologies.
    /// </summary>
    /// <remarks>
    /// The default <see cref="BitToggleButtonAriaMode.Auto"/> drops <c>aria-pressed</c> when the accessible name
    /// of the toggle button changes between the two states, since a changing name already conveys the state and
    /// announcing both makes the toggle button ambiguous.
    /// <br />
    /// <c>aria-pressed</c> is a state of the button role alone, so a <c>role</c> written on the component by hand -
    /// a <c>menuitemcheckbox</c> in a menu, an <c>option</c> in a listbox - takes it with it whatever this mode says.
    /// Where that role reads <c>aria-checked</c> instead, the toggle button writes the checked state there by itself.
    /// </remarks>
    [Parameter] public BitToggleButtonAriaMode? AriaMode { get; set; }

    /// <summary>
    /// If true, the toggle button automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// If true, enters the loading state automatically for as long as the <see cref="OnClick"/>,
    /// <see cref="OnChanging"/> and <see cref="OnChange"/> callbacks take, preventing subsequent clicks by default.
    /// </summary>
    /// <remarks>
    /// The window opens before the first of them is invoked and closes once the last has returned, so an
    /// asynchronous toggle needs no loading flag of its own. <see cref="ToggleAsync"/> takes the same path.
    /// </remarks>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// Gets or sets the check mark icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckMarkIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CheckMarkIcon { get; set; }

    /// <summary>
    /// The name of the check mark icon that renders in the checked state when <see cref="ShowCheckMark"/> is enabled.
    /// </summary>
    [Parameter] public string? CheckMarkIconName { get; set; }

    /// <summary>
    /// The content of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the toggle button.
    /// </summary>
    [Parameter] public BitToggleButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the IsChecked parameter.
    /// </summary>
    [Parameter] public bool? DefaultIsChecked { get; set; }

    /// <summary>
    /// Keeps the space of the check mark reserved in the unchecked state so the content does not shift while toggling.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FixedCheckMark { get; set; }

    /// <summary>
    /// Preserves the foreground color of the toggle button through hover and press.
    /// </summary>
    /// <remarks>
    /// The color it holds is the one that reads against the <see cref="Color"/> role, which is what the
    /// <see cref="BitVariant.Outline"/> and <see cref="BitVariant.Text"/> variants swap in as soon as a fill
    /// appears behind the content. With a background color those two otherwise draw the content in roughly the
    /// color of the page at rest. A checked toggle button holds its checked foreground instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FixedColor { get; set; }

    /// <summary>
    /// Expands the toggle button width to 100% of the available width.
    /// </summary>
    /// <remarks>
    /// The minimum width of the size class goes with it: a toggle button measured by its container should not be
    /// pushed out of one narrower than the minimum, which some design systems set as high as 96px.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon name that renders inside the toggle button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered and changes the styles accordingly.
    /// </summary>
    /// <remarks>
    /// The wording is not thrown away with the text: where no <see cref="BitComponentBase.AriaLabel"/> is given,
    /// the <see cref="Text"/> of the state becomes the accessible name of the toggle button, and a
    /// <see cref="LoadingLabel"/> is announced rather than shown beside the spinner, which would stretch the
    /// square out of shape.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the content of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Determines if the toggle button is in the checked state.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsChecked { get; set; }

    /// <summary>
    /// Determines whether the toggle button is in the loading state, which covers its content
    /// with a spinner and prevents subsequent clicks unless <see cref="Reclickable"/> is enabled.
    /// </summary>
    /// <remarks>
    /// The content stays in place behind the spinner rather than being removed, so the accessible name of the
    /// toggle button does not disappear while it is busy. <see cref="LoadingDelay"/> holds the spinner back for
    /// a fast toggle without lifting the click guard.
    /// </remarks>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsLoading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the spinner appears after the toggle button enters the loading state,
    /// which keeps a fast toggle from flashing one. The click guard of the loading state applies immediately
    /// regardless of the delay.
    /// </summary>
    [Parameter] public int LoadingDelay { get; set; }

    /// <summary>
    /// The loading label text to show next to the spinner icon.
    /// </summary>
    /// <remarks>
    /// It is also announced by a live region beside the toggle button when the loading state begins, since the
    /// spinner itself conveys nothing to a screen reader. On an <see cref="IconOnly"/> toggle button the
    /// announcement is all of it: the label is not shown, since it would stretch the square while it lasts.
    /// </remarks>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The position of the loading label in regards to the spinner icon.
    /// </summary>
    [Parameter] public BitLabelPosition LoadingLabelPosition { get; set; } = BitLabelPosition.End;

    /// <summary>
    /// The custom template used to replace the default content of the toggle button in the loading state.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Keeps the text of the toggle button on a single line and ends it with an ellipsis where it does not fit,
    /// for a toggle button whose width is decided by its container rather than by its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Callback for when the IsChecked value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Callback invoked before the checked state changes, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitToggleButtonChangeArgs"/> to keep the current state.
    /// Since the callback is awaited, it can also run asynchronous work like a confirmation prompt.
    /// </remarks>
    [Parameter] public EventCallback<BitToggleButtonChangeArgs> OnChanging { get; set; }

    /// <summary>
    /// Callback for when the toggle button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The aria-label of the toggle button when it is not checked.
    /// </summary>
    [Parameter] public string? OffAriaLabel { get; set; }

    /// <summary>
    /// The color of the toggle button when it is not checked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? OffColor { get; set; }

    /// <summary>
    /// Gets or sets the icon to display when the toggle button is not checked using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OffIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="OffIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OffIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: OffIcon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: OffIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? OffIcon { get; set; }

    /// <summary>
    /// The icon of the toggle button when it is not checked.
    /// </summary>
    [Parameter] public string? OffIconName { get; set; }

    /// <summary>
    /// The custom content of the toggle button when it is not checked.
    /// </summary>
    /// <remarks>
    /// A template that differs from the one of the other state usually changes the accessible name with it, which
    /// the automatic <see cref="AriaMode"/> cannot detect the way it detects a changing text. Set
    /// <see cref="BitComponentBase.AriaLabel"/> to pin the name down, or set <see cref="AriaMode"/> explicitly.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? OffTemplate { get; set; }

    /// <summary>
    /// The text of the toggle button when it is not checked.
    /// </summary>
    /// <remarks>
    /// Providing a different text per state changes the accessible name of the toggle button, which by default
    /// suppresses <c>aria-pressed</c>. Provide an <see cref="BitComponentBase.AriaLabel"/> to keep the name stable,
    /// or set <see cref="AriaMode"/> explicitly.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? OffText { get; set; }

    /// <summary>
    /// The title of the toggle button when it is not checked.
    /// </summary>
    /// <remarks>
    /// The title is what names a toggle button that has no text of its own - an icon-only one, typically - so a
    /// title that differs per state changes the accessible name there and suppresses <c>aria-pressed</c> just as
    /// a per-state text does. Add an <see cref="BitComponentBase.AriaLabel"/> to keep the name stable while the
    /// tooltip varies.
    /// </remarks>
    [Parameter] public string? OffTitle { get; set; }

    /// <summary>
    /// The visual variant of the toggle button when it is not checked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? OffVariant { get; set; }

    /// <summary>
    /// The aria-label of the toggle button when it is checked.
    /// </summary>
    [Parameter] public string? OnAriaLabel { get; set; }

    /// <summary>
    /// The color of the toggle button when it is checked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? OnColor { get; set; }

    /// <summary>
    /// Gets or sets the icon to display when the toggle button is checked using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OnIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="OnIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: OnIcon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: OnIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? OnIcon { get; set; }

    /// <summary>
    /// The icon of the toggle button when it is checked.
    /// </summary>
    [Parameter] public string? OnIconName { get; set; }

    /// <summary>
    /// The custom content of the toggle button when it is checked.
    /// </summary>
    /// <remarks>
    /// A template that differs from the one of the other state usually changes the accessible name with it, which
    /// the automatic <see cref="AriaMode"/> cannot detect the way it detects a changing text. Set
    /// <see cref="BitComponentBase.AriaLabel"/> to pin the name down, or set <see cref="AriaMode"/> explicitly.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? OnTemplate { get; set; }

    /// <summary>
    /// The text of the toggle button when it is checked.
    /// </summary>
    /// <remarks>
    /// Providing a different text per state changes the accessible name of the toggle button, which by default
    /// suppresses <c>aria-pressed</c>. Provide an <see cref="BitComponentBase.AriaLabel"/> to keep the name stable,
    /// or set <see cref="AriaMode"/> explicitly.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? OnText { get; set; }

    /// <summary>
    /// The title of the toggle button when it is checked.
    /// </summary>
    /// <remarks>
    /// The title is what names a toggle button that has no text of its own - an icon-only one, typically - so a
    /// title that differs per state changes the accessible name there and suppresses <c>aria-pressed</c> just as
    /// a per-state text does. Add an <see cref="BitComponentBase.AriaLabel"/> to keep the name stable while the
    /// tooltip varies.
    /// </remarks>
    [Parameter] public string? OnTitle { get; set; }

    /// <summary>
    /// The visual variant of the toggle button when it is checked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? OnVariant { get; set; }

    /// <summary>
    /// Enables re-clicking while the toggle button is in the loading state.
    /// </summary>
    /// <remarks>
    /// A loading toggle button otherwise stops responding to the pointer altogether - it keeps neither the
    /// hover shade nor the pointer cursor, since both read as actionable on a control that ignores every click.
    /// Enabling this keeps them along with the clicks.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reclickable { get; set; }

    /// <summary>
    /// Renders a check mark in the checked state so the state is not conveyed by color alone, which is also what
    /// keeps the state readable in Windows High Contrast, where the background carrying it is discarded.
    /// </summary>
    /// <remarks>
    /// The check mark is part of the default body, so a toggle button given a template renders none.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ShowCheckMark { get; set; }

    /// <summary>
    /// The size of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the toggle button.
    /// </summary>
    [Parameter] public BitToggleButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Text { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the toggle button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the toggle button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Gives focus to the root element of the toggle button.
    /// </summary>
    public ValueTask FocusAsync() => RootElement.FocusAsync();

    /// <summary>
    /// Toggles the checked state of the toggle button, going through the same
    /// cancellation and change notification path as a click does.
    /// </summary>
    public async Task ToggleAsync()
    {
        if (IsLoading && Reclickable is false) return;

        await ChangeIsChecked(IsChecked is false, autoLoading: true);

        // Unlike a click, a programmatic call has no event handler behind it to request a render - and no
        // guarantee of arriving on the renderer's thread either, since the caller may be a timer or a
        // background task, where StateHasChanged on its own throws.
        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-tgb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsChecked ? $"bit-tgb-chk {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => (GetTemplate() is null && GetText().HasNoValue()) || IconOnly ? "bit-tgb-ntx" : string.Empty);

        ClassBuilder.Register(() => FixedColor ? "bit-tgb-fxc" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-tgb-flw" : string.Empty);

        // The grid that stacks the loading visuals over the content follows what is actually shown rather than
        // the parameter, so a toggle button inside its LoadingDelay keeps laying out as a plain one.
        ClassBuilder.Register(() => _showLoading ? "bit-tgb-lda" : string.Empty);

        // The pointer affordance follows the click guard instead, which applies from the first click whether or
        // not the spinner has appeared yet: a toggle button that swallows clicks must not keep the hover shade
        // and the pointer cursor of one that takes them.
        ClassBuilder.Register(() => IsLoading && Reclickable is false ? "bit-tgb-lod" : string.Empty);

        ClassBuilder.Register(() => NoWrap ? "bit-tgb-nwr" : string.Empty);

        ClassBuilder.Register(() => IconPosition is BitIconPosition.End ? "bit-tgb-eni" : string.Empty);

        ClassBuilder.Register(() => GetColor() switch
        {
            BitColor.Primary => "bit-tgb-pri",
            BitColor.Secondary => "bit-tgb-sec",
            BitColor.Tertiary => "bit-tgb-ter",
            BitColor.Info => "bit-tgb-inf",
            BitColor.Success => "bit-tgb-suc",
            BitColor.Warning => "bit-tgb-wrn",
            BitColor.SevereWarning => "bit-tgb-swr",
            BitColor.Error => "bit-tgb-err",
            BitColor.PrimaryBackground => "bit-tgb-pbg",
            BitColor.SecondaryBackground => "bit-tgb-sbg",
            BitColor.TertiaryBackground => "bit-tgb-tbg",
            BitColor.PrimaryForeground => "bit-tgb-pfg",
            BitColor.SecondaryForeground => "bit-tgb-sfg",
            BitColor.TertiaryForeground => "bit-tgb-tfg",
            BitColor.PrimaryBorder => "bit-tgb-pbr",
            BitColor.SecondaryBorder => "bit-tgb-sbr",
            BitColor.TertiaryBorder => "bit-tgb-tbr",
            _ => "bit-tgb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tgb-sm",
            BitSize.Medium => "bit-tgb-md",
            BitSize.Large => "bit-tgb-lg",
            _ => "bit-tgb-md"
        });

        ClassBuilder.Register(() => GetVariant() switch
        {
            BitVariant.Fill => "bit-tgb-fil",
            BitVariant.Outline => "bit-tgb-otl",
            BitVariant.Text => "bit-tgb-txt",
            _ => "bit-tgb-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsChecked ? Styles?.Checked : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        // The cascaded values are applied here as well as in OnParametersSet, since DefaultIsChecked is only
        // read while the component initializes and would otherwise arrive one lifecycle step too late.
        CascadingParameters?.UpdateParameters(this);

        if (IsCheckedHasBeenSet is false && DefaultIsChecked.HasValue)
        {
            await AssignIsChecked(DefaultIsChecked.Value);
        }

        await base.OnInitializedAsync();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitToggleButtonParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        UpdateLoadingVisuals();

        base.OnParametersSet();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        CancelLoadingDelay();

        await base.DisposeAsync(disposing);
    }



    /// <summary>
    /// The tab order of the toggle button, falling back to the browser default so the disabled state's
    /// tabindex does not stick around after re-enabling.
    /// </summary>
    private string? GetTabIndex(bool ariaHidden)
    {
        // A control hidden from assistive technologies must not be reachable by Tab either, or a keyboard
        // user lands on something a screen reader has nothing to say about.
        if (ariaHidden) return "-1";

        if (IsEnabled is false && AllowDisabledFocus is false) return "-1";

        return TabIndex;
    }

    private void UpdateLoadingVisuals()
    {
        if (IsLoading)
        {
            if (_showLoading || _loadingDelayCts is not null) return;

            if (LoadingDelay < 1)
            {
                SetShowLoading(true);
                return;
            }

            _loadingDelayCts = new();
            _ = ShowLoadingAfterDelay(_loadingDelayCts.Token);
        }
        else
        {
            SetShowLoading(false);
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

        SetShowLoading(true);

        await InvokeAsync(StateHasChanged);
    }

    // The stacking grid is a registered class, so the builder has to be told when the value behind it moves -
    // nothing else does, since the field is not a parameter.
    private void SetShowLoading(bool value)
    {
        if (_showLoading == value) return;

        _showLoading = value;

        ClassBuilder.Reset();
    }

    private void CancelLoadingDelay()
    {
        if (_loadingDelayCts is null) return;

        _loadingDelayCts.Cancel();
        _loadingDelayCts.Dispose();
        _loadingDelayCts = null;
    }



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsLoading && Reclickable is false) return;

        // The auto-loading window opens before OnClick rather than after it. An async OnClick handler is part of
        // what the click is waiting for, so a guard that only starts once it has returned leaves open exactly the
        // window it exists to close - and the button would show no spinner for the whole of it.
        // Snapshotted so the cleanup pairs with the entry even if the parameter changes mid-await.
        var autoLoading = AutoLoading;

        await BeginAutoLoading(autoLoading);

        try
        {
            await OnClick.InvokeAsync(e);

            // the loading state is already held open by this method, so the change does not open a second one
            await ChangeIsChecked(IsChecked is false, autoLoading: false);
        }
        finally
        {
            await EndAutoLoading(autoLoading);
        }
    }

    private async Task ChangeIsChecked(bool value, bool autoLoading)
    {
        if (IsEnabled is false) return;

        autoLoading = autoLoading && AutoLoading;

        await BeginAutoLoading(autoLoading);

        try
        {
            if (OnChanging.HasDelegate)
            {
                var args = new BitToggleButtonChangeArgs(value);

                await OnChanging.InvokeAsync(args);

                if (args.Cancel) return;
            }

            if (await AssignIsChecked(value) is false) return;

            await OnChange.InvokeAsync(value);
        }
        finally
        {
            await EndAutoLoading(autoLoading);
        }
    }

    private async Task BeginAutoLoading(bool autoLoading)
    {
        if (autoLoading is false) return;

        await AssignIsLoading(true);

        // The loading visuals normally follow the parameter through OnParametersSet, which a state change the
        // component makes to itself never reaches - so the spinner has to be driven from here, or an auto-loading
        // toggle button would hold the click guard up without ever showing one.
        UpdateLoadingVisuals();

        // Reclickable lets clicks overlap, so count the in-flight operations and only
        // clear the loading state once the last one has completed
        _pendingChanges++;
    }

    private async Task EndAutoLoading(bool autoLoading)
    {
        if (autoLoading is false) return;

        if (--_pendingChanges > 0) return;

        await AssignIsLoading(false);

        UpdateLoadingVisuals();
    }



    private BitColor? GetColor() => (IsChecked ? OnColor : OffColor) ?? Color;

    private BitVariant? GetVariant() => (IsChecked ? OnVariant : OffVariant) ?? Variant;

    private RenderFragment? GetTemplate() => (IsChecked ? OnTemplate : OffTemplate) ?? ChildContent;

    private BitIconInfo? GetCheckMarkIcon() => BitIconInfo.From(CheckMarkIcon, CheckMarkIconName ?? "Accept");

    private BitIconInfo? GetIcon()
    {
        if (IsChecked)
        {
            var icon = BitIconInfo.From(OnIcon, OnIconName);

            if (icon is not null) return icon;
        }
        else
        {
            var icon = BitIconInfo.From(OffIcon, OffIconName);

            if (icon is not null) return icon;
        }

        return BitIconInfo.From(Icon, IconName);
    }

    private string? GetText()
    {
        if (IsChecked && OnText.HasValue()) return OnText;

        if (IsChecked is false && OffText.HasValue()) return OffText;

        return Text;
    }

    private string? GetTitle()
    {
        if (IsChecked && OnTitle.HasValue()) return OnTitle;

        if (IsChecked is false && OffTitle.HasValue()) return OffTitle;

        return Title;
    }

    private string? GetAriaLabel()
    {
        if (IsChecked && OnAriaLabel.HasValue()) return OnAriaLabel;

        if (IsChecked is false && OffAriaLabel.HasValue()) return OffAriaLabel;

        return AriaLabel;
    }

    /// <summary>
    /// The accessible name an icon-only toggle button takes from its text, which it renders nowhere: a name the
    /// page did write would otherwise be dropped on the floor and leave a screen reader announcing the button as
    /// nothing at all. Being read off the text rather than asked for, it gives way to an <c>aria-label</c> the page
    /// wrote by hand (see the razor), and it is not applied over a template, whose own content names the button.
    /// </summary>
    private string? GetIconOnlyAriaLabel() => IconOnly && GetTemplate() is null ? GetText() : null;

    private string? GetRole() => AriaMode is BitToggleButtonAriaMode.Switch ? "switch" : null;

    private string? GetAriaPressed()
        => AriaMode switch
        {
            BitToggleButtonAriaMode.Pressed => IsChecked.ToString().ToLower(),
            BitToggleButtonAriaMode.Switch or BitToggleButtonAriaMode.None or BitToggleButtonAriaMode.Expanded => null,
            _ => AccessibleNameChanges() ? null : IsChecked.ToString().ToLower()
        };

    private string? GetAriaChecked()
    {
        if (AriaMode is BitToggleButtonAriaMode.Switch) return IsChecked.ToString().ToLower();

        // These two named the state attribute themselves - none at all, or aria-expanded - so nothing is added
        // beside what they asked for.
        if (AriaMode is BitToggleButtonAriaMode.None or BitToggleButtonAriaMode.Expanded) return null;

        // Otherwise the state follows the pattern the page named: aria-pressed belongs to the button role alone,
        // and a role that reads aria-checked instead is left with no state at all unless it is written here.
        if (_ariaCheckedRoles.Contains(GetSplattedAttribute("role")) is false) return null;

        // This one is read off the role rather than asked for, and an inference gives way to what the page wrote
        // itself - unlike an explicit AriaMode, which is the parameter for saying what the state attribute is.
        return GetSplattedAttribute("aria-checked") is null ? IsChecked.ToString().ToLower() : null;
    }

    private string? GetAriaExpanded()
        => AriaMode is BitToggleButtonAriaMode.Expanded ? IsChecked.ToString().ToLower() : null;

    /// <summary>
    /// Reports whether the accessible name of the toggle button differs between the two states, in which case
    /// the name itself conveys the state and announcing aria-pressed on top of it becomes ambiguous.
    /// </summary>
    private bool AccessibleNameChanges()
    {
        // aria-labelledby wins the accessible name computation, so a stable value there pins the name for both states.
        // The hyphenated name arrives as a splatted attribute rather than as the parameter (see the razor), and the
        // name a screen reader computes does not care which of the two the page reached for.
        if ((AriaLabelledBy ?? GetSplattedAttribute("aria-labelledby")).HasValue()) return false;

        return AccessibleName(true) != AccessibleName(false);
    }

    /// <summary>
    /// What a screen reader would name the toggle button in the given state, walked in the order the accessible
    /// name computation uses: aria-label, then the rendered content, then the title attribute. The title is the
    /// step that is easy to forget - it is what names an icon-only toggle button that was given no aria-label,
    /// so a per-state OnTitle/OffTitle changes the name there as surely as a per-state text does elsewhere.
    /// </summary>
    private string? AccessibleName(bool isChecked)
    {
        var ariaLabel = isChecked
            ? (OnAriaLabel.HasValue() ? OnAriaLabel : AriaLabel)
            : (OffAriaLabel.HasValue() ? OffAriaLabel : AriaLabel);

        if (ariaLabel.HasValue()) return ariaLabel;

        // An aria-label written by hand outranks the content and the title here exactly as the parameter does, and
        // being the same in both states it is what keeps the name - and with it aria-pressed - stable over a pair of
        // per-state texts.
        var splattedAriaLabel = GetSplattedAttribute("aria-label");

        if (splattedAriaLabel.HasValue()) return splattedAriaLabel;

        // A template is content this cannot read, and it is never empty, so the walk stops here rather than
        // falling through to the title. Both states answer the same, which is what leaves aria-pressed in place
        // for a pair of templates - the remarks on OnTemplate say to pin the name down where they read apart.
        if ((((isChecked ? OnTemplate : OffTemplate) ?? ChildContent)) is not null) return null;

        // The text names the button in both of the ways it can be reached: rendered beside the icon where there is
        // room for it, and read as the aria-label of an icon-only one, which renders none. So it is walked here
        // whether or not IconOnly is set - the walk has already returned for a template, which is the one case
        // where the text is neither rendered nor borrowed.
        var text = isChecked
            ? (OnText.HasValue() ? OnText : Text)
            : (OffText.HasValue() ? OffText : Text);

        if (text.HasValue()) return text;

        return isChecked
            ? (OnTitle.HasValue() ? OnTitle : Title)
            : (OffTitle.HasValue() ? OffTitle : Title);
    }

    private string GetLoadingLabelPositionClass()
        => LoadingLabelPosition switch
        {
            BitLabelPosition.Top => "bit-tgb-top",
            BitLabelPosition.Start => "bit-tgb-srt",
            BitLabelPosition.End => "bit-tgb-end",
            BitLabelPosition.Bottom => "bit-tgb-btm",
            _ => "bit-tgb-end"
        };
}
