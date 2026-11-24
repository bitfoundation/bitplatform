using System;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CopyButton;

[TestClass, DoNotParallelize]
public class BitCopyButtonTests : BunitTestContext
{
    private const string CopyIdentifier = "BitBlazorUI.Extras.copyToClipboard";

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    [TestMethod]
    public void TheCopyButtonShouldRenderAnIconButtonNamedCopyWithAnEmptyLiveRegion()
    {
        var component = RenderComponent<BitCopyButton>(p => p.Add(c => c.Value, "hello"));

        var root = component.Find(".bit-cpb");
        var button = component.Find("button");

        Assert.AreEqual("SPAN", root.TagName);
        Assert.IsTrue(button.ClassList.Contains("bit-btn"));
        Assert.IsTrue(button.ClassList.Contains("bit-btn-ntx"), "a copy button with no text is an icon button");
        Assert.AreEqual("Copy", button.GetAttribute("title"));
        Assert.AreEqual("Copy", button.QuerySelector(".bit-btn-srl")!.TextContent.Trim(), "the name is kept as screen-reader-only text");
        // The Copy glyph is drawn from the subset font of the Extras package, not being one of the core font's.
        Assert.IsNotNull(button.QuerySelector("i.bit-icon--Copy.bit-icon-ex.bit-icon-ex--Copy"));

        var live = component.Find(".bit-cpb-liv");
        Assert.AreEqual("status", live.GetAttribute("role"));
        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.AreEqual(string.Empty, live.TextContent);
    }

