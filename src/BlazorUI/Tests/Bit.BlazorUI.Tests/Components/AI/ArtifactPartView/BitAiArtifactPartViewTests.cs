using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ArtifactPartView;

[TestClass]
public class BitAiArtifactPartViewTests : BitAiTestBase
{
    private static (BitAiConversation Conversation, BitAiArtifactPart Part) WithArtifact(BitAiArtifactKind kind, string title, string content)
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", kind, title);
        artifact.SetContent(content);
        artifact.Complete();

        var part = new BitAiArtifactPart("a1") { ArtifactVersion = artifact.Version };
        conversation.AddMessage(Assistant(part));
        return (conversation, part);
    }

    private IRenderedComponent<BitAiArtifactPartView> RenderPart(BitAiConversation? conversation, BitAiArtifactPart part) =>
        RenderComponent<BitAiArtifactPartView>(p => p.Add(c => c.Part, part).Add(c => c.Conversation, conversation));



    [TestMethod]
    public void AnArtifactPartRendersTheArtifactPanelRatherThanAnUnknownElement()
    {
        var (conversation, part) = WithArtifact(BitAiArtifactKind.Document, "Project README", "# Hello\n\nWorld");

        var view = RenderPart(conversation, part);

        Assert.IsFalse(view.Markup.Contains("artifactpartview", System.StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(view.Find(".bit-aiapv > .bit-aiatv"));
        Assert.AreEqual("Project README", view.Find(".bit-aiatv-ttl").TextContent);
        Assert.HasCount(1, view.FindComponents<BitAiStreamingMarkdown>());
        Assert.IsNotNull(view.Find("button[aria-label='Download']"));
        Assert.IsNotNull(view.Find("button[aria-label='Expand']"));
    }

    [TestMethod]
    public void TheArtifactIsOpenedAtTheVersionTheTurnProduced()
    {
        var (conversation, part) = WithArtifact(BitAiArtifactKind.Code, "Program", "first draft");
        var artifact = conversation.FindArtifact("a1")!;
        artifact.BeginRevision();
        artifact.SetContent("second draft");
        artifact.Complete();

        var view = RenderPart(conversation, part);

        Assert.AreEqual(part.ArtifactVersion, view.FindComponent<BitAiArtifactView>().Instance.InitialVersion);
        StringAssert.Contains(view.Find(".bit-aiatv-bdy").TextContent, "first draft");
    }

    [TestMethod]
    public void AnArtifactThatCannotBeResolvedRendersAPlaceholder()
    {
        var (conversation, _) = WithArtifact(BitAiArtifactKind.Code, "x", "y");

        var view = RenderPart(conversation, new BitAiArtifactPart("missing"));

        Assert.IsEmpty(view.FindComponents<BitAiArtifactView>());
        var placeholder = view.Find(".bit-aiapv > .bit-aiapv-plh");
        StringAssert.Contains(placeholder.TextContent, "Artifact");
        Assert.AreEqual("missing", view.FindComponent<BitTag>().Instance.Title);
        // A neutral tag through the host-default class, never a Color, so the app's --bit-Tag-* variables reach it.
        Assert.IsTrue(placeholder.ClassList.Contains("bit-tag-dft-ter"));
        Assert.IsNull(view.FindComponent<BitTag>().Instance.Color);
    }

    [TestMethod]
    public void APartWithoutAConversationRendersAPlaceholder()
    {
        var view = RenderPart(null, new BitAiArtifactPart("a1"));

        Assert.IsEmpty(view.FindComponents<BitAiArtifactView>());
        Assert.IsNotNull(view.Find(".bit-aiapv-plh"));
    }

    [TestMethod]
    public void BitAiArtifactPartViewShouldRenderTheRootAndHandItsClassesOn()
    {
        var (conversation, part) = WithArtifact(BitAiArtifactKind.Code, "Program", "x");

        var view = RenderComponent<BitAiArtifactPartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.Conversation, conversation)
            .Add(c => c.Id, "p1")
            .Add(c => c.Class, "custom")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "c-root", ArtifactView = new() { Header = "c-header" } }));

        var root = view.Find(".bit-aiapv");

        Assert.AreEqual("p1", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.IsTrue(view.Find(".bit-aiatv-hdr").ClassList.Contains("c-header"));
    }

    [TestMethod]
    public void BitAiArtifactPartViewShouldHandItsPlaceholderClassesOn()
    {
        var view = RenderComponent<BitAiArtifactPartView>(p => p
            .Add(c => c.Part, new BitAiArtifactPart("missing"))
            .Add(c => c.Classes, new() { Placeholder = new() { Root = "c-placeholder", Icon = "c-icon" } }));

        Assert.IsTrue(view.Find(".bit-aiapv-plh").ClassList.Contains("c-placeholder"));

        // The glyph comes from the package's own icon font, beside the app's own icon classes.
        var icon = view.Find(".bit-aiapv-plh .bit-icon-ai");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--MapLayers"));
        Assert.IsTrue(icon.ClassList.Contains("c-icon"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiArtifactPartViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var view = RenderComponent<BitAiArtifactPartView>(p => p
            .Add(c => c.Part, new BitAiArtifactPart("missing"))
            .Add(c => c.Visibility, visibility));

        var style = view.Find(".bit-aiapv").GetAttribute("style");

        if (expected is null) Assert.IsTrue(string.IsNullOrEmpty(style));
        else StringAssert.Contains(style, expected);
    }

    [TestMethod]
    public void BitAiArtifactPartViewShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiArtifactPartViewParams { Class = "cascaded", Classes = new() { Placeholder = new() { Root = "cascaded-placeholder" } } }
            });
            p.AddChildContent<BitAiArtifactPartView>(c => c.Add(x => x.Part, new BitAiArtifactPart("missing")));
        });

        Assert.IsTrue(component.Find(".bit-aiapv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aiapv-plh").ClassList.Contains("cascaded-placeholder"));
    }

    [TestMethod]
    public void BitAiArtifactPartViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiArtifactPartView", BitAiArtifactPartViewParams.ParamName);
        Assert.AreEqual(BitAiArtifactPartViewParams.ParamName, new BitAiArtifactPartViewParams().Name);
    }
}
