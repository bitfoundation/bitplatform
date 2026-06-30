namespace Bit.BlazorUI;

// Toolbar render pipeline. Groups are rendered in a computed order (default = the original
// order). Custom items and host-specified ordering are layered over this seam.
public partial class BitRichTextEditor
{
    private ElementReference _toolbarRef = default!;

    /// <summary>Custom toolbar items and ordering. Null uses the default group order.</summary>
    [Parameter] public BitRichTextEditorToolbarConfig? ToolbarConfig { get; set; }

    // Stable identifiers for the built-in groups, in default display order. The ids are sourced
    // from BitRichTextEditorToolbarConfig.GroupIds so callers and this table never drift apart.
    private static readonly (string Id, BitRichTextEditorToolbar Flag)[] DefaultGroupOrder =
    [
        (BitRichTextEditorToolbarConfig.GroupIds.History, BitRichTextEditorToolbar.History),
        (BitRichTextEditorToolbarConfig.GroupIds.BlockFormat, BitRichTextEditorToolbar.BlockFormat),
        (BitRichTextEditorToolbarConfig.GroupIds.Font, BitRichTextEditorToolbar.Font),
        (BitRichTextEditorToolbarConfig.GroupIds.Inline, BitRichTextEditorToolbar.Inline),
        (BitRichTextEditorToolbarConfig.GroupIds.Color, BitRichTextEditorToolbar.Color),
        (BitRichTextEditorToolbarConfig.GroupIds.Script, BitRichTextEditorToolbar.Script),
        (BitRichTextEditorToolbarConfig.GroupIds.Lists, BitRichTextEditorToolbar.Lists),
        (BitRichTextEditorToolbarConfig.GroupIds.Indent, BitRichTextEditorToolbar.Indent),
        (BitRichTextEditorToolbarConfig.GroupIds.Blocks, BitRichTextEditorToolbar.Blocks),
        (BitRichTextEditorToolbarConfig.GroupIds.Link, BitRichTextEditorToolbar.Link),
        (BitRichTextEditorToolbarConfig.GroupIds.Media, BitRichTextEditorToolbar.Media),
        (BitRichTextEditorToolbarConfig.GroupIds.Image, BitRichTextEditorToolbar.Image),
        (BitRichTextEditorToolbarConfig.GroupIds.Table, BitRichTextEditorToolbar.Table),
        (BitRichTextEditorToolbarConfig.GroupIds.Rule, BitRichTextEditorToolbar.Rule),
        (BitRichTextEditorToolbarConfig.GroupIds.Alignment, BitRichTextEditorToolbar.Alignment),
        (BitRichTextEditorToolbarConfig.GroupIds.Direction, BitRichTextEditorToolbar.Direction),
        (BitRichTextEditorToolbarConfig.GroupIds.Emoji, BitRichTextEditorToolbar.Emoji),
        (BitRichTextEditorToolbarConfig.GroupIds.Find, BitRichTextEditorToolbar.Find),
        (BitRichTextEditorToolbarConfig.GroupIds.Source, BitRichTextEditorToolbar.Source),
        (BitRichTextEditorToolbarConfig.GroupIds.FullScreen, BitRichTextEditorToolbar.FullScreen),
        (BitRichTextEditorToolbarConfig.GroupIds.Clear, BitRichTextEditorToolbar.Clear),
    ];

    /// <summary>
    /// The ordered list of toolbar entry ids to render. Built-in group ids are included only
    /// when their flag is enabled; custom item ids are interleaved per ToolbarConfig.
    /// </summary>
    private IEnumerable<string> OrderedToolbarIds()
    {
        var enabledGroups = DefaultGroupOrder.Where(g => Has(g.Flag)).Select(g => g.Id).ToList();
        var customIds = ToolbarConfig?.CustomItems?.Take(50).Select(i => i.Id).ToList() ?? [];

        if (ToolbarConfig?.Order is { Count: > 0 } order)
        {
            var known = new HashSet<string>(enabledGroups.Concat(customIds), StringComparer.OrdinalIgnoreCase);
            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Ordered entries first (skip unknown ids).
            foreach (var id in order)
                if (known.Contains(id) && emitted.Add(id))
                    yield return id;
            // Append omitted entries in default order.
            foreach (var id in enabledGroups.Concat(customIds))
                if (emitted.Add(id))
                    yield return id;
            yield break;
        }

        foreach (var id in enabledGroups) yield return id;
        foreach (var id in customIds) yield return id;
    }

