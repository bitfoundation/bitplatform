using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

/// <summary>
/// Covers the BitParams cascade of the Message: what a BitMessageParams fills in, what it leaves alone because
/// the message wrote it for itself, and that what the message derives from its parameters (its classes, its
/// styles, its buttons and its countdown) follows the cascaded values.
/// </summary>
[TestClass]
public class BitMessageParamsTests : BunitTestContext
{
    // What a message says - its content, its title, the words that stand in for its icon, the focus it takes - and the
    // state and the callbacks of one showing of it are written per message, so they have no place on the params object.
    // How it is announced (its role and urgency) is shared like its look, so those two are cascaded.
    private static readonly string[] _notCascaded =
    [
        nameof(BitMessage.CascadingParameters),
        nameof(BitMessage.Actions),
        nameof(BitMessage.AutoFocus),
        nameof(BitMessage.ChildContent),
        nameof(BitMessage.Content),
        nameof(BitMessage.Dismissed),
        nameof(BitMessage.DismissedChanged),
        nameof(BitMessage.Expanded),
        nameof(BitMessage.ExpandedChanged),
        nameof(BitMessage.IconAriaLabel),
        nameof(BitMessage.IconTemplate),
        nameof(BitMessage.OnDismiss),
        nameof(BitMessage.OnDismissing),
        nameof(BitMessage.Title),
        nameof(BitMessage.TitleTemplate),
    ];

