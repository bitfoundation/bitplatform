using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

// Frame inspection intentionally uses the same framework-internal RenderTree APIs as the
// rewriter under test (see the note in BmotionChildContentRewriter).
#pragma warning disable BL0006

namespace Bit.Bmotion.Tests.Components;

[TestClass]
public class ChildContentRewriterTests
{
    private static BmotionInjection DefaultPlan(string tag, string? authorId, string? authorStyle)
        => new("bm-test", "opacity:0;", AddPathLength: false);

    /// <summary>Runs the rewriter over a fragment and returns the output builder's frames.</summary>
    private static (RenderTreeFrame[] Frames, int Count, bool Injected) Rewrite(
        RenderFragment content, BmotionInjectionPlanner? plan = null)
    {
        var output = new RenderTreeBuilder();
        var injected = BmotionChildContentRewriter.Render(output, content, plan ?? DefaultPlan);
        var range = output.GetFrames();
        return (range.Array, range.Count, injected);
    }

    private static Dictionary<string, object?> AttributesOf(RenderTreeFrame[] frames, int elementIndex)
    {
        var result = new Dictionary<string, object?>();
        var end = elementIndex + frames[elementIndex].ElementSubtreeLength;
        for (var i = elementIndex + 1; i < end && frames[i].FrameType == RenderTreeFrameType.Attribute; i++)
            result[frames[i].AttributeName] = frames[i].AttributeValue;
        return result;
    }

