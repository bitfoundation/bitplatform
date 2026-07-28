namespace Bit.BlazorUI;

public class BitThemeProvider : ComponentBase
{
    /// <summary>
    /// The content of the ThemeProvider.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The element used for the root node. Ignored whenever no wrapping element is rendered - that is,
    /// when there is nothing to apply at this layer: both <see cref="Theme"/> and
    /// <see cref="ParentTheme"/> are <see langword="null"/>, or <see cref="Theme"/> is
    /// <see langword="null"/> and an unnamed <see cref="ParentTheme"/> is passed through directly
    /// (no <see cref="ThemeName"/> to re-expose it under).
    /// </summary>
    /// <remarks>
    /// When a wrapping element is rendered it defaults to <c>display:contents</c> so it produces no
    /// layout box and stays transparent to the surrounding flex/grid layout, while still letting the
    /// applied CSS custom properties inherit to descendants. Supply <c>style="display:…"</c> via the
    /// splatted attributes to opt back into a real box (e.g. when you also set box-model styles on it).
    /// </remarks>
    [Parameter] public string? RootElement { get; set; }

    /// <summary>
    /// The BitTheme instance used to customize the theme.
    /// </summary>
    [Parameter] public BitTheme? Theme { get; set; }

    /// <summary>
    /// Opts this provider into treating <see cref="Theme"/> and <see cref="ParentTheme"/> as
    /// immutable ("frozen"). When <see langword="true"/>, the provider skips the relatively
    /// expensive per-render rebuild (the <see cref="BitThemeMapper"/> merge that allocates a fresh
    /// all-token <see cref="BitTheme"/> plus the ~300-entry CSS-variable string build) whenever both
    /// references are unchanged since the last render - it just keeps the previously cached result.
    /// </summary>
    /// <remarks>
    /// This is a performance opt-in for the common case where callers hand the provider a stable
    /// <see cref="BitTheme"/> instance and never mutate it in place. The trade-off is that in-place
    /// mutation of a frozen theme is NOT detected (the references don't change, so nothing is
    /// rebuilt or re-cascaded); assign a new <see cref="BitTheme"/> instance to apply changes.
    /// Leave this <see langword="false"/> (the default) to keep the mutation-in-place behavior where
    /// every parameters update rebuilds from the current token values.
    /// </remarks>
    [Parameter] public bool Frozen { get; set; }

    /// <summary>
    /// Optional name for <see cref="CascadingValue{T}"/>; when set, consumers use <c>[CascadingParameter(Name = …)]</c>.
    /// The cascaded <see cref="BitTheme"/> is the merge of <see cref="Theme"/> with <see cref="ParentTheme"/> (same as inline CSS variables on this provider's root).
    /// </summary>
    [Parameter] public string? ThemeName { get; set; }

    /// <summary>
    /// Catch-all for HTML attributes splatted onto the wrapping element (e.g. <c>class</c>, <c>id</c>,
    /// <c>data-*</c>, ARIA roles). Only emitted when this provider renders a wrapping element - that
    /// is, when a local <see cref="Theme"/> is applied (its CSS variables need a host element), or
    /// when a cascading <see cref="ParentTheme"/> is re-exposed under a custom <see cref="ThemeName"/>.
    /// When there is nothing to apply at this layer (no local <see cref="Theme"/> and no
    /// <see cref="ThemeName"/>) the provider renders just <see cref="ChildContent"/> and these
    /// attributes are ignored.
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

