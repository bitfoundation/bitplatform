namespace Bit.BlazorUI;

/// <summary>
/// TagsInput is an input component that allows users to add tags (keywords) by typing text and pressing Enter.
/// </summary>
public partial class BitTagsInput : BitInputBase<ICollection<string>?>
{
    [Inject] private IJSRuntime _js { get; set; } = default!;

    private bool _hasFocus;
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
    /// Callback invoked before a tag is added. Set <c>args.Cancel = true</c> to cancel the add.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputBeforeArgs> OnBeforeAdd { get; set; }

    /// <summary>
    /// Callback invoked before a tag is removed. Set <c>args.Cancel = true</c> to cancel the remove.
    /// </summary>
    [Parameter] public EventCallback<BitTagsInputBeforeArgs> OnBeforeRemove { get; set; }

    /// <summary>
    /// Callback for when one or more tags are added. Receives the list of all newly added tags.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> OnAdd { get; set; }

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

        OnValueChanged += HandleOnValueChanged;

        SetDefaultValue();

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



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        UpdatePlaceholder();
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
            var separatorArray = Separators.ToArray();

            if (separatorArray.Any(s => _inputText.Contains(s)))
            {
                var textWithoutSeparators = separatorArray.Aggregate(_inputText, (t, s) => t.Replace(s, string.Empty)).Trim();

                await TryAddTags(_inputText.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                
                // If all adds were rejected (e.g. duplicates), strip separators from _inputText
                if (_inputText.Length > 0)
                {
                    _inputText = textWithoutSeparators;
                }
            }
        }
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (e.Key == "Enter")
        {
            // JS capture-phase listener already called preventDefault() as needed;
            // just process the tag addition here.
            await TryAddTag();
        }
        else if (e.Key == "Backspace" && _inputText.Length == 0)
        {
            await RemoveLastTag();
        }
        else if (e.Key == "Tab" && _inputText.Trim().Length > 0)
        {
            // JS already prevented focus move in capture phase; add the tag.
            await TryAddTag();
        }
        else if (Separators is not null && e.Key.Length == 1 && Separators.Any(s => s == e.Key))
        {
            // JS already prevented the separator char from being typed in capture phase;
            // add the current input text as a tag.
            await TryAddTag();
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
        await OnAdd.InvokeAsync([text]);
    }

    private async Task TryAddTags(string[] tags)
    {
        var list = CurrentValue is not null ? new List<string>(CurrentValue) : [];
        var addedTags = new List<string>();

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
            addedTags.Add(text);
        }

        if (addedTags.Count == 0) return;

        _inputText = string.Empty;
        await SetCurrentValueAsync(list);
        await OnAdd.InvokeAsync(addedTags);
    }

    private async Task HandleRemoveTag(int index, string tag)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (OnBeforeRemove.HasDelegate)
        {
            var args = new BitTagsInputBeforeArgs { Tag = tag };
            await OnBeforeRemove.InvokeAsync(args);
            if (args.Cancel) return;
        }

        var list = CurrentValue is not null ? new List<string>(CurrentValue) : [];
        list.RemoveAt(index);

        await SetCurrentValueAsync(list.Count > 0 ? list : null);
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
        await OnRemove.InvokeAsync(lastTag);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        await base.DisposeAsync(disposing);
    }
}
