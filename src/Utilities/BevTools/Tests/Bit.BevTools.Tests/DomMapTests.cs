using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// Components mapped to the page and back (<see cref="BevDomMap"/>): the paths come from the renderer's own render trees
/// and must mirror how Blazor's browser renderer turns frames into logical DOM nodes.
/// </summary>
[TestClass]
public class DomMapTests : BunitContext
{
    public DomMapTests()
    {
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        Services.AddBitBevToolsServices(o => o.Enabled = true);
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private BevComponentRecord Record<T>(Func<T, bool>? where = null) where T : IComponent
    {
        Session.Components.Refresh(force: true);
        return Session.Components.All.Single(r => !r.IsDisposed && r.Component is T c && (where is null || where(c)));
    }

    private BevDomLocation Locate(BevComponentRecord record)
    {
        var location = BevDomMap.Locate(Session, record, out var error);
        Assert.IsNotNull(location, error);
        return location;
    }

    private BevDomOwner? Resolve(BevDomLocation root, IReadOnlyList<BevDomStep> path, bool includeHidden = false) =>
        BevDomMap.Resolve(Session, path, root.Anchored, root.RootSignature, root.RootOrdinal, root.RootCount, includeHidden, out _);

    [TestMethod]
    public void A_path_counts_one_logical_child_per_element_text_markup_and_component_and_none_for_regions_and_attributes()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 3));

        // <h1> markup is child 0 of the page, <main> child 1; inside <main>: the text, then three items from a region.
        var second = Locate(Record<DomMapItem>(i => i.Index == 1));
        CollectionAssert.AreEqual(new[] { new BevDomStep(1, "main"), new BevDomStep(2, "#c") }, second.Path.TakeLast(2).ToArray());

        // The footer follows the region's three items: child 4 of <main>, its leaf component its only child.
        var leaf = Locate(Record<DomMapLeaf>(l => l.Label == "footer"));
        CollectionAssert.AreEqual(new[] { new BevDomStep(1, "main"), new BevDomStep(4, "footer"), new BevDomStep(0, "#c") }, leaf.Path.TakeLast(3).ToArray());
        Assert.AreEqual("DomMapLeaf", leaf.Name);
    }

    [TestMethod]
    public void Every_component_resolves_back_from_its_own_path()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 4));
        Session.Components.Refresh(force: true);
        var records = Session.Components.All.Where(r => !r.IsDisposed && !r.IsHidden && r.Type.Namespace == typeof(DomMapPage).Namespace).ToList();
        Assert.IsTrue(records.Count >= 9, "the page, four items, four item leaves and the footer leaf");

        foreach (var record in records)
        {
            var location = Locate(record);
            var owner = Resolve(location, location.Path);
            Assert.IsNotNull(owner, record.ToString());
            Assert.AreSame(record, owner.Record, record.ToString());
            Assert.AreEqual(location.Path.Count, owner.Depth, "the owner's container is the whole path");
        }
    }

    [TestMethod]
    public void A_node_inside_a_component_resolves_to_it_with_the_element_named()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 2));
        var item = Record<DomMapItem>(i => i.Index == 0);
        var location = Locate(item);

        // <li class="item">: its text node, then the leaf component inside it.
        var owner = Resolve(location, [.. location.Path, new BevDomStep(0, "li"), new BevDomStep(0, "#text")]);
        Assert.IsNotNull(owner);
        Assert.AreSame(item, owner.Record);
        Assert.AreEqual("<li.item>", owner.Element);
        Assert.AreEqual(location.Path.Count, owner.Depth);

        var button = Resolve(location, [.. location.Path, new BevDomStep(0, "li"), new BevDomStep(1, "#c"), new BevDomStep(0, "button")]);
        Assert.IsNotNull(button);
        Assert.AreSame(Record<DomMapLeaf>(l => l.Label == "item-0"), button.Record);
        Assert.AreEqual("<button#leaf-item-0>", button.Element);
        Assert.AreEqual(location.Path.Count + 2, button.Depth);
    }

    [TestMethod]
    public void Nodes_parsed_from_markup_belong_to_the_component_that_rendered_the_markup()
    {
        Render<DomMapPage>();
        var page = Record<DomMapPage>();
        var location = Locate(page);
        var owner = Resolve(location, [.. location.Path, new BevDomStep(0, "#c"), new BevDomStep(0, "h1")]);
        Assert.IsNotNull(owner);
        Assert.AreSame(page, owner.Record);
        Assert.IsTrue(owner.InMarkup);
    }

    [TestMethod]
    public void A_path_that_does_not_match_the_latest_render_is_reported_not_guessed()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 2));
        var location = Locate(Record<DomMapPage>());

        // <main> is child 1, not 0, and it is not a <section>; and child 9 does not exist.
        Assert.IsNull(BevDomMap.Resolve(Session, [.. location.Path, new BevDomStep(0, "main")], false, location.RootSignature, 0, 1, false, out var error));
        StringAssert.Contains(error, "does not match");
        Assert.IsNull(Resolve(location, [.. location.Path, new BevDomStep(1, "section")]));
        Assert.IsNull(Resolve(location, [.. location.Path, new BevDomStep(9, "div")]));
    }

    [TestMethod]
    public void Keyed_items_keep_mapping_to_their_own_component_after_reordering()
    {
        var cut = Render<DomMapPage>(ps => ps.Add(p => p.Items, 3));
        var last = Record<DomMapItem>(i => i.Index == 2);
        cut.Render(ps => ps.Add(p => p.Reverse, true));

        // Reversed: item 2 is now the first child after the text.
        var location = Locate(last);
        Assert.AreEqual(new BevDomStep(1, "#c"), location.Path[^1]);
        Assert.AreSame(last, Resolve(location, location.Path)!.Record);
    }

    [TestMethod]
    public void Framework_components_resolve_to_the_nearest_application_component_unless_internals_are_shown()
    {
        Render<DomMapCascading>();
        var page = Record<DomMapCascading>();
        var location = Locate(page);

        // <section> is rendered by CascadingValue's child content: the frames are CascadingValue's.
        BevDomStep[] path = [.. location.Path, new BevDomStep(0, "#c"), new BevDomStep(0, "section")];
        var owner = Resolve(location, path);
        Assert.IsNotNull(owner);
        Assert.AreSame(page, owner.Record);
        StringAssert.StartsWith(owner.RenderedBy, "CascadingValue");
        Assert.AreEqual(location.Path.Count, owner.Depth, "the outline is the page's, not the CascadingValue's");

        var hidden = Resolve(location, path, includeHidden: true);
        Assert.IsNotNull(hidden);
        Assert.IsTrue(hidden.Record.IsHidden);
        Assert.IsNull(hidden.RenderedBy);
        Assert.AreEqual(location.Path.Count + 1, hidden.Depth);
    }

    [TestMethod]
    public void The_root_the_panel_renders_in_is_the_anchor()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 1));
        var footer = Record<DomMapLeaf>(l => l.Label == "footer");
        Assert.IsFalse(Locate(footer).Anchored, "no panel: roots are found by signature");

        Session.PanelComponent = footer.Component;
        var location = Locate(footer);
        Assert.IsTrue(location.Anchored);
        Assert.AreSame(footer, BevDomMap.Resolve(Session, location.Path, anchored: true, null, 0, 1, false, out _)!.Record);
        Assert.IsNull(BevDomMap.Resolve(Session, location.Path, anchored: false, location.RootSignature, 0, 1, false, out var error), "the anchor root is not a candidate for unanchored paths");
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void A_disposed_component_cannot_be_located()
    {
        var cut = Render<DomMapPage>(ps => ps.Add(p => p.Items, 2));
        var item = Record<DomMapItem>(i => i.Index == 1);
        cut.Render(ps => ps.Add(p => p.Items, 1));
        Session.Components.Refresh(force: true);

        Assert.IsTrue(item.IsDisposed);
        Assert.IsNull(BevDomMap.Locate(Session, item, out var error));
        StringAssert.Contains(error, "disposed");
    }

    [TestMethod]
    public void The_protocol_locates_and_resolves_with_the_same_answers()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 2));
        var leaf = Record<DomMapLeaf>(l => l.Label == "item-1");

        using var located = JsonDocument.Parse(BevToolsProtocol.Handle(Session, "locate", JsonSerializer.Serialize(new { instanceId = leaf.InstanceId })));
        var root = located.RootElement;
        Assert.IsFalse(root.TryGetProperty("error", out _), root.ToString());
        Assert.AreEqual(leaf.InstanceId, root.GetProperty("instanceId").GetInt64());
        var path = root.GetProperty("path").EnumerateArray().Select(s => new { index = s.GetProperty("index").GetInt32(), kind = s.GetProperty("kind").GetString() }).ToArray();
        Assert.AreEqual("#c", path[^1].kind);

        var request = JsonSerializer.Serialize(new { path, anchored = false, signature = root.GetProperty("rootSignature").GetString(), ordinal = 0 });
        using var resolved = JsonDocument.Parse(BevToolsProtocol.Handle(Session, "resolve", request));
        Assert.AreEqual(leaf.InstanceId, resolved.RootElement.GetProperty("instanceId").GetInt64(), resolved.RootElement.ToString());
        Assert.AreEqual(path.Length, resolved.RootElement.GetProperty("depth").GetInt32());

        using var malformed = JsonDocument.Parse(BevToolsProtocol.Handle(Session, "resolve", "{\"path\":[{\"index\":-1,\"kind\":\"div\"}]}"));
        Assert.IsTrue(malformed.RootElement.TryGetProperty("error", out _));
    }

    [TestMethod]
    public void Reference_captures_named_events_markup_in_elements_and_svg_map_as_the_browser_inserts_them()
    {
        // @ref on an element and on a component, @formname, a markup block inside an element and SVG children: each
        // either is one logical node (element, markup, component) or none (captures, named events), as insertFrame does.
        Render<DomMapFrames>();
        var frames = Record<DomMapFrames>();
        var leaf = Record<DomMapLeaf>(l => l.Label == "captured");
        var location = Locate(leaf);

        // <form>: @formname and the element capture add nothing; <em> markup is child 0, the captured leaf child 1.
        CollectionAssert.AreEqual(new[] { new BevDomStep(0, "form"), new BevDomStep(1, "#c") }, location.Path.TakeLast(2).ToArray());
        Assert.AreSame(leaf, Resolve(location, location.Path)!.Record);

        var root = Locate(frames);
        var markup = Resolve(root, [.. root.Path, new BevDomStep(0, "form"), new BevDomStep(0, "#c"), new BevDomStep(0, "em")]);
        Assert.IsTrue(markup!.InMarkup);
        Assert.AreSame(frames, markup.Record);

        var circle = Resolve(root, [.. root.Path, new BevDomStep(1, "svg"), new BevDomStep(0, "g"), new BevDomStep(0, "circle")]);
        Assert.AreSame(frames, circle!.Record);
        Assert.AreEqual("<circle#dot>", circle.Element);
        Assert.AreEqual("<foreignObject>", Resolve(root, [.. root.Path, new BevDomStep(1, "svg"), new BevDomStep(1, "foreignObject")])!.Element, "SVG tag names keep their case; matching ignores it");
    }

    [TestMethod]
    public void Identical_roots_are_matched_by_position_only_when_the_page_holds_exactly_this_runtimes()
    {
        Render<DomMapLeaf>(ps => ps.Add(p => p.Label, "twin"));
        Render<DomMapLeaf>(ps => ps.Add(p => p.Label, "twin"));
        Session.Components.Refresh(force: true);
        var twins = Session.Components.All.Where(r => r.Component is DomMapLeaf { Label: "twin" }).OrderBy(r => r.ComponentId).ToList();
        var second = Locate(twins[1]);
        Assert.AreEqual(2, second.RootCount);
        Assert.AreEqual(1, second.RootOrdinal);
        Assert.IsTrue(second.RootAmbiguous);

        // The page holds exactly these two: the second one in the page is the second one here, and the answer says the
        // match was by position.
        var owner = BevDomMap.Resolve(Session, second.Path, false, second.RootSignature, 1, sameCount: 2, false, out _);
        Assert.AreSame(twins[1], owner!.Record);
        Assert.IsTrue(owner.Ambiguous);

        // A third identical root in the page belongs to another runtime: position means nothing, so no answer.
        Assert.IsNull(BevDomMap.Resolve(Session, second.Path, false, second.RootSignature, 1, sameCount: 3, false, out var error));
        StringAssert.Contains(error, "cannot tell which one");
    }

    [TestMethod]
    public void An_elements_description_never_reads_past_its_own_frames()
    {
        // A stale attribute frame right after an element's subtree - what a reused buffer holds - is not the element's.
        RenderTreeFrame[] frames = [.. new DomMapStaleFrames().Frames()];
        Assert.AreEqual("<p>", BevComponentWiringReader.DescribeElement(frames, 0));
    }

    [TestMethod]
    public void Mapping_stays_cheap_on_wide_and_deep_trees()
    {
        // Flash renders locates every component that rendered, up to 60 a tick: a list of a thousand rows must not
        // make that tick expensive, nor a recursive tree make one lookup slow.
        Render<WideTree>(ps => ps.Add(p => p.Count, 1000));
        Session.Components.Refresh(force: true);
        var rows = Session.Components.All.Where(r => r.Type == typeof(ChildComponent)).ToList();
        Assert.AreEqual(1000, rows.Count);

        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var row in rows)
        {
            Assert.IsNotNull(BevDomMap.Locate(Session, row, out _));
        }

        var wide = System.Diagnostics.Stopwatch.GetElapsedTime(start);
        Assert.IsTrue(wide.TotalMilliseconds < 1000, $"locating 1000 siblings took {wide.TotalMilliseconds:0} ms");

        Render<DeepTree>(ps => ps.Add(p => p.Depth, 500));
        Session.Components.Refresh(force: true);
        var deepest = Session.Components.All.Where(r => r.Type == typeof(DeepTree)).OrderByDescending(r => r.Depth).First();
        start = System.Diagnostics.Stopwatch.GetTimestamp();
        var location = Locate(deepest);
        var owner = Resolve(location, location.Path);
        var deep = System.Diagnostics.Stopwatch.GetElapsedTime(start);
        Assert.AreSame(deepest, owner!.Record);
        Assert.IsTrue(location.Path.Count >= 500);
        Assert.IsTrue(deep.TotalMilliseconds < 200, $"locating and resolving 500 levels deep took {deep.TotalMilliseconds:0} ms");
    }

    [TestMethod]
    public void A_batch_locates_exactly_what_single_lookups_do()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 5));
        Render<DomMapCascading>();
        Session.Components.Refresh(force: true);
        var batch = new BevDomMapBatch();
        foreach (var record in Session.Components.All.Where(r => !r.IsDisposed && r.Type.Namespace == typeof(DomMapPage).Namespace))
        {
            var single = BevDomMap.Locate(Session, record, out _);
            var batched = BevDomMap.Locate(Session, record, out _, batch);
            Assert.IsNotNull(single);
            Assert.IsNotNull(batched);
            CollectionAssert.AreEqual(single.Path.ToArray(), batched.Path.ToArray(), record.ToString());
            Assert.AreEqual(single with { Path = [] }, batched with { Path = [] });
        }

        Assert.IsTrue(batch.Children.Count > 0);
    }

    [TestMethod]
    public void Locating_is_counted_as_BevTools_overhead()
    {
        Render<DomMapPage>(ps => ps.Add(p => p.Items, 2));
        var before = Session.Overhead.PageMappingMs;
        for (var i = 0; i < 50; i++)
        {
            Locate(Record<DomMapLeaf>(l => l.Label == "footer"));
        }

        Assert.IsTrue(Session.Overhead.PageMappingMs > before);
    }
}

