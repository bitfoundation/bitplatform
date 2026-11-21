using System;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.CodeBlock;

[TestClass, DoNotParallelize]
public class BitCodeBlockTests : BunitTestContext
{
    private const string CopyIdentifier = "BitBlazorUI.CodeBlock.copy";

    private sealed class GermanStrings : BitStrings
    {
        public override string Copy => "Kopieren";
        public override string Copied => "Kopiert";
        public override string Code => "Quelltext";
    }

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    private IRenderedComponent<BitCodeBlock> Render(Action<ComponentParameterCollectionBuilder<BitCodeBlock>>? parameters = null)
        => RenderComponent<BitCodeBlock>(p => parameters?.Invoke(p));

    [TestMethod]
    public void BitCodeBlockShouldRenderTheCodeInAPreAndACode()
    {
        var com = Render(p => p.Add(c => c.Code, "dotnet build"));

        var root = com.Find(".bit-cdb");
        var code = com.Find(".bit-cdb-bdy > pre.bit-cdb-pre > code.bit-cdb-cod");

        Assert.AreEqual("dotnet build", code.TextContent);
        // No header without a title, a language or the terminal look; the copy button floats on its own.
        Assert.AreEqual(0, com.FindAll(".bit-cdb-hdr").Count);
        Assert.AreEqual("bit-cdb-cpb", root.Children[0].ClassName?.Trim());
        Assert.AreEqual(0, com.FindAll(".bit-cdb-lin").Count);
    }

    [TestMethod]
    public void BitCodeBlockShouldDrawEveryLineEndingAsANewLine()
    {
        var com = Render(p => p.Add(c => c.Code, "one\r\ntwo\rthree"));

        Assert.AreEqual("one\ntwo\nthree", com.Find(".bit-cdb-cod").TextContent);
    }

    [TestMethod]
    public void BitCodeBlockShouldRenderItsChildContentInPlaceOfTheCode()
    {
        var com = Render(p =>
        {
            p.Add(c => c.Code, "plain");
            p.AddChildContent("<span class=\"kw\">var</span> x;");
        });

        Assert.AreEqual("var x;", com.Find(".bit-cdb-cod").TextContent);
        Assert.IsNotNull(com.Find(".bit-cdb-cod > .kw"));
    }

    [TestMethod]
    public void BitCodeBlockShouldNameItsScrollingBoxAndLetTheKeyboardReachIt()
    {
        var com = Render(p => p.Add(c => c.Code, "x"));

        var body = com.Find(".bit-cdb-bdy");

        Assert.AreEqual("group", body.GetAttribute("role"));
        Assert.AreEqual("0", body.GetAttribute("tabindex"));
        Assert.AreEqual("Code", body.GetAttribute("aria-label"));

        com.Render(p => p.Add(c => c.Language, "bash"));
        Assert.AreEqual("bash", com.Find(".bit-cdb-bdy").GetAttribute("aria-label"));

        com.Render(p => p.Add(c => c.Title, "install.sh"));
        Assert.AreEqual("install.sh", com.Find(".bit-cdb-bdy").GetAttribute("aria-label"));

        com.Render(p => p.Add(c => c.AriaLabel, "The install script"));
        Assert.AreEqual("The install script", com.Find(".bit-cdb-bdy").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCodeBlockShouldShowTheTitleAndTheLanguageInAHeaderWithTheCopyButton()
    {
        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.Title, "Program.cs");
            p.Add(c => c.Language, "C#");
        });

