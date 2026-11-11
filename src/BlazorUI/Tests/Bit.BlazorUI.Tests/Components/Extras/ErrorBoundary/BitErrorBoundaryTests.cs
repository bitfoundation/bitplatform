using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

[TestClass]
public partial class BitErrorBoundaryTests : BunitTestContext
{
    [TestMethod]
    public void BitErrorBoundaryShouldRenderChildContentWhenNoError()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        var safe = component.Find(".safe");

        Assert.AreEqual("Hello", safe.TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderBodyAsChildContentAlias()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"body-content\">Body</div>")));
        });

        Assert.AreEqual("Body", component.Find(".body-content").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldPreferBodyOverChildContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"body-content\">Body</div>")));
            parameters.AddChildContent("<div class=\"child-content\">Child</div>");
        });

        component.Find(".body-content");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".child-content"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderDefaultErrorAndShowException()
    {
        var called = false;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => called = true));
            parameters.Add(p => p.ChildContent, ThrowingContent("boom"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsNotNull(errorRoot);
        Assert.Contains("Oops, Something went wrong", errorRoot.TextContent);
        Assert.Contains("boom", errorRoot.TextContent);
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderExceptionByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("hidden detail"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-exp"));
        Assert.IsFalse(component.Markup.Contains("hidden detail", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTitleAndMessage()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, "Custom title");
            parameters.Add(p => p.Message, "Custom message");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.Contains("Custom title", errorRoot.TextContent);
        Assert.Contains("Custom message", errorRoot.TextContent);
        Assert.IsFalse(errorRoot.TextContent.Contains("Oops", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldDropTheHeadingWithAnEmptyTitle()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.Message, "Only a message");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.Contains("Only a message", component.Find(".bit-erb").TextContent);

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ttl"));
    }

    [TestMethod]
    public void BitErrorBoundaryExceptionBlockShouldBeKeyboardReachable()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var block = component.Find(".bit-erb-exp");

        Assert.AreEqual("0", block.GetAttribute("tabindex"));

        // A focusable stop with no name is one a screen reader has nothing to announce for.
        Assert.AreEqual("region", block.GetAttribute("role"));
        Assert.AreEqual("Exception details", block.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyACustomExceptionLabel()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ExceptionLabel, "What the server said");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var block = component.Find(".bit-erb-exp");

        Assert.AreEqual("region", block.GetAttribute("role"));
        Assert.AreEqual("What the server said", block.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldDropTheExceptionRegionWithAnEmptyExceptionLabel()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ExceptionLabel, string.Empty);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var block = component.Find(".bit-erb-exp");

        // A landmark nobody can tell apart is worse than no landmark at all.
        Assert.IsFalse(block.HasAttribute("role"));
        Assert.AreEqual("0", block.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderTheCopyButtonByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var buttons = component.FindAll(".bit-erb-ftr button");

        Assert.HasCount(3, buttons);
        Assert.IsFalse(component.Markup.Contains("Copy details", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryCopyButtonShouldPutTheExceptionOnTheClipboard()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("copy me"));
        });

        var copyButton = component.FindAll(".bit-erb-ftr button").First(btn => btn.TextContent.Contains("Copy details", StringComparison.Ordinal));

        copyButton.Click();

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Extras.copyToClipboard"].Single();

        Assert.Contains("copy me", (string)invocation.Arguments[0]!);

        // A copy leaves nothing else on the screen to show for itself, so the button says what it did.
        Assert.Contains("Copied", component.Find(".bit-erb-ftr").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryCopyButtonShouldCarryCustomTexts()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.CopyText, "Take the details");
            parameters.Add(p => p.CopiedText, "On the clipboard");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var copyButton = component.FindAll(".bit-erb-ftr button").First(btn => btn.TextContent.Contains("Take the details", StringComparison.Ordinal));

        copyButton.Click();

        Assert.Contains("On the clipboard", component.Find(".bit-erb-ftr").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTheFooterForTheCopyButtonAlone()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, true);
            parameters.Add(p => p.HideHomeButton, true);
            parameters.Add(p => p.HideRecoverButton, true);
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.ContainsSingle(component.FindAll(".bit-erb-ftr button"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyTheCopyButtonClassesAndStyles()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.Add(p => p.Classes, new BitErrorBoundaryClassStyles { CopyButton = new() { Root = "custom-copy" } });
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles { CopyButton = new() { Root = "letter-spacing:2px" } });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var copyButton = component.Find(".bit-erb-ftr .custom-copy");

        Assert.Contains("letter-spacing:2px", copyButton.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderMessageElementWithoutMessage()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-msg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldAnnounceTheHeaderAlone()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Message, "What to do next");
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var header = component.Find(".bit-erb-hdr");

        // The alert holds the icon, the title and the message - and neither the stack trace nor the buttons,
        // which a live region would read out in full.
        Assert.AreEqual("alert", header.GetAttribute("role"));
        Assert.IsNotNull(header.QuerySelector(".bit-erb-svg"));
        Assert.IsNotNull(header.QuerySelector(".bit-erb-ttl"));
        Assert.IsNotNull(header.QuerySelector(".bit-erb-msg"));
        Assert.IsNull(header.QuerySelector(".bit-erb-exp"));
        Assert.IsNull(header.QuerySelector(".bit-erb-ftr"));
        Assert.IsFalse(component.Find(".bit-erb").HasAttribute("aria-live"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNameTheErrorUIByItsTitleAndDescribeItByItsMessage()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, "Broken");
            parameters.Add(p => p.Message, "Try again later");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");
        var title = component.Find(".bit-erb-ttl");
        var message = component.Find(".bit-erb-msg");

        Assert.AreEqual("group", errorRoot.GetAttribute("role"));
        Assert.IsFalse(string.IsNullOrEmpty(title.Id));
        Assert.AreEqual(title.Id, errorRoot.GetAttribute("aria-labelledby"));
        Assert.AreEqual(message.Id, errorRoot.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotNameAnUntitledErrorUI()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsFalse(errorRoot.HasAttribute("aria-labelledby"));
        Assert.IsFalse(errorRoot.HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotGroupAnUntitledErrorUIWhoseMessageIsTheAlert()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.Message, "Try again later");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.IsFalse(component.Find(".bit-erb").HasAttribute("role"));
        Assert.AreEqual("alert", component.Find(".bit-erb-hdr").GetAttribute("role"));
    }

    // The icon is hidden from a screen reader, so a header holding nothing else has nothing for an alert to read out,
    // and the root takes the alert instead - with or without the icon - rather than letting the error appear silently.
    [TestMethod,
        DataRow(false),
        DataRow(true)]
    public void BitErrorBoundaryShouldMakeTheRootTheAlertWhenTheHeaderHasNoText(bool hideIcon)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.HideIcon, hideIcon);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual("alert", component.Find(".bit-erb").GetAttribute("role"));

        if (hideIcon)
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-hdr"));
        }
        else
        {
            Assert.IsFalse(component.Find(".bit-erb-hdr").HasAttribute("role"));
        }
    }

    // A role or aria-live of the page's own makes the root the live region, announced the way the page asked - and
    // an alert nested in it would announce the error a second time, assertively.
    [TestMethod,
        DataRow("role", "status"),
        DataRow("role", "alert"),
        DataRow("role", "Log"),
        DataRow("aria-live", "polite"),
        DataRow("aria-live", "off")]
    public void BitErrorBoundaryShouldLeaveTheAnnouncementToALiveRegionOfThePage(string attribute, string value)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddUnmatched(attribute, value);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual(value, component.Find(".bit-erb").GetAttribute(attribute));
        Assert.IsFalse(component.Find(".bit-erb-hdr").HasAttribute("role"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldKeepTheAlertUnderASplattedRoleThatIsNoLiveRegion()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddUnmatched("role", "region");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual("region", component.Find(".bit-erb").GetAttribute("role"));
        Assert.AreEqual("alert", component.Find(".bit-erb-hdr").GetAttribute("role"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderAnEmptyHeader()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideIcon, true);
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-hdr"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldLeaveTheAnnouncementToTheFocusWithAutoFocus()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.IsFalse(component.Find(".bit-erb-hdr").HasAttribute("role"));
        Assert.AreEqual("group", component.Find(".bit-erb").GetAttribute("role"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldKeepTheAlertWithAutoFocusAndNoTitle()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.Title, string.Empty);
            parameters.Add(p => p.Message, "Nothing names the error UI, so the focus has nothing to announce.");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual("alert", component.Find(".bit-erb-hdr").GetAttribute("role"));
    }

    // What drops the alert is the focus actually being moved for this error, not the parameter: an AutoFocus turned
    // on after the error was caught moves nothing, so the alert stays.
    [TestMethod]
    public void BitErrorBoundaryShouldKeepTheAlertWhenAutoFocusIsTurnedOnAfterTheError()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.AreEqual("alert", component.Find(".bit-erb-hdr").GetAttribute("role"));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldPutTheAlertBackWhenTheFocusCannotBeMoved()
    {
        Context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetException(new InvalidOperationException("No focus"));

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.WaitForAssertion(() => Assert.AreEqual("alert", component.Find(".bit-erb-hdr").GetAttribute("role")));
    }

    // The root AutoFocus parks the focus on is marked as such, so that the stylesheet drops its ring - and a tabindex
    // the page wrote itself, -1 included, is a stop it asked for and keeps the browser's outline.
    [TestMethod,
        DataRow(null, true),
        DataRow("-1", false),
        DataRow("0", false)]
    public void BitErrorBoundaryShouldMarkOnlyTheRootItMadeFocusableAsAFocusAnchor(string? splattedTabIndex, bool isAnchor)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            if (splattedTabIndex is not null) parameters.AddUnmatched("tabindex", splattedTabIndex);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual(splattedTabIndex ?? "-1", errorRoot.GetAttribute("tabindex"));
        Assert.AreEqual(isAnchor, errorRoot.ClassList.Contains("bit-erb-anc"));
    }

    [TestMethod,
        DataRow(null, "H3"),
        DataRow(1, "H1"),
        DataRow(4, "H4"),
        DataRow(0, "H1"),
        DataRow(9, "H6")]
    public void BitErrorBoundaryShouldRenderTheTitleAtItsHeadingLevel(int? headingLevel, string expectedTag)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HeadingLevel, headingLevel);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var title = component.Find(".bit-erb-ttl");

        Assert.AreEqual(expectedTag, title.TagName);
        // The look stays the same at every level.
        Assert.IsTrue(title.ClassList.Contains("bit-txt-h3"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTheBuiltInIllustrationByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var svg = component.Find(".bit-erb-svg");

        Assert.AreEqual("true", svg.GetAttribute("aria-hidden"));
        component.Find(".bit-erb-svg-bg");
        component.Find(".bit-erb-svg-fg");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderIconNameInsteadOfTheIllustration()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.IconName, "ErrorBadge");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var icon = component.Find(".bit-erb-ico");

        Assert.Contains("bit-icon--ErrorBadge", icon.ClassName!);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-bug"));
            parameters.Add(p => p.IconName, "ErrorBadge");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var icon = component.Find(".bit-erb-ico");

        Assert.Contains("fa-solid", icon.ClassName!);
        Assert.Contains("fa-bug", icon.ClassName);
        Assert.IsFalse(icon.ClassName!.Contains("ErrorBadge", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderIconTemplate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.IconName, "ErrorBadge");
            parameters.Add(p => p.IconTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"custom-icon\">!</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".custom-icon");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ico"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldHideIcon()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideIcon, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ico"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderCustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        // Replacing the buttons and not the row: the custom footer lands in the same footer element the
        // default buttons are laid out in, which is what keeps them lined up rather than stacked.
        var footer = component.Find(".bit-erb-ftr");

        Assert.AreEqual(1, footer.QuerySelectorAll(".custom-footer").Length);
        Assert.AreEqual(0, footer.QuerySelectorAll("button").Length);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyTheFooterClassAndStyleToACustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.Classes, new BitErrorBoundaryClassStyles { Footer = "custom-ftr-class" });
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles { Footer = "gap:2rem" });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var footer = component.Find(".bit-erb-ftr");

        Assert.Contains("custom-ftr-class", footer.ClassName!);
        Assert.Contains("gap:2rem", footer.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderAFooterWithNothingToPutInIt()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, true);
            parameters.Add(p => p.HideHomeButton, true);
            parameters.Add(p => p.HideRecoverButton, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ftr"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderAdditionalButtons()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".extra-btn");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRenderAdditionalButtonsWithCustomFooter()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-footer\">Custom</div>")));
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".custom-footer");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".extra-btn"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderTheThreeDefaultButtonsWithCustomTexts()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RefreshText, "Reload it");
            parameters.Add(p => p.HomeText, "Take me home");
            parameters.Add(p => p.RecoverText, "Try again");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var buttons = component.FindAll(".bit-erb-ftr button");

        Assert.HasCount(3, buttons);
        Assert.Contains("Reload it", buttons[0].TextContent);
        Assert.Contains("Take me home", buttons[1].TextContent);
        Assert.Contains("Try again", buttons[2].TextContent);
    }

    [TestMethod]
    [DataRow(true, false, false, 2)]
    [DataRow(false, true, false, 2)]
    [DataRow(false, false, true, 2)]
    [DataRow(true, true, true, 0)]
    public void BitErrorBoundaryShouldHideTheDefaultButtons(bool hideRefresh, bool hideHome, bool hideRecover, int expectedCount)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, hideRefresh);
            parameters.Add(p => p.HideHomeButton, hideHome);
            parameters.Add(p => p.HideRecoverButton, hideRecover);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        if (expectedCount == 0)
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ftr"));
        }
        else
        {
            Assert.HasCount(expectedCount, component.FindAll(".bit-erb-ftr button"));
        }
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderFooterForAdditionalButtonsWithEveryDefaultButtonHidden()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HideRefreshButton, true);
            parameters.Add(p => p.HideHomeButton, true);
            parameters.Add(p => p.HideRecoverButton, true);
            parameters.Add(p => p.AdditionalButtons, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"extra-btn\">Extra</span>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb-ftr");
        component.Find(".extra-btn");
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldResetAfterError()
    {
        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        var recoverButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase));
        recoverButton.Click();

        var safe = component.Find(".throw-once-safe");
        Assert.AreEqual("Recovered", safe.TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryRecoverShouldRaiseOnRecover()
    {
        ThrowOnceComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase)).Click();

        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Manual, lastReason);
    }

    [TestMethod]
    public async Task BitErrorBoundaryRecoverMethodShouldRaiseOnRecoverAsManual()
    {
        ThrowOnceComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        await component.InvokeAsync(component.Instance.Recover);

        component.Find(".throw-once-safe");
        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Manual, lastReason);
    }

    [TestMethod]
    public async Task BitErrorBoundaryRecoverShouldDoNothingWithoutAnError()
    {
        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        await component.InvokeAsync(component.Instance.Recover);

        Assert.AreEqual(0, recovered);
        Assert.IsNull(lastReason);
        component.Find(".safe");
    }

    [TestMethod]
    public void BitErrorBoundaryHomeButtonShouldNavigate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HomeUrl, "https://example.com/home");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var homeButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Home", StringComparison.OrdinalIgnoreCase));

        homeButton.Click();

        var navMan = Services.GetRequiredService<BunitNavigationManager>();

        Assert.AreEqual("https://example.com/home", navMan.Uri);
    }

    [TestMethod]
    public void BitErrorBoundaryHomeButtonShouldNavigateToTheRootByDefault()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.FindAll("button").First(btn => btn.TextContent.Contains("Home", StringComparison.OrdinalIgnoreCase)).Click();

        var navMan = Services.GetRequiredService<BunitNavigationManager>();

        Assert.AreEqual(navMan.BaseUri, navMan.Uri);
    }

    [TestMethod]
    public void BitErrorBoundaryRefreshButtonShouldInvokeNavigation()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var navMan = Services.GetRequiredService<BunitNavigationManager>();

        var initialCount = navMan.History.Count;

        var refreshButton = component.FindAll("button").First(btn => btn.TextContent.Contains("Refresh", StringComparison.OrdinalIgnoreCase));

        refreshButton.Click();

        Assert.IsGreaterThan(initialCount, navMan.History.Count);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderErrorContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorContent, (RenderFragment<Exception>)(exception => b => b.AddMarkupContent(0, $"<div class=\"error-content\">{exception.Message}</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("from error content"));
        });

        Assert.AreEqual("from error content", component.Find(".error-content").TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRenderErrorTemplateWithTheContext()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(context => b => b.AddMarkupContent(0, $"<div class=\"error-template\">{context.Exception.Message}</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("from error template"));
        });

        Assert.AreEqual("from error template", component.Find(".error-template").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryErrorTemplateShouldTakePrecedenceOverErrorContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorContent, (RenderFragment<Exception>)(_ => b => b.AddMarkupContent(0, "<div class=\"error-content\">content</div>")));
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(_ => b => b.AddMarkupContent(0, "<div class=\"error-template\">template</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".error-template");

        Assert.Throws<ElementNotFoundException>(() => component.Find(".error-content"));
    }

    [TestMethod]
    public void BitErrorBoundaryErrorTemplateContextShouldRecover()
    {
        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(context => b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "class", "template-recover");
                b.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, context.Recover));
                b.AddContent(3, "Recover");
                b.CloseElement();
            }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".template-recover").Click();

        Assert.AreEqual("Recovered", component.Find(".throw-once-safe").TextContent);
    }

    [TestMethod]
    public async Task BitErrorBoundaryErrorTemplateContextShouldRefreshAndGoHome()
    {
        BitErrorBoundaryContext? context = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HomeUrl, "https://example.com/start");
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(ctx => b =>
            {
                context = ctx;
                b.AddMarkupContent(0, "<div class=\"tpl\">Template</div>");
            }));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var navMan = Services.GetRequiredService<BunitNavigationManager>();
        var initialCount = navMan.History.Count;

        await component.InvokeAsync(() => context!.Refresh());
        Assert.IsGreaterThan(initialCount, navMan.History.Count, "Refresh reloads the page like the default UI's Refresh button");

        await component.InvokeAsync(() => context!.GoHome());
        Assert.AreEqual("https://example.com/start", navMan.Uri);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldHandTheErrorTemplateTheSameContextWhileTheExceptionStays()
    {
        var contexts = new List<BitErrorBoundaryContext>();
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(ctx => b =>
            {
                contexts.Add(ctx);
                b.AddMarkupContent(0, "<div class=\"tpl\">Template</div>");
            }));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Render(parameters => parameters.Add(p => p.Title, "Another title"));

        Assert.IsGreaterThanOrEqualTo(2, contexts.Count, "the template is rendered again with the boundary");
        Assert.IsTrue(contexts.All(c => ReferenceEquals(c, contexts[0])), "a template bound to the context is not handed a new object on every render");
        Assert.AreSame(component.Instance.CaughtException, contexts[0].Exception);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldSurviveANavigationAfterItIsDisposed()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverOnNavigation, true);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, _ => recovered++));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        Context.DisposeComponentsAsync().GetAwaiter().GetResult();

        // A boundary that is gone has let go of the navigation manager, so the navigation reaches no handler of it.
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/another-page");

        Assert.AreEqual(0, recovered);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyClassAndStyleToTheErrorUI()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.Contains("custom-class", errorRoot.ClassName!);
        Assert.Contains("color: red", errorRoot.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyClassesAndStyles()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.Message, "a message");
            parameters.Add(p => p.Classes, new BitErrorBoundaryClassStyles
            {
                Root = "cls-root",
                Icon = "cls-icon",
                Title = "cls-title",
                Message = "cls-message",
                Exception = "cls-exception",
                Footer = "cls-footer"
            });
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles
            {
                Root = "outline-width: 2px;",
                Title = "font-style: italic"
            });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.Contains("cls-root", errorRoot.ClassName!);
        Assert.Contains("outline-width: 2px;", errorRoot.GetAttribute("style")!);
        component.Find(".cls-icon");
        component.Find(".cls-title");
        component.Find(".cls-message");
        component.Find(".cls-exception");
        component.Find(".cls-footer");
        Assert.Contains("font-style: italic", component.Find(".cls-title").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitErrorBoundaryStyleShouldBeAppendedToTheRootStyles()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.Styles, new BitErrorBoundaryClassStyles { Root = "background: blue" });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var style = component.Find(".bit-erb").GetAttribute("style");

        Assert.Contains("background: blue", style!);
        Assert.Contains("color: red", style);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyIdAndSplattedAttributes()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Id, "the-boundary");
            parameters.AddUnmatched("data-test", "value");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("the-boundary", errorRoot.GetAttribute("id"));
        Assert.AreEqual("value", errorRoot.GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldKeepSplattedAccessibilityAttributes()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddUnmatched("role", "status");
            parameters.AddUnmatched("aria-live", "polite");
            parameters.AddUnmatched("aria-atomic", "false");
            parameters.AddUnmatched("tabindex", "3");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("status", errorRoot.GetAttribute("role"));
        Assert.AreEqual("polite", errorRoot.GetAttribute("aria-live"));
        Assert.AreEqual("false", errorRoot.GetAttribute("aria-atomic"));
        Assert.AreEqual("3", errorRoot.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldKeepSplattedIdAndDirWhenTheParametersAreNotSet()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.HtmlAttributes, new System.Collections.Generic.Dictionary<string, object>
            {
                ["id"] = "splatted-id",
                ["dir"] = "rtl"
            });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("splatted-id", errorRoot.GetAttribute("id"));
        Assert.AreEqual("rtl", errorRoot.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitErrorBoundaryIdAndDirParametersShouldWinOverTheSplattedOnes()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Id, "the-boundary");
            parameters.Add(p => p.Dir, BitDir.Ltr);
            parameters.Add(p => p.HtmlAttributes, new System.Collections.Generic.Dictionary<string, object>
            {
                ["id"] = "splatted-id",
                ["dir"] = "rtl"
            });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual("the-boundary", errorRoot.GetAttribute("id"));
        Assert.AreEqual("ltr", errorRoot.GetAttribute("dir"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldMergeTheSplattedClassAndStyle()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "color: red");
            parameters.Add(p => p.HtmlAttributes, new System.Collections.Generic.Dictionary<string, object>
            {
                ["class"] = "splatted-class",
                ["style"] = "font-weight: bold"
            });
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");
        var style = errorRoot.GetAttribute("style");

        Assert.Contains("bit-erb", errorRoot.ClassName!);
        Assert.Contains("custom-class", errorRoot.ClassName);
        Assert.Contains("splatted-class", errorRoot.ClassName);
        Assert.Contains("color: red", style!);
        Assert.Contains("font-weight: bold", style);
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "rtl", true)]
    [DataRow(BitDir.Ltr, "ltr", false)]
    [DataRow(BitDir.Auto, "auto", false)]
    public void BitErrorBoundaryShouldApplyDir(BitDir dir, string expectedDir, bool expectedRtlClass)
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.AreEqual(expectedDir, errorRoot.GetAttribute("dir"));
        Assert.AreEqual(expectedRtlClass, errorRoot.ClassName!.Contains("bit-rtl", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyAutoFocus()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsTrue(errorRoot.HasAttribute("autofocus"));
        Assert.AreEqual("-1", errorRoot.GetAttribute("tabindex"));

        // The attribute alone is not the feature: an error UI is rendered into a document that is already
        // up, where autofocus is not honored, so the boundary has to ask for the focus itself.
        Context.JSInterop.VerifyFocusAsyncInvoke();
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotMoveTheFocusWithoutAutoFocus()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        var errorRoot = component.Find(".bit-erb");

        Assert.IsFalse(errorRoot.HasAttribute("autofocus"));
        Assert.IsFalse(errorRoot.HasAttribute("tabindex"));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldMoveTheFocusOnlyOncePerError()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");

        var focusCount = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.Title, "Another title");
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        Assert.HasCount(focusCount, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotMoveTheFocusWithAnErrorTemplate()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<BitErrorBoundaryContext>)(ctx => b => b.AddMarkupContent(0, "<div class=\"tpl\">Template</div>")));
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".tpl");

        // There is no element of the boundary's on the page for it to move the focus to - that is the
        // template's own to make.
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldLogThroughTheErrorBoundaryLogger()
    {
        var logger = new FakeErrorBoundaryLogger();
        Services.AddSingleton<IErrorBoundaryLogger>(logger);

        RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("logged"));
        });

        Assert.ContainsSingle(logger.Logged);
        Assert.AreEqual("logged", logger.Logged[0].Message);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotLogWithNoLogging()
    {
        var logger = new FakeErrorBoundaryLogger();
        Services.AddSingleton<IErrorBoundaryLogger>(logger);

        var called = false;

        RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.NoLogging, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => called = true));
            parameters.Add(p => p.ChildContent, ThrowingContent("not logged"));
        });

        Assert.IsEmpty(logger.Logged);
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldStillRenderTheErrorUIWhenTheLoggerThrows()
    {
        Services.AddSingleton<IErrorBoundaryLogger>(new ThrowingErrorBoundaryLogger());

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ChildContent, ThrowingContent("err"));
        });

        component.Find(".bit-erb");
    }

    [TestMethod]
    public async Task BitErrorBoundaryCaptureShouldRenderTheErrorUI()
    {
        var caught = default(Exception);
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, exception => caught = exception));
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        component.Find(".safe");

        var captured = new InvalidOperationException("captured out of band");

        await component.InvokeAsync(() => component.Instance.Capture(captured));

        var errorRoot = component.Find(".bit-erb");

        Assert.Contains("captured out of band", errorRoot.TextContent);
        Assert.AreSame(captured, caught);
        Assert.AreSame(captured, component.Instance.CaughtException);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".safe"));
    }

    [TestMethod]
    public async Task BitErrorBoundaryCaptureAsyncShouldRenderTheErrorUI()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        await component.Instance.CaptureAsync(new InvalidOperationException("captured async"));

        Assert.Contains("captured async", component.Find(".bit-erb").TextContent);
    }

    [TestMethod]
    public async Task BitErrorBoundaryCaptureShouldLogAndBeRecoverable()
    {
        var logger = new FakeErrorBoundaryLogger();
        Services.AddSingleton<IErrorBoundaryLogger>(logger);

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent("<div class=\"safe\">Hello</div>");
        });

        await component.InvokeAsync(() => component.Instance.Capture(new InvalidOperationException("captured")));

        Assert.ContainsSingle(logger.Logged);

        component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase)).Click();

        component.Find(".safe");
        Assert.IsNull(component.Instance.CaughtException);
    }

    [TestMethod]
    public async Task BitErrorBoundaryCaptureShouldKeepTheFirstExceptionWhileErrored()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.ChildContent, ThrowingContent("the first one"));
        });

        await component.InvokeAsync(() => component.Instance.Capture(new InvalidOperationException("the second one")));

        var errorRoot = component.Find(".bit-erb");

        Assert.Contains("the first one", errorRoot.TextContent);
        Assert.IsFalse(errorRoot.TextContent.Contains("the second one", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BitErrorBoundaryShouldCascadeItselfToItsContent()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<CapturingChildComponent>(0);
                b.AddAttribute(1, nameof(CapturingChildComponent.Exception), new InvalidOperationException("captured from the cascade"));
                b.CloseComponent();
            });
        });

        var child = component.FindComponent<CapturingChildComponent>().Instance;

        Assert.IsTrue(child.WasCascaded);

        await component.InvokeAsync(child.CaptureNow);

        Assert.Contains("captured from the cascade", component.Find(".bit-erb").TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverWhenRecoverKeysChange()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        component.Render(parameters => parameters.Add(p => p.RecoverKeys, [2]));

        component.Find(".throw-switch-safe");
        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Keys, lastReason);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRecoverWhenRecoverKeysStayTheSame()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.Title, "First");
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        component.Render(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.Title, "Second");
        });

        Assert.Contains("Second", component.Find(".bit-erb").TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".throw-switch-safe"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverWhenRecoverKeysAppear()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        // A list of keys that was not there and now is differs from what the boundary last saw, which is
        // the change it is rather than nothing at all.
        component.Render(parameters => parameters.Add(p => p.RecoverKeys, [1]));

        component.Find(".throw-switch-safe");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverWhenRecoverKeysDisappear()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        component.Render(parameters => parameters.Add(p => p.RecoverKeys, (System.Collections.Generic.IEnumerable<object?>?)null));

        component.Find(".throw-switch-safe");
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRecoverOnTheFirstRenderWithRecoverKeys()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverKeys, [1]);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, _ => recovered++));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        Assert.AreEqual(0, recovered);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverOnNavigation()
    {
        ThrowSwitchComponent.Reset();

        var recovered = 0;
        BitErrorBoundaryRecoverReason? lastReason = null;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.RecoverOnNavigation, true);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, reason => { recovered++; lastReason = reason; }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/another-page");

        component.WaitForAssertion(() => component.Find(".throw-switch-safe"));
        Assert.AreEqual(1, recovered);
        Assert.AreEqual(BitErrorBoundaryRecoverReason.Navigation, lastReason);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotRecoverOnNavigationByDefault()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/another-page");

        component.Find(".bit-erb");
        Assert.Throws<ElementNotFoundException>(() => component.Find(".throw-switch-safe"));
    }

    [TestMethod]
    public async Task BitErrorBoundaryRecoverShouldResetTheErrorCount()
    {
        ThrowSwitchComponent.Reset();

        var errors = 0;
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            // One error is all this boundary absorbs, so a second one being absorbed as well is what
            // says the count was reset by the recovery in between.
            parameters.Add(p => p.MaximumErrorCount, 1);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, _ => errors++));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowSwitchComponent>(0);
                b.CloseComponent();
            });
        });

        component.Find(".bit-erb");
        Assert.AreEqual(1, errors);

        await component.InvokeAsync(component.Instance.Recover);

        component.Find(".bit-erb");
        Assert.AreEqual(2, errors);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldNotAffectAnOuterBoundary()
    {
        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.Title, "Outer");
            parameters.AddChildContent(b =>
            {
                b.AddMarkupContent(0, "<div class=\"outer-safe\">Outer content</div>");
                b.OpenComponent<BitErrorBoundary>(1);
                b.AddAttribute(2, nameof(BitErrorBoundary.Title), "Inner");
                b.AddAttribute(3, nameof(BitErrorBoundary.ChildContent), ThrowingContent("inner error"));
                b.CloseComponent();
            });
        });

        component.Find(".outer-safe");

        var errorRoots = component.FindAll(".bit-erb");

        Assert.ContainsSingle(errorRoots);
        Assert.Contains("Inner", errorRoots[0].TextContent);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldShowWhatTheRecoverHandlerThrew()
    {
        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowException, true);
            parameters.Add(p => p.OnRecover, EventCallback.Factory.Create<BitErrorBoundaryRecoverReason>(this, async _ =>
            {
                await Task.Yield();

                throw new InvalidOperationException("the recovery itself failed");
            }));
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.FindAll("button").First(btn => btn.TextContent.Contains("Recover", StringComparison.OrdinalIgnoreCase)).Click();

        // The handler was the one place that was to put right whatever threw, so what it threw is the
        // boundary's own error rather than an exception nothing is left to await.
        component.WaitForAssertion(() =>
            Assert.Contains("the recovery itself failed", component.Find(".bit-erb-exp").TextContent));
    }

    [TestMethod]
    public async Task BitErrorBoundaryShouldNotSayCopiedForACopyItRecoveredOutOfAsync()
    {
        var handler = Context.JSInterop.SetupVoid("BitBlazorUI.Extras.copyToClipboard", _ => true);

        ThrowOnceComponent.Reset();

        var component = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.ShowCopyButton, true);
            parameters.AddChildContent(b =>
            {
                b.OpenComponent<ThrowOnceComponent>(0);
                b.CloseComponent();
            });
        });

        component.FindAll(".bit-erb-ftr button").First(btn => btn.TextContent.Contains("Copy details", StringComparison.Ordinal)).Click();

        await component.InvokeAsync(component.Instance.Recover);

        component.Find(".throw-once-safe");

        // The clipboard write lands after the boundary has already cleared the error the copy belonged
        // to, so it has nothing left to report on. It is completed on the dispatcher, so the copy's
        // continuation has run by the time the turn is over - completed from the test thread, it would be
        // posted to the dispatcher and the assertion below could pass before it ever ran.
        await component.InvokeAsync(() => handler.SetVoidResult());

        // The error UI went with the recovery, so the markup could not say "Copied" right now either way:
        // where a stale message would show is on the next error the boundary puts up, whose button has to
        // offer a copy of its own.
        await component.InvokeAsync(() => component.Instance.Capture(new InvalidOperationException("the next one")));

        var footer = component.Find(".bit-erb-ftr").TextContent;

        Assert.Contains("Copy details", footer);
        Assert.IsFalse(footer.Contains("Copied", StringComparison.Ordinal));
    }

    private static RenderFragment ThrowingContent(string message) => b =>
    {
        b.OpenComponent<ThrowingComponent>(0);
        b.AddAttribute(1, "Message", message);
        b.CloseComponent();
    };
}
