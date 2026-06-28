namespace Bit.BlazorUI;

// Toolbar render pipeline. Groups are rendered in a computed order (default = the original
// order). Custom items and host-specified ordering are layered over this seam.
public partial class BitRichTextEditor
{
    private ElementReference _toolbarRef = default!;

    /// <summary>Custom toolbar items and ordering. Null uses the default group order.</summary>
    [Parameter] public BitRichTextEditorToolbarConfig? ToolbarConfig { get; set; }

    // Stable identifiers for the built-in groups, in default display order.
    private static readonly (string Id, BitRichTextEditorToolbar Flag)[] DefaultGroupOrder =
    [
        ("history", BitRichTextEditorToolbar.History),
        ("blockformat", BitRichTextEditorToolbar.BlockFormat),
        ("font", BitRichTextEditorToolbar.Font),
        ("inline", BitRichTextEditorToolbar.Inline),
        ("color", BitRichTextEditorToolbar.Color),
        ("script", BitRichTextEditorToolbar.Script),
        ("lists", BitRichTextEditorToolbar.Lists),
        ("indent", BitRichTextEditorToolbar.Indent),
        ("blocks", BitRichTextEditorToolbar.Blocks),
        ("link", BitRichTextEditorToolbar.Link),
        ("media", BitRichTextEditorToolbar.Media),
        ("image", BitRichTextEditorToolbar.Image),
        ("table", BitRichTextEditorToolbar.Table),
        ("rule", BitRichTextEditorToolbar.Rule),
        ("alignment", BitRichTextEditorToolbar.Alignment),
        ("direction", BitRichTextEditorToolbar.Direction),
        ("emoji", BitRichTextEditorToolbar.Emoji),
        ("find", BitRichTextEditorToolbar.Find),
        ("source", BitRichTextEditorToolbar.Source),
        ("fullscreen", BitRichTextEditorToolbar.FullScreen),
        ("clear", BitRichTextEditorToolbar.Clear),
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
        builder.AddAttribute(7, "title", item.AriaLabel);
        builder.AddAttribute(8, "aria-label", item.AriaLabel);
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