public class DomMapPage : ComponentBase
{
    [Parameter] public int Items { get; set; } = 3;

    [Parameter] public bool Reverse { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddMarkupContent(0, "<h1>Title</h1>");
        builder.OpenElement(1, "main");
        builder.AddAttribute(2, "class", "content");
        builder.AddContent(3, "intro");
        builder.OpenRegion(4);
        var indexes = Enumerable.Range(0, Items);
        foreach (var index in Reverse ? indexes.Reverse() : indexes)
        {
            builder.OpenComponent<DomMapItem>(0);
            builder.SetKey(index);
            builder.AddComponentParameter(1, nameof(DomMapItem.Index), index);
            builder.CloseComponent();
        }

        builder.CloseRegion();
        builder.OpenElement(5, "footer");
        builder.OpenComponent<DomMapLeaf>(6);
        builder.AddComponentParameter(7, nameof(DomMapLeaf.Label), "footer");
        builder.CloseComponent();
        builder.CloseElement();
        builder.CloseElement();
    }
}

public class DomMapItem : ComponentBase
{
    [Parameter] public int Index { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "li");
        builder.AddAttribute(1, "class", "item");
        builder.AddContent(2, "item " + Index);
        builder.OpenComponent<DomMapLeaf>(3);
        builder.AddComponentParameter(4, nameof(DomMapLeaf.Label), "item-" + Index);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public class DomMapLeaf : ComponentBase
{
    [Parameter] public string? Label { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "id", "leaf-" + Label);
        builder.AddContent(2, Label);
        builder.CloseElement();
    }
}

