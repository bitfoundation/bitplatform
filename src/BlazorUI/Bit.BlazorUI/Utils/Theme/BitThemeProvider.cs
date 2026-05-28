namespace Bit.BlazorUI;

public sealed class BitThemeProvider : ComponentBase
{
    /// <summary>
    /// The content of the ThemeProvider.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The element used for the root node. Ignored when no wrapping element is needed
    /// (i.e. when both <see cref="Theme"/> and <see cref="ParentTheme"/> are <see langword="null"/>).
    /// </summary>
    [Parameter] public string? RootElement { get; set; }

    /// <summary>
    /// The BitTheme instance used to customize the theme.
    /// </summary>
    [Parameter] public BitTheme? Theme { get; set; }

    /// <summary>
    /// Optional name for <see cref="CascadingValue{T}"/>; when set, consumers use <c>[CascadingParameter(Name = …)]</c>.
    /// The cascaded <see cref="BitTheme"/> is the merge of <see cref="Theme"/> with <see cref="ParentTheme"/> (same as inline CSS variables on this provider's root).
    /// </summary>
    [Parameter] public string? ThemeName { get; set; }

    /// <summary>
    /// Catch-all for HTML attributes splatted onto the wrapping element (e.g. <c>class</c>, <c>id</c>,
    /// <c>data-*</c>, ARIA roles). Only emitted when this provider renders a wrapping element —
    /// when both <see cref="Theme"/> and <see cref="ParentTheme"/> are <see langword="null"/> the
    /// provider renders just <see cref="ChildContent"/> and these attributes are ignored.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }


    [CascadingParameter] public BitTheme? ParentTheme { get; set; }


    // Cached merge result and the inline `style` string.
    //
    // BitTheme is mutable (every node is a class with public setters), so callers can keep a
    // single Theme/ParentTheme instance and mutate sub-properties between renders. We therefore
    // rebuild the merge + CSS string on every parameters update and use CSS-string equality as a
    // cheap structural check: when the rebuilt output matches the cached one we keep the cached
    // merged-theme reference, which keeps CascadingValue<BitTheme?> reference-stable so
    // descendants don't re-render on parent re-renders that didn't actually change tokens.
    // (CascadingValue<T> for reference types uses ReferenceEquals for change detection, and
    // BitThemeMapper.Merge produces a fresh BitTheme on every call.)
    private BitTheme? _cachedMergedTheme;
    private string? _cachedCssVarStyle;

    protected override void OnParametersSet()
    {
        if (Theme is null && ParentTheme is null)
        {
            // No tokens to apply at this layer; we'll render ChildContent directly.
            _cachedMergedTheme = null;
            _cachedCssVarStyle = null;
            return;
        }

        if (Theme is null)
        {
            // Local theme not set but a ParentTheme is cascading from above. Re-cascade the
            // parent so consumers below us still see it (the previous implementation broke
            // the cascade when Theme was null and rendered ChildContent without the wrapper).
            // No inline style to emit at this layer; the ancestor provider owns those CSS vars.
            _cachedMergedTheme = ParentTheme;
            _cachedCssVarStyle = null;
            return;
        }

        var mergedTheme = ParentTheme is null
            ? Theme
            : BitThemeMapper.Merge(Theme, ParentTheme);

        var cssVarStyle = BuildCssVarStyle(mergedTheme);

        // Suppress propagation when the produced output is identical to the previous render —
        // this preserves the cascaded reference and avoids waking up every descendant consumer
        // when a parent re-renders without touching the theme.
        if (_cachedMergedTheme is not null && string.Equals(_cachedCssVarStyle, cssVarStyle, StringComparison.Ordinal))
        {
            return;
        }

        _cachedMergedTheme = mergedTheme;
        _cachedCssVarStyle = cssVarStyle;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_cachedMergedTheme is null)
        {
            // No local Theme override and no parent theme to re-cascade: render ChildContent as-is.
            builder.AddContent(0, ChildContent);
            return;
        }

        builder.OpenElement(1, RootElement ?? "div");

        // Splat user attributes individually so we can pull out any "style" entry and merge it
        // with our CSS-variable declarations (last-write-wins semantics on RenderTreeBuilder mean
        // a single AddMultipleAttributes followed by AddAttribute("style", ...) would silently
        // drop the user's style).
        string? userStyle = null;
        if (AdditionalAttributes is not null)
        {
            foreach (var kv in AdditionalAttributes)
            {
                if (string.Equals(kv.Key, "style", StringComparison.OrdinalIgnoreCase))
                {
                    userStyle = kv.Value?.ToString();
                    continue;
                }

                builder.AddAttribute(2, kv.Key, kv.Value);
            }
        }

        // Compose the inline style. CSS-variable declarations come first so the user-supplied
        // style wins on conflicting properties (typical "splat overrides component defaults").
        // When Theme is null but ParentTheme is non-null we have no CSS vars to emit, but a user
        // style still needs to make it onto the element.
        var style = _cachedCssVarStyle is null
            ? userStyle
            : userStyle is null
                ? _cachedCssVarStyle
                : $"{_cachedCssVarStyle};{userStyle}";

        if (style is not null)
        {
            builder.AddAttribute(3, "style", style);
        }

        builder.OpenComponent<CascadingValue<BitTheme?>>(4);
        if (ThemeName is not null)
        {
            builder.AddAttribute(5, "Name", ThemeName);
        }
        builder.AddAttribute(6, "Value", _cachedMergedTheme);
        // IMPORTANT: do NOT close over a mutable outer sequence counter. The lambda runs lazily
        // during the cascade's render pass, so capturing a mutable local would feed Blazor a
        // sequence number that varies per render and defeats its diff. Sequence numbers inside a
        // RenderFragment are local to that fragment, so a constant is what we want.
        builder.AddAttribute(7, "ChildContent", (RenderFragment)(b => b.AddContent(0, ChildContent)));
        builder.CloseComponent();

        builder.CloseElement();
    }

    private static string BuildCssVarStyle(BitTheme theme)
    {
        var cssVars = BitThemeUtilities.ToCssVariables(theme);
        return string.Join(';', cssVars.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}
