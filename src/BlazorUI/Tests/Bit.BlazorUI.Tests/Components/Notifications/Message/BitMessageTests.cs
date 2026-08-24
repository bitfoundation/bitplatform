using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

[TestClass]
public class BitMessageTests : BunitTestContext
{
    private const string LongText = "In the beginning, there is silence - a blank canvas yearning to be filled.";

    // The auto-dismiss callback fires off the render loop, so there is no render for WaitForAssertion to
    // hang its re-check on. The condition is polled from the test thread instead.
    private static void WaitUntil(Func<bool> condition, int timeoutMilliseconds = 5000)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            if (condition()) return;

            Thread.Sleep(20);
        }
    }



    [TestMethod]
    public void BitMessageShouldRenderTheDefaultStructure()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.AddChildContent("Hello");
        });

        var root = component.Find(".bit-msg");

        Assert.AreEqual("DIV", root.TagName);
        // The defaults: the Fill variant, the Info color and the Medium size.
        Assert.IsTrue(root.ClassList.Contains("bit-msg-fil"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-inf"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-md"));

        Assert.HasCount(1, component.FindAll(".bit-msg-rct"));
        Assert.HasCount(1, component.FindAll(".bit-msg-con"));
        Assert.HasCount(1, component.FindAll(".bit-msg-ict"));
        Assert.HasCount(1, component.FindAll(".bit-msg-cnc"));
        Assert.HasCount(1, component.FindAll(".bit-msg-cnw"));
        Assert.AreEqual("Hello", component.Find(".bit-msg-cnt").TextContent.Trim());

        // Nothing optional renders on its own.
        Assert.IsEmpty(component.FindAll(".bit-msg-ttl"));
        Assert.IsEmpty(component.FindAll(".bit-msg-act"));
        Assert.IsEmpty(component.FindAll(".bit-msg-exb"));
        Assert.IsEmpty(component.FindAll(".bit-msg-dmb"));
    }

    [TestMethod]
    public void BitMessageContentShouldTakePrecedenceOverChildContent()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.AddChildContent("<span>child</span>");
            parameters.Add(p => p.Content, "<span>content</span>");
        });

        Assert.AreEqual("content", component.Find(".bit-msg-cnt").TextContent.Trim());
    }



    [TestMethod,
        DataRow(BitColor.Primary, "bit-msg-pri", "Info"),
        DataRow(BitColor.Secondary, "bit-msg-sec", "Info"),
        DataRow(BitColor.Tertiary, "bit-msg-ter", "Info"),
        DataRow(BitColor.Info, "bit-msg-inf", "Info"),
        DataRow(BitColor.Success, "bit-msg-suc", "Completed"),
        DataRow(BitColor.Warning, "bit-msg-wrn", "Info"),
        DataRow(BitColor.SevereWarning, "bit-msg-swr", "Warning"),
        DataRow(BitColor.Error, "bit-msg-err", "ErrorBadge"),
        DataRow(BitColor.PrimaryBackground, "bit-msg-pbg", "Info"),
        DataRow(BitColor.SecondaryBackground, "bit-msg-sbg", "Info"),
        DataRow(BitColor.TertiaryBackground, "bit-msg-tbg", "Info"),
        DataRow(BitColor.PrimaryForeground, "bit-msg-pfg", "Info"),
        DataRow(BitColor.SecondaryForeground, "bit-msg-sfg", "Info"),
        DataRow(BitColor.TertiaryForeground, "bit-msg-tfg", "Info"),
        DataRow(BitColor.PrimaryBorder, "bit-msg-pbr", "Info"),
        DataRow(BitColor.SecondaryBorder, "bit-msg-sbr", "Info"),
        DataRow(BitColor.TertiaryBorder, "bit-msg-tbr", "Info")
    ]
    public void BitMessageShouldTakeCorrectColorAndIcon(BitColor color, string expectedClass, string expectedIcon)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-msg").ClassList.Contains(expectedClass));
        Assert.IsTrue(component.Find(".bit-msg-ict > i").ClassList.Contains($"bit-icon--{expectedIcon}"));
    }

    [TestMethod,
        DataRow(null, "bit-msg-fil"),
        DataRow(BitVariant.Fill, "bit-msg-fil"),
        DataRow(BitVariant.Outline, "bit-msg-otl"),
        DataRow(BitVariant.Text, "bit-msg-txt")
    ]
    public void BitMessageShouldRespectVariant(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(component.Find(".bit-msg").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(null, "bit-msg-md"),
        DataRow(BitSize.Small, "bit-msg-sm"),
        DataRow(BitSize.Medium, "bit-msg-md"),
        DataRow(BitSize.Large, "bit-msg-lg")
    ]
    public void BitMessageShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-msg").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMessageShouldRespectSquare(bool square)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Square, square);
        });

        Assert.AreEqual(square, component.Find(".bit-msg").ClassList.Contains("bit-msg-sqr"));
    }

    [TestMethod,
        DataRow(BitAlignment.Start, "flex-start"),
        DataRow(BitAlignment.End, "flex-end"),
        DataRow(BitAlignment.Center, "center"),
        DataRow(BitAlignment.SpaceBetween, "space-between"),
        DataRow(BitAlignment.SpaceAround, "space-around"),
        DataRow(BitAlignment.SpaceEvenly, "space-evenly"),
        DataRow(BitAlignment.Baseline, "baseline"),
        DataRow(BitAlignment.Stretch, "stretch"),
        DataRow(null, "flex-start")
    ]
    public void BitMessageShouldRespectAlignment(BitAlignment? alignment, string expectedValue)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Alignment, alignment);
        });

        var style = component.Find(".bit-msg").GetAttribute("style");

        StringAssert.Contains(style, $"--bit-msg-justifycontent:{expectedValue}");
    }

    [TestMethod,
        DataRow(1),
        DataRow(7),
        DataRow(24)
    ]
    public void BitMessageShouldRespectElevationInsideTheScale(int elevation)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Elevation, elevation);
        });

        StringAssert.Contains(component.Find(".bit-msg").GetAttribute("style"), $"--bit-msg-boxshadow:var(--bit-shd-{elevation})");
    }

    [TestMethod,
        DataRow(null),
        DataRow(0),
        DataRow(-1),
        DataRow(25),
        DataRow(100)
    ]
    public void BitMessageShouldIgnoreElevationOutsideTheScale(int? elevation)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Elevation, elevation);
        });

        var style = component.Find(".bit-msg").GetAttribute("style") ?? string.Empty;

        Assert.IsFalse(style.Contains("--bit-msg-boxshadow"));
    }



    [TestMethod,
        DataRow("Emoji2"),
        DataRow("WordLogo")
    ]
    public void BitMessageShouldRespectCustomIcon(string iconName)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = component.Find(".bit-msg-ict > i");
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitMessageIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji2");
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid circle-info"));
        });

        var icon = component.Find(".bit-msg-ict > i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-circle-info"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Emoji2"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMessageShouldRespectHideIcon(bool hideIcon)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.HideIcon, hideIcon);
        });

        Assert.HasCount(hideIcon ? 0 : 1, component.FindAll(".bit-msg-ict"));
    }

    [TestMethod]
    public void BitMessageIconShouldBeHiddenFromAssistiveTechnology()
    {
        var component = RenderComponent<BitMessage>();

        Assert.AreEqual("true", component.Find(".bit-msg-ict").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitMessageIconTemplateShouldTakeThePlaceOfTheIcon()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IconName, "Emoji2");
            parameters.Add(p => p.Icon, BitIconInfo.Bi("gear-fill"));
            parameters.Add(p => p.IconTemplate, "<span class='custom-icon'>*</span>");
        });

        Assert.HasCount(1, component.FindAll(".bit-msg-ict .custom-icon"));
        Assert.IsEmpty(component.FindAll(".bit-msg-ico"));
    }

    [TestMethod]
    public void BitMessageShouldNotRenderTheIconTemplateWhileTheIconIsHidden()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.HideIcon, true);
            parameters.Add(p => p.IconTemplate, "<span class='custom-icon'>*</span>");
        });

        Assert.IsEmpty(component.FindAll(".bit-msg-ict"));
        Assert.IsEmpty(component.FindAll(".custom-icon"));
    }

    [TestMethod]
    public void BitMessageShouldRenderTheIconAriaLabelInsideTheAnnouncedRegion()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.IconAriaLabel, "Error");
            parameters.Add(p => p.Title, "The title");
            parameters.AddChildContent("Something went wrong.");
        });

        var label = component.Find(".bit-msg-cnc .bit-msg-ilb");

        Assert.AreEqual("Error", label.TextContent);
        // It leads the announcement, so it has to come before the title and the content in the DOM.
        Assert.IsTrue(label.ParentElement!.Children[0].ClassList.Contains("bit-msg-ilb"));
    }

    [TestMethod]
    public void BitMessageShouldNotRenderAnIconLabelWithoutOne()
    {
        var component = RenderComponent<BitMessage>();

        Assert.IsEmpty(component.FindAll(".bit-msg-ilb"));
    }

    [TestMethod]
    public void BitMessageShouldRenderTheIconAriaLabelEvenWithoutAnIcon()
    {
        // The label stands for the severity, not for the glyph, so hiding the glyph does not silence it.
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.HideIcon, true);
            parameters.Add(p => p.IconAriaLabel, "Warning");
        });

        Assert.AreEqual("Warning", component.Find(".bit-msg-ilb").TextContent);
    }



    [TestMethod]
    public void BitMessageShouldRenderTheTitle()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Title, "Upload failed");
            parameters.AddChildContent("The file is too large.");
        });

        Assert.AreEqual("Upload failed", component.Find(".bit-msg-ttl").TextContent.Trim());
        Assert.AreEqual("The file is too large.", component.Find(".bit-msg-cnt").TextContent.Trim());
    }

    [TestMethod,
        DataRow(null, "DIV"),
        DataRow("h2", "H2"),
        DataRow("h3", "H3"),
        DataRow("span", "SPAN")
    ]
    public void BitMessageShouldRenderTheTitleAsTheGivenElement(string? element, string expectedTag)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Title, "The title");
            parameters.Add(p => p.TitleElement, element);
        });

        var title = component.Find(".bit-msg-ttl");

        Assert.AreEqual(expectedTag, title.TagName);
        Assert.AreEqual("The title", title.TextContent);
    }

    [TestMethod]
    public void BitMessageTitleTemplateShouldTakePrecedenceOverTitle()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Title, "plain");
            parameters.Add(p => p.TitleTemplate, "<b>markup</b>");
        });

        var title = component.Find(".bit-msg-ttl");

        Assert.AreEqual("markup", title.TextContent.Trim());
        Assert.HasCount(1, title.QuerySelectorAll("b"));
    }

    [TestMethod]
    public void BitMessageWithoutATitleShouldNotOptIntoTheInlineLayout()
    {
        var component = RenderComponent<BitMessage>();

        Assert.IsFalse(component.Find(".bit-msg-cnw").ClassList.Contains("bit-msg-cwi"));
    }

    [TestMethod,
        // A title shares the line with the content on a single-line message ...
        DataRow(false, false, true),
        // ... and stacks above it once the message is allowed to wrap.
        DataRow(true, false, false),
        // A truncated message is a single line until it is expanded.
        DataRow(false, true, true)
    ]
    public void BitMessageShouldPlaceTheTitleInlineOnlyOnASingleLine(bool multiline, bool truncate, bool expectedInline)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Title, "Heads up");
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.Truncate, truncate);
        });

        Assert.AreEqual(expectedInline, component.Find(".bit-msg-cnw").ClassList.Contains("bit-msg-cwi"));
    }

    [TestMethod]
    public void BitMessageShouldStackTheTitleOnceTheTruncatedContentIsExpanded()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Title, "Heads up");
            parameters.Add(p => p.Truncate, true);
            parameters.AddChildContent(LongText);
        });

        Assert.IsTrue(component.Find(".bit-msg-cnw").ClassList.Contains("bit-msg-cwi"));

        component.Find(".bit-msg-exb").Click();

        Assert.IsFalse(component.Find(".bit-msg-cnw").ClassList.Contains("bit-msg-cwi"));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMessageShouldRespectMultiline(bool multiline)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Multiline, multiline);
            parameters.AddChildContent(LongText);
        });

        Assert.AreEqual(multiline, component.Find(".bit-msg-cnt").ClassList.Contains("bit-msg-mcn"));
    }

    [TestMethod]
    public void BitMessageShouldRenderTheActionsInline()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Actions, "<button>Retry</button>");
        });

        var actions = component.Find(".bit-msg-act");

        Assert.IsFalse(actions.ClassList.Contains("bit-msg-mac"));
        // Inline actions live inside the root container, next to the content.
        Assert.AreEqual("bit-msg-rct", actions.ParentElement!.ClassList.First());
    }

    [TestMethod]
    public void BitMessageShouldMoveTheActionsToTheirOwnRowInMultilineMode()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Multiline, true);
            parameters.Add(p => p.Actions, "<button>Retry</button>");
        });

        var actions = component.Find(".bit-msg-act");

        Assert.IsTrue(actions.ClassList.Contains("bit-msg-mac"));
        // The multiline row is a sibling of the root container, under the message root.
        Assert.IsTrue(actions.ParentElement!.ClassList.Contains("bit-msg"));
    }

    [TestMethod,
        DataRow("<div><button>Action</button></div>")
    ]
    public void BitMessageShouldRespectAction(string actions)
    {
        var component = RenderComponent<BitMessage>(parameter =>
        {
            parameter.Add(p => p.Actions, actions);
        });

        var actionsTemplate = component.Find(".bit-msg-act").ChildNodes;
        actionsTemplate.MarkupMatches(actions);
    }

    [TestMethod]
    public void BitMessageShouldNotRenderTheActionsWhenNoneWereGiven()
    {
        var component = RenderComponent<BitMessage>();

        Assert.IsEmpty(component.FindAll(".bit-msg-act"));
    }



    [TestMethod]
    public void BitMessageDismissButtonShouldWorkCorrect()
    {
        var currentCount = 0;
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        var dismissButton = component.Find(".bit-msg-dmb");

        dismissButton.Click();

        Assert.AreEqual(1, currentCount);
    }

    [TestMethod]
    public void BitMessageShouldNotRenderTheDismissButtonWithoutAHandler()
    {
        var component = RenderComponent<BitMessage>();

        Assert.IsEmpty(component.FindAll(".bit-msg-dmb"));
    }

    [TestMethod]
    public void BitMessageShouldRenderTheDismissButtonForADismissibleMessageWithoutAHandler()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
        });

        Assert.HasCount(1, component.FindAll(".bit-msg-dmb"));
    }

    [TestMethod]
    public void BitMessageShouldTakeItselfOffThePageWhenItIsDismissible()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.AddChildContent("Hello");
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldStayOnThePageWhenOnlyTheHandlerReportsTheDismissal()
    {
        // The long-standing contract: OnDismiss reports the dismissal and its owner decides what to do
        // about it, so the message itself must not disappear.
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.AreEqual(1, dismissCount);
        Assert.HasCount(1, component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldInvokeTheDismissHandlerWhileDismissingItself()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.AreEqual(1, dismissCount);
        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldRenderNothingWhileDismissed()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissed, true);
            parameters.AddChildContent("Hello");
        });

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageDismissedShouldBeTwoWayBindable()
    {
        var isDismissed = false;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Bind(p => p.Dismissed, isDismissed, v => isDismissed = v);
            parameters.Add(p => p.OnDismiss, () => { });
        });

        component.Find(".bit-msg-dmb").Click();

        // A bound Dismissed is an opt-in of its own: the message owns its dismissal without Dismissible.
        Assert.IsTrue(isDismissed);
        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldComeBackWhenDismissedIsSetBackToFalse()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.Dismissed, true);
            parameters.AddChildContent("Hello");
        });

        Assert.IsEmpty(component.FindAll(".bit-msg"));

        component.Render(parameters => parameters.Add(p => p.Dismissed, false));

        Assert.HasCount(1, component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldNotDismissItselfWhileDismissedIsControlledFromOutside()
    {
        // A one-way Dismissed is owned by the consumer, so the message reports the dismissal and leaves the
        // state alone rather than fighting the value it was handed.
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.Dismissed, false);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.AreEqual(1, dismissCount);
        Assert.HasCount(1, component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public async Task BitMessageShouldBeDismissableThroughItsPublicMethod()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        await component.InvokeAsync(() => component.Instance.DismissAsync());

        Assert.AreEqual(1, dismissCount);
        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod,
        DataRow("Emoji2"),
        DataRow("WordLogo")
    ]
    public void BitMessageShouldRespectCustomDismissIcon(string iconName)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissIconName, iconName);
            parameters.Add(p => p.OnDismiss, () => { });
        });

        var icon = component.Find(".bit-msg-dmi");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitMessageDismissIconShouldTakePrecedenceOverDismissIconName()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissIconName, "Emoji2");
            parameters.Add(p => p.DismissIcon, BitIconInfo.Bi("x-circle-fill"));
            parameters.Add(p => p.OnDismiss, () => { });
        });

        var icon = component.Find(".bit-msg-dmi");

        Assert.IsTrue(icon.ClassList.Contains("bi"));
        Assert.IsTrue(icon.ClassList.Contains("bi-x-circle-fill"));
    }

    [TestMethod]
    public void BitMessageDismissButtonShouldCarryAnAccessibleName()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => { });
        });

        var button = component.Find(".bit-msg-dmb");

        Assert.AreEqual("button", button.GetAttribute("type"));
        Assert.AreEqual("Dismiss", button.GetAttribute("aria-label"));
        Assert.AreEqual("Dismiss", button.GetAttribute("title"));
        Assert.AreEqual("true", component.Find(".bit-msg-dmi").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitMessageShouldRespectCustomDismissAriaLabel()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissAriaLabel, "بستن");
            parameters.Add(p => p.OnDismiss, () => { });
        });

        Assert.AreEqual("بستن", component.Find(".bit-msg-dmb").GetAttribute("aria-label"));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMessageShouldRespectTruncate(bool truncate)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, truncate);
            parameters.AddChildContent(LongText);
        });

        Assert.HasCount(truncate ? 1 : 0, component.FindAll(".bit-msg-exb"));
    }

    [TestMethod]
    public void BitMessageShouldNotRenderTheExpanderInMultilineMode()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.Multiline, true);
            parameters.AddChildContent(LongText);
        });

        Assert.IsEmpty(component.FindAll(".bit-msg-exb"));
    }

    [TestMethod]
    public void BitMessageExpanderShouldToggleTheExpandedState()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.AddChildContent(LongText);
        });

        var content = component.Find(".bit-msg-cnc");
        var button = component.Find(".bit-msg-exb");

        Assert.IsFalse(content.ClassList.Contains("bit-msg-cnx"));
        Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
        Assert.AreEqual("Expand", button.GetAttribute("aria-label"));

        button.Click();

        Assert.IsTrue(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
        Assert.AreEqual("true", component.Find(".bit-msg-exb").GetAttribute("aria-expanded"));
        Assert.AreEqual("Collapse", component.Find(".bit-msg-exb").GetAttribute("aria-label"));

        component.Find(".bit-msg-exb").Click();

        Assert.IsFalse(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
    }

    [TestMethod]
    public void BitMessageExpanderShouldPointAtTheContentItControls()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Id, "the-message");
            parameters.Add(p => p.Truncate, true);
            parameters.AddChildContent(LongText);
        });

        Assert.AreEqual("the-message-cnt", component.Find(".bit-msg-exb").GetAttribute("aria-controls"));
        Assert.AreEqual("the-message-cnt", component.Find(".bit-msg-cnc").GetAttribute("id"));
    }

    [TestMethod]
    public void BitMessageShouldRespectCustomExpanderAriaLabels()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.ExpandAriaLabel, "Show more");
            parameters.Add(p => p.CollapseAriaLabel, "Show less");
            parameters.AddChildContent(LongText);
        });

        Assert.AreEqual("Show more", component.Find(".bit-msg-exb").GetAttribute("aria-label"));

        component.Find(".bit-msg-exb").Click();

        Assert.AreEqual("Show less", component.Find(".bit-msg-exb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMessageDefaultExpanderIconShouldBeTheCollapseGlyphTurnedOver()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.AddChildContent(LongText);
        });

        var icon = component.Find(".bit-msg-exi");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--DoubleChevronUp"));
        Assert.IsTrue(icon.ClassList.Contains("bit-ico-r180"));

        component.Find(".bit-msg-exb").Click();

        var collapseIcon = component.Find(".bit-msg-exi");

        Assert.IsTrue(collapseIcon.ClassList.Contains("bit-icon--DoubleChevronUp"));
        Assert.IsFalse(collapseIcon.ClassList.Contains("bit-ico-r180"));
    }

    [TestMethod]
    public void BitMessageShouldRespectCustomExpandAndCollapseIconNames()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.ExpandIconName, "ChevronDownEnd");
            parameters.Add(p => p.CollapseIconName, "ChevronUpEnd");
            parameters.AddChildContent(LongText);
        });

        var icon = component.Find(".bit-msg-exi");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--ChevronDownEnd"));
        // A custom glyph already points the right way, so it is never turned over.
        Assert.IsFalse(icon.ClassList.Contains("bit-ico-r180"));

        component.Find(".bit-msg-exb").Click();

        Assert.IsTrue(component.Find(".bit-msg-exi").ClassList.Contains("bit-icon--ChevronUpEnd"));
    }

    [TestMethod]
    public void BitMessageShouldRespectCustomExpandAndCollapseIcons()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.ExpandIcon, BitIconInfo.Bi("chevron-double-down"));
            parameters.Add(p => p.CollapseIcon, BitIconInfo.Bi("chevron-double-up"));
            parameters.AddChildContent(LongText);
        });

        Assert.IsTrue(component.Find(".bit-msg-exi").ClassList.Contains("bi-chevron-double-down"));
        Assert.IsFalse(component.Find(".bit-msg-exi").ClassList.Contains("bit-ico-r180"));

        component.Find(".bit-msg-exb").Click();

        Assert.IsTrue(component.Find(".bit-msg-exi").ClassList.Contains("bi-chevron-double-up"));
    }

    [TestMethod]
    public void BitMessageExpandedShouldBeTwoWayBindable()
    {
        var isExpanded = false;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Bind(p => p.Expanded, isExpanded, v => isExpanded = v);
            parameters.AddChildContent(LongText);
        });

        component.Find(".bit-msg-exb").Click();

        Assert.IsTrue(isExpanded);
        Assert.IsTrue(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
    }

    [TestMethod]
    public void BitMessageExpandedShouldBeControllableFromOutside()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.AddChildContent(LongText);
        });

        Assert.IsFalse(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));

        component.Render(parameters => parameters.Add(p => p.Expanded, true));

        Assert.IsTrue(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
        Assert.AreEqual("true", component.Find(".bit-msg-exb").GetAttribute("aria-expanded"));
    }



    [TestMethod,
        DataRow(false, false),
        DataRow(true, false),
        DataRow(true, true)
    ]
    public void BitMessageShouldIgnoreExpandedWithoutSomethingFoldedAway(bool multiline, bool truncate)
    {
        // Expanded is only meaningful where the expander button renders; anywhere else it must not start
        // rewrapping a message that was asked to stay on one line.
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Multiline, multiline);
            parameters.Add(p => p.Truncate, truncate);
            parameters.Add(p => p.Expanded, true);
            parameters.AddChildContent(LongText);
        });

        Assert.IsFalse(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
    }



    [TestMethod,
        DataRow("alert", BitColor.Info),
        DataRow("alert", BitColor.Success),
        DataRow("alert", BitColor.Warning),
        DataRow("alert", BitColor.SevereWarning),
        DataRow("alert", BitColor.Error),

        DataRow(null, BitColor.Primary),
        DataRow(null, BitColor.Secondary),
        DataRow(null, BitColor.Tertiary),
        DataRow(null, BitColor.Info),
        DataRow(null, BitColor.Success),
        DataRow(null, BitColor.Warning),
        DataRow(null, BitColor.SevereWarning),
        DataRow(null, BitColor.Error),
        DataRow(null, BitColor.PrimaryBackground),
        DataRow(null, BitColor.SecondaryBorder),
    ]
    public void BitMessageRoleTest(string role, BitColor type)
    {
        var component = RenderComponent<BitMessage>(parameter =>
        {
            parameter.Add(p => p.Role, role);
            parameter.Add(p => p.Color, type);
        });

        var textEl = component.Find(".bit-msg-cnc");
        var expectedRole = role is not null ? role : GetRole(type);

        Assert.AreEqual(expectedRole, textEl.GetAttribute("role"));
    }

    [TestMethod]
    public void BitMessageWithATitleShouldBeAGroupNamedByIt()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Id, "the-message");
            parameters.Add(p => p.Title, "Upload failed");
        });

        var root = component.Find(".bit-msg");

        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("the-message-ttl", root.GetAttribute("aria-labelledby"));
        Assert.AreEqual("the-message-ttl", component.Find(".bit-msg-ttl").GetAttribute("id"));
    }

    [TestMethod]
    public void BitMessageAriaLabelShouldTakePrecedenceOverTheTitleAsTheGroupName()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Title, "Upload failed");
            parameters.Add(p => p.AriaLabel, "Upload status");
        });

        var root = component.Find(".bit-msg");

        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Upload status", root.GetAttribute("aria-label"));
        Assert.IsFalse(root.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitMessageShouldNotOverrideAConsumerSuppliedRootRole()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMessage>(0);
            builder.AddAttribute(1, nameof(BitMessage.Title), "Upload failed");
            builder.AddAttribute(2, "role", "alert");
            builder.CloseComponent();
        });

        Assert.AreEqual("alert", component.Find(".bit-msg").GetAttribute("role"));
    }

    [TestMethod]
    public void BitMessageWithoutANameShouldNotBeAGroup()
    {
        // An unnamed group announces nothing and only adds a boundary to walk in and out of.
        var root = RenderComponent<BitMessage>().Find(".bit-msg");

        Assert.IsFalse(root.HasAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitMessageWithoutAColorShouldAnnouncePolitely()
    {
        var component = RenderComponent<BitMessage>();

        Assert.AreEqual("status", component.Find(".bit-msg-cnc").GetAttribute("role"));
    }



    [TestMethod]
    public void BitMessageShouldAutoDismissAfterTheGivenTime()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        WaitUntil(() => dismissCount == 1);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldAutoDismissItselfWithoutAHandlerWhenItIsDismissible()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
        });

        WaitUntil(() => component.FindAll(".bit-msg").Count == 0);

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldNotAutoDismissWhileItIsDisabled()
    {
        // A turned-off message has its dismiss button turned off with it, so the countdown that would dismiss
        // it on the reader's behalf has no business running either.
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(50));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        Thread.Sleep(300);

        Assert.AreEqual(0, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldArmTheCountdownOnceItIsEnabledAgain()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Render(parameters => parameters.Add(p => p.IsEnabled, true));

        WaitUntil(() => dismissCount == 1);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldNotAutoDismissWithoutAHandler()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(50));
        });

        // Nothing to call means nothing to arm, so the pause listeners are not wired up either.
        Assert.ThrowsExactly<MissingEventHandlerException>(() => component.Find(".bit-msg").MouseEnter());
    }

    [TestMethod,
        DataRow(0),
        DataRow(-1)
    ]
    public void BitMessageShouldIgnoreANonPositiveAutoDismissTime(int seconds)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(seconds));
            parameters.Add(p => p.OnDismiss, () => { });
        });

        Assert.ThrowsExactly<MissingEventHandlerException>(() => component.Find(".bit-msg").MouseEnter());
    }

    [TestMethod]
    public void BitMessageShouldWireUpThePauseListenersOnlyWhenTheCountdownIsArmed()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.Add(p => p.OnDismiss, () => { });
        });

        // No exception means the listeners are there; the countdown is long enough not to fire meanwhile.
        component.Find(".bit-msg").MouseEnter();
        component.Find(".bit-msg").MouseLeave();
        component.Find(".bit-msg").FocusIn();
        component.Find(".bit-msg").FocusOut();
    }

    [TestMethod]
    public void BitMessageShouldHoldTheAutoDismissCountdownWhileThePointerIsOverIt()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(600));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg").MouseEnter();

        // Well past the countdown, but the pointer is holding it.
        Thread.Sleep(1500);
        Assert.AreEqual(0, dismissCount);

        component.Find(".bit-msg").MouseLeave();

        WaitUntil(() => dismissCount == 1);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldNotAutoDismissAfterBeingDismissedByHand()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.AreEqual(1, dismissCount);

        Thread.Sleep(800);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldNotReviveTheCountdownAfterAManualDismiss()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(200));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg-dmb").Click();

        // A parameter set after the dismiss must not start the countdown over.
        component.Render(parameters => parameters.Add(p => p.Color, BitColor.Warning));

        Thread.Sleep(700);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldNotRestartTheCountdownOnAnUnrelatedRerender()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(400));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        // Three parameter sets while the countdown runs; a countdown restarted by any of them would keep
        // pushing the callback out of reach of the wait below.
        for (var i = 0; i < 3; i++)
        {
            Thread.Sleep(150);
            component.Render(parameters => parameters.Add(p => p.Class, $"round-{i}"));
        }

        WaitUntil(() => dismissCount == 1, 1000);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldRearmTheCountdownWhenTheAutoDismissTimeChanges()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        Assert.AreEqual(0, dismissCount);

        component.Render(parameters => parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100)));

        WaitUntil(() => dismissCount == 1);

        Assert.AreEqual(1, dismissCount);
    }



    [TestMethod]
    public void BitMessageShouldLetTheDismissalBeCancelled()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
            parameters.Add<BitMessageDismissArgs>(p => p.OnDismissing, args => args.Cancel = true);
        });

        component.Find(".bit-msg-dmb").Click();

        // A cancelled dismissal is not a dismissal: the message stays, and nobody is told it went.
        Assert.AreEqual(0, dismissCount);
        Assert.HasCount(1, component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldReportWhatTheDismissButtonDid()
    {
        BitMessageDismissReason? reason = null;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add<BitMessageDismissArgs>(p => p.OnDismissing, args => reason = args.Reason);
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.AreEqual(BitMessageDismissReason.Button, reason);
    }

    [TestMethod]
    public void BitMessageShouldReportWhatTheEscapeKeyDid()
    {
        BitMessageDismissReason? reason = null;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.DismissOnEscape, true);
            parameters.Add<BitMessageDismissArgs>(p => p.OnDismissing, args => reason = args.Reason);
        });

        component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(BitMessageDismissReason.Escape, reason);
    }

    [TestMethod]
    public void BitMessageShouldReportWhatTheCountdownDid()
    {
        BitMessageDismissReason? reason = null;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
            parameters.Add<BitMessageDismissArgs>(p => p.OnDismissing, args => reason = args.Reason);
        });

        WaitUntil(() => reason is not null);

        Assert.AreEqual(BitMessageDismissReason.AutoDismiss, reason);
    }

    [TestMethod]
    public async Task BitMessageShouldReportWhatTheDismissMethodDid()
    {
        BitMessageDismissReason? reason = null;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add<BitMessageDismissArgs>(p => p.OnDismissing, args => reason = args.Reason);
        });

        await component.InvokeAsync(() => component.Instance.DismissAsync());

        Assert.AreEqual(BitMessageDismissReason.Programmatic, reason);
    }

    [TestMethod]
    public void BitMessageShouldHoldTheCountdownThroughItsPublicApi()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(150));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Instance.PauseAutoDismiss();

        Thread.Sleep(400);

        Assert.AreEqual(0, dismissCount);

        component.Instance.ResumeAutoDismiss();

        WaitUntil(() => dismissCount == 1);

        Assert.AreEqual(1, dismissCount);
    }



    [TestMethod]
    public void BitMessageShouldDrawTheAutoDismissCountdown()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.ShowAutoDismissProgress, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
        });

        Assert.HasCount(1, component.FindAll(".bit-msg-prg"));
        // The bar is the countdown drawn out, so its animation lasts exactly as long as the countdown does.
        StringAssert.Contains(component.Find(".bit-msg-prb").GetAttribute("style"), "animation-duration:30000ms");
        Assert.AreEqual("true", component.Find(".bit-msg-prg").GetAttribute("aria-hidden"));
    }

    [TestMethod,
        DataRow(false, true),
        DataRow(true, false)
    ]
    public void BitMessageShouldNotDrawACountdownThatIsNotRunning(bool dismissible, bool hasAutoDismissTime)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, dismissible);
            parameters.Add(p => p.ShowAutoDismissProgress, true);

            if (hasAutoDismissTime)
            {
                parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            }
        });

        Assert.IsEmpty(component.FindAll(".bit-msg-prg"));
    }

    [TestMethod]
    public void BitMessageShouldNotDrawTheCountdownWithoutBeingAsked()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
        });

        Assert.IsEmpty(component.FindAll(".bit-msg-prg"));
    }

    [TestMethod]
    public void BitMessageShouldRespectTheAutoDismissProgressClassesAndStyles()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.ShowAutoDismissProgress, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.Add(p => p.Classes, new BitMessageClassStyles
            {
                AutoDismissProgress = "cls-prg",
                AutoDismissProgressBar = "cls-prb",
            });
            parameters.Add(p => p.Styles, new BitMessageClassStyles
            {
                AutoDismissProgress = "height:6px",
                AutoDismissProgressBar = "background-color:red",
            });
        });

        Assert.IsTrue(component.Find(".bit-msg-prg").ClassList.Contains("cls-prg"));
        Assert.IsTrue(component.Find(".bit-msg-prb").ClassList.Contains("cls-prb"));
        StringAssert.Contains(component.Find(".bit-msg-prg").GetAttribute("style"), "height:6px");

        // The custom style is spliced onto the duration rather than replacing it.
        var barStyle = component.Find(".bit-msg-prb").GetAttribute("style");
        StringAssert.Contains(barStyle, "animation-duration:30000ms");
        StringAssert.Contains(barStyle, "background-color:red");
    }



    [TestMethod]
    public void BitMessageShouldDismissOnEscape()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissOnEscape, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldIgnoreOtherKeysWhenDismissOnEscapeIsOn()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissOnEscape, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldNotListenForKeysWhenDismissOnEscapeIsOff()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.OnDismiss, () => { });
        });

        Assert.ThrowsExactly<MissingEventHandlerException>(
            () => component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Escape" }));
    }

    [TestMethod]
    public void BitMessageShouldNotListenForKeysWithoutADismissHandler()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissOnEscape, true);
        });

        Assert.ThrowsExactly<MissingEventHandlerException>(
            () => component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Escape" }));
    }

    [TestMethod]
    public void BitMessageShouldAcceptTheLegacyEscKeyName()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DismissOnEscape, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Esc" });

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldDismissOnEscapeWithoutAHandlerWhenItIsDismissible()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.DismissOnEscape, true);
        });

        component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }



    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitMessageShouldRespectIsEnabled(bool isEnabled)
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
            parameters.AddChildContent(LongText);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-msg").ClassList.Contains("bit-dis"));
        Assert.AreEqual(isEnabled is false, component.Find(".bit-msg-dmb").HasAttribute("disabled"));
        Assert.AreEqual(isEnabled is false, component.Find(".bit-msg-exb").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMessageShouldNotDismissOrExpandWhileDisabled()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.DismissOnEscape, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
            parameters.AddChildContent(LongText);
        });

        component.Find(".bit-msg-dmb").Click();
        component.Find(".bit-msg-exb").Click();

        Assert.AreEqual(0, dismissCount);
        Assert.IsFalse(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
    }



    [TestMethod]
    public void BitMessageShouldRespectAriaLabelAndTabIndex()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Upload status");
            parameters.Add(p => p.TabIndex, "0");
        });

        var root = component.Find(".bit-msg");

        Assert.AreEqual("Upload status", root.GetAttribute("aria-label"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitMessageShouldMakeItselfFocusableForAutoFocus()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
        });

        var root = component.Find(".bit-msg");

        Assert.IsTrue(root.HasAttribute("autofocus"));
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitMessageShouldKeepAnExplicitTabIndexWhileAutoFocusing()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.TabIndex, "0");
        });

        Assert.AreEqual("0", component.Find(".bit-msg").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitMessageShouldNotBeFocusableWithoutAutoFocus()
    {
        var root = RenderComponent<BitMessage>().Find(".bit-msg");

        Assert.IsFalse(root.HasAttribute("autofocus"));
        Assert.IsFalse(root.HasAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Auto, "auto")
    ]
    public void BitMessageShouldRespectDir(BitDir dir, string expectedDir)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-msg");

        Assert.AreEqual(expectedDir, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitMessageShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-msg").GetAttribute("style") ?? string.Empty;

        if (expectedStyle.Length == 0)
        {
            // A visible message says nothing about its visibility, whatever else its style carries.
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
        else
        {
            Assert.IsTrue(style.Contains(expectedStyle));
        }
    }

    [TestMethod]
    public void BitMessageShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Style, "color:red");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = component.Find(".bit-msg");

        StringAssert.Contains(root.GetAttribute("style"), "color:red");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
    }

    [TestMethod]
    public void BitMessageShouldRespectClasses()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.Title, "The title");
            parameters.Add(p => p.OnDismiss, () => { });
            parameters.Add(p => p.IconAriaLabel, "Error");
            parameters.Add(p => p.Actions, "<button>Retry</button>");
            parameters.Add(p => p.Classes, new BitMessageClassStyles
            {
                Root = "cls-root",
                RootContainer = "cls-rct",
                Container = "cls-con",
                IconContainer = "cls-ict",
                Icon = "cls-ico",
                IconLabel = "cls-ilb",
                ContentContainer = "cls-cnc",
                ContentWrapper = "cls-cnw",
                Title = "cls-ttl",
                Content = "cls-cnt",
                Actions = "cls-act",
                ExpanderButton = "cls-exb",
                ExpanderIcon = "cls-exi",
                DismissButton = "cls-dmb",
                DismissIcon = "cls-dmi",
            });
            parameters.AddChildContent(LongText);
        });

        Assert.IsTrue(component.Find(".bit-msg").ClassList.Contains("cls-root"));
        Assert.IsTrue(component.Find(".bit-msg-rct").ClassList.Contains("cls-rct"));
        Assert.IsTrue(component.Find(".bit-msg-con").ClassList.Contains("cls-con"));
        Assert.IsTrue(component.Find(".bit-msg-ict").ClassList.Contains("cls-ict"));
        Assert.IsTrue(component.Find(".bit-msg-ico").ClassList.Contains("cls-ico"));
        Assert.IsTrue(component.Find(".bit-msg-ilb").ClassList.Contains("cls-ilb"));
        Assert.IsTrue(component.Find(".bit-msg-cnc").ClassList.Contains("cls-cnc"));
        Assert.IsTrue(component.Find(".bit-msg-cnw").ClassList.Contains("cls-cnw"));
        Assert.IsTrue(component.Find(".bit-msg-ttl").ClassList.Contains("cls-ttl"));
        Assert.IsTrue(component.Find(".bit-msg-cnt").ClassList.Contains("cls-cnt"));
        Assert.IsTrue(component.Find(".bit-msg-act").ClassList.Contains("cls-act"));
        Assert.IsTrue(component.Find(".bit-msg-exb").ClassList.Contains("cls-exb"));
        Assert.IsTrue(component.Find(".bit-msg-exi").ClassList.Contains("cls-exi"));
        Assert.IsTrue(component.Find(".bit-msg-dmb").ClassList.Contains("cls-dmb"));
        Assert.IsTrue(component.Find(".bit-msg-dmi").ClassList.Contains("cls-dmi"));
    }

    [TestMethod]
    public void BitMessageShouldRespectStyles()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.Title, "The title");
            parameters.Add(p => p.OnDismiss, () => { });
            parameters.Add(p => p.IconAriaLabel, "Error");
            parameters.Add(p => p.Actions, "<button>Retry</button>");
            parameters.Add(p => p.Styles, new BitMessageClassStyles
            {
                Root = "padding:1rem",
                RootContainer = "margin:1px",
                Container = "margin:2px",
                IconContainer = "margin:3px",
                Icon = "font-size:1rem",
                IconLabel = "color:green",
                ContentContainer = "margin:4px",
                ContentWrapper = "margin:5px",
                Title = "color:red",
                Content = "color:blue",
                Actions = "gap:1rem",
                ExpanderButton = "margin:6px",
                ExpanderIcon = "font-size:2rem",
                DismissButton = "margin:7px",
                DismissIcon = "font-size:3rem",
            });
            parameters.AddChildContent(LongText);
        });

        StringAssert.Contains(component.Find(".bit-msg").GetAttribute("style"), "padding:1rem");
        StringAssert.Contains(component.Find(".bit-msg-rct").GetAttribute("style"), "margin:1px");
        StringAssert.Contains(component.Find(".bit-msg-con").GetAttribute("style"), "margin:2px");
        StringAssert.Contains(component.Find(".bit-msg-ict").GetAttribute("style"), "margin:3px");
        StringAssert.Contains(component.Find(".bit-msg-ico").GetAttribute("style"), "font-size:1rem");
        StringAssert.Contains(component.Find(".bit-msg-ilb").GetAttribute("style"), "color:green");
        StringAssert.Contains(component.Find(".bit-msg-cnc").GetAttribute("style"), "margin:4px");
        StringAssert.Contains(component.Find(".bit-msg-cnw").GetAttribute("style"), "margin:5px");
        StringAssert.Contains(component.Find(".bit-msg-ttl").GetAttribute("style"), "color:red");
        StringAssert.Contains(component.Find(".bit-msg-cnt").GetAttribute("style"), "color:blue");
        StringAssert.Contains(component.Find(".bit-msg-act").GetAttribute("style"), "gap:1rem");
        StringAssert.Contains(component.Find(".bit-msg-exb").GetAttribute("style"), "margin:6px");
        StringAssert.Contains(component.Find(".bit-msg-exi").GetAttribute("style"), "font-size:2rem");
        StringAssert.Contains(component.Find(".bit-msg-dmb").GetAttribute("style"), "margin:7px");
        StringAssert.Contains(component.Find(".bit-msg-dmi").GetAttribute("style"), "font-size:3rem");
    }

    [TestMethod,
        DataRow("data-test", "the-value"),
        DataRow("aria-describedby", "some-id")
    ]
    public void BitMessageShouldRespectArbitraryHtmlAttributes(string name, string value)
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so
        // supply them as raw component attributes (as real markup would) rather than via the builder,
        // which rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMessage>(0);
            builder.AddAttribute(1, name, value);
            builder.CloseComponent();
        });

        Assert.AreEqual(value, component.Find(".bit-msg").GetAttribute(name));
    }

    [TestMethod]
    public void BitMessageShouldNotOverrideAConsumerSuppliedHandler()
    {
        var consumerCount = 0;

        // The auto-dismiss hold registers an onmouseenter of its own; an onmouseenter written on the
        // component has to keep winning.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMessage>(0);
            builder.AddAttribute(1, nameof(BitMessage.AutoDismissTime), TimeSpan.FromSeconds(30));
            builder.AddAttribute(2, nameof(BitMessage.OnDismiss), EventCallback.Factory.Create(this, () => { }));
            builder.AddAttribute(3, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, _ => consumerCount++));
            builder.CloseComponent();
        });

        component.Find(".bit-msg").MouseEnter();

        Assert.AreEqual(1, consumerCount);
    }



    private static string GetRole(BitColor type)
     => type switch
     {
         BitColor.Error or BitColor.SevereWarning or BitColor.Warning => "alert",
         _ => "status",
     };

    [TestMethod]
    public void BitMessageShouldAutoDismissWhenOnlyTheDismissedBindingOwnsIt()
    {
        // Dismissing does something here without a button being involved: the binding takes the message off the
        // page, so the countdown has somewhere to go and must run.
        var isDismissed = false;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Bind(p => p.Dismissed, isDismissed, v => isDismissed = v);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
            parameters.AddChildContent("Hello");
        });

        // A binding is not a reason to offer a button - it is the outside that dismisses this message.
        Assert.IsEmpty(component.FindAll(".bit-msg-dmb"));

        WaitUntil(() => isDismissed);

        Assert.IsTrue(isDismissed);
    }

    [TestMethod]
    public void BitMessageShouldDismissOnEscapeWhenOnlyTheDismissedBindingOwnsIt()
    {
        var isDismissed = false;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Bind(p => p.Dismissed, isDismissed, v => isDismissed = v);
            parameters.Add(p => p.DismissOnEscape, true);
            parameters.AddChildContent("Hello");
        });

        component.Find(".bit-msg").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isDismissed);
        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldWireUpThePauseListenersForADismissedBindingAlone()
    {
        var isDismissed = false;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Bind(p => p.Dismissed, isDismissed, v => isDismissed = v);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.AddChildContent("Hello");
        });

        // No exception means the countdown is armed and holds like any other.
        component.Find(".bit-msg").MouseEnter();
        component.Find(".bit-msg").MouseLeave();
    }



    [TestMethod]
    public void BitMessageShouldRenderTheTextOnASecondRenderWhileTheAnnouncementIsDelayed()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DelayedAnnouncement, true);
            parameters.Add(p => p.Title, "Saved");
            parameters.AddChildContent("Hello");
        });

        // The live region arrives first and the text lands in it on the render that follows, which is the whole
        // point: a region that appears with its text already in it is a change of nothing to announce.
        Assert.AreEqual(2, component.RenderCount);
        Assert.HasCount(1, component.FindAll(".bit-msg-cnc"));
        Assert.AreEqual("Hello", component.Find(".bit-msg-cnt").TextContent.Trim());
        Assert.AreEqual("Saved", component.Find(".bit-msg-ttl").TextContent.Trim());
    }

    [TestMethod]
    public void BitMessageShouldRenderTheTextRightAwayWithoutADelayedAnnouncement()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.AddChildContent("Hello");
        });

        Assert.AreEqual(1, component.RenderCount);
        Assert.AreEqual("Hello", component.Find(".bit-msg-cnt").TextContent.Trim());
    }

    [TestMethod]
    public void BitMessageShouldDelayTheAnnouncementAgainForEachShowing()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.DelayedAnnouncement, true);
            parameters.Add(p => p.Dismissible, true);
            parameters.AddChildContent("Hello");
        });

        component.Find(".bit-msg-dmb").Click();

        Assert.IsEmpty(component.FindAll(".bit-msg"));

        component.Render(parameters => parameters.Add(p => p.Dismissed, false));

        // A re-shown message is a new sighting of it, so its text is handed to the region a second time.
        Assert.AreEqual("Hello", component.Find(".bit-msg-cnt").TextContent.Trim());
    }



    [TestMethod]
    public void BitMessageDismissButtonShouldBorrowTheTitleAsItsDescription()
    {
        // Every message on a page has the same "Dismiss" on its button, so the title is what tells them apart.
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.Title, "Upload failed");
            parameters.AddChildContent("Hello");
        });

        Assert.AreEqual(component.Find(".bit-msg-ttl").Id, component.Find(".bit-msg-dmb").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitMessageDismissButtonShouldNotBeDescribedByAnAbsentTitle()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.AddChildContent("Hello");
        });

        Assert.IsNull(component.Find(".bit-msg-dmb").GetAttribute("aria-describedby"));
    }



    [TestMethod]
    public void BitMessageShouldHoldTheCountdownBarThroughItsPublicApi()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.ShowAutoDismissProgress, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.AddChildContent("Hello");
        });

        Assert.IsFalse(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau"));

        component.Instance.PauseAutoDismiss();

        // The bar has to hold where the countdown holds, or the two disagree about how much time is left.
        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau")));

        component.Instance.ResumeAutoDismiss();

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau")));
    }

    [TestMethod]
    public void BitMessageShouldHoldTheCountdownBarWhileThePointerIsOverIt()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.ShowAutoDismissProgress, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.AddChildContent("Hello");
        });

        component.Find(".bit-msg").MouseEnter();

        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau")));

        component.Find(".bit-msg").MouseLeave();

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau")));
    }



    [TestMethod]
    public async Task BitMessageShouldExpandAndCollapseThroughItsPublicMethods()
    {
        var isExpanded = false;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Bind(p => p.Expanded, isExpanded, v => isExpanded = v);
            parameters.AddChildContent(LongText);
        });

        await component.InvokeAsync(() => component.Instance.ExpandAsync());

        Assert.IsTrue(isExpanded);
        Assert.IsTrue(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));

        await component.InvokeAsync(() => component.Instance.CollapseAsync());

        Assert.IsFalse(isExpanded);
        Assert.IsFalse(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));

        await component.InvokeAsync(() => component.Instance.ToggleExpandAsync());

        Assert.IsTrue(isExpanded);
        Assert.AreEqual("true", component.Find(".bit-msg-exb").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public async Task BitMessageShouldExpandThroughItsPublicMethodsWhileDisabled()
    {
        // The button is turned off with the message, but a call is the consumer's own doing rather than the
        // reader's, so it goes through the way DismissAsync does.
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.AddChildContent(LongText);
        });

        await component.InvokeAsync(() => component.Instance.ExpandAsync());

        Assert.IsTrue(component.Find(".bit-msg-cnc").ClassList.Contains("bit-msg-cnx"));
    }



    [TestMethod]
    public void BitMessageShouldTakeTheFocusWhileAutoFocusing()
    {
        // The autofocus attribute alone is only honoured while the element is being inserted, so the move is
        // made by hand as well - which is what covers a message brought back into markup already on the page.
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.AddChildContent("Hello");
        });

        Assert.IsGreaterThan(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitMessageShouldNotTakeTheFocusWithoutAutoFocus()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.AddChildContent("Hello");
        });

        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void BitMessageShouldTakeTheFocusAgainForEachShowing()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.Dismissible, true);
            parameters.AddChildContent("Hello");
        });

        var focusCount = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        component.Find(".bit-msg-dmb").Click();
        component.Render(parameters => parameters.Add(p => p.Dismissed, false));

        Assert.AreEqual(focusCount + 1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }


    [TestMethod]
    public void BitMessageShouldKeepTheCountdownHeldWhileTheFocusIsStillInsideIt()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(150));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
            parameters.AddChildContent("Hello");
        });

        component.Find(".bit-msg").MouseEnter();
        component.Find(".bit-msg").FocusIn();

        // Letting go of one of the reasons to hold the countdown is not letting go of the others.
        component.Find(".bit-msg").MouseLeave();

        Thread.Sleep(400);

        Assert.AreEqual(0, dismissCount);

        component.Find(".bit-msg").FocusOut();

        WaitUntil(() => dismissCount == 1);

        Assert.AreEqual(1, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldKeepTheCountdownBarHeldWhileTheFocusIsStillInsideIt()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.ShowAutoDismissProgress, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(30));
            parameters.AddChildContent("Hello");
        });

        component.Find(".bit-msg").MouseEnter();
        component.Find(".bit-msg").FocusIn();
        component.Find(".bit-msg").MouseLeave();

        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau")));

        component.Find(".bit-msg").FocusOut();

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-msg-prb").ClassList.Contains("bit-msg-pau")));
    }

    [TestMethod]
    public async Task BitMessageShouldDismissOnlyOnceWhileAnAwaitedGuardIsStillRunning()
    {
        // OnDismissing is awaited, so a second attempt arriving while the first is still asking would otherwise
        // run a dismissal of its own alongside it and report the same dismissal twice.
        var dismissCount = 0;
        var gate = new TaskCompletionSource();

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
            parameters.Add<BitMessageDismissArgs>(p => p.OnDismissing, async _ => await gate.Task);
        });

        var first = component.InvokeAsync(() => component.Instance.DismissAsync());
        var second = component.InvokeAsync(() => component.Instance.DismissAsync());

        gate.SetResult();

        await first;
        await second;

        Assert.AreEqual(1, dismissCount);
        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageExpanderButtonShouldBorrowTheTitleAsItsDescription()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.Add(p => p.Title, "Details");
            parameters.AddChildContent(LongText);
        });

        Assert.AreEqual(component.Find(".bit-msg-ttl").Id, component.Find(".bit-msg-exb").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitMessageExpanderButtonShouldNotBeDescribedByAnAbsentTitle()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Truncate, true);
            parameters.AddChildContent(LongText);
        });

        Assert.IsNull(component.Find(".bit-msg-exb").GetAttribute("aria-describedby"));
    }


    [TestMethod]
    public void BitMessageShouldNotCarryAStaleHoldIntoTheNextShowing()
    {
        var dismissCount = 0;

        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(150));
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
            parameters.AddChildContent("Hello");
        });

        // Held by the pointer, then taken off the page by hand: no mouseleave is ever coming for that hold, so
        // keeping it would leave the next showing counting down forever.
        component.Find(".bit-msg").MouseEnter();
        component.Find(".bit-msg-dmb").Click();

        Assert.IsEmpty(component.FindAll(".bit-msg"));

        component.Render(parameters => parameters.Add(p => p.Dismissed, false));

        WaitUntil(() => dismissCount == 2);

        Assert.AreEqual(2, dismissCount);
    }

    [TestMethod]
    public void BitMessageShouldNotDrawACountdownWhileItIsDisabled()
    {
        var component = RenderComponent<BitMessage>(parameters =>
        {
            parameters.Add(p => p.Dismissible, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.ShowAutoDismissProgress, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(10));
            parameters.AddChildContent("Hello");
        });

        // A turned-off message has no countdown running, so there is nothing to draw.
        Assert.IsEmpty(component.FindAll(".bit-msg-prg"));
    }

}