public class DomMapCascading : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<int>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<int>.Value), 1);
        builder.AddComponentParameter(2, nameof(CascadingValue<int>.ChildContent), (RenderFragment)(b =>
        {
            b.OpenElement(0, "section");
            b.AddContent(1, "inside");
            b.CloseElement();
        }));
        builder.CloseComponent();
    }
}

public class DomMapFrames : ComponentBase
{
    private ElementReference _form;
    private DomMapLeaf? _leaf;

    public ElementReference Form => _form;

    public DomMapLeaf? Leaf => _leaf;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "form");
        builder.AddAttribute(1, "method", "post");
        builder.AddNamedEvent("onsubmit", "frames-form");
        builder.AddElementReferenceCapture(2, reference => _form = reference);
        builder.AddMarkupContent(3, "<em>markup</em>");
        builder.OpenComponent<DomMapLeaf>(4);
        builder.AddComponentParameter(5, nameof(DomMapLeaf.Label), "captured");
        builder.AddComponentReferenceCapture(6, component => _leaf = (DomMapLeaf)component);
        builder.CloseComponent();
        builder.CloseElement();
        builder.OpenElement(7, "svg");
        builder.OpenElement(8, "g");
        builder.OpenElement(9, "circle");
        builder.AddAttribute(10, "id", "dot");
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenElement(11, "foreignObject");
        builder.CloseElement();
        builder.CloseElement();
    }
}

/// <summary>A render tree buffer reused the way the renderer reuses one: the frames of the previous tree stay past its end.</summary>
public class DomMapStaleFrames
{
    public RenderTreeFrame[] Frames()
    {
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "p");
        builder.AddAttribute(1, "id", "stale");
        builder.CloseElement();
        builder.Clear();
        builder.OpenElement(0, "p");
        builder.CloseElement();
        return builder.GetFrames().Array;
    }
}
