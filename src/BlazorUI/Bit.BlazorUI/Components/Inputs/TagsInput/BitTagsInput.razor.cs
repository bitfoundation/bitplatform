namespace Bit.BlazorUI;

/// <summary>
/// TagsInput is an input component that allows users to add tags (keywords) by typing text and pressing Enter.
/// </summary>
public partial class BitTagsInput : BitInputBase<ICollection<string>?>
{
    [Inject] private IJSRuntime _js { get; set; } = default!;

    private bool _hasFocus;
    private bool _preventKeyDown;
    private string _inputText = string.Empty;
    private string _inputId = string.Empty;
    private string _labelId = string.Empty;
    private string? _currentPlaceholder;
    private string? _separatorsJson;



    /// <summary>
    /// Whether the input should receive focus on first render.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// When set to true, pressing Enter (or a confirm key) while the input is empty will not be
    /// suppressed, allowing the event to propagate (e.g., to submit a form).
    /// </summary>
    [Parameter] public bool CancelConfirmKeysOnEmpty { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the component.
    /// </summary>
    [Parameter] public BitTagsInputClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets the icon for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the dismiss button from the built-in Fluent UI icons.
    /// Defaults to Cancel when not set.
    /// </summary>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// Whether duplicate tags are allowed.
    /// </summary>
    [Parameter] public bool Duplicates { get; set; }

    /// <summary>
    /// The label displayed above the input.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// A custom template for the label.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum number of characters allowed for each individual tag.
    /// </summary>
    [Parameter] public int MaxLength { get; set; }

    /// <summary>
    /// The maximum number of tags allowed.
    /// </summary>
    [Parameter] public int MaxTags { get; set; }

    /// <summary>
    /// Whether the input should have no border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Callback invoked before a tag is added. Return false to cancel the add.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputBeforeArgs> OnBeforeAdd { get; set; }

    /// <summary>
    /// Callback invoked before a tag is removed. Return false to cancel the remove.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputBeforeArgs> OnBeforeRemove { get; set; }

    /// <summary>
    /// Callback for when a tag is added.
    /// </summary>
    [Parameter] public EventCallback<string> OnAdd { get; set; }

    /// <summary>
    /// Callback fired when a duplicate tag entry is attempted (and <see cref="Duplicates"/> is false).
    /// </summary>
    [Parameter] public EventCallback<string> OnTagExists { get; set; }

    /// <summary>
    /// Callback for when a tag is removed.
    /// </summary>
    [Parameter] public EventCallback<string> OnRemove { get; set; }

    /// <summary>
    /// Callback for when the input receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when the input loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// The placeholder text for the input.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The character(s) used to separate tags when typing. Defaults to Enter key only.
    /// Also used to split pasted text into multiple tags.
    /// </summary>
    [Parameter] public IEnumerable<string>? Separators { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the component.
    /// </summary>
    [Parameter] public BitTagsInputClassStyles? Styles { get; set; }

    /// <summary>
    /// A custom template for rendering each tag.
    /// </summary>
    [Parameter] public RenderFragment<string>? TagTemplate { get; set; }



    /// <summary>
    /// Removes all tags.
    /// </summary>
    public async Task Clear()
    {
        if (IsEnabled is false || ReadOnly) return;

        _inputText = string.Empty;
        await SetCurrentValueAsync(null);
        UpdatePlaceholder();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-tgi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => NoBorder ? "bit-tgi-nbd" : string.Empty);
        ClassBuilder.Register(() => _hasFocus ? $"bit-tgi-fcs {Classes?.Focused}" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        _separatorsJson = Separators is not null
            ? System.Text.Json.JsonSerializer.Serialize(Separators)
            : null;

        await base.OnParametersSetAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        _inputId = $"BitTagsInput-{UniqueId}-input";
        _labelId = $"BitTagsInput-{UniqueId}-label";

        UpdatePlaceholder();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await _js.BitTagsInputSetup(InputElement);

            if (AutoFocus && IsEnabled)
            {
                await InputElement.FocusAsync();
            }
        }
    }

    protected override bool TryParseValueFromString(string? value, out ICollection<string>? result, out string? parsingErrorMessage)
    {
        result = value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        parsingErrorMessage = null;
        return true;
    }

    protected override string? FormatValueAsString(ICollection<string>? value)
    {
        return value is not null ? string.Join(",", value) : null;
    }



    private void UpdatePlaceholder()
    {
        _currentPlaceholder = CurrentValue is null || CurrentValue.Count == 0 ? Placeholder : null;
    }

    private async Task HandleContainerClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await InputElement.FocusAsync();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();

        await TryAddTag();
        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnInput(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        _inputText = e.Value?.ToString() ?? string.Empty;

        if (MaxLength > 0 && _inputText.Length > MaxLength)
        {
            _inputText = _inputText[..MaxLength];
        }

        if (Separators is not null)
        {
            foreach (var separator in Separators)
            {
                if (_inputText.Contains(separator))
                {
                    var textWithoutSeparator = _inputText.Replace(separator, string.Empty).Trim();
                    await TryAddTags(_inputText.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    // If add was rejected (e.g. duplicate), _inputText still has the separator; strip it
                    if (_inputText.Length > 0)
                    {
                        _inputText = textWithoutSeparator;
                    }
                    return;
                }
            }
        }
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly)
        {
            _preventKeyDown = false;
            return;
        }

        if (e.Key == "Enter")
        {
            var hasText = _inputText.Trim().Length > 0;
            // Prevent the key event (e.g. form submit) unless input was empty and CancelConfirmKeysOnEmpty is true
            _preventKeyDown = hasText || CancelConfirmKeysOnEmpty is false;
            await TryAddTag();
        }
        else if (e.Key == "Backspace" && _inputText.Length == 0)
        {
            _preventKeyDown = false;
            await RemoveLastTag();
        }
        else if (e.Key == "Tab" && _inputText.Trim().Length > 0)
        {
            // JS already prevented focus move in capture phase; just add the tag
            _preventKeyDown = false;
            await TryAddTag();
        }
        else if (Separators is not null && e.Key.Length == 1 && Separators.Any(s => s == e.Key))
        {
            // JS already prevented the separator char from being typed in capture phase;
            // consume the event here and add the current input text as a tag
            _preventKeyDown = false;
            await TryAddTag();
        }
        else
        {
            _preventKeyDown = false;
        }
    }

    private async Task TryAddTag()
    {
        var text = _inputText.Trim();

        if (text.Length == 0) return;

        if (MaxLength > 0 && text.Length > MaxLength)
        {
            text = text[..MaxLength];
        }

        if (MaxTags > 0 && CurrentValue is not null && CurrentValue.Count >= MaxTags) return;

        if (Duplicates is false && CurrentValue is not null && CurrentValue.Contains(text))
        {
            await OnTagExists.InvokeAsync(text);
            return;
        }

        if (OnBeforeAdd.HasDelegate)
        {
            var args = new BitTagsInputBeforeArgs { Tag = text };
            await OnBeforeAdd.InvokeAsync(args);
            if (args.Cancel) return;
        }

        var list = CurrentValue is not null ? new List<string>(CurrentValue) : [];
        list.Add(text);

        _inputText = string.Empty;

        await SetCurrentValueAsync(list);
        UpdatePlaceholder();
        await OnAdd.InvokeAsync(text);
    }

    private async Task TryAddTags(string[] tags)
    {
        var list = CurrentValue is not null ? new List<string>(CurrentValue) : [];
        var added = false;

        foreach (var tag in tags)
        {
            var text = MaxLength > 0 && tag.Length > MaxLength ? tag[..MaxLength] : tag;
            if (text.Length == 0) continue;
            if (MaxTags > 0 && list.Count >= MaxTags) break;
            if (Duplicates is false && list.Contains(text))
            {
                await OnTagExists.InvokeAsync(text);
                continue;
            }

            if (OnBeforeAdd.HasDelegate)
            {
                var args = new BitTagsInputBeforeArgs { Tag = text };
                await OnBeforeAdd.InvokeAsync(args);
                if (args.Cancel) continue;
            }

            list.Add(text);
            added = true;
            await OnAdd.InvokeAsync(text);
        }

        if (added is false) return;

        _inputText = string.Empty;
        await SetCurrentValueAsync(list);
        UpdatePlaceholder();
    }

    private async Task HandleRemoveTag(string tag)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (OnBeforeRemove.HasDelegate)
        {
            var args = new BitTagsInputBeforeArgs { Tag = tag };
            await OnBeforeRemove.InvokeAsync(args);
            if (args.Cancel) return;
        }

        var list = CurrentValue is not null ? new List<string>(CurrentValue) : [];
        list.Remove(tag);

        await SetCurrentValueAsync(list.Count > 0 ? list : null);
        UpdatePlaceholder();
        await OnRemove.InvokeAsync(tag);
    }

    private async Task RemoveLastTag()
    {
        if (CurrentValue is null || CurrentValue.Count == 0) return;

        var list = new List<string>(CurrentValue);
        var lastTag = list[^1];

        if (OnBeforeRemove.HasDelegate)
        {
            var args = new BitTagsInputBeforeArgs { Tag = lastTag };
            await OnBeforeRemove.InvokeAsync(args);
            if (args.Cancel) return;
        }

        list.RemoveAt(list.Count - 1);

        await SetCurrentValueAsync(list.Count > 0 ? list : null);
        UpdatePlaceholder();
        await OnRemove.InvokeAsync(lastTag);
    }
}