    [TestMethod]
    public void BitMessageParamsShouldCarryEveryParameterThatIsNotPerMessage()
    {
        // A parameter added to the message without its counterpart here is one a BitParams cascade silently ignores.
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitMessage).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                           .Select(p => p.Name)
                                           .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitMessageParams).GetProperty(name), $"BitMessageParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitMessageParamsShouldCoverEveryParameterItNames()
    {
        // The other direction: a params property that points at nothing is silently ignored by the cascade.
        foreach (var property in typeof(BitMessageParams).GetProperties())
        {
            if (property.Name == nameof(IBitComponentParams.Name)) continue;

            var target = typeof(BitMessage).GetProperty(property.Name);

            Assert.IsNotNull(target, $"BitMessage has no {property.Name}.");
            Assert.IsTrue(target.IsDefined(typeof(ParameterAttribute), true), $"BitMessage.{property.Name} is not a parameter.");
        }
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Dismissible = true,
            Class = "cascaded",
            Id = "cascaded-id",
            AriaLabel = "Heads up",
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var root = component.Find(".bit-msg");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        // A cascaded label names the message, which makes it a group the way the message's own label does.
        Assert.AreEqual("Heads up", root.GetAttribute("aria-label"));
        Assert.AreEqual("group", root.GetAttribute("role"));

        // The cascaded Disabled reaches the dismiss button the way the message's own does.
        var dismiss = component.Find(".bit-msg-dmb");
        Assert.IsTrue(dismiss.HasAttribute("disabled"));
        Assert.AreEqual("true", dismiss.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitMessageShouldApplyTheCascadedIconsAndEscapeShortcut()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Icon = BitIconInfo.Css("cascaded-icon"),
            IconName = "Warning",
            DismissIcon = BitIconInfo.Css("cascaded-xmark"),
            DismissIconName = "ChromeClose",
            Dismissible = true,
            DismissOnEscape = true,
            DelayedAnnouncement = true,
        });

        // A cascaded Icon wins over a cascaded IconName, the way it does on the message itself.
        Assert.IsTrue(component.Find(".bit-msg-ico").ClassList.Contains("cascaded-icon"));
        Assert.IsFalse(component.Find(".bit-msg-ico").ClassList.Contains("bit-icon--Warning"));
        Assert.IsTrue(component.Find(".bit-msg-dmi").ClassList.Contains("cascaded-xmark"));

        // The Escape shortcut is wired up and claimed off the cascaded value, and the delayed text has landed by now.
        Assert.AreEqual("claim", component.Find(".bit-msg").GetAttribute("data-bit-esc"));
        component.WaitForAssertion(() => Assert.AreEqual("Hello", component.Find(".bit-msg-cnt").TextContent.Trim()));

        component.Find(".bit-msg").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldApplyTheCascadedExpanderIcons()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Truncate = true,
            ExpandIcon = BitIconInfo.Css("cascaded-expand"),
            ExpandIconName = "ChevronDown",
            CollapseIcon = BitIconInfo.Css("cascaded-collapse"),
            CollapseIconName = "ChevronUp",
            CollapseAriaLabel = "Show less",
        });

        var icon = component.Find(".bit-msg-exi");
        Assert.IsTrue(icon.ClassList.Contains("cascaded-expand"));
        // A custom expand glyph already points down, so it is not turned over.
        Assert.IsFalse(icon.ClassList.Contains("bit-ico-r180"));

        component.Find(".bit-msg-exb").Click();

        Assert.IsTrue(component.Find(".bit-msg-exi").ClassList.Contains("cascaded-collapse"));
        Assert.AreEqual("Show less", component.Find(".bit-msg-exb").GetAttribute("aria-label"));
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitMessageParams messageParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { messageParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMessage>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitMessage.ChildContent), (RenderFragment)(b => b.AddContent(0, "Hello")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitMessageParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitMessage)}", BitMessageParams.ParamName);
        Assert.AreEqual("BitParams.BitMessage", BitMessageParams.ParamName);
    }

    [TestMethod]
    public void BitMessageParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitMessageParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitMessageParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedClasses()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Color = BitColor.Error,
            Variant = BitVariant.Outline,
            Size = BitSize.Large,
            Square = true,
            Tinted = true,
            Classes = new() { Root = "cascaded-root", Content = "cascaded-content" },
        });

        var root = component.Find(".bit-msg");

        Assert.IsTrue(root.ClassList.Contains("bit-msg-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-sqr"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-tnt"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-msg-cnt").ClassList.Contains("cascaded-content"));

        // The icon and the urgency follow the cascaded color, as they follow one set on the message itself.
        Assert.IsTrue(component.Find(".bit-msg-ico").ClassList.Contains("bit-icon--ErrorBadge"));
        Assert.AreEqual("alert", component.Find(".bit-msg-cnc").GetAttribute("role"));
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedStyles()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Alignment = BitAlignment.Center,
            Elevation = 4,
            Multiline = true,
            MaxLines = 2,
            Styles = new() { Root = "outline: 1px solid red;" },
        });

        var root = component.Find(".bit-msg");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-msg-justifycontent:center"));
        Assert.IsTrue(style.Contains("--bit-msg-boxshadow:var(--bit-shd-4)"));
        Assert.IsTrue(style.Contains("--bit-msg-maxlines:2"));
        Assert.IsTrue(style.Contains("outline: 1px solid red;"));
        Assert.IsTrue(component.Find(".bit-msg-cnt").ClassList.Contains("bit-msg-clp"));
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedBehavior()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Truncate = true,
            Dismissible = true,
            HideIcon = true,
            TitleElement = "h4",
            DismissAriaLabel = "Close",
            ExpandAriaLabel = "Show more",
            DismissIconName = "Blocked2Solid",
        }, builder => builder.AddAttribute(1, nameof(BitMessage.Title), "Heads up"));

        Assert.IsEmpty(component.FindAll(".bit-msg-ict"));
        Assert.AreEqual("H4", component.Find(".bit-msg-ttl").TagName);
        Assert.AreEqual("Show more", component.Find(".bit-msg-exb").GetAttribute("aria-label"));

        var dismiss = component.Find(".bit-msg-dmb");

        Assert.AreEqual("Close", dismiss.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-msg-dmi").ClassList.Contains("bit-icon--Blocked2Solid"));

        // A cascaded Dismissible hands the message its own dismissal, so it takes itself off the page.
        dismiss.Click();

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldReflowThroughTheCascade()
    {
        RenderWithParams(new BitMessageParams { AutoMultiline = true });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.Message.observeOverflow"];

        Assert.HasCount(1, invocation);
        Assert.AreEqual(true, invocation[0].Arguments[3]);
    }

    [TestMethod]
    public void BitMessageShouldArmACascadedCountdown()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Dismissible = true,
            ShowAutoDismissProgress = true,
            AutoDismissTime = TimeSpan.FromMilliseconds(100),
        });

        Assert.HasCount(1, component.FindAll(".bit-msg-prb"));

        // Waited for on the message's own renders rather than polled from the test thread: bUnit drops its parsed
        // DOM before it rebuilds the markup, so a FindAll landing in between parses the old markup and keeps it
        // cached for good - the countdown's dismissal is the last render there is to replace it.
        var message = component.FindComponent<BitMessage>();

        message.WaitForAssertion(() => Assert.IsEmpty(message.FindAll(".bit-msg")), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitMessageShouldApplyTheCascadedRoleAndPoliteness()
    {
        // A region of standing notices sets how they are announced once, for every message in it.
        var component = RenderWithParams(new BitMessageParams
        {
            Color = BitColor.Error,
            Role = "note",
            Politeness = BitPoliteness.Off,
        });

        var region = component.Find(".bit-msg-cnc");

        Assert.AreEqual("note", region.GetAttribute("role"));
        Assert.AreEqual("off", region.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitMessageOwnRoleAndPolitenessShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Role = "note",
            Politeness = BitPoliteness.Off,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMessage.Role), "alert");
            builder.AddAttribute(2, nameof(BitMessage.Politeness), (BitPoliteness?)BitPoliteness.Assertive);
        });

        var region = component.Find(".bit-msg-cnc");

        Assert.AreEqual("alert", region.GetAttribute("role"));
        Assert.AreEqual("assertive", region.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedPageHolds()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            PauseOnPageHidden = true,
            PauseOnWindowBlur = true,
        }, builder => builder.AddAttribute(1, nameof(BitMessage.PauseOnWindowBlur), false));

        var instance = component.FindComponent<BitMessage>().Instance;

        Assert.IsTrue(instance.PauseOnPageHidden);
        // The message's own value wins, false included.
        Assert.IsFalse(instance.PauseOnWindowBlur);
    }

    [TestMethod]
    public void BitMessageDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Color = BitColor.Success,
            Variant = BitVariant.Outline,
            Size = BitSize.Small,
            DismissAriaLabel = "Close",
            Dismissible = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMessage.Color), BitColor.Warning);
            builder.AddAttribute(2, nameof(BitMessage.Variant), BitVariant.Fill);
            builder.AddAttribute(3, nameof(BitMessage.DismissAriaLabel), "Hide");
        });

        var root = component.Find(".bit-msg");

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(root.ClassList.Contains("bit-msg-wrn"));
        Assert.IsFalse(root.ClassList.Contains("bit-msg-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-msg-otl"));
        Assert.AreEqual("Hide", component.Find(".bit-msg-dmb").GetAttribute("aria-label"));

        // What the message left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-msg-sm"));
    }

    [TestMethod]
    public void BitMessageShouldKeepItsDefaultsWithoutCascadingParameters()
    {
        var component = RenderComponent<BitMessage>(parameters => parameters.AddChildContent("Hello"));

        var instance = component.Instance;

        Assert.IsNull(instance.CascadingParameters);
        Assert.IsFalse(instance.Dismissible);
        Assert.IsFalse(instance.Truncate);
        Assert.AreEqual("Dismiss", instance.DismissAriaLabel);
    }

    [TestMethod]
    public void BitMessageParamsShouldLeaveUnsetValuesAlone()
    {
        var component = RenderWithParams(new BitMessageParams());

        var instance = component.FindComponent<BitMessage>().Instance;
        var root = component.Find(".bit-msg");

        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Variant);
        Assert.IsNull(instance.Size);
        Assert.IsNull(instance.AutoDismissTime);
        Assert.IsFalse(instance.Dismissible);
        Assert.IsFalse(instance.Multiline);
        Assert.AreEqual("Dismiss", instance.DismissAriaLabel);
        Assert.AreEqual("Expand", instance.ExpandAriaLabel);
        Assert.AreEqual("Collapse", instance.CollapseAriaLabel);
        // An unset Color and Size publish no class: the stylesheet draws the Info, medium message they stand for.
        Assert.IsFalse(root.ClassList.Contains("bit-msg-inf"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-msg-md"));
        Assert.IsEmpty(component.FindAll(".bit-msg-dmb"));
    }
}
