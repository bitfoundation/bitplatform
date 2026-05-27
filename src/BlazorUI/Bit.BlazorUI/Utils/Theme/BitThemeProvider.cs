using System.Runtime.CompilerServices;

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


    // Cached merge result and the inline `style` string. Both are recomputed only when one of the
    // input themes is replaced with a different instance (reference change). Caching keeps the
    // CascadingValue<BitTheme?> value reference-stable across re-renders so descendants do not
    // re-render just because the provider's parent re-rendered (CascadingValue uses reference
    // equality for change detection on reference types).
    private BitTheme? _cachedMergedTheme;
    private string? _cachedCssVarStyle;
    private BitTheme? _lastTheme;
    private BitTheme? _lastParentTheme;
    private bool _hasCachedInputs;

    // Per-theme CSS-variable dictionary cache. The mapper produces a ~280-entry dictionary on every
    // call; for a theme reference that doesn't change we want to reuse it. ConditionalWeakTable
    // ties the cache to the theme's lifetime — when the BitTheme is GC'd the entry goes with it.
    private static readonly ConditionalWeakTable<BitTheme, IReadOnlyDictionary<string, string>> _cssVarCache = new();

    protected override void OnParametersSet()
    {
        if (!_hasCachedInputs
            || !ReferenceEquals(Theme, _lastTheme)
            || !ReferenceEquals(ParentTheme, _lastParentTheme))
        {
            _lastTheme = Theme;
            _lastParentTheme = ParentTheme;
            _hasCachedInputs = true;

            if (Theme is null && ParentTheme is null)
            {
                // No tokens to apply at this layer; we'll render ChildContent directly.
                _cachedMergedTheme = null;
                _cachedCssVarStyle = null;
            }
            else if (Theme is null)
            {
                // Local theme not set but a ParentTheme is cascading from above. Re-cascade the
                // parent so consumers below us still see it (the previous implementation broke
                // the cascade when Theme was null and rendered ChildContent without the wrapper).
                _cachedMergedTheme = ParentTheme;
                _cachedCssVarStyle = null;
            }
            else
            {
                _cachedMergedTheme = ParentTheme is null
                    ? Theme
                    : BitThemeMapper.Merge(Theme, ParentTheme);

                _cachedCssVarStyle = BuildCssVarStyle(_cachedMergedTheme);
            }
        }
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

        // Splatted user attributes go first, so the inline CSS-variable style we own can't be
        // overwritten by a user-supplied "style".
        if (AdditionalAttributes is not null)
        {
            builder.AddMultipleAttributes(2, AdditionalAttributes);
        }

        // Only emit the inline style when this layer actually contributes CSS variables.
        // When Theme is null but ParentTheme is non-null we just re-cascade without restyling.
        if (_cachedCssVarStyle is not null)
        {
            builder.AddAttribute(3, "style", _cachedCssVarStyle);
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
        // ConditionalWeakTable.GetValue calls the factory under an internal lock, so cache misses
        // produce a single mapping per theme instance even under concurrent reads. Wrapping in a
        // lambda gives us a clean Func<BitTheme, IReadOnlyDictionary<string, string>> match —
        // BitThemeUtilities.ToCssVariables takes BitTheme? which is a nullable-mismatched delegate.
        var cssVars = _cssVarCache.GetValue(theme, t => BitThemeUtilities.ToCssVariables(t));
        return string.Join(';', cssVars.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}
