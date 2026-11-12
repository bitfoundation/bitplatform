namespace Bit.Bcrdt.Tests;

/// <summary>
/// <see cref="BcrdtXmlFragment"/>, <see cref="BcrdtXmlElement"/> and <see cref="BcrdtXmlText"/>: a tree whose
/// every part merges, travels with its names, and is ignored identically wherever a peer breaks its rules.
/// </summary>
[TestClass]
public class XmlTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private static BcrdtDocument Doc(ulong clientId, bool garbageCollect = true) =>
        new(new BcrdtDocumentOptions { ClientId = clientId, GarbageCollect = garbageCollect });

    private static void Sync(BcrdtDocument from, BcrdtDocument to) =>
        to.ApplyUpdate(from.EncodeStateAsUpdate(to.GetStateVector()), origin: "sync");

    private static void SyncAll(params BcrdtDocument[] docs)
    {
        foreach (BcrdtDocument source in docs)
        {
            foreach (BcrdtDocument target in docs)
            {
                if (!ReferenceEquals(source, target)) Sync(source, target);
            }
        }
    }

    private static BcrdtXmlElement Paragraph(string text)
    {
        var paragraph = new BcrdtXmlElement("p");
        paragraph.Push(new BcrdtXmlText(text));
        return paragraph;
    }

    /// <summary>A type claiming to be a node, which a tree must still refuse.</summary>
    private sealed class ForeignNode : IBcrdtXmlNode
    {
        public BcrdtXmlContainer? ParentNode => null;

        public string ToXmlString() => "<foreign/>";
    }

    // ---------------------------------------------------------------- the local surface

    [TestMethod]
    public void A_fragment_holds_elements_and_texts_in_order()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");

        var heading = new BcrdtXmlElement("h1");
        var text = new BcrdtXmlText("loose");
        tree.Push(heading, text);
        tree.Insert(1, Paragraph("one"), Paragraph("two"));

        Assert.AreEqual(4, tree.Count);
        Assert.AreSame(heading, tree[0]);
        Assert.AreSame(text, tree[3]);
        Assert.AreEqual("<h1></h1><p>one</p><p>two</p>loose", tree.ToXmlString());
        Assert.AreSequenceEqual(new object[] { heading, tree[1], tree[2], text }, tree.ToArray());

        tree.Delete(1, 2);
        Assert.AreEqual("<h1></h1>loose", tree.ToXmlString());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tree.Delete(1, 2));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tree.Insert(3, new BcrdtXmlText("x")));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = tree[2]);

        tree.Clear();
        Assert.AreEqual(0, tree.Count);
        Assert.AreEqual("", tree.ToXmlString());
        Assert.IsTrue(heading.IsDeleted);
    }

    [TestMethod]
    public void An_element_has_a_name_attributes_and_children()
    {
        using BcrdtDocument doc = Doc(1);
        var link = new BcrdtXmlElement("a");
        doc.GetXmlFragment("tree").Push(link);

        link.SetAttribute("href", "https://bitplatform.dev");
        link.SetAttribute("data-rank", 3);
        link.Push(new BcrdtXmlText("bit"));

        Assert.AreEqual("a", link.Name);
        Assert.AreEqual(BcrdtTypeKind.XmlElement, link.Kind);
        Assert.AreEqual("https://bitplatform.dev", link.GetAttribute("href"));
        Assert.AreEqual(3L, link.GetAttribute("data-rank"));
        Assert.IsTrue(link.HasAttribute("href"));
        Assert.AreSequenceEqual(new[] { "data-rank", "href" }, link.GetAttributes().Keys.ToArray());

        // Null is absence: an attribute that is present has a value.
        link.SetAttribute("data-rank", null);
        Assert.IsFalse(link.HasAttribute("data-rank"));
        link.RemoveAttribute("href");
        link.RemoveAttribute("never-set");
        Assert.IsEmpty(link.GetAttributes());
        Assert.AreEqual("<a>bit</a>", link.ToXmlString());
    }

    [TestMethod]
    public void Only_elements_and_texts_can_be_children()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");

        Assert.ThrowsExactly<ArgumentException>(() => tree.Push(new ForeignNode()));
        Assert.ThrowsExactly<ArgumentException>(() => tree.Push(new BcrdtXmlText("fine"), null!));
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtXmlElement("p").Push(new ForeignNode()));
        Assert.AreEqual(0, tree.Count, "nothing of a refused batch is inserted");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("1p")]
    [DataRow("-p")]
    [DataRow("a b")]
    [DataRow("<p>")]
    [DataRow("p\"")]
    [DataRow("p&q")]
    public void An_invalid_name_is_refused_for_elements_and_attributes(string name)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtXmlElement(name));

        using BcrdtDocument doc = Doc(1);
        var element = new BcrdtXmlElement("p");
        Assert.ThrowsExactly<ArgumentException>(() => element.SetAttribute(name, "v"));
        doc.GetXmlFragment("tree").Push(element);
        Assert.ThrowsExactly<ArgumentException>(() => element.SetAttribute(name, "v"));
    }

    [TestMethod]
    public void A_name_with_a_lone_surrogate_or_past_the_length_limit_is_refused()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtXmlElement("p" + (char)0xD800));
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtXmlElement(new string('a', BcrdtXmlElement.MaxNameLength + 1)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BcrdtXmlElement(null!));

        Assert.AreEqual(BcrdtXmlElement.MaxNameLength, new BcrdtXmlElement(new string('a', BcrdtXmlElement.MaxNameLength)).Name.Length);
    }

    [TestMethod]
    [DataRow("p")]
    [DataRow("_x")]
    [DataRow("svg:rect")]
    [DataRow("h-1.2")]
    [DataRow("café")]
    [DataRow("élément")]
    public void Valid_xml_names_are_accepted(string name)
    {
        using BcrdtDocument doc = Doc(1);
        var element = new BcrdtXmlElement(name);
        element.SetAttribute(name, name);
        doc.GetXmlFragment("tree").Push(element);

        Assert.AreEqual(name, element.Name);
        Assert.AreEqual(name, element.GetAttribute(name));
    }

    [TestMethod]
    public void A_name_outside_the_basic_plane_is_accepted()
    {
        // U+10000, written as its surrogate pair.
        string name = "x" + (char)0xD800 + (char)0xDC00;
        Assert.AreEqual(name, new BcrdtXmlElement(name).Name);
    }

    [TestMethod]
    public void An_attribute_holds_a_value_never_a_collaborative_type()
    {
        var element = new BcrdtXmlElement("p");

        Assert.ThrowsExactly<NotSupportedException>(() => element.SetAttribute("data", new BcrdtMap()));
        Assert.ThrowsExactly<NotSupportedException>(() => element.SetAttribute("data", new BcrdtXmlText("x")));
        Assert.ThrowsExactly<NotSupportedException>(() => element.SetAttribute("data", new object()));
        Assert.IsEmpty(element.GetAttributes());
    }

    [TestMethod]
    public void A_tree_built_detached_is_inserted_in_one_transaction()
    {
        var list = new BcrdtXmlElement("ul");
        list.SetAttribute("class", "tasks");
        var first = new BcrdtXmlElement("li");
        first.Push(new BcrdtXmlText("write"));
        var text = new BcrdtXmlText("test");
        var second = new BcrdtXmlElement("li");
        second.Push(text);
        list.Push(first, second);

        // Detached, the tree already answers for itself.
        Assert.AreEqual(2, list.Count);
        Assert.AreSame(list, first.ParentNode);
        Assert.AreSame(second, text.ParentNode);
        Assert.AreEqual("tasks", list.GetAttribute("class"));
        Assert.AreSequenceEqual(new object[] { first, first[0], second, text }, list.Descendants().ToArray());

        using BcrdtDocument doc = Doc(1);
        int updates = 0;
        doc.Updated += _ => updates++;
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        tree.Push(list);

        Assert.AreEqual(1, updates);
        Assert.IsTrue(text.IsAttached);
        Assert.AreSame(tree, list.ParentNode);
        Assert.AreSame(list, second.ParentNode);
        Assert.AreEqual("<ul class=\"tasks\"><li>write</li><li>test</li></ul>", tree.ToXmlString());

        text.Insert(4, "ed");
        Assert.AreEqual("<ul class=\"tasks\"><li>write</li><li>tested</li></ul>", tree.ToXmlString());
    }

    [TestMethod]
    public void A_node_lives_in_exactly_one_place()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        BcrdtXmlElement paragraph = Paragraph("once");
        tree.Push(paragraph);

        Assert.ThrowsExactly<InvalidOperationException>(() => tree.Push(paragraph));
        Assert.ThrowsExactly<InvalidOperationException>(() => doc.GetXmlFragment("other").Push(paragraph));

        var waiting = new BcrdtXmlText("waiting");
        var holder = new BcrdtXmlElement("div");
        holder.Push(waiting);
        Assert.ThrowsExactly<InvalidOperationException>(() => new BcrdtXmlElement("div").Push(waiting));
        Assert.ThrowsExactly<InvalidOperationException>(() => holder.Push(waiting));

        // A detached element cannot end up inside itself.
        var outer = new BcrdtXmlElement("section");
        var inner = new BcrdtXmlElement("div");
        outer.Push(inner);
        Assert.ThrowsExactly<InvalidOperationException>(() => inner.Push(outer));

        // A copy can go anywhere.
        tree.Push(paragraph.Clone());
        Assert.AreEqual("<p>once</p><p>once</p>", tree.ToXmlString());
    }

    [TestMethod]
    public void Removing_a_waiting_node_from_a_detached_container_frees_it()
    {
        var holder = new BcrdtXmlElement("div");
        var text = new BcrdtXmlText("free");
        holder.Push(text);
        holder.Delete(0);

        Assert.IsNull(text.ParentNode);
        using BcrdtDocument doc = Doc(1);
        doc.GetXmlFragment("tree").Push(text);
        Assert.AreEqual("free", doc.GetXmlFragment("tree").ToXmlString());
    }

    [TestMethod]
    public void Markup_is_escaped_and_attributes_render_in_ordinal_order()
    {
        using BcrdtDocument doc = Doc(1);
        var element = new BcrdtXmlElement("p");
        doc.GetXmlFragment("tree").Push(element);

        element.SetAttribute("title", "a \"quoted\" <b> & 'more'\n\tline");
        element.SetAttribute("b", true);
        element.SetAttribute("a", 1.5);
        element.SetAttribute("c", new List<object?> { 1, "x", null, new Dictionary<string, object?> { ["z"] = 1, ["y"] = false } });
        element.Push(new BcrdtXmlText("x < y && y > z\r\n" + (char)0x1 + (char)0xD800 + "!"));

        Assert.AreEqual(
            "<p a=\"1.5\" b=\"true\" c=\"[1,&quot;x&quot;,null,{&quot;y&quot;:false,&quot;z&quot;:1}]\" " +
            "title=\"a &quot;quoted&quot; &lt;b&gt; &amp; 'more'&#10;&#9;line\">" +
            "x &lt; y &amp;&amp; y &gt; z&#13;\n" + (char)0xFFFD + (char)0xFFFD + "!</p>",
            element.ToXmlString());
    }

    [TestMethod]
    public void Text_renders_its_characters_without_marks_or_embeds()
    {
        using BcrdtDocument doc = Doc(1);
        var text = new BcrdtXmlText("bold move");
        doc.GetXmlFragment("tree").Push(text);
        text.Format(0, 4, new Dictionary<string, object?> { ["bold"] = true });
        text.InsertEmbed(4, new Dictionary<string, object?> { ["image"] = "a.png" });

        Assert.AreEqual("bold move", text.ToXmlString());
        Assert.AreEqual(10, text.Count);
    }

    [TestMethod]
    public void The_projection_is_deterministic_and_describes_the_whole_tree()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var heading = new BcrdtXmlElement("h1");
        heading.SetAttribute("level", 1);
        heading.SetAttribute("class", "title");
        heading.Push(new BcrdtXmlText("Title"));
        tree.Push(heading, new BcrdtXmlText("tail"));

        var projection = (List<object?>)tree.ToJson();
        var element = (SortedDictionary<string, object?>)projection[0]!;

        Assert.AreSequenceEqual(new[] { "attributes", "children", "name" }, element.Keys.ToArray());
        Assert.AreEqual("h1", element["name"]);
        Assert.AreSequenceEqual(new[] { "class", "level" }, ((IReadOnlyDictionary<string, object?>)element["attributes"]!).Keys.ToArray());
        Assert.AreSequenceEqual(new object[] { "Title" }, (List<object?>)element["children"]!);
        Assert.AreEqual("tail", projection[1]);
    }

    [TestMethod]
    public void A_clone_is_a_detached_deep_copy()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var section = new BcrdtXmlElement("section");
        section.SetAttribute("id", "s1");
        section.Push(Paragraph("body"));
        tree.Push(section);
        ((BcrdtXmlText)((BcrdtXmlElement)section[0])[0]).Format(0, 4, new Dictionary<string, object?> { ["italic"] = true });

        BcrdtXmlElement copy = section.Clone();
        Assert.IsFalse(copy.IsAttached);
        Assert.AreEqual("section", copy.Name);
        Assert.AreEqual("s1", copy.GetAttribute("id"));

        tree.Push(copy);
        section.SetAttribute("id", "changed");
        Assert.AreEqual("<section id=\"changed\"><p>body</p></section><section id=\"s1\"><p>body</p></section>", tree.ToXmlString());

        var copiedText = (BcrdtXmlText)((BcrdtXmlElement)copy[0])[0];
        Assert.IsTrue((bool?)copiedText.ToDelta()[0].Attributes!["italic"]);

        BcrdtXmlFragment fragmentCopy = tree.Clone();
        Assert.AreEqual(2, fragmentCopy.Count);
        doc.GetMap("archive").Set("v1", fragmentCopy);
        Assert.AreEqual(tree.ToXmlString(), doc.GetMap("archive").GetXmlFragment("v1")!.ToXmlString());
    }

    [TestMethod]
    public void Descendants_walk_the_tree_in_document_order()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var list = new BcrdtXmlElement("ul");
        list.Push(Paragraph("a"), Paragraph("b"));
        tree.Push(new BcrdtXmlElement("h1"), list, new BcrdtXmlText("end"));

        string[] order =
        [
            .. tree.Descendants().Select(node => node is BcrdtXmlElement element ? element.Name : node.ToXmlString()),
        ];

        Assert.AreSequenceEqual(new[] { "h1", "ul", "p", "a", "p", "b", "end" }, order);
        Assert.AreEqual(2, tree.Descendants().OfType<BcrdtXmlElement>().Count(element => element.Name == "p"));
    }

    [TestMethod]
    public void A_text_in_a_tree_keeps_its_rich_text_behaviour()
    {
        using BcrdtDocument doc = Doc(1);
        var text = new BcrdtXmlText();
        doc.GetXmlFragment("tree").Push(Paragraph("unused"), text);

        text.ApplyDelta([BcrdtDeltaOperation.InsertText("hello world")]);
        text.Format(6, 5, new Dictionary<string, object?> { ["bold"] = true });

        IReadOnlyList<BcrdtDeltaOperation> delta = text.ToDelta();
        Assert.HasCount(2, delta);
        Assert.AreEqual("world", delta[1].Content);
        Assert.IsTrue((bool?)delta[1].Attributes!["bold"]);
        Assert.IsInstanceOfType<BcrdtText>(text);
        Assert.AreEqual(BcrdtTypeKind.XmlText, text.Kind);
        Assert.AreSame(doc.GetXmlFragment("tree"), text.ParentNode);
    }

    [TestMethod]
    public void Container_events_report_child_and_attribute_changes()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var element = new BcrdtXmlElement("p");
        tree.Push(new BcrdtXmlText("first"), element);

        List<string> seen = [];
        using IDisposable treeSubscription = tree.Observe((e, _) =>
        {
            var xml = (BcrdtXmlEvent)e;
            seen.Add($"tree children={xml.ChildListChanged} delta={string.Join(",", xml.Delta.Select(op => $"{op.Kind}:{op.Length}"))}");
        });
        using IDisposable elementSubscription = element.Observe((e, _) =>
        {
            var xml = (BcrdtXmlEvent)e;
            seen.Add($"p children={xml.ChildListChanged} attributes={string.Join(",", xml.AttributesChanged)}");
        });

        doc.Transact(_ =>
        {
            tree.Insert(1, new BcrdtXmlText("second"));
            tree.Delete(0);
        });
        element.SetAttribute("class", "lead");
        element.Push(new BcrdtXmlText("child"));

        Assert.AreSequenceEqual(
            new[]
            {
                "tree children=True delta=Delete:1,Insert:1",
                "p children=False attributes=class",
                "p children=True attributes=",
            }, seen);
    }

    [TestMethod]
    public void A_deep_observer_sees_edits_anywhere_in_the_tree_with_their_path()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var text = new BcrdtXmlText("abc");
        var paragraph = new BcrdtXmlElement("p");
        paragraph.Push(text);
        tree.Push(new BcrdtXmlElement("h1"), paragraph);

        List<string> paths = [];
        using IDisposable subscription = tree.ObserveDeep((events, _) =>
        {
            foreach (BcrdtEvent e in events) paths.Add($"{e.GetType().Name}@{string.Join("/", e.Path)}");
        });

        text.Insert(3, "d");

        Assert.AreSequenceEqual(new[] { "BcrdtTextEvent@1/0" }, paths);
    }

    [TestMethod]
    public void A_deep_event_after_an_ignored_entry_gives_the_child_index_the_tree_uses()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var text = new BcrdtXmlText("abc");
        var paragraph = new BcrdtXmlElement("p");
        paragraph.Push(text);
        tree.Push(new BcrdtXmlElement("h1"), paragraph);

        // What a misbehaving peer could put between the two children: plain values the tree does not count.
        doc.Transact(transaction => BcrdtTypeOperations.ListInsert(transaction, tree, 1, ["loose", 42L]));
        Assert.AreSame(paragraph, tree[1]);

        List<string> paths = [];
        using IDisposable subscription = tree.ObserveDeep((events, _) =>
        {
            foreach (BcrdtEvent e in events) paths.Add(string.Join("/", e.Path));
        });

        text.Insert(3, "d");

        Assert.AreSequenceEqual(new[] { "1/0" }, paths);
    }

    [TestMethod]
    public void A_fragment_nests_inside_a_map()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtMap notes = doc.GetMap("notes");
        BcrdtXmlFragment body = notes.EnsureXmlFragment("n1");
        body.Push(Paragraph("note"));

        Assert.AreSame(body, notes.EnsureXmlFragment("n1"));
        Assert.AreSame(body, notes.GetXmlFragment("n1"));
        Assert.IsNull(body.ContainingItem!.Parent as BcrdtXmlContainer);
        Assert.AreEqual("<p>note</p>", body.ToXmlString());
    }

    [TestMethod]
    public async Task The_collaborative_document_exposes_root_fragments()
    {
        await using var service = new BcrdtCollaborationService(new BcrdtCollaborationOptions { CreateUndoManager = false });
        BcrdtCollaborativeDocument document = await service.OpenAsync("xml", cancellationToken: TestContext.CancellationToken);

        document.XmlFragment("body").Push(Paragraph("hi"));

        Assert.AreEqual("<p>hi</p>", document.Crdt.GetXmlFragment("body").ToXmlString());
    }

    [TestMethod]
    public void The_type_factory_creates_fragments_and_texts_but_needs_a_name_for_an_element()
    {
        Assert.IsInstanceOfType<BcrdtXmlFragment>(BcrdtTypeFactory.Create(BcrdtTypeKind.XmlFragment));
        Assert.IsInstanceOfType<BcrdtXmlText>(BcrdtTypeFactory.Create(BcrdtTypeKind.XmlText));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtTypeFactory.Create(BcrdtTypeKind.XmlElement));
    }

    [TestMethod]
    public void Nesting_elements_past_the_limit_is_refused()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlContainer container = doc.GetXmlFragment("tree");

        // The fragment is the root, at depth zero; an element inside it is at one.
        for (int depth = 1; depth <= BcrdtAbstractType.MaxNestingDepth; depth++)
        {
            var element = new BcrdtXmlElement("div");
            container.Push(element);
            container = element;
        }

        Assert.ThrowsExactly<InvalidOperationException>(() => container.Push(new BcrdtXmlElement("div")));
        Assert.ThrowsExactly<InvalidOperationException>(() => container.Push(new BcrdtXmlText("deep")));

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
        Assert.HasCount(BcrdtAbstractType.MaxNestingDepth, peer.GetXmlFragment("tree").Descendants());
        BcrdtConvergenceHarness.AssertConverged(doc, peer);
    }

    [TestMethod]
    public void A_detached_tree_too_deep_for_where_it_goes_is_refused_before_any_of_it_is_placed()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlContainer container = doc.GetXmlFragment("tree");
        var levels = new List<BcrdtXmlContainer>();
        for (int depth = 1; depth <= BcrdtAbstractType.MaxNestingDepth - 2; depth++)
        {
            var element = new BcrdtXmlElement("div");
            container.Push(element);
            levels.Add(container = element);
        }

        // Four levels: three elements and a text. Under the deepest element, at 126, the text would be at 130.
        var detached = new BcrdtXmlElement("section");
        var inner = new BcrdtXmlElement("p");
        var innermost = new BcrdtXmlElement("em");
        innermost.Push(new BcrdtXmlText("deep"));
        inner.Push(innermost);
        detached.Push(inner);

        long clock = doc.GetStateVector().Get(doc.ClientId);
        string markup = detached.ToXmlString();
        Assert.ThrowsExactly<InvalidOperationException>(() => container.Push(detached));
        Assert.AreEqual(clock, doc.GetStateVector().Get(doc.ClientId), "nothing of it was placed");
        Assert.AreEqual(0, container.Count);
        Assert.IsFalse(detached.IsAttached);
        Assert.AreEqual(markup, detached.ToXmlString(), "and it is intact");
        Assert.AreEqual(1, innermost.Count);

        // Two levels up, at 124, the text lands at 128: as deep as a type goes.
        levels[^3].Push(detached);
        Assert.IsTrue(detached.IsAttached);
        Assert.AreEqual("deep", innermost.ToArray().OfType<BcrdtXmlText>().Single().ToString());
    }

    // ---------------------------------------------------------------- the wire

    [TestMethod]
    public void A_tree_round_trips_with_its_names_attributes_kinds_and_marks()
    {
        using BcrdtDocument author = Doc(1);
        BcrdtXmlFragment tree = author.GetXmlFragment("tree");
        var heading = new BcrdtXmlElement("h2");
        heading.SetAttribute("id", "intro");
        heading.SetAttribute("data-order", new List<object?> { 1, 2 });
        var text = new BcrdtXmlText("Intro");
        heading.Push(text);
        tree.Push(heading, new BcrdtXmlText("tail"));
        text.Format(0, 5, new Dictionary<string, object?> { ["bold"] = true });
        author.GetMap("meta").Set("aside", Paragraph("aside"));

        using BcrdtDocument reader = Doc(2);
        reader.ApplyUpdate(author.EncodeStateAsUpdate());

        BcrdtXmlFragment received = reader.GetXmlFragment("tree");
        var receivedHeading = (BcrdtXmlElement)received[0];
        Assert.AreEqual("h2", receivedHeading.Name);
        Assert.AreEqual("intro", receivedHeading.GetAttribute("id"));
        Assert.IsInstanceOfType<BcrdtXmlText>(receivedHeading[0]);
        Assert.IsInstanceOfType<BcrdtXmlText>(received[1]);
        Assert.AreEqual(tree.ToXmlString(), received.ToXmlString());
        Assert.IsTrue((bool?)((BcrdtXmlText)receivedHeading[0]).ToDelta()[0].Attributes!["bold"]);
        Assert.AreEqual("<p>aside</p>", reader.GetMap("meta").Get<BcrdtXmlElement>("aside")!.ToXmlString());
        BcrdtConvergenceHarness.AssertConverged(author, reader);
    }

    [TestMethod]
    public void Concurrent_edits_to_one_tree_all_survive()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        var shared = new BcrdtXmlElement("p");
        shared.Push(new BcrdtXmlText("shared"));
        a.GetXmlFragment("tree").Push(shared);
        SyncAll(a, b);

        var mine = (BcrdtXmlElement)a.GetXmlFragment("tree")[0];
        var theirs = (BcrdtXmlElement)b.GetXmlFragment("tree")[0];

        a.GetXmlFragment("tree").Insert(0, Paragraph("from a"));
        b.GetXmlFragment("tree").Insert(0, Paragraph("from b"));
        mine.SetAttribute("class", "a");
        theirs.SetAttribute("id", "b");
        ((BcrdtXmlText)mine[0]).Insert(0, "A:");
        ((BcrdtXmlText)theirs[0]).Insert(6, ":B");

        SyncAll(a, b);

        string markup = a.GetXmlFragment("tree").ToXmlString();
        Assert.AreEqual(markup, b.GetXmlFragment("tree").ToXmlString());
        Assert.AreEqual(3, a.GetXmlFragment("tree").Count);
        Assert.Contains("<p>from a</p>", markup);
        Assert.Contains("<p>from b</p>", markup);
        Assert.EndsWith("<p class=\"a\" id=\"b\">A:shared:B</p>", markup);
    }

    [TestMethod]
    public void A_tree_that_arrives_before_its_root_is_declared_is_adopted()
    {
        using BcrdtDocument author = Doc(1);
        author.GetXmlFragment("tree").Push(Paragraph("early"));

        using BcrdtDocument reader = Doc(2);
        reader.ApplyUpdate(author.EncodeStateAsUpdate());
        Assert.IsInstanceOfType<BcrdtUntypedType>(reader.RootTypes["tree"]);

        BcrdtXmlFragment adopted = reader.GetXmlFragment("tree");
        Assert.AreEqual("<p>early</p>", adopted.ToXmlString());
        Assert.AreSame(adopted, ((BcrdtXmlElement)adopted[0]).ParentNode);

        adopted.Push(Paragraph("late"));
        Sync(reader, author);
        Assert.AreEqual("<p>early</p><p>late</p>", author.GetXmlFragment("tree").ToXmlString());
    }

    /// <summary>
    /// An update holding one element, made by client 9 at the root <c>tree</c>, whose name is written as
    /// given - which no replica could produce.
    /// </summary>
    private static byte[] ElementUpdate(string name) => HandWrittenUpdates.Build((ref BcrdtBufferWriter writer) =>
    {
        writer.WriteVarUInt(1);                              // one client
        writer.WriteVarUInt(1);                              // one block
        writer.WriteVarUInt(9);                              // client
        writer.WriteVarUInt(0);                              // first clock
        writer.WriteByte((byte)BcrdtContentRef.Type);        // no origins, no key
        writer.WriteByte(1);                                 // the parent is a root, by name
        writer.WriteString("tree");
        writer.WriteVarUInt((ulong)BcrdtTypeKind.XmlElement);
        writer.WriteString(name);
        writer.WriteVarUInt(0);                              // no deletions
    });

    [TestMethod]
    public void A_hand_written_element_update_is_accepted_when_its_name_is_valid()
    {
        // The control for the hostile cases below: the same bytes with a good name apply.
        using BcrdtDocument doc = Doc(1);
        doc.ApplyUpdate(ElementUpdate("p"));
        Assert.AreEqual("<p></p>", doc.GetXmlFragment("tree").ToXmlString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("1p")]
    [DataRow("a b")]
    [DataRow("p><script")]
    public void A_peer_element_with_an_invalid_name_is_rejected_and_the_document_is_untouched(string name)
    {
        AssertRejectedLeavingUntouched(ElementUpdate(name));
    }

    [TestMethod]
    public void A_peer_element_with_a_name_past_the_limit_or_a_lone_surrogate_is_rejected()
    {
        AssertRejectedLeavingUntouched(ElementUpdate(new string('a', BcrdtXmlElement.MaxNameLength + 1)));
        AssertRejectedLeavingUntouched(ElementUpdate("p" + (char)0xDC00));
    }

    [TestMethod]
    public void One_invalid_name_rejects_the_whole_update()
    {
        // A real update with two elements, the second renamed in the bytes to something invalid: the
        // valid first element must not be applied either.
        using BcrdtDocument author = Doc(1);
        author.GetXmlFragment("tree").Push(new BcrdtXmlElement("valid"), new BcrdtXmlElement("zqzqz"));
        byte[] update = author.EncodeStateAsUpdate();

        byte[] marker = System.Text.Encoding.UTF8.GetBytes("zqzqz");
        int at = update.AsSpan().IndexOf(marker);
        Assert.IsGreaterThanOrEqualTo(0, at);
        update[at] = (byte)'1';

        AssertRejectedLeavingUntouched(update);
    }

    private static void AssertRejectedLeavingUntouched(byte[] update)
    {
        using BcrdtDocument doc = Doc(1);
        doc.GetXmlFragment("tree").Push(Paragraph("mine"));
        string before = BcrdtConvergenceHarness.Describe(doc);
        BcrdtStateVector stateVector = doc.GetStateVector();
        int updates = 0;
        doc.Updated += _ => updates++;

        Assert.ThrowsExactly<BcrdtFormatException>(() => doc.ApplyUpdate(update));

        Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(doc));
        Assert.IsTrue(stateVector.StructurallyEquals(doc.GetStateVector()));
        Assert.IsFalse(doc.HasPendingUpdates);
        Assert.AreEqual(0, updates);

        // Still a working replica.
        doc.GetXmlFragment("tree").Push(Paragraph("after"));
        Assert.AreEqual("<p>mine</p><p>after</p>", doc.GetXmlFragment("tree").ToXmlString());
    }

    [TestMethod]
    [DataRow(9UL)]
    [DataRow(200UL)]
    public void A_nested_type_kind_past_the_tree_kinds_is_rejected(ulong kind)
    {
        byte[] payload = HandWrittenUpdates.Build((ref BcrdtBufferWriter writer) => writer.WriteVarUInt(kind));
        var reader = new BcrdtBufferReader(payload);

        try
        {
            BcrdtContentType.Read(ref reader);
            Assert.Fail($"Kind {kind} was accepted.");
        }
        catch (BcrdtFormatException)
        {
        }
    }

    [TestMethod]
    public void Content_a_peer_puts_in_a_tree_against_its_rules_is_ignored_identically_everywhere()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtXmlFragment tree = a.GetXmlFragment("tree");
        var element = new BcrdtXmlElement("p");
        tree.Push(new BcrdtXmlText("kept"), element);

        // What a misbehaving peer could write through the generic surface: plain values, a plain text
        // and a map among the children, and attributes that are not attributes.
        a.Transact(transaction =>
        {
            BcrdtTypeOperations.ListInsert(transaction, tree, 1, ["value", 42L, new BcrdtText("plain"), new BcrdtMap()]);
            BcrdtTypeOperations.MapSet(transaction, element, "not a name", "x");
            BcrdtTypeOperations.MapSet(transaction, element, "nested", new BcrdtMap());
            BcrdtTypeOperations.MapSet(transaction, element, "empty", null);
            BcrdtTypeOperations.MapSet(transaction, element, "real", "yes");
        });

        Sync(a, b);
        BcrdtXmlFragment received = b.GetXmlFragment("tree");

        foreach (BcrdtXmlFragment side in new[] { tree, received })
        {
            Assert.AreEqual(2, side.Count);
            Assert.AreEqual("<p real=\"yes\"></p>", side[1].ToXmlString());
            Assert.AreEqual("kept<p real=\"yes\"></p>", side.ToXmlString());
            Assert.AreSequenceEqual(new[] { "real" }, ((BcrdtXmlElement)side[1]).GetAttributes().Keys.ToArray());
            Assert.IsNull(((BcrdtXmlElement)side[1]).GetAttribute("nested"));
        }

        // Indices go around what is ignored: inserting at 1 lands between the two real children.
        List<string> deltas = [];
        using IDisposable subscription = received.Observe((e, _) =>
            deltas.Add(string.Join(",", e.Delta.Select(op => $"{op.Kind}:{op.Length}"))));
        received.Insert(1, new BcrdtXmlText("between"));
        Sync(b, a);

        Assert.AreEqual("keptbetween<p real=\"yes\"></p>", tree.ToXmlString());
        Assert.AreEqual(tree.ToXmlString(), received.ToXmlString());
        Assert.AreSequenceEqual(new[] { "Retain:1,Insert:1" }, deltas);

        tree.Delete(0, 3);
        Sync(a, b);
        Assert.AreEqual(0, received.Count);
        BcrdtConvergenceHarness.AssertStructurallyIdentical(a, b);
    }

    [TestMethod]
    public void A_root_used_as_another_kind_by_a_peer_shows_only_its_nodes_in_the_tree()
    {
        using BcrdtDocument arrayUser = Doc(1);
        using BcrdtDocument treeUser = Doc(2);
        arrayUser.GetArray("tree").Push("loose", new BcrdtMap());
        treeUser.GetXmlFragment("tree").Push(Paragraph("node"));

        SyncAll(arrayUser, treeUser);

        Assert.AreEqual("<p>node</p>", treeUser.GetXmlFragment("tree").ToXmlString());
        Assert.AreEqual(3, arrayUser.GetArray("tree").Count);
    }

    // ---------------------------------------------------------------- undo

    [TestMethod]
    public void Undo_takes_back_an_inserted_element_and_redo_restores_it()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        using var undo = new BcrdtUndoManager(tree, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });

        tree.Push(Paragraph("one"));
        var two = new BcrdtXmlElement("p");
        two.SetAttribute("class", "x");
        two.Push(new BcrdtXmlText("two"));
        tree.Push(two);

        undo.Undo();
        Assert.AreEqual("<p>one</p>", tree.ToXmlString());

        undo.Redo();
        Assert.AreEqual("<p>one</p><p class=\"x\">two</p>", tree.ToXmlString());
    }

    [TestMethod]
    public void Undo_restores_an_attributes_previous_value()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtXmlFragment tree = doc.GetXmlFragment("tree");
        var element = new BcrdtXmlElement("p");
        element.SetAttribute("class", "before");
        tree.Push(element);

        using var undo = new BcrdtUndoManager(tree, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });
        element.SetAttribute("class", "after");
        element.RemoveAttribute("class");

        undo.Undo();
        Assert.AreEqual("after", element.GetAttribute("class"));
        undo.Undo();
        Assert.AreEqual("before", element.GetAttribute("class"));
        undo.Redo();
        Assert.AreEqual("after", element.GetAttribute("class"));
    }

    [TestMethod]
    public void Undo_takes_back_only_this_replicas_text_edit_inside_the_tree()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        a.GetXmlFragment("tree").Push(Paragraph("base"));
        SyncAll(a, b);

        BcrdtXmlFragment tree = a.GetXmlFragment("tree");
        using var undo = new BcrdtUndoManager(tree, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });
        var mine = (BcrdtXmlText)((BcrdtXmlElement)tree[0])[0];
        mine.Insert(4, " mine");

        var theirs = (BcrdtXmlText)((BcrdtXmlElement)b.GetXmlFragment("tree")[0])[0];
        theirs.Insert(0, "theirs ");
        Sync(b, a);

        Assert.AreEqual("<p>theirs base mine</p>", tree.ToXmlString());
        undo.Undo();
        Assert.AreEqual("<p>theirs base</p>", tree.ToXmlString());

        SyncAll(a, b);
        Assert.AreEqual("<p>theirs base</p>", b.GetXmlFragment("tree").ToXmlString());
    }

    [TestMethod]
    public void Undo_brings_back_a_deleted_element_with_its_name_attributes_and_children()
    {
        using BcrdtDocument a = Doc(1, garbageCollect: true);
        using BcrdtDocument b = Doc(2);
        BcrdtXmlFragment tree = a.GetXmlFragment("tree");
        var element = new BcrdtXmlElement("blockquote");
        element.SetAttribute("cite", "source");
        element.Push(Paragraph("quoted"));
        tree.Push(element);

        using var undo = new BcrdtUndoManager(tree, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });
        tree.Delete(0);
        Assert.AreEqual("", tree.ToXmlString());

        undo.Undo();
        Assert.AreEqual("<blockquote cite=\"source\"><p>quoted</p></blockquote>", tree.ToXmlString());

        Sync(a, b);
        Assert.AreEqual(tree.ToXmlString(), b.GetXmlFragment("tree").ToXmlString());
    }

    // ---------------------------------------------------------------- randomized

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_tree_edits_converge_under_loss_duplication_and_partitions(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.25,
            DuplicateProbability = 0.25,
            MaxLatencySteps = 5,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var generator = new BcrdtOperationGenerator(seed, new BcrdtOperationGeneratorOptions
        {
            Text = false,
            Array = false,
            Map = false,
            Xml = true,
        });

        List<string> history = [];
        for (int round = 0; round < 40; round++)
        {
            if (round == 10) network.Partition(peers[0], peers[1]);
            if (round == 25) network.HealPartitions();

            foreach (BcrdtSimulatedPeer peer in peers)
            {
                history.Add($"{peer.Name}: {generator.ApplyRandomTransaction(peer.Doc)}");
            }
            network.Step();
        }

        network.Flush();
        network.SynchronizeAll();

        try
        {
            BcrdtConvergenceHarness.AssertStructurallyIdentical([.. peers.Select(peer => peer.Doc)]);
            string markup = peers[0].Doc.GetXmlFragment("tree").ToXmlString();
            foreach (BcrdtSimulatedPeer peer in peers)
            {
                Assert.AreEqual(markup, peer.Doc.GetXmlFragment("tree").ToXmlString(), $"{peer.Name} renders differently.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Seed {seed} diverged. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_tree_edits_mixed_with_every_other_type_converge(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var generator = new BcrdtOperationGenerator(seed, new BcrdtOperationGeneratorOptions { Set = true, Counter = true, Xml = true });

        List<string> history = [];
        for (int round = 0; round < 30; round++)
        {
            foreach (BcrdtSimulatedPeer peer in peers)
            {
                history.Add($"{peer.Name}: {generator.ApplyRandomTransaction(peer.Doc)}");
            }
            network.Step();
        }

        network.Flush();
        network.SynchronizeAll();

        try
        {
            BcrdtConvergenceHarness.AssertConverged(network);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Seed {seed} diverged. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_tree_edits_undo_without_diverging(int seed)
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        var generator = new BcrdtOperationGenerator(seed, new BcrdtOperationGeneratorOptions
        {
            Text = false,
            Array = false,
            Map = false,
            Xml = true,
        });
        using var undo = new BcrdtUndoManager(a.GetXmlFragment("tree"), new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero });
        var random = new Random(seed);

        List<string> history = [];
        for (int round = 0; round < 30; round++)
        {
            history.Add($"a: {generator.ApplyRandomTransaction(a)}");
            history.Add($"b: {generator.ApplyRandomTransaction(b)}");
            if (random.Next(3) == 0)
            {
                undo.Undo();
                history.Add("a: undo");
            }
            if (random.Next(5) == 0)
            {
                undo.Redo();
                history.Add("a: redo");
            }
            if (random.Next(2) == 0) SyncAll(a, b);
        }

        SyncAll(a, b);

        try
        {
            BcrdtConvergenceHarness.AssertStructurallyIdentical(a, b);
            Assert.AreEqual(a.GetXmlFragment("tree").ToXmlString(), b.GetXmlFragment("tree").ToXmlString());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Seed {seed} diverged. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
    }

    public TestContext TestContext { get; set; }
}