    /// <summary>
    /// Validates custom toolbar items before they are used for ordering, lookup, title, or
    /// aria-label. <c>required</c> only guarantees the members are assigned, not that they are
    /// meaningful, so reject null/empty/whitespace ids and aria-labels fast with a clear message.
    /// </summary>
    private void ValidateCustomItems()
    {
        if (ToolbarConfig?.CustomItems is not { } items) return;

        // Track ids case-insensitively: OrderedToolbarIds() de-duplicates ids the same way and
        // RenderCustomItem() resolves by the first case-insensitive match, so a duplicate id
        // would silently hide every later item that shares it.
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("A BitRichTextEditor custom toolbar item has a blank Id.", nameof(ToolbarConfig));

            if (seenIds.Add(item.Id) is false)
                throw new ArgumentException($"BitRichTextEditor has duplicate custom toolbar item Id '{item.Id}'.", nameof(ToolbarConfig));

            // Custom ids share the namespace used by OrderedToolbarIds()/RenderGroup() to resolve
            // built-in groups; a collision with a reserved id (e.g. history, image) would shadow
            // or be shadowed by a built-in group, so reject it outright.
            if (DefaultGroupOrder.Any(g => string.Equals(g.Id, item.Id, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"BitRichTextEditor custom toolbar item Id '{item.Id}' collides with a built-in toolbar group id.", nameof(ToolbarConfig));

            if (string.IsNullOrWhiteSpace(item.AriaLabel))
                throw new ArgumentException($"BitRichTextEditor custom toolbar item '{item.Id}' has a blank AriaLabel.", nameof(ToolbarConfig));

            // A custom item must render something visible: require either a Label or an Icon so a
            // bare item (which would otherwise fall back to showing its raw Id) is rejected fast.
            if (item.Icon is null && string.IsNullOrWhiteSpace(item.Label))
                throw new ArgumentException($"BitRichTextEditor custom toolbar item '{item.Id}' must specify a Label or an Icon.", nameof(ToolbarConfig));
        }
    }

    private void RenderCustomItem(RenderTreeBuilder builder, string id)
    {
        var item = ToolbarConfig?.CustomItems?.FirstOrDefault(i =>
            string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"bit-rte-grp {Classes?.Group}");
        builder.AddAttribute(2, "style", Styles?.Group);
        builder.OpenElement(3, "button");
        builder.AddAttribute(4, "type", "button");
        builder.AddAttribute(5, "class", $"bit-rte-btn {Classes?.Button}");
        builder.AddAttribute(6, "style", Styles?.Button);
        // Icon-only items may omit a visible label; fall back through Label then Id so the
        // button always exposes a usable accessible name and tooltip.
        var accessibleName = item.AriaLabel ?? item.Label ?? item.Id;
        builder.AddAttribute(7, "title", accessibleName);
        builder.AddAttribute(8, "aria-label", accessibleName);
        builder.AddAttribute(9, "disabled", ControlsDisabled);
        builder.AddAttribute(10, "onclick", EventCallback.Factory.Create(this, () => InvokeCustomItemAsync(item)));
        if (item.Icon is not null) builder.AddContent(11, item.Icon);
        else builder.AddContent(12, item.Label ?? item.Id);
        builder.CloseElement();
        builder.CloseElement();
    }

    private async Task InvokeCustomItemAsync(BitRichTextEditorToolbarItem item)
    {
        try
        {
            await item.OnActivate(this);
        }
        catch (Exception ex)
        {
            // Keep host callback internals out of the user-facing error; log them for telemetry.
            System.Diagnostics.Debug.WriteLine($"BitRichTextEditor toolbar action '{item.Id}' failed: {ex}");
            await RaiseErrorAsync(new BitRichTextEditorError("custom-action-failed", $"Toolbar action '{item.Id}' failed."));
        }
    }
}
