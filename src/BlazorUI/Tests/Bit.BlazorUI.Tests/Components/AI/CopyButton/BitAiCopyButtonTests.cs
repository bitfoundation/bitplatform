using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.CopyButton;

[TestClass]
public class BitAiCopyButtonTests : BitAiTestBase
{
    private const string CopyIdentifier = "BitBlazorUI.AiCopyButton.copy";

    [TestMethod]
    public void TheCopyButtonAnnouncesTheCopy()
    {
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetResult(true);

        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "hello")
            .Add(c => c.ConfirmationDuration, TimeSpan.FromMilliseconds(50)));

        Assert.AreEqual("Copy", button.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual(string.Empty, button.Find("[role=status]").TextContent);

        button.Find("button").Click();

        button.WaitForAssertion(() => Assert.AreEqual("Copied", button.Find("[role=status]").TextContent));
        Assert.AreEqual("Copied", button.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual("Copied", button.Find("button").GetAttribute("title"));
        button.WaitForAssertion(() => Assert.AreEqual("Copy", button.Find("button").GetAttribute("aria-label")), TimeSpan.FromSeconds(2));
        Assert.AreEqual(string.Empty, button.Find("[role=status]").TextContent);
    }

    [TestMethod]
    public void TheTextIsHandedToTheClipboardScriptAndOnCopiedIsRaised()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);
        var copied = 0;

        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "the code")
            .Add(c => c.OnCopied, () => copied++));

        button.Find("button").Click();

        button.WaitForAssertion(() => Assert.AreEqual(1, copied));
        Assert.AreEqual("the code", invocation.Invocations.Single().Arguments[0]);
    }

    [TestMethod]
    public void AFailedCopyConfirmsNothing()
    {
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetResult(false);
        var copied = false;

        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "hello")
            .Add(c => c.OnCopied, () => copied = true));

        button.Find("button").Click();

        Assert.IsFalse(copied);
        Assert.AreEqual("Copy", button.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual(string.Empty, button.Find("[role=status]").TextContent);
    }

    [TestMethod]
    public void NothingToCopyDisablesTheButton()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);

        var button = RenderComponent<BitAiCopyButton>();

        Assert.AreEqual("true", button.Find("button").GetAttribute("aria-disabled"));

        button.Find("button").Click();

        Assert.AreEqual(0, invocation.Invocations.Count);
    }

    [TestMethod]
    public void ADisabledCopyButtonCopiesNothing()
    {
        var invocation = Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true);
        invocation.SetResult(true);

        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "hello")
            .Add(c => c.Disabled, true));

        Assert.IsTrue(button.Find(".bit-aicpb").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", button.Find("button").GetAttribute("aria-disabled"));

        button.Find("button").Click();

        Assert.AreEqual(0, invocation.Invocations.Count);
    }

    [TestMethod]
    public void TheLabelNamesTheButtonAndIsShownOnlyWhenAskedFor()
    {
        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "x")
            .Add(c => c.Label, "Copy code"));

        Assert.AreEqual("Copy code", button.Find("button").GetAttribute("aria-label"));
        Assert.AreEqual(string.Empty, button.Find("button").TextContent.Trim());

        button.Render(p => p.Add(c => c.ShowLabel, true));

        StringAssert.Contains(button.Find("button").TextContent, "Copy code");
    }

    [TestMethod]
    public void TheButtonIsANeutralIconButton()
    {
        var button = RenderComponent<BitAiCopyButton>(p => p.Add(c => c.Content, "x"));

        var element = button.Find("button");

        Assert.IsTrue(element.ClassList.Contains("bit-btn"));
        Assert.IsTrue(element.ClassList.Contains("bit-btn-dft-ter"));
        // The Copy glyph is drawn from the subset font of the AI package, not being one of the core font's.
        Assert.IsNotNull(element.QuerySelector("i.bit-icon--Copy.bit-icon-ai.bit-icon-ai--Copy"));
    }

    [TestMethod]
    public void TheConfirmationGlyphIsTheCoreCheckMark()
    {
        Context.JSInterop.Setup<bool>(CopyIdentifier, _ => true).SetResult(true);

        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "x")
            .Add(c => c.Classes, new BitAiCopyButtonClassStyles { Button = new() { Icon = "icon-class" } }));

        Assert.IsNotNull(button.Find("button").QuerySelector("i.bit-icon-ai--Copy.icon-class"));

        button.Find("button").Click();

        button.WaitForAssertion(() => Assert.IsNotNull(button.Find("button").QuerySelector("i.bit-icon--Accept.icon-class")));
        Assert.IsNull(button.Find("button").QuerySelector("i.bit-icon-ai"));
    }

    [TestMethod]
    public void BitAiCopyButtonShouldRenderTheRootClassAndPartClassStyles()
    {
        var button = RenderComponent<BitAiCopyButton>(p => p
            .Add(c => c.Content, "x")
            .Add(c => c.Id, "copy")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin: 1px")
            .Add(c => c.Classes, new BitAiCopyButtonClassStyles { Root = "root-class", LiveRegion = "live-class", Button = new() { Root = "button-class" } })
            .Add(c => c.Styles, new BitAiCopyButtonClassStyles { LiveRegion = "color: red" }));

        var root = button.Find(".bit-aicpb");

        Assert.AreEqual("copy", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsTrue(button.Find(".bit-aicpb-liv").ClassList.Contains("live-class"));
        Assert.AreEqual("color: red", button.Find(".bit-aicpb-liv").GetAttribute("style"));
        Assert.IsTrue(button.Find("button").ClassList.Contains("button-class"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiCopyButtonShouldRespectDir(BitDir? dir, string? expected)
    {
        var button = RenderComponent<BitAiCopyButton>(p => p.Add(c => c.Dir, dir));

        var root = button.Find(".bit-aicpb");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiCopyButtonShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var button = RenderComponent<BitAiCopyButton>(p => p.Add(c => c.Visibility, visibility));

        var style = button.Find(".bit-aicpb").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void BitAiCopyButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiCopyButton", BitAiCopyButtonParams.ParamName);
        Assert.AreEqual(BitAiCopyButtonParams.ParamName, new BitAiCopyButtonParams().Name);
    }

    [TestMethod]
    public void BitAiCopyButtonParamsShouldCarryEveryParameterThatBelongsToAGroupOfCopyButtons()
    {
        string[] notCascaded =
        [
            nameof(BitAiCopyButton.CascadingParameters),
            nameof(BitAiCopyButton.CascadedOptions),
            nameof(BitAiCopyButton.Content),
            nameof(BitAiCopyButton.Label),
            nameof(BitAiCopyButton.OnCopied),
        ];
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAiCopyButton).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                .Select(p => p.Name)
                                                .Where(n => baseParameters.Contains(n) is false && notCascaded.Contains(n) is false)
                                                .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAiCopyButtonParams).GetProperty(name), $"BitAiCopyButtonParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAiCopyButtonShouldTakeTheCascadedValuesButKeepItsOwn()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiCopyButtonParams { Class = "cascaded", ShowLabel = true, Classes = new() { LiveRegion = "cascaded-live" } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiCopyButton>(0);
                builder.AddAttribute(1, nameof(BitAiCopyButton.Content), "x");
                builder.CloseComponent();
                builder.OpenComponent<BitAiCopyButton>(2);
                builder.AddAttribute(3, nameof(BitAiCopyButton.Content), "y");
                builder.AddAttribute(4, nameof(BitAiCopyButton.ShowLabel), false);
                builder.CloseComponent();
            });
        });

        var roots = component.FindAll(".bit-aicpb");

        Assert.IsTrue(roots[0].ClassList.Contains("cascaded"));
        StringAssert.Contains(roots[0].QuerySelector("button")!.TextContent, "Copy");
        Assert.IsTrue(roots[0].QuerySelector(".bit-aicpb-liv")!.ClassList.Contains("cascaded-live"));
        Assert.AreEqual(string.Empty, roots[1].QuerySelector("button")!.TextContent.Trim());
    }
}
