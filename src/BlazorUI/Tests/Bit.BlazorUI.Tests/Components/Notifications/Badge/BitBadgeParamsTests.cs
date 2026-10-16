using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Badge;

/// <summary>
/// Covers the BitParams cascade of the Badge: what a BitBadgeParams fills in, what it leaves alone because the badge
/// wrote it for itself, and that what the badge derives from its parameters (its classes, its capped content, its
/// link, its live region and its offsets) follows the cascaded values.
/// </summary>
[TestClass]
public class BitBadgeParamsTests : BunitTestContext
{
    // What a badge sits on and what it does when clicked are written per badge, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitBadge.CascadingParameters),
        nameof(BitBadge.ChildContent),
        nameof(BitBadge.OnClick),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitBadgeParams badgeParams, Action<RenderTreeBuilder>? addAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { badgeParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitBadge>(0);
                addAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitBadgeParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitBadge)}", BitBadgeParams.ParamName);
        Assert.AreEqual("BitParams.BitBadge", BitBadgeParams.ParamName);
    }

    [TestMethod]
    public void BitBadgeParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitBadgeParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitBadgeParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitBadgeParamsShouldCarryEveryParameterThatIsNotPerBadge()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitBadge).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitBadgeParams).GetProperty(name), $"BitBadgeParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitBadgeParamsShouldCoverEveryParameterItNames()
    {
        // The other direction: a params property that points at nothing is silently ignored by the cascade.
        foreach (var property in typeof(BitBadgeParams).GetProperties())
        {
            if (property.Name == nameof(IBitComponentParams.Name)) continue;

            var target = typeof(BitBadge).GetProperty(property.Name);

            Assert.IsNotNull(target, $"BitBadge has no {property.Name}.");
            Assert.IsTrue(target.IsDefined(typeof(ParameterAttribute), true), $"BitBadge.{property.Name} is not a parameter.");
        }
    }

    [TestMethod]
    public void BitBadgeShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitBadgeParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Shape = BitShape.Rounded,
            Variant = BitVariant.Outline,
            Position = BitPosition.BottomStart,
            Overlap = true,
            Bordered = true,
            Pulse = true,
            Reversed = true,
            Title = "Cascaded title",
        }, builder => builder.AddAttribute(1, nameof(BitBadge.Content), 5));

        var root = component.Find(".bit-bdg");

        Assert.IsTrue(root.ClassList.Contains("bit-bdg-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-bst"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-orp"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-brd"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-pls"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-rvs"));
        Assert.AreEqual("Cascaded title", component.Find(".bit-bdg-ctn").GetAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeShouldApplyCascadedContentIconAndDescription()
    {
        var component = RenderWithParams(new BitBadgeParams
        {
            Content = 7,
            IconName = "Mail",
            Description = "7 unread messages",
            Classes = new() { Badge = "cascaded-badge", Icon = "cascaded-icon", Content = "cascaded-content", Description = "cascaded-description" },
            Styles = new() { Badge = "color: red;", Icon = "color: green;", Content = "color: blue;", Description = "color: pink;" },
        });

        Assert.AreEqual("7", component.Find(".bit-bdg-con").TextContent);
        Assert.IsTrue(component.Find(".bit-bdg-ico").ClassList.Contains("bit-icon--Mail"));
        Assert.AreEqual("7 unread messages", component.Find(".bit-bdg-vhd").TextContent);

        // The cascaded description is what the badge says, so what it shows is hidden from assistive technologies.
        Assert.AreEqual("true", component.Find(".bit-bdg-con").GetAttribute("aria-hidden"));

        Assert.IsTrue(component.Find(".bit-bdg-ctn").ClassList.Contains("cascaded-badge"));
        Assert.IsTrue(component.Find(".bit-bdg-ico").ClassList.Contains("cascaded-icon"));
        Assert.IsTrue(component.Find(".bit-bdg-con").ClassList.Contains("cascaded-content"));
        Assert.IsTrue(component.Find(".bit-bdg-vhd").ClassList.Contains("cascaded-description"));
        Assert.AreEqual("color: red;", component.Find(".bit-bdg-ctn").GetAttribute("style"));
        Assert.AreEqual("color: green;", component.Find(".bit-bdg-ico").GetAttribute("style"));
        Assert.AreEqual("color: blue;", component.Find(".bit-bdg-con").GetAttribute("style"));
        Assert.AreEqual("color: pink;", component.Find(".bit-bdg-vhd").GetAttribute("style"));
    }

    [TestMethod]
    public void BitBadgeShouldApplyCascadedIconAndContentTemplate()
    {
        RenderFragment template = builder => builder.AddMarkupContent(0, "<b class=\"tpl\">new</b>");

        var component = RenderWithParams(new BitBadgeParams
        {
            Content = 7,
            Icon = BitIconInfo.Css("my-icon"),
            IconName = "Mail",
            ContentTemplate = template,
        });

        // The cascaded Icon wins over the cascaded IconName, and the template over the content, as they do on the badge.
        var icon = component.Find(".bit-bdg-ico");
        Assert.IsTrue(icon.ClassList.Contains("my-icon"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Mail"));
        Assert.AreEqual(1, component.FindAll(".tpl").Count);
        Assert.AreEqual("new", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldApplyACascadedDot()
    {
        var component = RenderWithParams(new BitBadgeParams { Dot = true, Description = "New mail" },
                                         builder => builder.AddAttribute(1, nameof(BitBadge.Content), 99));

        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-dot"));
        Assert.AreEqual(0, component.FindAll(".bit-bdg-con").Count);
        Assert.AreEqual("New mail", component.Find(".bit-bdg-vhd").TextContent);
    }

    [TestMethod]
    public void BitBadgeShouldApplyACascadedHidden()
    {
        var component = RenderWithParams(new BitBadgeParams { Hidden = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitBadge.Content), 5);
            builder.AddAttribute(2, nameof(BitBadge.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"child\">child</span>")));
        });

        // The badge is taken off the page while its child content keeps rendering.
        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
        Assert.AreEqual(1, component.FindAll(".child").Count);
    }

    [TestMethod]
    public void BitBadgeShouldApplyCascadedLiveAndDecorative()
    {
        var live = RenderWithParams(new BitBadgeParams { Live = true }, builder => builder.AddAttribute(1, nameof(BitBadge.Content), 5));

        var region = live.Find(".bit-bdg-lvr");

        Assert.AreEqual("status", region.GetAttribute("role"));
        Assert.AreEqual("5", region.TextContent);
        Assert.AreEqual("true", live.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));

        var decorative = RenderWithParams(new BitBadgeParams { Decorative = true }, builder => builder.AddAttribute(1, nameof(BitBadge.Content), 5));

        Assert.AreEqual(0, decorative.FindAll(".bit-bdg-lvr").Count);
        Assert.AreEqual("true", decorative.Find(".bit-bdg-wrp").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitBadgeShouldApplyACascadedInline()
    {
        var component = RenderWithParams(new BitBadgeParams { Inline = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitBadge.Content), 5);
            builder.AddAttribute(2, nameof(BitBadge.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"child\">child</span>")));
        });

        var root = component.Find(".bit-bdg");

        Assert.IsTrue(root.ClassList.Contains("bit-bdg-inl"));
        // An inline badge with no position of its own follows the writing direction rather than the physical right.
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-ten"));
        Assert.IsTrue(component.Find(".bit-bdg-wrp").ClassList.Contains("bit-bdg-stl"));
    }

    [TestMethod]
    public void BitBadgeDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitBadgeParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Dot = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitBadge.Content), 5);
            builder.AddAttribute(2, nameof(BitBadge.Color), BitColor.Error);
            builder.AddAttribute(3, nameof(BitBadge.Dot), false);
        });

        var root = component.Find(".bit-bdg");

        Assert.IsTrue(root.ClassList.Contains("bit-bdg-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-bdg-suc"));
        // Dot written as false by the badge opts it out of the cascaded Dot, so its content shows.
        Assert.IsFalse(root.ClassList.Contains("bit-bdg-dot"));
        Assert.IsTrue(root.ClassList.Contains("bit-bdg-lg"));
        Assert.AreEqual("5", component.Find(".bit-bdg-con").TextContent);
    }

    [TestMethod]
    public void BitBadgeCascadedMaxShouldCapTheContentOfTheBadge()
    {
        var component = RenderWithParams(new BitBadgeParams { Max = 99 },
                                         builder => builder.AddAttribute(1, nameof(BitBadge.Content), 150));

        // The cap is read together with the content, so a Max that arrives only through the cascade still caps it.
        Assert.AreEqual("99+", component.Find(".bit-bdg-con").TextContent);
        Assert.AreEqual("150", component.Find(".bit-bdg-ctn").GetAttribute("title"));
    }

    [TestMethod]
    public void BitBadgeCascadedShowZeroShouldHideAnEmptiedCounter()
    {
        var component = RenderWithParams(new BitBadgeParams { ShowZero = false },
                                         builder => builder.AddAttribute(1, nameof(BitBadge.Content), 0));

        Assert.AreEqual(0, component.FindAll(".bit-bdg-ctn").Count);
    }

    [TestMethod]
    public void BitBadgeCascadedHrefShouldTurnTheBadgeIntoALink()
    {
        var component = RenderWithParams(new BitBadgeParams { Href = "/inbox", Target = "_blank", Rel = BitLinkRels.NoFollow },
                                         builder => builder.AddAttribute(1, nameof(BitBadge.Content), 5));

        var anchor = component.Find("a.bit-bdg-ctn");

        Assert.AreEqual("/inbox", anchor.GetAttribute("href"));
        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        // The rel is derived from the href, the rel and the target together, so it is recalculated once the cascade has filled them in.
        Assert.AreEqual("nofollow noopener", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitBadgeCascadedTargetShouldProtectTheLinkOfTheBadge()
    {
        var component = RenderWithParams(new BitBadgeParams { Target = "_blank" }, builder =>
        {
            builder.AddAttribute(1, nameof(BitBadge.Content), 5);
            builder.AddAttribute(2, nameof(BitBadge.Href), "https://bitplatform.dev");
        });

        var anchor = component.Find("a.bit-bdg-ctn");

        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual("noopener", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitBadgeCascadedOffsetsShouldBeWrittenOntoTheRoot()
    {
        var component = RenderWithParams(new BitBadgeParams { OffsetX = "2px", OffsetY = "-3px" },
                                         builder => builder.AddAttribute(1, nameof(BitBadge.Content), 5));

        var style = component.Find(".bit-bdg").GetAttribute("style")!;

        StringAssert.Contains(style, "--bit-bdg-ofs-x:2px");
        StringAssert.Contains(style, "--bit-bdg-ofs-y:-3px");
    }

    [TestMethod]
    public void BitBadgeShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitBadgeParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
            Visibility = BitVisibility.Hidden,
            AriaLabel = "Inbox",
            TabIndex = "3",
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitBadge.Content), 5);
            builder.AddAttribute(2, nameof(BitBadge.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var root = component.Find(".bit-bdg");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));

        var style = root.GetAttribute("style")!;
        StringAssert.Contains(style, "margin: 1px;");
        StringAssert.Contains(style, "visibility:hidden");

        // The cascaded Disabled, label and tab index reach the button the badge became, the way the badge's own do.
        var button = component.Find("button.bit-bdg-ctn");
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.AreEqual("Inbox", button.GetAttribute("aria-label"));
        Assert.AreEqual("3", button.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitBadgeParamsUpdateParametersShouldSetAllProperties()
    {
        RenderFragment template = builder => builder.AddContent(0, "tpl");
        var classes = new BitBadgeClassStyles { Badge = "cascaded-badge" };
        var styles = new BitBadgeClassStyles { Badge = "color: red;" };

        var @params = new BitBadgeParams
        {
            Bordered = true,
            Classes = classes,
            Color = BitColor.Warning,
            Decorative = true,
            Content = 7,
            ContentTemplate = template,
            Description = "Seven",
            Dot = true,
            Hidden = true,
            Href = "/inbox",
            Icon = BitIconInfo.Css("my-icon"),
            IconName = "Mail",
            Inline = true,
            Live = true,
            Max = 5,
            OffsetX = "1px",
            OffsetY = "2px",
            Overlap = true,
            Position = BitPosition.Center,
            Pulse = true,
            Rel = BitLinkRels.NoFollow,
            Reversed = true,
            Shape = BitShape.Square,
            ShowZero = false,
            Size = BitSize.Small,
            Styles = styles,
            Target = "_self",
            Title = "Title",
            Variant = BitVariant.Text,
        };

        var badge = new BitBadge();

        @params.UpdateParameters(badge);

        Assert.IsTrue(badge.Bordered);
        Assert.AreSame(classes, badge.Classes);
        Assert.AreEqual(BitColor.Warning, badge.Color);
        Assert.IsTrue(badge.Decorative);
        Assert.AreEqual(7, badge.Content);
        Assert.AreSame(template, badge.ContentTemplate);
        Assert.AreEqual("Seven", badge.Description);
        Assert.IsTrue(badge.Dot);
        Assert.IsTrue(badge.Hidden);
        Assert.AreEqual("/inbox", badge.Href);
        Assert.AreEqual("my-icon", badge.Icon?.Name);
        Assert.AreEqual("Mail", badge.IconName);
        Assert.IsTrue(badge.Inline);
        Assert.IsTrue(badge.Live);
        Assert.AreEqual(5, badge.Max);
        Assert.AreEqual("1px", badge.OffsetX);
        Assert.AreEqual("2px", badge.OffsetY);
        Assert.IsTrue(badge.Overlap);
        Assert.AreEqual(BitPosition.Center, badge.Position);
        Assert.IsTrue(badge.Pulse);
        Assert.AreEqual(BitLinkRels.NoFollow, badge.Rel);
        Assert.IsTrue(badge.Reversed);
        Assert.AreEqual(BitShape.Square, badge.Shape);
        Assert.IsFalse(badge.ShowZero);
        Assert.AreEqual(BitSize.Small, badge.Size);
        Assert.AreSame(styles, badge.Styles);
        Assert.AreEqual("_self", badge.Target);
        Assert.AreEqual("Title", badge.Title);
        Assert.AreEqual(BitVariant.Text, badge.Variant);
    }

    [TestMethod]
    public void BitBadgeParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var component = RenderWithParams(new BitBadgeParams
        {
            Content = 1,
            Description = "Cascaded",
            Variant = BitVariant.Outline,
            ShowZero = false,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitBadge.Content), 0);
            builder.AddAttribute(2, nameof(BitBadge.Description), "Own");
            builder.AddAttribute(3, nameof(BitBadge.ShowZero), true);
        });

        Assert.AreEqual("0", component.Find(".bit-bdg-con").TextContent);
        Assert.AreEqual("Own", component.Find(".bit-bdg-vhd").TextContent);
        Assert.IsTrue(component.Find(".bit-bdg").ClassList.Contains("bit-bdg-otl"));
    }

    [TestMethod]
    public void BitBadgeParamsUpdateParametersShouldIgnoreNull()
    {
        new BitBadgeParams { Color = BitColor.Error }.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitBadgeShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var plain = RenderComponent<BitBadge>(parameters => parameters.Add(p => p.Content, 5));

        var component = RenderWithParams(new BitBadgeParams(), builder => builder.AddAttribute(1, nameof(BitBadge.Content), 5));

        var root = component.Find(".bit-bdg");
        var instance = component.FindComponent<BitBadge>().Instance;

        Assert.AreEqual(plain.Find(".bit-bdg").ClassName, root.ClassName);
        Assert.AreEqual(plain.Find(".bit-bdg").GetAttribute("style"), root.GetAttribute("style"));
        Assert.AreEqual("5", component.Find(".bit-bdg-con").TextContent);

        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Size);
        Assert.IsNull(instance.Shape);
        Assert.IsNull(instance.Variant);
        Assert.IsNull(instance.Position);
        Assert.IsNull(instance.Max);
        Assert.IsTrue(instance.ShowZero);
        Assert.IsFalse(instance.Dot);
        Assert.IsFalse(instance.Hidden);
        Assert.IsFalse(instance.Disabled);
    }
}