        Assert.AreEqual("Program.cs", com.Find(".bit-cdb-hdr > .bit-cdb-ttl").TextContent);
        Assert.AreEqual("C#", com.Find(".bit-cdb-hdr > .bit-cdb-lng").TextContent);
        Assert.IsNotNull(com.Find(".bit-cdb-hdr > .bit-cdb-cpb"));
        Assert.AreEqual(1, com.FindAll(".bit-cdb-cpb").Count);
        Assert.AreEqual(0, com.FindAll(".bit-cdb-dts").Count);
    }

    [TestMethod]
    public void BitCodeBlockShouldDrawATerminalWithDotsAndAPromptPerCommandLine()
    {
        var com = Render(p =>
        {
            p.Add(c => c.Terminal, true);
            p.Add(c => c.Code, "dotnet tool install --global Bit.Cli\n\nbit new MyApp\n");
        });

        Assert.IsTrue(com.Find(".bit-cdb").ClassList.Contains("bit-cdb-trm"));
        Assert.AreEqual("true", com.Find(".bit-cdb-hdr > .bit-cdb-dts").GetAttribute("aria-hidden"));
        Assert.AreEqual(3, com.FindAll(".bit-cdb-dot").Count);

        // A prompt-bearing span per command, none for the blank line, and the text of the code left exactly as it was.
        var lines = com.FindAll(".bit-cdb-lin");
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("dotnet tool install --global Bit.Cli\n", lines[0].TextContent);
        Assert.AreEqual("bit new MyApp\n", lines[1].TextContent);
        Assert.AreEqual("dotnet tool install --global Bit.Cli\n\nbit new MyApp\n", com.Find(".bit-cdb-cod").TextContent);
    }

    [TestMethod]
    public void BitCodeBlockShouldWriteItsMaxHeightAsAPrivateVariable()
    {
        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.MaxHeight, "10rem");
        });

        StringAssert.Contains(com.Find(".bit-cdb").GetAttribute("style"), "--bit-cdb-mxh:10rem");

        com.Render(p => p.Add(c => c.MaxHeight, null));

        Assert.IsFalse((com.Find(".bit-cdb").GetAttribute("style") ?? string.Empty).Contains("--bit-cdb-mxh"));
    }

    [TestMethod]
    public void BitCodeBlockShouldHideTheCopyButton()
    {
        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.ShowCopyButton, false);
        });

        Assert.AreEqual(0, com.FindAll(".bit-cdb-cpb").Count);
        // The live region stays, empty.
        Assert.AreEqual(string.Empty, com.Find(".bit-cdb-liv").TextContent);
    }

    [TestMethod]
    public void BitCodeBlockShouldCopyTheCodeAndConfirmIt()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);
        var copies = 0;

        var com = Render(p =>
        {
            p.Add(c => c.Code, "line 1\r\nline 2");
            p.Add(c => c.OnCopy, () => copies++);
        });

        var button = com.Find(".bit-cdb-cpb");
        Assert.AreEqual("button", button.GetAttribute("type"));
        Assert.AreEqual("Copy", button.GetAttribute("aria-label"));
        Assert.AreEqual("Copy", button.GetAttribute("title"));
        Assert.AreEqual("status", com.Find(".bit-cdb-liv").GetAttribute("role"));
        Assert.AreEqual("polite", com.Find(".bit-cdb-liv").GetAttribute("aria-live"));
        Assert.AreEqual(string.Empty, com.Find(".bit-cdb-liv").TextContent);

        button.Click();

        com.WaitForAssertion(() => Assert.AreEqual("Copied", com.Find(".bit-cdb-liv").TextContent));
        Assert.AreEqual("Copied", com.Find(".bit-cdb-cpb").GetAttribute("aria-label"));
        Assert.AreEqual(1, copies);

        // The Code is copied as it was given, line endings and all, and not as a click on the code itself.
        var arguments = invocation.Invocations.Single().Arguments;
        Assert.AreEqual("line 1\r\nline 2", arguments[1]);
        Assert.AreEqual(false, arguments[2]);

        // The confirmation goes away again.
        com.WaitForAssertion(() => Assert.AreEqual("Copy", com.Find(".bit-cdb-cpb").GetAttribute("aria-label")), TimeSpan.FromSeconds(5));
        Assert.AreEqual(string.Empty, com.Find(".bit-cdb-liv").TextContent);
    }

    [TestMethod]
    public void BitCodeBlockShouldHandTheScriptNoTextToCopyItsRenderedContent()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);

        var com = Render(p => p.AddChildContent("dotnet run"));

        com.Find(".bit-cdb-cpb").Click();

        // No Code: the script reads the text of the code element it is handed.
        Assert.IsNull(invocation.Invocations.Single().Arguments[1]);
    }

    [TestMethod]
    public void BitCodeBlockShouldConfirmNothingWhenTheCopyFails()
    {
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetResult(false);
        var copies = 0;

        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.OnCopy, () => copies++);
        });

        com.Find(".bit-cdb-cpb").Click();

        Assert.AreEqual(0, copies);
        Assert.AreEqual("Copy", com.Find(".bit-cdb-cpb").GetAttribute("aria-label"));
        Assert.AreEqual(string.Empty, com.Find(".bit-cdb-liv").TextContent);
    }

    [TestMethod]
    public void BitCodeBlockShouldConfirmNothingWhenTheScriptThrows()
    {
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetException(new InvalidOperationException("denied"));

        var com = Render(p => p.Add(c => c.Code, "x"));

        com.Find(".bit-cdb-cpb").Click();

        Assert.AreEqual("Copy", com.Find(".bit-cdb-cpb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCodeBlockShouldSayTheWordsItIsGiven()
    {
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetResult(true);

        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.CopyText, "Copy the command");
            p.Add(c => c.CopiedText, "On the clipboard");
        });

        Assert.AreEqual("Copy the command", com.Find(".bit-cdb-cpb").GetAttribute("aria-label"));

        com.Find(".bit-cdb-cpb").Click();

        com.WaitForAssertion(() => Assert.AreEqual("On the clipboard", com.Find(".bit-cdb-liv").TextContent));
        Assert.AreEqual("On the clipboard", com.Find(".bit-cdb-cpb").GetAttribute("title"));
    }

    [TestMethod]
    public void BitCodeBlockShouldSayTheTranslatedWordsOfTheStrings()
    {
        BitStrings.Current = new GermanStrings();
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetResult(true);

        var com = Render(p => p.Add(c => c.Code, "x"));

        Assert.AreEqual("Kopieren", com.Find(".bit-cdb-cpb").GetAttribute("aria-label"));
        Assert.AreEqual("Quelltext", com.Find(".bit-cdb-bdy").GetAttribute("aria-label"));

        com.Find(".bit-cdb-cpb").Click();

        com.WaitForAssertion(() => Assert.AreEqual("Kopiert", com.Find(".bit-cdb-liv").TextContent));
    }

    [TestMethod]
    public void BitCodeBlockShouldCopyOnAClickOfTheCodeOnlyWhenAskedTo()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);

        var com = Render(p => p.Add(c => c.Code, "x"));

        // Without CopyOnClick a click on the code copies nothing.
        com.Find(".bit-cdb-bdy").Click();
        Assert.AreEqual(0, invocation.Invocations.Count);
        Assert.IsNull(com.Find(".bit-cdb-bdy").GetAttribute("title"));
        Assert.IsFalse(com.Find(".bit-cdb").ClassList.Contains("bit-cdb-coc"));

        com.Render(p => p.Add(c => c.CopyOnClick, true));

        Assert.IsTrue(com.Find(".bit-cdb").ClassList.Contains("bit-cdb-coc"));
        Assert.AreEqual("Copy", com.Find(".bit-cdb-bdy").GetAttribute("title"));

        com.Find(".bit-cdb-bdy").Click();

        com.WaitForAssertion(() => Assert.AreEqual("Copied", com.Find(".bit-cdb-liv").TextContent));
        // The script is told the copy comes from a click on the code, so it leaves a selection being made alone.
        Assert.AreEqual(true, invocation.Invocations.Single().Arguments[2]);
    }

    [TestMethod]
    public void ADisabledBitCodeBlockShouldCopyNothing()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);

        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.CopyOnClick, true);
            p.Add(c => c.Disabled, true);
        });

        var button = com.Find(".bit-cdb-cpb");
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.IsNull(com.Find(".bit-cdb-bdy").GetAttribute("title"));

        com.Find(".bit-cdb-bdy").Click();

        Assert.AreEqual(0, invocation.Invocations.Count);
        // The code itself stays readable and selectable.
        Assert.AreEqual("x", com.Find(".bit-cdb-cod").TextContent);
    }

    [TestMethod]
    public void BitCodeBlockWithNothingToCopyShouldDisableItsCopyButton()
    {
        var com = Render();

        var button = com.Find(".bit-cdb-cpb");
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitCodeBlockShouldApplyItsClassesAndStylesToItsParts()
    {
        var com = Render(p =>
        {
            p.Add(c => c.Code, "x");
            p.Add(c => c.Terminal, true);
            p.Add(c => c.Title, "t");
            p.Add(c => c.Language, "l");
            p.Add(c => c.Classes, new BitCodeBlockClassStyles
            {
                Root = "c-root",
                Header = "c-hdr",
                Dots = "c-dts",
                Title = "c-ttl",
                Language = "c-lng",
                CopyButton = "c-cpb",
                Body = "c-bdy",
                Pre = "c-pre",
                Code = "c-cod",
                LiveRegion = "c-liv",
            });
            p.Add(c => c.Styles, new BitCodeBlockClassStyles
            {
                Root = "color:red",
                Header = "color:orange",
                Dots = "color:yellow",
                Title = "color:green",
                Language = "color:blue",
                CopyButton = "color:indigo",
                Body = "color:violet",
                Pre = "color:black",
                Code = "color:white",
                LiveRegion = "color:gray",
            });
        });

        foreach (var (part, cssClass, style) in new[]
        {
            ("bit-cdb", "c-root", "color:red"),
            ("bit-cdb-hdr", "c-hdr", "color:orange"),
            ("bit-cdb-dts", "c-dts", "color:yellow"),
            ("bit-cdb-ttl", "c-ttl", "color:green"),
            ("bit-cdb-lng", "c-lng", "color:blue"),
            ("bit-cdb-cpb", "c-cpb", "color:indigo"),
            ("bit-cdb-bdy", "c-bdy", "color:violet"),
            ("bit-cdb-pre", "c-pre", "color:black"),
            ("bit-cdb-cod", "c-cod", "color:white"),
            ("bit-cdb-liv", "c-liv", "color:gray"),
        })
        {
            var element = com.Find($".{part}");
            Assert.IsTrue(element.ClassList.Contains(cssClass), $"{part} misses {cssClass}");
            StringAssert.Contains(element.GetAttribute("style"), style, $"{part} misses {style}");
        }
    }
}