    // ── Injection ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Render_InjectsIdAndStyle_IntoFirstRootElement()
    {
        var (frames, _, injected) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "box");
            b.AddContent(2, "hi");
            b.CloseElement();
        });

        Assert.IsTrue(injected);
        Assert.AreEqual(RenderTreeFrameType.Element, frames[0].FrameType);
        var attrs = AttributesOf(frames, 0);
        Assert.AreEqual("box", attrs["class"]);
        Assert.AreEqual("bm-test", attrs["id"]);
        Assert.AreEqual("opacity:0;", attrs["style"]);
    }

    [TestMethod]
    public void Render_OnlyFirstRootElement_IsInjected()
    {
        var (frames, count, _) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.OpenElement(1, "span"); // child of target: untouched
            b.CloseElement();
            b.CloseElement();
            b.OpenElement(2, "p");    // sibling root: untouched
            b.CloseElement();
        });

        var ids = 0;
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Attribute && frames[i].AttributeName == "id")
                ids++;
        Assert.AreEqual(1, ids);
        Assert.AreEqual("bm-test", AttributesOf(frames, 0)["id"]);
    }

    [TestMethod]
    public void Render_PassesAuthorIdAndStyle_ToPlanner_AndMergesThem()
    {
        string? seenId = null, seenStyle = null;
        var (frames, _, _) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "id", "custom-id");
            b.AddAttribute(2, "style", "color:red;");
            b.CloseElement();
        }, (tag, id, style) =>
        {
            seenId = id;
            seenStyle = style;
            return new BmotionInjection("custom-id", "opacity:0;", false);
        });

        Assert.AreEqual("custom-id", seenId);
        Assert.AreEqual("color:red;", seenStyle);
        var attrs = AttributesOf(frames, 0);
        Assert.AreEqual("custom-id", attrs["id"]);
        // single merged style (motion first so the author's declarations win), no duplicate
        Assert.AreEqual("opacity:0;color:red;", attrs["style"]);
        Assert.AreEqual(2, attrs.Count);
    }

    [TestMethod]
    public void Render_AddsPathLength_UnlessAuthored()
    {
        static BmotionInjection Plan(string tag, string? id, string? style) => new("bm-test", null, AddPathLength: true);

        var (frames, _, _) = Rewrite(b =>
        {
            b.OpenElement(0, "path");
            b.CloseElement();
        }, Plan);
        Assert.AreEqual("1", AttributesOf(frames, 0)["pathLength"]);

        (frames, _, _) = Rewrite(b =>
        {
            b.OpenElement(0, "path");
            b.AddAttribute(1, "pathLength", "5");
            b.CloseElement();
        }, Plan);
        Assert.AreEqual("5", AttributesOf(frames, 0)["pathLength"]);
    }

    [TestMethod]
    public void Render_PlannerReceivesTagName()
    {
        string? seenTag = null;
        Rewrite(b =>
        {
            b.OpenElement(0, "circle");
            b.CloseElement();
        }, (tag, id, style) => { seenTag = tag; return new BmotionInjection("bm-test", null, false); });

        Assert.AreEqual("circle", seenTag);
    }

    // ── Root discovery ────────────────────────────────────────────────────────

    [TestMethod]
    public void Render_DescendsIntoRegions_ToFindRootElement()
    {
        var (frames, count, injected) = Rewrite(b =>
        {
            b.OpenRegion(0);
            b.OpenElement(1, "div");
            b.CloseElement();
            b.CloseRegion();
        });

        Assert.IsTrue(injected);
        var elementIndex = -1;
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Element) { elementIndex = i; break; }
        Assert.AreEqual("bm-test", AttributesOf(frames, elementIndex)["id"]);
    }

    [TestMethod]
    public void Render_SkipsComponentRoot_InjectsIntoNextElement()
    {
        var (frames, count, injected) = Rewrite(b =>
        {
            b.OpenComponent<DummyComponent>(0);
            b.AddComponentParameter(1, "Value", 42);
            b.CloseComponent();
            b.OpenElement(2, "div");
            b.CloseElement();
        });

        Assert.IsTrue(injected);
        // The component and its parameter replay untouched.
        Assert.AreEqual(RenderTreeFrameType.Component, frames[0].FrameType);
        Assert.AreEqual(typeof(DummyComponent), frames[0].ComponentType);
        Assert.AreEqual(42, frames[1].AttributeValue);
        // The element sibling receives the injection.
        var elementIndex = -1;
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Element) { elementIndex = i; break; }
        Assert.AreEqual("bm-test", AttributesOf(frames, elementIndex)["id"]);
    }

    [TestMethod]
    public void Render_TextOnlyContent_ReturnsFalse()
    {
        var (_, _, injected) = Rewrite(b => b.AddContent(0, "just text"));
        Assert.IsFalse(injected);
    }

    [TestMethod]
    public void Render_ComponentOnlyContent_ReturnsFalse()
    {
        var (_, _, injected) = Rewrite(b =>
        {
            b.OpenComponent<DummyComponent>(0);
            b.CloseComponent();
        });
        Assert.IsFalse(injected);
    }

    // ── Static-markup injection (Razor collapses static child markup into one frame) ──

    private static string MarkupOf(RenderTreeFrame[] frames, int count)
    {
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Markup)
                return frames[i].MarkupContent;
        Assert.Fail("no markup frame in output");
        return null!;
    }

    [TestMethod]
    public void Render_InjectsIntoStaticMarkup_FirstOpeningTag()
    {
        var (frames, count, injected) = Rewrite(b =>
            b.AddMarkupContent(0, "\n    <div class=\"box\"><span>hi</span></div>\n"));

        Assert.IsTrue(injected);
        var markup = MarkupOf(frames, count);
        StringAssert.Contains(markup, "<div class=\"box\" id=\"bm-test\" style=\"opacity:0;\">");
        StringAssert.Contains(markup, "<span>hi</span>"); // inner tags untouched
    }

    [TestMethod]
    public void Render_StaticMarkup_MergesAuthorStyle_AndAdoptsAuthorId()
    {
        string? seenId = null, seenStyle = null;
        var (frames, count, _) = Rewrite(b =>
            b.AddMarkupContent(0, "<div id=\"my-id\" style=\"color:red;\" class=\"box\"></div>"),
            (tag, id, style) =>
            {
                seenId = id;
                seenStyle = style;
                return new BmotionInjection("my-id", "opacity:0;", false);
            });

        Assert.AreEqual("my-id", seenId);
        Assert.AreEqual("color:red;", seenStyle);
        var markup = MarkupOf(frames, count);
        StringAssert.Contains(markup, "id=\"my-id\"");
        StringAssert.Contains(markup, "style=\"opacity:0;color:red;\"");
        Assert.AreEqual(1, markup.Split("style=").Length - 1, "style attribute must not be duplicated");
        Assert.AreEqual(1, markup.Split("id=\"").Length - 1, "id attribute must not be duplicated");
    }

    [TestMethod]
    public void Render_StaticMarkup_SelfClosingSvgShape_GetsPathLength()
    {
        var (frames, count, _) = Rewrite(b =>
            b.AddMarkupContent(0, "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0L10 10\" /></svg>"),
            (tag, id, style) => new BmotionInjection("bm-test", null, AddPathLength: tag == "svg"));

        // The FIRST root element (the svg) is the target, exactly like the element-frame path.
        var markup = MarkupOf(frames, count);
        StringAssert.Contains(markup, "<svg viewBox=\"0 0 10 10\" id=\"bm-test\" pathLength=\"1\">");
        StringAssert.Contains(markup, "<path d=\"M0 0L10 10\" />"); // inner path untouched
    }

    [TestMethod]
    public void Render_WhitespaceMarkup_IsSkipped_ElementFrameStillInjected()
    {
        var (frames, count, injected) = Rewrite(b =>
        {
            b.AddMarkupContent(0, "\n    ");
            b.OpenElement(1, "div");
            b.CloseElement();
        });

        Assert.IsTrue(injected);
        Assert.AreEqual("\n    ", MarkupOf(frames, count)); // untouched
        var elementIndex = -1;
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Element) { elementIndex = i; break; }
        Assert.AreEqual("bm-test", AttributesOf(frames, elementIndex)["id"]);
    }

    [TestMethod]
    public void Render_CommentOnlyMarkup_ReturnsFalse()
    {
        var (_, _, injected) = Rewrite(b => b.AddMarkupContent(0, "<!-- nothing here -->"));
        Assert.IsFalse(injected);
    }

    // ── Replay fidelity ───────────────────────────────────────────────────────

    [TestMethod]
    public void Render_PreservesEventCallback_WithExplicitReceiver()
    {
        // A lambda whose target differs from the receiver forces the boxed-EventCallback frame
        // shape; the replay must round-trip it without stringifying it.
        var receiver = new DummyComponent();
        var captured = 0;
        var callback = EventCallback.Factory.Create(receiver, () => captured++);

        var (frames, _, _) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "onclick", callback);
            b.CloseElement();
        });

        var onclick = AttributesOf(frames, 0)["onclick"];
        Assert.IsNotNull(onclick);
        Assert.IsNotInstanceOfType<string>(onclick);
    }

    [TestMethod]
    public void Render_PreservesKeys_OnElementsAndComponents()
    {
        var (frames, count, _) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.SetKey("el-key");
            b.CloseElement();
            b.OpenComponent<DummyComponent>(1);
            b.SetKey("cmp-key");
            b.CloseComponent();
        });

        Assert.AreEqual("el-key", frames[0].ElementKey);
        var componentIndex = -1;
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.Component) { componentIndex = i; break; }
        Assert.AreEqual("cmp-key", frames[componentIndex].ComponentKey);
    }

    [TestMethod]
    public void Render_PreservesElementReferenceCapture_AfterInjectedAttributes()
    {
        Action<ElementReference> capture = _ => { };
        var (frames, count, _) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "box");
            b.AddElementReferenceCapture(2, capture);
            b.AddContent(3, "hi");
            b.CloseElement();
        });

        var captureIndex = -1;
        for (var i = 0; i < count; i++)
            if (frames[i].FrameType == RenderTreeFrameType.ElementReferenceCapture) { captureIndex = i; break; }
        Assert.IsTrue(captureIndex > 0, "capture frame missing");
        Assert.AreSame(capture, frames[captureIndex].ElementReferenceCaptureAction);
        // All attribute frames (author + injected) must precede the capture frame.
        for (var i = captureIndex + 1; i < count; i++)
            Assert.AreNotEqual(RenderTreeFrameType.Attribute, frames[i].FrameType);
    }

    [TestMethod]
    public void Render_PreservesMarkupAndText()
    {
        var (frames, count, _) = Rewrite(b =>
        {
            b.OpenElement(0, "div");
            b.AddContent(1, "text");
            b.AddMarkupContent(2, "<b>markup</b>");
            b.CloseElement();
        });

        var sawText = false;
        var sawMarkup = false;
        for (var i = 0; i < count; i++)
        {
            if (frames[i].FrameType == RenderTreeFrameType.Text && frames[i].TextContent == "text") sawText = true;
            if (frames[i].FrameType == RenderTreeFrameType.Markup && frames[i].MarkupContent == "<b>markup</b>") sawMarkup = true;
        }
        Assert.IsTrue(sawText);
        Assert.IsTrue(sawMarkup);
    }

    private sealed class DummyComponent : ComponentBase
    {
        [Parameter] public int Value { get; set; }
    }
}
