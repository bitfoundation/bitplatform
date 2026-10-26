using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.CodeBlockView;

[TestClass]
public class BitAiCodeBlockViewTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiCodeBlockViewShouldRenderItsParts()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "var x = 1;")
            .Add(c => c.Language, "csharp"));

        var root = component.Find(".bit-aicbv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
        Assert.AreEqual("csharp", root.QuerySelector(".bit-aicbv-hdr > .bit-aicbv-lng")!.TextContent);
        Assert.IsNotNull(root.QuerySelector(".bit-aicbv-hdr button"));

        var code = root.QuerySelector(".bit-aicbv-bdy > pre.bit-aicbv-pre > code.bit-aicbv-cod")!;
        Assert.IsTrue(code.ClassList.Contains("language-csharp"));
        Assert.AreEqual("var x = 1;", code.TextContent);
    }

    [TestMethod]
    public void TheLabelIsTheFileNameThenTheLanguageThenText()
    {
        Assert.AreEqual("Program.cs", RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "x")
            .Add(c => c.Language, "csharp")
            .Add(c => c.FileName, "Program.cs")).Find(".bit-aicbv-lng").TextContent);

        var plain = RenderComponent<BitAiCodeBlockView>(p => p.Add(c => c.Code, "x"));

        Assert.AreEqual("text", plain.Find(".bit-aicbv-lng").TextContent);
        Assert.IsTrue(plain.Find(".bit-aicbv-cod").ClassList.Contains("language-text"));
    }

    [TestMethod]
    public void TheHeaderCanBeLeftOut()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "x")
            .Add(c => c.ShowHeader, false));

        Assert.IsEmpty(component.FindAll(".bit-aicbv-hdr"));
        Assert.IsEmpty(component.FindAll("button"));
    }

    [TestMethod]
    public void RegisteredOptionsApplyWithoutAnOptionsProvider()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { ShowCodeCopy = false });

        var component = RenderComponent<BitAiCodeBlockView>(p => p.Add(c => c.Code, "x"));

        Assert.IsEmpty(component.FindAll("button"));
        Assert.IsNotNull(component.Find(".bit-aicbv-hdr"));
    }

    [TestMethod]
    public void TheCodeIsHighlightedOnlyOnceItStopsStreaming()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "public int Count = 1;")
            .Add(c => c.Language, "csharp")
            .Add(c => c.IsStreaming, true));

        Assert.IsEmpty(component.FindAll(".bit-aicbv-cod span"));
        Assert.AreEqual("public int Count = 1;", component.Find(".bit-aicbv-cod").TextContent);

        component.Render(p => p.Add(c => c.IsStreaming, false));

        Assert.IsNotEmpty(component.FindAll(".bit-aicbv-kwd"));
        Assert.IsNotEmpty(component.FindAll(".bit-aicbv-num"));
        Assert.AreEqual("public int Count = 1;", component.Find(".bit-aicbv-cod").TextContent);
    }

    [TestMethod]
    public void EveryTokenKindHasAClassOfItsOwn()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "// note\nvar name = \"text\";")
            .Add(c => c.Language, "csharp"));

        Assert.AreEqual("// note", component.Find(".bit-aicbv-cmt").TextContent);
        Assert.AreEqual("\"text\"", component.Find(".bit-aicbv-str").TextContent);

        // Plain text needs no span: every span of the code is a token of a kind.
        var kinds = component.FindAll(".bit-aicbv-cod span").Select(s => s.ClassName).Distinct().ToArray();
        Assert.IsTrue(kinds.All(k => k!.StartsWith("bit-aicbv-")), string.Join(", ", kinds));
    }

    [TestMethod]
    public void LineNumbersWrapEveryLineAndLeaveTheTextAsItWas()
    {
        const string code = "/* one\ntwo */\nint x = 1;\n";

        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, code)
            .Add(c => c.Language, "csharp")
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { ShowLineNumbers = true }));

        var lines = component.FindAll(".bit-aicbv-cod > span.bit-aicbv-lin");

        // The line break that ends the code opens no empty line of its own.
        Assert.HasCount(3, lines);
        Assert.AreEqual(code, component.Find(".bit-aicbv-cod").TextContent);
        Assert.AreEqual("/* one\n", lines[0].TextContent);

        // A comment that runs over two lines is split after the break, and each line carries its half.
        Assert.AreEqual("/* one\n", lines[0].QuerySelector(".bit-aicbv-cmt")!.TextContent);
        Assert.AreEqual("two */", lines[1].QuerySelector(".bit-aicbv-cmt")!.TextContent);
        Assert.AreEqual("1", lines[2].QuerySelector(".bit-aicbv-num")!.TextContent);
    }

    [TestMethod]
    public void LineNumbersAlsoWrapStreamingCode()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "a\nb")
            .Add(c => c.IsStreaming, true)
            .Add(c => c.Classes, new() { Line = "c-line" })
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { ShowLineNumbers = true }));

        var lines = component.FindAll(".bit-aicbv-lin");

        Assert.HasCount(2, lines);
        Assert.IsTrue(lines.All(l => l.ClassList.Contains("c-line")));
        Assert.AreEqual("a\nb", component.Find(".bit-aicbv-cod").TextContent);
    }

    // A "\r" left in the text is a character of its own, which an engine copying a selection turns into a second break
    // (Firefox), doubling every line of the code.
    [TestMethod]
    public void TheCodeIsDrawnWithLineFeedsAloneWhateverItsLineEndings()
    {
        const string code = "int a = 1;\r\nint b = 2;\r\n";

        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, code)
            .Add(c => c.Language, "csharp")
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { ShowLineNumbers = true }));

        Assert.HasCount(2, component.FindAll(".bit-aicbv-cod > span.bit-aicbv-lin"));
        Assert.AreEqual("int a = 1;\nint b = 2;\n", component.Find(".bit-aicbv-cod").TextContent);
        var drawn = component.Find(".bit-aicbv-cod").OuterHtml;
        Assert.IsFalse(drawn.Contains('\r') || drawn.Contains("&#xD;", System.StringComparison.OrdinalIgnoreCase));
        Assert.HasCount(2, component.FindAll(".bit-aicbv-typ"));
        Assert.AreEqual("2", component.FindAll(".bit-aicbv-lin")[1].QuerySelector(".bit-aicbv-num")!.TextContent);

        // What the copy button copies is the code as it was given.
        Assert.AreEqual(code, component.FindComponent<BitAiCopyButton>().Instance.Content);
    }

    // A scrolling box holds nothing that takes the focus, and WebKit leaves it out of the tab order: the body is marked for
    // the page script (BitAiScrollStops.ts) that puts it in the tab order while it has something to scroll.
    [TestMethod]
    public void TheBodyTakesTheFocusSoAKeyboardCanScrollIt()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p.Add(c => c.Code, "x"));

        Assert.IsTrue(component.Find(".bit-aicbv-bdy").HasAttribute("data-bit-ai-scroll"));
        Assert.IsNull(component.Find(".bit-aicbv-bdy").GetAttribute("tabindex"));

        // A tab stop is announced by its name, so the body is a group named for the code it holds: its file, else its
        // language, else just code.
        Assert.AreEqual("group", component.Find(".bit-aicbv-bdy").GetAttribute("role"));
        Assert.AreEqual("Code", component.Find(".bit-aicbv-bdy").GetAttribute("aria-label"));

        component.Render(p => p.Add(c => c.Language, "csharp"));
        Assert.AreEqual("csharp code", component.Find(".bit-aicbv-bdy").GetAttribute("aria-label"));

        component.Render(p => p.Add(c => c.FileName, "Program.cs"));
        Assert.AreEqual("Program.cs", component.Find(".bit-aicbv-bdy").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void WithoutLineNumbersNoLineIsWrapped()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "a\nb")
            .Add(c => c.Language, "csharp"));

        Assert.IsEmpty(component.FindAll(".bit-aicbv-lin"));
    }

    [TestMethod]
    public void BitAiCodeBlockViewShouldApplyTheClassesAndStylesOfItsParts()
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "x")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Classes, new() { Root = "c-root", Header = "c-hdr", Label = "c-lng", Body = "c-bdy", Pre = "c-pre", Code = "c-cod" })
            .Add(c => c.Styles, new() { Root = "margin: 1px", Header = "margin: 2px" }));

        var root = component.Find(".bit-aicbv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsTrue(component.Find(".bit-aicbv-hdr").ClassList.Contains("c-hdr"));
        Assert.AreEqual("margin: 2px", component.Find(".bit-aicbv-hdr").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aicbv-lng").ClassList.Contains("c-lng"));
        Assert.IsTrue(component.Find(".bit-aicbv-bdy").ClassList.Contains("c-bdy"));
        Assert.IsTrue(component.Find(".bit-aicbv-pre").ClassList.Contains("c-pre"));
        Assert.IsTrue(component.Find(".bit-aicbv-cod").ClassList.Contains("c-cod"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiCodeBlockViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "x")
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aicbv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl")]
    public void BitAiCodeBlockViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiCodeBlockView>(p => p
            .Add(c => c.Code, "x")
            .Add(c => c.Dir, dir));

        Assert.AreEqual(expected, component.Find(".bit-aicbv").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitAiCodeBlockViewShouldTakeTheCascadedParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiCodeBlockViewParams { Class = "cascaded", ShowHeader = false, Classes = new() { Pre = "cascaded-pre" } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiCodeBlockView>(0);
                builder.AddAttribute(1, nameof(BitAiCodeBlockView.Code), "x");
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-aicbv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aicbv-pre").ClassList.Contains("cascaded-pre"));
        Assert.IsEmpty(component.FindAll(".bit-aicbv-hdr"));
        Assert.AreEqual("BitParams.BitAiCodeBlockView", new BitAiCodeBlockViewParams().Name);
    }

    [TestMethod]
    public void AShowHeaderSetOnTheBlockWinsOverTheCascadedOne()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitAiCodeBlockViewParams { ShowHeader = false } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiCodeBlockView>(0);
                builder.AddAttribute(1, nameof(BitAiCodeBlockView.Code), "x");
                builder.AddAttribute(2, nameof(BitAiCodeBlockView.ShowHeader), true);
                builder.CloseComponent();
            });
        });

        Assert.IsNotNull(component.Find(".bit-aicbv-hdr"));
    }
}