    // The Theme/ParentTheme references the cached result above was built from. Only consulted when
    // Frozen is true, to short-circuit the merge + CSS rebuild when neither reference changed.
    private BitTheme? _lastTheme;
    private BitTheme? _lastParentTheme;

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
            // Local theme not set, but a ParentTheme is cascading from above. Keep it as the value
            // we would re-expose, and emit no inline style at this layer (the ancestor provider owns
            // those CSS vars). BuildRenderTree only renders a wrapper + CascadingValue when a custom
            // ThemeName needs the value re-exposed under that name; otherwise it renders ChildContent
            // directly and relies on the ancestor's existing unnamed cascade, avoiding a redundant
            // wrapper element and duplicate CascadingValue.
            _cachedMergedTheme = ParentTheme;
            _cachedCssVarStyle = null;
            return;
        }

        // Frozen fast path: when the caller opts into treating the theme references as immutable,
        // skip the merge + CSS-string rebuild entirely if neither Theme nor ParentTheme changed by
        // reference since the cached result was produced. _cachedCssVarStyle is only non-null after a
        // rebuild in this branch, so this guards against reusing a stale cache from the Theme-null /
        // no-tokens branches above.
        if (Frozen
            && _cachedCssVarStyle is not null
            && ReferenceEquals(Theme, _lastTheme)
            && ReferenceEquals(ParentTheme, _lastParentTheme))
        {
            return;
        }

        // Always produce a FRESH merged BitTheme (BitThemeMapper.Merge allocates a new instance),
        // even when there is no ParentTheme. This keeps the CascadingValue<BitTheme?> change-detection
        // behavior consistent: previously the no-parent path cascaded the caller's own Theme instance,
        // so an in-place mutation of that instance produced the same reference and CascadingValue
        // (which compares reference types with ReferenceEquals) never notified [CascadingParameter]
        // consumers - whereas the with-parent path always allocated a new reference and did notify.
        // Going through Merge in both cases means a token change yields a new reference (consumers are
        // notified), while the CSS-string equality suppression below preserves the cached reference
        // when nothing actually changed (consumers are not woken up needlessly).
        var mergedTheme = BitThemeMapper.Merge(Theme, ParentTheme ?? new BitTheme());

        var cssVarStyle = BuildCssVarStyle(mergedTheme);

        // Record the references this result was built from so a subsequent Frozen render can detect
        // that nothing changed by reference and skip the rebuild above.
        _lastTheme = Theme;
        _lastParentTheme = ParentTheme;

        // Suppress propagation when the produced output is identical to the previous render -
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

        if (_cachedCssVarStyle is null && ThemeName is null)
        {
            // Nothing to apply at this layer: no CSS variables to emit (Theme is null - only a
            // ParentTheme is cascading from above) and no custom ThemeName to re-expose the value
            // under. The ancestor's unnamed CascadingValue<BitTheme> already reaches our
            // descendants, so rendering ChildContent directly preserves the cascade while avoiding
            // a redundant wrapper element and a duplicate CascadingValue.
            builder.AddContent(0, ChildContent);
            return;
        }

        builder.OpenElement(1, RootElement ?? "div");

        // Collect user attributes into a single dictionary (splatted via AddMultipleAttributes so
        // the render-tree diff reconciles a changing attribute set correctly), pulling out any
        // "style" entry first so we can merge it with our CSS-variable declarations. It is added
        // separately below rather than left in the splat because last-write-wins semantics on
        // RenderTreeBuilder mean a splat followed by AddAttribute("style", ...) would silently drop
        // the user's style.
        string? userStyle = null;
        if (AdditionalAttributes is not null)
        {
            var attributes = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in AdditionalAttributes)
            {
                if (string.Equals(kv.Key, "style", StringComparison.OrdinalIgnoreCase))
                {
                    userStyle = kv.Value?.ToString();
                    continue;
                }

                attributes[kv.Key] = kv.Value;
            }

            builder.AddMultipleAttributes(2, attributes);
        }

        // Compose the inline style. The wrapper defaults to `display:contents` so it produces no
        // layout box of its own: the element must exist for CSS custom properties to inherit to
        // descendants (inheritance follows the DOM tree, not the box tree), but it should not
        // perturb the surrounding flex/grid layout or child selectors the way a default `div`
        // block box would. The CSS-variable declarations come next, and the user-supplied style
        // comes last so a caller can override both the layout mode (e.g. `style="display:flex"`)
        // and any individual token on conflict.
        var style = "display:contents";
        if (_cachedCssVarStyle is not null)
        {
            style = $"{style};{_cachedCssVarStyle}";
        }
        if (userStyle is not null)
        {
            style = $"{style};{userStyle}";
        }

        builder.AddAttribute(3, "style", style);

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
        var cssVars = BitThemeMapper.MapToCssVariables(theme);

        // Re-declare the semantic aliases whose target primitives this theme overrides, so app CSS
        // reading the alias tier inside this provider tracks the override (an alias's var() is
        // substituted where the alias is DEFINED, so the :root-level default would otherwise keep
        // the document palette's value). See BitThemeMapper.AugmentWithSemanticAliasReSubstitution.
        BitThemeMapper.AugmentWithSemanticAliasReSubstitution(cssVars);

        return string.Join(';', cssVars.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}