    [TestMethod]
    public void TheValueShouldBeHandedToTheClipboardScriptAndOnCopyRaisedWithIt()
    {
        var handler = Context.JSInterop.SetupVoid(CopyIdentifier, _ => true);
        handler.SetVoidResult();
        string? copied = null;

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "the code")
            .Add(c => c.OnCopy, (string value) => copied = value));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("the code", copied));
        Assert.AreEqual("the code", handler.Invocations.Single().Arguments[0]);
    }

    [TestMethod]
    public void ACopyShouldBeConfirmedAnnouncedAndResetAfterTheCopiedDuration()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Text, "Copy code")
            .Add(c => c.CopiedDuration, TimeSpan.FromMilliseconds(50)));

        Assert.AreEqual("Copy code", component.Find(".bit-btn-prt").TextContent.Trim());

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("Copied", component.Find(".bit-cpb-liv").TextContent));
        Assert.AreEqual("Copied", component.Find(".bit-btn-prt").TextContent.Trim());
        // The check mark of the core font, with no glyph class of the Extras subset left on it.
        Assert.IsNotNull(component.Find("button").QuerySelector("i.bit-icon--Accept"));
        Assert.IsNull(component.Find("button").QuerySelector("i.bit-icon-ex"));

        component.WaitForAssertion(() => Assert.AreEqual("Copy code", component.Find(".bit-btn-prt").TextContent.Trim()), TimeSpan.FromSeconds(2));
        Assert.AreEqual(string.Empty, component.Find(".bit-cpb-liv").TextContent);
        Assert.IsNotNull(component.Find("button").QuerySelector("i.bit-icon-ex--Copy"));
    }

    [TestMethod]
    public void TheCopiedTextAndIconShouldReplaceTheDefaultOnes()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Text, "Copy link")
            .Add(c => c.IconName, "Link")
            .Add(c => c.CopiedText, "Link copied!")
            .Add(c => c.CopiedIconName, "CompletedSolid")
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1)));

        var button = component.Find("button");
        Assert.IsNotNull(button.QuerySelector("i.bit-icon--Link"));
        Assert.IsNull(button.QuerySelector("i.bit-icon-ex"), "a custom icon gets no glyph class of the Extras subset");

        button.Click();

        component.WaitForAssertion(() => Assert.AreEqual("Link copied!", component.Find(".bit-cpb-liv").TextContent));
        Assert.AreEqual("Link copied!", component.Find(".bit-btn-prt").TextContent.Trim());
        Assert.IsNotNull(component.Find("button").QuerySelector("i.bit-icon--CompletedSolid"));
    }

    [TestMethod]
    public void AnExternalIconShouldWinOverTheIconName()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Icon, BitIconInfo.Fa("regular copy"))
            .Add(c => c.IconName, "Link")
            .Add(c => c.CopiedIcon, BitIconInfo.Fa("solid check"))
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1)));

        Assert.IsNotNull(component.Find("button").QuerySelector("i.fa-regular.fa-copy"));
        Assert.IsNull(component.Find("button").QuerySelector("i.bit-icon--Link"));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.IsNotNull(component.Find("button").QuerySelector("i.fa-solid.fa-check")));
    }

    [TestMethod]
    public void AnIconButtonShouldBeNamedCopiedWhileTheCopyIsConfirmed()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1)));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("Copied", component.Find("button").GetAttribute("title")));
        Assert.AreEqual("Copied", component.Find(".bit-btn-srl").TextContent.Trim());
    }

    [TestMethod]
    public void TheAriaLabelShouldNameAnIconButton()
    {
        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.AriaLabel, "Copy the install command"));

        var button = component.Find("button");

        Assert.AreEqual("Copy the install command", button.GetAttribute("aria-label"));
        Assert.AreEqual("Copy the install command", button.GetAttribute("title"));
    }

    [TestMethod]
    public void AButtonWithTextShouldHaveNoTooltip()
    {
        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Text, "Copy"));

        var button = component.Find("button");

        Assert.IsFalse(button.ClassList.Contains("bit-btn-ntx"));
        Assert.IsFalse(button.HasAttribute("title"));
    }

    [TestMethod]
    public void AFailedCopyShouldConfirmNothingAndRaiseOnCopyFailed()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetException(new JSException("denied"));
        var copied = false;
        Exception? failure = null;

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Text, "Copy")
            .Add(c => c.OnCopy, () => copied = true)
            .Add(c => c.OnCopyFailed, (Exception e) => failure = e));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.IsNotNull(failure));
        Assert.IsInstanceOfType<JSException>(failure);
        Assert.IsFalse(copied);
        Assert.AreEqual("Copy", component.Find(".bit-btn-prt").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(".bit-cpb-liv").TextContent);
    }

    [TestMethod]
    public void NoValueShouldDisableTheButton()
    {
        var handler = Context.JSInterop.SetupVoid(CopyIdentifier, _ => true);
        handler.SetVoidResult();

        var component = RenderComponent<BitCopyButton>();

        Assert.AreEqual("true", component.Find("button").GetAttribute("aria-disabled"));

        component.Find("button").Click();

        Assert.AreEqual(0, handler.Invocations.Count);
    }

    [TestMethod]
    public void NoValueShouldMarkTheRootDisabledAndDropThePageActivationHandlers()
    {
        var clicks = 0;

        var withoutValue = RenderSplatted(null, () => clicks++).Find(".bit-cpb");

        Assert.IsTrue(withoutValue.ClassList.Contains("bit-dis"));
        Assert.IsFalse(withoutValue.HasAttribute("blazor:onclick"));

        var withValue = RenderSplatted("hello", () => clicks++).Find(".bit-cpb");

        Assert.IsFalse(withValue.ClassList.Contains("bit-dis"));

        withValue.Click();

        Assert.AreEqual(1, clicks);
    }

    private IRenderedComponent<BitParams> RenderSplatted(string? value, Action onClick)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCopyButton>(0);
                builder.AddAttribute(1, nameof(BitCopyButton.Value), value);
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void AFailedCopyShouldTakeOffTheConfirmationOfAnEarlierOne()
    {
        var handler = Context.JSInterop.SetupVoid(CopyIdentifier, _ => true);
        handler.SetVoidResult();
        Exception? failure = null;

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Text, "Copy")
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1))
            .Add(c => c.OnCopyFailed, (Exception e) => failure = e));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("Copied", component.Find(".bit-btn-prt").TextContent.Trim()));

        handler.SetException(new JSException("denied"));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.IsNotNull(failure));
        Assert.AreEqual("Copy", component.Find(".bit-btn-prt").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(".bit-cpb-liv").TextContent);
    }

    [TestMethod]
    public void AChangedValueShouldTakeOffTheConfirmation()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "row 1")
            .Add(c => c.Text, "Copy")
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1)));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("Copied", component.Find(".bit-btn-prt").TextContent.Trim()));

        // The same value handed in again keeps the confirmation.
        component.Render(p => p.Add(c => c.Value, "row 1"));

        Assert.AreEqual("Copied", component.Find(".bit-btn-prt").TextContent.Trim());

        component.Render(p => p.Add(c => c.Value, "row 7"));

        Assert.AreEqual("Copy", component.Find(".bit-btn-prt").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(".bit-cpb-liv").TextContent);
    }

    [TestMethod]
    public void ACopyWhileConfirmedShouldStillBeAnnounced()
    {
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();
        var copies = 0;

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1))
            .Add(c => c.OnCopy, () => copies++));

        component.Find("button").Click();
        component.WaitForAssertion(() => Assert.AreEqual(1, copies));

        component.Find("button").Click();
        component.WaitForAssertion(() => Assert.AreEqual(2, copies));

        // The text sits in a keyed node of its own, which every copy replaces.
        Assert.AreEqual("Copied", component.Find(".bit-cpb-liv > span").TextContent);
    }

    [TestMethod]
    public void AZeroCopiedDurationShouldCopyWithoutConfirming()
    {
        var handler = Context.JSInterop.SetupVoid(CopyIdentifier, _ => true);
        handler.SetVoidResult();
        string? copied = null;

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Text, "Copy")
            .Add(c => c.CopiedDuration, TimeSpan.Zero)
            .Add(c => c.OnCopy, (string value) => copied = value));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("hello", copied));
        Assert.AreEqual("Copy", component.Find(".bit-btn-prt").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(".bit-cpb-liv").TextContent);
    }

    [TestMethod]
    public void AButtonClassChangedInPlaceShouldReachTheButton()
    {
        var classes = new BitCopyButtonClassStyles { Button = new() { Icon = "icon-a" } };

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Classes, classes));

        Assert.IsNotNull(component.Find("button").QuerySelector("i.bit-icon-ex--Copy.icon-a"));

        classes.Button.Icon = "icon-b";
        component.Render();

        Assert.IsNotNull(component.Find("button").QuerySelector("i.bit-icon-ex--Copy.icon-b"));
        Assert.IsNull(component.Find("button").QuerySelector("i.icon-a"));
    }

    [TestMethod]
    public void AnEmptyValueShouldStillBeCopied()
    {
        var handler = Context.JSInterop.SetupVoid(CopyIdentifier, _ => true);
        handler.SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p.Add(c => c.Value, string.Empty));

        Assert.IsNull(component.Find("button").GetAttribute("aria-disabled"));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual(string.Empty, handler.Invocations.Single().Arguments[0]));
    }

    [TestMethod]
    public void ADisabledCopyButtonShouldCopyNothing()
    {
        var handler = Context.JSInterop.SetupVoid(CopyIdentifier, _ => true);
        handler.SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-cpb").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find("button").GetAttribute("aria-disabled"));

        component.Find("button").Click();

        Assert.AreEqual(0, handler.Invocations.Count);
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-btn-fil")]
    [DataRow(BitVariant.Outline, "bit-btn-otl")]
    [DataRow(BitVariant.Text, "bit-btn-txt")]
    public void TheVariantShouldReachTheButton(BitVariant variant, string variantClass)
    {
        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Variant, variant));

        Assert.IsTrue(component.Find("button").ClassList.Contains(variantClass));
    }

    [TestMethod]
    public void TheColorAndSizeShouldReachTheButton()
    {
        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Color, BitColor.Error)
            .Add(c => c.Size, BitSize.Small));

        var button = component.Find("button");

        Assert.IsTrue(button.ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(button.ClassList.Contains("bit-btn-sm"));
    }

    [TestMethod]
    public void TheClassesAndStylesShouldReachEveryPart()
    {
        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.Class, "root-class")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Classes, new() { Root = "root-part", Button = new() { Root = "button-class", Icon = "icon-class" }, LiveRegion = "live-class" })
            .Add(c => c.Styles, new() { Root = "padding:2px", Button = new() { Root = "color:red" }, LiveRegion = "top:0" }));

        var root = component.Find(".bit-cpb");
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("root-part"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");

        var button = component.Find("button");
        Assert.IsTrue(button.ClassList.Contains("button-class"));
        StringAssert.Contains(button.GetAttribute("style"), "color:red");
        // The page's own icon class is kept beside the glyph classes of the Extras subset.
        Assert.IsNotNull(button.QuerySelector("i.bit-icon-ex--Copy.icon-class"));

        var live = component.Find(".bit-cpb-liv");
        Assert.IsTrue(live.ClassList.Contains("live-class"));
        Assert.AreEqual("top:0", live.GetAttribute("style"));
    }

    [TestMethod]
    public void TheDefaultTextsShouldComeFromTheStringsInForce()
    {
        BitStrings.Current = new GermanStrings();
        Context.JSInterop.SetupVoid(CopyIdentifier, _ => true).SetVoidResult();

        var component = RenderComponent<BitCopyButton>(p => p
            .Add(c => c.Value, "hallo")
            .Add(c => c.CopiedDuration, TimeSpan.FromMinutes(1)));

        Assert.AreEqual("Kopieren", component.Find("button").GetAttribute("title"));

        component.Find("button").Click();

        component.WaitForAssertion(() => Assert.AreEqual("Kopiert", component.Find(".bit-cpb-liv").TextContent));
    }

    private sealed class GermanStrings : BitStrings
    {
        public override string Copy => "Kopieren";
        public override string Copied => "Kopiert";
    }
}
