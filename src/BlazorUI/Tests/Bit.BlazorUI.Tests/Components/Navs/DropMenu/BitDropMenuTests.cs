using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.DropMenu;

[TestClass]
public class BitDropMenuTests : BunitTestContext
{
    [TestMethod]
    public void BitDropMenuShouldRenderRootElement()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var root = component.Find(".bit-drm");
        var button = component.Find(".bit-drm-btn");

        Assert.IsNotNull(root);
        Assert.IsNotNull(button);
        Assert.AreEqual("button", button.GetAttribute("type"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotRenderTypeAttributeOnTheRootElement()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var root = component.Find(".bit-drm");

        // The root is a plain container: a type attribute on it is invalid html and a tabindex on it
        // would make the component a second tab stop next to its own button.
        Assert.IsNull(root.GetAttribute("type"));
        Assert.IsNull(root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderIconAndText()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.IconName, "Home");
            parameters.Add(p => p.Text, "MenuText");
        });

        var icon = component.Find(".bit-drm-icn");
        var text = component.Find(".bit-drm-txt");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Home"));
        Assert.AreEqual("MenuText", text.TextContent);
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheExternalIcon()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-house"));
            parameters.Add(p => p.ChevronDownIcon, BitIconInfo.Bi("chevron-down"));
        });

        var icon = component.Find(".bit-drm-icn");
        var chevron = component.Find(".bit-drm-chv");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-house"));
        Assert.IsTrue(chevron.ClassList.Contains("bi"));
        Assert.IsTrue(chevron.ClassList.Contains("bi-chevron-down"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheDefaultChevron()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var chevron = component.Find(".bit-drm-chv");

        Assert.IsTrue(chevron.ClassList.Contains("bit-icon--ChevronRight"));
        Assert.IsTrue(chevron.ClassList.Contains("bit-ico-r90"));
        Assert.AreEqual("true", chevron.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheCustomChevron()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.ChevronDownIconName, "DoubleChevronDown");
        });

        var chevron = component.Find(".bit-drm-chv");

        Assert.IsTrue(chevron.ClassList.Contains("bit-icon--DoubleChevronDown"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotRenderTheChevronWhenNoChevronIsSet()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.NoChevron, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-drm-chv").Count);
    }

    [TestMethod]
    public void BitDropMenuShouldToggleCalloutOnClick()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, @"<div class=""body"">BodyContent</div>")));
        });

        var overlay = component.Find(".bit-drm-ovl");
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:none"));

        var button = component.Find(".bit-drm-btn");
        button.Click();

        overlay = component.Find(".bit-drm-ovl");
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:block"));

        overlay.Click();

        overlay = component.Find(".bit-drm-ovl");
        Assert.IsTrue(overlay.GetAttribute("style").Contains("display:none"));
    }

    [TestMethod]
    public void BitDropMenuShouldCloseTheCalloutOnASecondActivationOfTheButton()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var button = component.Find(".bit-drm-btn");

        button.Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        button.Click();
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddTheOpenedClassesWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.IsFalse(component.Find(".bit-drm").ClassList.Contains("bit-drm-omn"));
        Assert.IsFalse(component.Find(".bit-drm-cal").ClassList.Contains("bit-drm-ocl"));

        component.Find(".bit-drm-btn").Click();

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("bit-drm-omn"));
        Assert.IsTrue(component.Find(".bit-drm-cal").ClassList.Contains("bit-drm-ocl"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotOpenWhenDisabled()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsEnabled, false);
        });

        var button = component.Find(".bit-drm-btn");

        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("bit-dis"));

        button.Click();

        Assert.IsTrue(component.Find(".bit-drm-ovl").GetAttribute("style").Contains("display:none"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotOpenWhenLoading()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IconName, "Home");
            parameters.Add(p => p.IsLoading, true);
        });

        var button = component.Find(".bit-drm-btn");

        // A loading button keeps the focus a keyboard user left on it, so it only says it is unavailable.
        Assert.IsFalse(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("bit-drm-ldg"));
        Assert.IsNotNull(component.Find(".bit-drm-spn"));
        Assert.AreEqual(0, component.FindAll(".bit-drm-icn").Count);

        button.Click();

        Assert.IsTrue(component.Find(".bit-drm-ovl").GetAttribute("style").Contains("display:none"));
    }

    [TestMethod]
    public void BitDropMenuShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitDropMenuHtmlAttributesTest>();

        component.MarkupMatches(@"
<div data-val-test=""bit"" class=""bit-drm bit-drm-md"" id:ignore>
    <button type=""button"" aria-haspopup=""dialog"" aria-expanded=""false"" class=""bit-drm-btn "" id:ignore aria-controls:ignore>
        <span class=""bit-drm-txt "">Menu</span>
        <i aria-hidden=""true"" class=""bit-drm-chv bit-icon bit-icon--ChevronRight bit-ico-r90 ""></i>
    </button>
</div>
<div aria-hidden=""true"" style=""display:none;"" class=""bit-drm-ovl "" id:ignore></div>
<div tabindex=""-1"" role=""dialog"" class=""bit-drm-cal bit-drm-fit"" id:ignore aria-labelledby:ignore>
    <div>Body</div>
</div>");

        var markup = component.Markup;

        Assert.IsTrue(markup.Contains("data-val-test=\"bit\""));
        Assert.IsTrue(markup.Contains("bit-drm"));
        Assert.IsTrue(markup.Contains("Body"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheMenuButtonAriaAttributes()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AriaLabel, "The menu");
            parameters.Add(p => p.AriaDescription, "Opens the quick settings");
            parameters.Add(p => p.Title, "The tooltip");
        });

        var button = component.Find(".bit-drm-btn");
        var callout = component.Find(".bit-drm-cal");

        Assert.AreEqual("dialog", button.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
        Assert.AreEqual(callout.Id, button.GetAttribute("aria-controls"));
        Assert.AreEqual(button.Id, callout.GetAttribute("aria-labelledby"));
        Assert.AreEqual("The menu", button.GetAttribute("aria-label"));
        // aria-describedby takes ids, so the text is rendered and pointed at rather than written into it.
        var description = component.Find(".bit-drm-dsc");
        Assert.AreEqual(description.Id, button.GetAttribute("aria-describedby"));
        Assert.AreEqual("Opens the quick settings", description.TextContent);
        Assert.AreEqual("The tooltip", button.GetAttribute("title"));

        // The accessible name belongs to the button, not to the container around it.
        Assert.IsNull(component.Find(".bit-drm").GetAttribute("aria-label"));

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderAriaHidden()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AriaHidden, true);
        });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheTabIndex()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find(".bit-drm-btn").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitDropMenuShouldTakeAnAriaHiddenButtonOutOfTheTabSequence()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.AriaHidden, true);
        });

        // A button hidden from assistive technology that the keyboard can still reach is focus a screen
        // reader cannot announce, so the tab index it was given does not apply to it.
        Assert.AreEqual("-1", component.Find(".bit-drm-btn").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddNoShadowClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-nsh"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddNoShadowClassWhenNoShadowIsSet()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.NoShadow, true);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-nsh"));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-drm-bpg")]
    [DataRow(BitColorKind.Secondary, "bit-drm-bsg")]
    [DataRow(BitColorKind.Tertiary, "bit-drm-btg")]
    [DataRow(BitColorKind.Transparent, "bit-drm-brg")]
    public void BitDropMenuShouldAddBackgroundClass(BitColorKind background, string expectedClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Background, background);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddBackgroundClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-bpg"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-bsg"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-btg"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-brg"));
    }

    [TestMethod]
    [DataRow(BitColorKind.Primary, "bit-drm-bpr")]
    [DataRow(BitColorKind.Secondary, "bit-drm-bsr")]
    [DataRow(BitColorKind.Tertiary, "bit-drm-btr")]
    [DataRow(BitColorKind.Transparent, "bit-drm-brr")]
    public void BitDropMenuShouldAddBorderClass(BitColorKind border, string expectedColorClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Border, border);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-brd"));
        Assert.IsTrue(callout.ClassList.Contains(expectedColorClass));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddBorderClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-brd"));
    }

    [TestMethod]
    public void BitDropMenuShouldCombineNoShadowBackgroundAndBorderClasses()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.NoShadow, true);
            parameters.Add(p => p.Background, BitColorKind.Secondary);
            parameters.Add(p => p.Border, BitColorKind.Tertiary);
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-nsh"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-bsg"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-brd"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-btr"));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-drm-pri")]
    [DataRow(BitColor.Secondary, "bit-drm-sec")]
    [DataRow(BitColor.Tertiary, "bit-drm-ter")]
    [DataRow(BitColor.Info, "bit-drm-inf")]
    [DataRow(BitColor.Success, "bit-drm-suc")]
    [DataRow(BitColor.Warning, "bit-drm-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-drm-swr")]
    [DataRow(BitColor.Error, "bit-drm-err")]
    [DataRow(BitColor.PrimaryBackground, "bit-drm-pbg")]
    [DataRow(BitColor.SecondaryBackground, "bit-drm-sbg")]
    [DataRow(BitColor.TertiaryBackground, "bit-drm-tbg")]
    [DataRow(BitColor.PrimaryForeground, "bit-drm-pfg")]
    [DataRow(BitColor.SecondaryForeground, "bit-drm-sfg")]
    [DataRow(BitColor.TertiaryForeground, "bit-drm-tfg")]
    [DataRow(BitColor.PrimaryBorder, "bit-drm-pbr")]
    [DataRow(BitColor.SecondaryBorder, "bit-drm-sbr")]
    [DataRow(BitColor.TertiaryBorder, "bit-drm-tbr")]
    public void BitDropMenuShouldAddColorClass(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddAColorClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.IsFalse(component.Find(".bit-drm").ClassList.Contains("bit-drm-pri"));
        Assert.IsFalse(component.Find(".bit-drm").ClassList.Contains("bit-drm-pbg"));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-drm-fil")]
    [DataRow(BitVariant.Outline, "bit-drm-otl")]
    [DataRow(BitVariant.Text, "bit-drm-tex")]
    public void BitDropMenuShouldAddVariantClass(BitVariant variant, string expectedClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDropMenuShouldNotAddAVariantClassByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        // Leaving Variant unset renders no variant class at all, which is what keeps the filled look
        // the drop menu has always had by default from being spelled out on every one of them.
        var root = component.Find(".bit-drm");

        Assert.IsFalse(root.ClassList.Contains("bit-drm-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-drm-otl"));
        Assert.IsFalse(root.ClassList.Contains("bit-drm-tex"));
    }

    [TestMethod]
    public void BitDropMenuShouldCombineTheVariantWithTheColorAndTheSize()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Variant, BitVariant.Outline);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Large);
        });

        var root = component.Find(".bit-drm");

        // The variant decides how the color is painted, so the two classes are always rendered together.
        Assert.IsTrue(root.ClassList.Contains("bit-drm-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-drm-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-drm-lg"));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-drm-sm")]
    [DataRow(BitSize.Medium, "bit-drm-md")]
    [DataRow(BitSize.Large, "bit-drm-lg")]
    public void BitDropMenuShouldAddSizeClass(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDropMenuShouldFallBackToTheMediumSize()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("bit-drm-md"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddFullWidthClass()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.FullWidth, true);
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("bit-drm-flw"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddTransparentClass()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Transparent, true);
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("bit-drm-trn"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddResponsiveAndPanelPositionClassesOnlyInResponsiveMode()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-res"));
        Assert.IsFalse(callout.ClassList.Contains("bit-drm-end"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
        });

        callout = component.Find(".bit-drm-cal");
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-res"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-end"));
    }

    [TestMethod]
    [DataRow(BitPanelPosition.Start, "bit-drm-sta")]
    [DataRow(BitPanelPosition.End, "bit-drm-end")]
    [DataRow(BitPanelPosition.Top, "bit-drm-top")]
    [DataRow(BitPanelPosition.Bottom, "bit-drm-btm")]
    public void BitDropMenuShouldAddPanelPositionClass(BitPanelPosition position, string expectedClass)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.PanelPosition, position);
        });

        Assert.IsTrue(component.Find(".bit-drm-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    [DataRow(BitPanelPosition.Start, BitSwipeOrientation.Horizontal)]
    [DataRow(BitPanelPosition.End, BitSwipeOrientation.Horizontal)]
    [DataRow(BitPanelPosition.Top, BitSwipeOrientation.Vertical)]
    [DataRow(BitPanelPosition.Bottom, BitSwipeOrientation.Vertical)]
    public void BitDropMenuShouldLockTheSwipeToTheAxisThePanelSlidesOn(BitPanelPosition position, BitSwipeOrientation expected)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.PanelPosition, position);
        });

        var setup = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Swipes.setup");

        // The arguments of Swipes.setup, in order: id, trigger, position, isRtl, orientationLock,
        // dotnetObj, isResponsive, scrollContainerId.
        Assert.AreEqual(component.Find(".bit-drm-cal").Id, setup.Arguments[0]);
        Assert.AreEqual(position, setup.Arguments[2]);
        Assert.AreEqual(expected, setup.Arguments[4]);
    }

    [TestMethod]
    public void BitDropMenuShouldRegisterAndDisposeTheSwipesWithTheResponsiveMode()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Swipes.setup"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
        });

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Swipes.setup"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, false);
        });

        // The gestures are registered against the callout with the geometry they were set up with, so
        // turning the responsive mode off has to reach the registration that is already there.
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Swipes.dispose"));
    }

    [TestMethod]
    public void BitDropMenuShouldApplyTheMaxHeight()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.MaxHeight, "10rem");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-mxh"));
        Assert.IsTrue(callout.GetAttribute("style")!.Contains("--bit-DropMenu-callout-max-height:10rem"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotApplyTheMaxHeightByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsFalse(callout.ClassList.Contains("bit-drm-mxh"));
        Assert.IsNull(callout.GetAttribute("style"));
    }

    [TestMethod]
    public void BitDropMenuShouldAddForceAnimationClassOnTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.ForceAnimation, true);
        });

        // The callout is relocated to the body while open, so it carries the opt-out class itself.
        Assert.IsTrue(component.Find(".bit-drm-cal").ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheRtlDirection()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-drm");
        var callout = component.Find(".bit-drm-cal");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        // The callout is relocated to the body, so it needs its own direction.
        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-rtl"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheTemplateInsteadOfTheDefaultContent()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IconName, "Home");
            parameters.Add(p => p.Template, (RenderFragment)(b => b.AddMarkupContent(0, @"<span class=""tmpl"">T</span>")));
        });

        Assert.IsNotNull(component.Find(".tmpl"));
        Assert.AreEqual(0, component.FindAll(".bit-drm-icn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-drm-txt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-drm-chv").Count);
    }

    [TestMethod]
    public void BitDropMenuShouldPreferBodyOverChildContent()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, @"<div class=""from-body""></div>")));
            parameters.Add(p => p.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, @"<div class=""from-child""></div>")));
        });

        Assert.AreEqual(1, component.FindAll(".from-body").Count);
        Assert.AreEqual(0, component.FindAll(".from-child").Count);
    }

    [TestMethod]
    public void BitDropMenuShouldApplyClassesAndStyles()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IconName, "Home");
            parameters.Add(p => p.Classes, new BitDropMenuClassStyles
            {
                Root = "cls-root",
                Button = "cls-button",
                Icon = "cls-icon",
                Text = "cls-text",
                ChevronDown = "cls-chevron",
                Overlay = "cls-overlay",
                Callout = "cls-callout"
            });
            parameters.Add(p => p.Styles, new BitDropMenuClassStyles
            {
                Root = "color:red",
                Button = "color:blue",
                Callout = "color:green"
            });
        });

        Assert.IsTrue(component.Find(".bit-drm").ClassList.Contains("cls-root"));
        Assert.IsTrue(component.Find(".bit-drm-btn").ClassList.Contains("cls-button"));
        Assert.IsTrue(component.Find(".bit-drm-icn").ClassList.Contains("cls-icon"));
        Assert.IsTrue(component.Find(".bit-drm-txt").ClassList.Contains("cls-text"));
        Assert.IsTrue(component.Find(".bit-drm-chv").ClassList.Contains("cls-chevron"));
        Assert.IsTrue(component.Find(".bit-drm-ovl").ClassList.Contains("cls-overlay"));
        Assert.IsTrue(component.Find(".bit-drm-cal").ClassList.Contains("cls-callout"));

        Assert.IsTrue(component.Find(".bit-drm").GetAttribute("style")!.Contains("color:red"));
        Assert.IsTrue(component.Find(".bit-drm-btn").GetAttribute("style")!.Contains("color:blue"));
        Assert.IsTrue(component.Find(".bit-drm-cal").GetAttribute("style")!.Contains("color:green"));
    }

    [TestMethod]
    public void BitDropMenuShouldApplyTheOpenedClassAndStyleOnlyWhileOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Classes, new BitDropMenuClassStyles { Opened = "cls-opened" });
            parameters.Add(p => p.Styles, new BitDropMenuClassStyles { Opened = "color:gold" });
        });

        var root = component.Find(".bit-drm");
        Assert.IsFalse(root.ClassList.Contains("cls-opened"));

        component.Find(".bit-drm-btn").Click();

        root = component.Find(".bit-drm");
        Assert.IsTrue(root.ClassList.Contains("cls-opened"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color:gold"));
    }

    [TestMethod]
    public void BitDropMenuShouldInvokeTheOnClickCallback()
    {
        var clicks = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void BitDropMenuShouldNotInvokeTheOnClickCallbackWhenDisabled()
    {
        var clicks = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void BitDropMenuShouldInvokeTheOnOpenAndOnDismissCallbacks()
    {
        var opens = 0;
        var dismisses = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OnOpen, () => opens++);
            parameters.Add(p => p.OnDismiss, () => dismisses++);
        });

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual(1, opens);
        Assert.AreEqual(0, dismisses);

        component.Find(".bit-drm-ovl").Click();

        Assert.AreEqual(1, opens);
        Assert.AreEqual(1, dismisses);
    }

    [TestMethod]
    public void BitDropMenuShouldSupportTwoWayBindingOfIsOpen()
    {
        var isOpen = false;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-drm-btn").Click();
        Assert.IsTrue(isOpen);

        component.Find(".bit-drm-ovl").Click();
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDropMenuShouldNotChangeAOneWayBoundIsOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsOpen, false);
        });

        component.Find(".bit-drm-btn").Click();

        // Without a change callback the open state stays owned by the parent.
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldRespectTheDefaultIsOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.DefaultIsOpen, true);
        });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.IsTrue(component.Find(".bit-drm-ovl").GetAttribute("style").Contains("display:block"));

        component.Find(".bit-drm-ovl").Click();

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldIgnoreTheDefaultIsOpenWhenIsOpenIsSet()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.IsOpen, false);
        });

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    [DataRow("ArrowDown")]
    [DataRow("ArrowUp")]
    public void BitDropMenuShouldOpenOnTheArrowKeys(string key)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldTakeTheScrollingKeysFromThePageOnTheButton()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var setup = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Utils.preventDefaultKeys");

        // The arrow keys open the callout, and their default behavior - scrolling the page - is not
        // something a Blazor keydown handler can suppress per key from within itself.
        Assert.AreEqual(component.Find(".bit-drm-btn").Id, setup.Arguments[0]);
        CollectionAssert.AreEqual(new[] { "ArrowDown", "ArrowUp" }, (string[])setup.Arguments[1]!);
    }

    [TestMethod]
    public void BitDropMenuShouldCloseOnEscapeFromTheButton()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldCloseOnEscapeFromTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        // Escape inside the callout is answered on the JS side, which only reports it while no callout opened
        // from inside this one is open - registered once, for the life of the component.
        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Utils.setupEscape");
        Assert.AreEqual(component.Find(".bit-drm-cal").Id, setup.Arguments[0]);

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        await component.InvokeAsync(() => component.Instance._OnEscape());

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.setupEscape"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotCloseOnAKeyDownBubblingUpToTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        // An Escape bubbling up from a dropdown inside the content is that dropdown's to answer: the callout
        // has no keydown handler of its own that could close the whole panel along with the dropdown's list.
        Assert.ThrowsExactly<MissingEventHandlerException>(() =>
            component.Find(".bit-drm-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" }));

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldCloseWhenTheKeyboardTabsOffTheButton()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        var before = CountInvocations("Blazor._internal.domWrapper.focus");

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        // The callout sits at the end of the body while open, so the tab sequence runs past it into the
        // page: leaving it open would float it over a page the keyboard has already moved on from. The
        // focus itself is left to the browser to move, since the whole point of the key is to move on.
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, CountInvocations("Blazor._internal.domWrapper.focus") - before);
    }

    [TestMethod]
    public void BitDropMenuShouldIgnoreATabOnAClosedButton()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var before = CountCalloutToggles();

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, CountCalloutToggles() - before);
    }

    [TestMethod]
    public void BitDropMenuShouldIgnoreOtherKeys()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldOpenCloseAndToggleProgrammatically()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        await component.InvokeAsync(() => component.Instance.Open());
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        await component.InvokeAsync(() => component.Instance.Close());
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldPositionTheCalloutOncePerStateChange()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        var before = CountCalloutToggles();

        component.Find(".bit-drm-btn").Click();

        // Assigning IsOpen and the open path itself must not each run their own layout pass, which
        // would position the callout twice and replay its entry animation.
        Assert.AreEqual(1, CountCalloutToggles() - before);

        before = CountCalloutToggles();

        component.Find(".bit-drm-ovl").Click();

        Assert.AreEqual(1, CountCalloutToggles() - before);
    }

    [TestMethod]
    public async Task BitDropMenuShouldNotInvokeTheOpenAndDismissCallbacksForANoOpStateChange()
    {
        var opens = 0;
        var dismisses = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OnOpen, () => opens++);
            parameters.Add(p => p.OnDismiss, () => dismisses++);
        });

        await component.InvokeAsync(() => component.Instance.Close());
        Assert.AreEqual(0, dismisses);

        await component.InvokeAsync(() => component.Instance.Open());
        await component.InvokeAsync(() => component.Instance.Open());
        Assert.AreEqual(1, opens);

        await component.InvokeAsync(() => component.Instance.Close());
        await component.InvokeAsync(() => component.Instance.Close());
        Assert.AreEqual(1, dismisses);
    }

    [TestMethod]
    public void BitDropMenuShouldNotRepositionACalloutTheParentKeepsOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsOpen, true);
        });

        var before = CountCalloutToggles();

        component.Find(".bit-drm-btn").Click();

        // The parent owns the state and keeps it at true, so there is nothing to close: toggling the
        // callout here would only replay its entry animation.
        Assert.AreEqual(0, CountCalloutToggles() - before);
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldApplyTheCalloutWidths()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Width, "16rem");
            parameters.Add(p => p.MinWidth, "8rem");
            parameters.Add(p => p.MaxWidth, "24rem");
        });

        var style = component.Find(".bit-drm-cal").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("--bit-DropMenu-callout-width:16rem"));
        Assert.IsTrue(style.Contains("--bit-DropMenu-callout-min-width:8rem"));
        Assert.IsTrue(style.Contains("--bit-DropMenu-callout-max-width:24rem"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotApplyTheCalloutWidthsByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.IsNull(component.Find(".bit-drm-cal").GetAttribute("style"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderAModalDialogOnlyWhenTheFocusIsTrapped()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        // The content is arbitrary, so the callout is a dialog in every mode rather than a menu whose items
        // and arrow-key navigation it does not have; it is only a modal one while it keeps the keyboard.
        Assert.AreEqual("dialog", component.Find(".bit-drm-btn").GetAttribute("aria-haspopup"));
        Assert.AreEqual("dialog", component.Find(".bit-drm-cal").GetAttribute("role"));
        Assert.IsNull(component.Find(".bit-drm-cal").GetAttribute("aria-modal"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TrapFocus, true);
        });

        Assert.AreEqual("dialog", component.Find(".bit-drm-btn").GetAttribute("aria-haspopup"));
        Assert.AreEqual("dialog", component.Find(".bit-drm-cal").GetAttribute("role"));
        Assert.AreEqual("true", component.Find(".bit-drm-cal").GetAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitDropMenuShouldSetUpAndDisposeTheFocusTrapWithTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TrapFocus, true);
        });

        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.setupFocusTrap"));

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.setupFocusTrap"));
        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.disposeFocusTrap"));

        component.Find(".bit-drm-ovl").Click();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.disposeFocusTrap"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotSetUpTheFocusTrapWithoutTrapFocus()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.setupFocusTrap"));
    }

    [TestMethod]
    public void BitDropMenuShouldFocusTheCalloutOnlyWhenItIsAskedTo()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        // A click leaves the focus on the trigger, which is where the pointer put it.
        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void BitDropMenuShouldFocusTheCalloutOnOpen(bool autoFocus, bool trapFocus)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AutoFocus, autoFocus);
            parameters.Add(p => p.TrapFocus, trapFocus);
        });

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    public void BitDropMenuShouldFocusTheCalloutWhenOpenedWithTheArrowKeys()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        // The arrow keys are how the keyboard reaches the content, so they always hand the focus over.
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("Spacebar")]
    public void BitDropMenuShouldFocusTheCalloutWhenTheButtonIsActivatedFromTheKeyboard(string key)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        // The browser dispatches a click of its own for these keys, which is the one that opens the
        // callout: the keydown only records that the activation came from the keyboard.
        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = key });
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotFocusTheCalloutOnAPointerClickAfterAKeyboardActivation()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        component.Find(".bit-drm-btn").Click();
        component.Find(".bit-drm-btn").Click();

        // The second click closes the callout, and the third opens it again with the pointer, which
        // leaves the focus where it put it: the keyboard intent belonged to the first activation only.
        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    public void BitDropMenuShouldFocusTheCalloutOnAnArrowKeyWhileItIsAlreadyOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        // The pointer opened it, so the content is showing while the keyboard is still on the trigger.
        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    public void BitDropMenuShouldFocusTheCalloutWhenTheOpenStateComesFromOutside()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.IsOpen, false);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.IsOpen, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement")));
    }

    [TestMethod]
    public async Task BitDropMenuShouldReturnTheFocusToTheButtonWhenTheCalloutHeldIt()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.containsActiveElement", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        var before = CountInvocations("Blazor._internal.domWrapper.focus");

        await component.InvokeAsync(() => component.Instance._OnEscape());

        Assert.AreEqual(1, CountInvocations("Blazor._internal.domWrapper.focus") - before);
    }

    [TestMethod]
    public void BitDropMenuShouldNotTouchTheFocusWhenTheCalloutDidNotHoldIt()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        var before = CountInvocations("Blazor._internal.domWrapper.focus");

        component.Find(".bit-drm-ovl").Click();

        // The pointer moved the focus somewhere of its own; pulling it back would take it from there.
        Assert.AreEqual(0, CountInvocations("Blazor._internal.domWrapper.focus") - before);
    }

    [TestMethod]
    public void BitDropMenuShouldFocusTheTriggerOnAPointerActivation()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        // Not every engine focuses a button it has just dispatched a click for, and a trigger that never
        // took the focus is a drop menu without its Escape key: the handler that closes the callout on
        // it sits on the trigger.
        Assert.AreEqual(1, CountInvocations("Blazor._internal.domWrapper.focus"));
        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void BitDropMenuShouldLeaveTheFocusToTheCalloutWhenTheCalloutIsTheOneTakingIt(bool autoFocus, bool trapFocus)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AutoFocus, autoFocus);
            parameters.Add(p => p.TrapFocus, trapFocus);
        });

        component.Find(".bit-drm-btn").Click();

        // Pulling the focus back to the trigger here would take it straight off the content the callout
        // was just asked to hand it to.
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
        Assert.AreEqual(0, CountInvocations("Blazor._internal.domWrapper.focus"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotFocusTheTriggerOnAKeyboardActivation()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        component.Find(".bit-drm-btn").Click();

        // The keyboard hands the focus over to the content, and it is already on the trigger anyway.
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.focusFirstElement"));
        Assert.AreEqual(0, CountInvocations("Blazor._internal.domWrapper.focus"));
    }

    [TestMethod]
    public void BitDropMenuShouldOpenAndCloseOnHover()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverCloseDelay, 0);
        });

        component.Find(".bit-drm").MouseEnter();

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".bit-drm").MouseLeave();

        component.WaitForAssertion(() => Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded")));
    }

    [TestMethod]
    public void BitDropMenuShouldKeepTheCalloutOpenWhileThePointerIsOnIt()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverCloseDelay, 500);
        });

        component.Find(".bit-drm").MouseEnter();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        // The pointer moves off the button and onto the callout before the close delay is up.
        component.Find(".bit-drm").MouseLeave();
        component.Find(".bit-drm-cal").MouseEnter();

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotHoverOnADeviceWithoutAPointerToHoverWith()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
        });

        // The JS side reports no hovering pointer, which is what a touch screen does.
        component.Find(".bit-drm").MouseEnter();

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".bit-drm-btn").Click();

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotCloseOnAClickWhileThePointerHoldsItOpen()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var clicks = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        component.Find(".bit-drm").MouseEnter();
        component.Find(".bit-drm-btn").Click();

        // Closing here would take away what the pointer has only just been shown, but the click itself
        // is still a click as far as the consumer is concerned.
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void BitDropMenuShouldOpenOnAClickInTheHoverModeWhenThePointerIsAway()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
        });

        // A keyboard activation of the trigger arrives as a click with the pointer nowhere near it.
        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldLetTheOverlayThroughInTheHoverMode()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
        });

        // The overlay covers the whole page while the callout is open, so it would swallow the very
        // mouseover events the hover mode is driven by.
        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-drm-ovl").ClassList.Contains("bit-drm-ovl-hov")));
    }

    [TestMethod]
    public void BitDropMenuShouldNotOpenOnHoverWhenDisabledOrLoading()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        component.Find(".bit-drm").MouseEnter();
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.IsLoading, true);
        });

        component.Find(".bit-drm").MouseEnter();
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldWaitOutTheHoverOpenDelay()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverOpenDelay, 60);
        });

        component.Find(".bit-drm").MouseEnter();

        // The delay is waited out on a timer of its own, so the open state arrives after the event that
        // asked for it rather than with it.
        component.WaitForAssertion(() => Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded")));
    }

    [TestMethod]
    public async Task BitDropMenuShouldDropAHoverThePointerTookBack()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isHoverDevice", _ => true).SetResult(true);

        var hoverOpenDelay = 60;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OpenOnHover, true);
            parameters.Add(p => p.HoverOpenDelay, hoverOpenDelay);
        });

        component.Find(".bit-drm").MouseEnter();
        component.Find(".bit-drm").MouseLeave();

        // Nothing happening is what is asserted here, so the wait has to outlast the open delay by enough
        // of a margin that a menu which did open would have opened by the time the assertion runs.
        await Task.Delay(hoverOpenDelay * 4);

        // The pointer was only passing over the button on its way somewhere else.
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldKeepTheCalloutWithinTheViewportByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.IsTrue(component.Find(".bit-drm-cal").ClassList.Contains("bit-drm-fit"));

        component.Find(".bit-drm-btn").Click();

        // With nothing else named as the scrollable part of the content, the callout itself is what the
        // positioning code caps to the room the viewport leaves. Argument 10 of Callouts.toggle is the
        // scrollContainerId, which is the argument every scrollContainerId assertion here reads.
        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");
        Assert.AreEqual(component.Find(".bit-drm-cal").Id, toggle.Arguments[10]);
    }

    [TestMethod]
    public void BitDropMenuShouldLeaveTheViewportFitToWhoeverTookItOver()
    {
        // A named scroll container, a max height and the responsive panel each size the content already.
        foreach (var parameters in new Action<ComponentParameterCollectionBuilder<BitDropMenu>>[]
        {
            p => p.Add(x => x.ScrollContainerId, "the-scroller"),
            p => p.Add(x => x.MaxHeight, "10rem"),
            p => p.Add(x => x.Responsive, true),
        })
        {
            var component = RenderComponent(parameters);

            Assert.IsFalse(component.Find(".bit-drm-cal").ClassList.Contains("bit-drm-fit"));
        }
    }

    [TestMethod]
    public void BitDropMenuShouldFollowTrapFocusChangingWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.setupFocusTrap"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TrapFocus, true);
        });

        // The trap is registered against the open callout, so it has to reach the one already open.
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.setupFocusTrap"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TrapFocus, false);
        });

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.disposeFocusTrap"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldNotOpenProgrammaticallyWhenDisabled()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(() => component.Instance.Open());
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldNotOpenProgrammaticallyWhenLoading()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsLoading, true);
        });

        await component.InvokeAsync(() => component.Instance.Open());
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        await component.InvokeAsync(() => component.Instance.Toggle());
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldCloseAnOpenCalloutWhenTheDropMenuIsDisabled()
    {
        var isOpen = true;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        // A disabled root takes no pointer events, so a callout left open here could never be closed
        // by leaving it in the hover mode.
        Assert.IsFalse(isOpen);
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldCloseAnOpenCalloutWhenTheDropMenuStartsLoading()
    {
        var isOpen = false;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-drm-btn").Click();
        Assert.IsTrue(isOpen);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsLoading, true);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        Assert.IsFalse(isOpen);
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotOpenADisabledDropMenuThatStartsOutOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultIsOpen, true);
        });

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, CountCalloutToggles());
    }

    [TestMethod]
    public void BitDropMenuShouldCloseOnAClickInsideTheCalloutWithAutoClose()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AutoClose, true);
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, @"<button class=""item"">Item</button>")));
        });

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".item").Click();

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldKeepTheCalloutOpenOnAContentClickByDefault()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, @"<button class=""item"">Item</button>")));
        });

        component.Find(".bit-drm-btn").Click();

        component.Find(".item").Click();

        // A callout hosting a form or a filter panel is meant to stay open while it is being used.
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldMarkTheButtonBusyWhileLoading()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.IsNull(component.Find(".bit-drm-btn").GetAttribute("aria-busy"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsLoading, true);
        });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitDropMenuShouldPassThePositioningInputsToTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.MatchWidth, true);
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.DropDirection, BitDropDirection.All);
        });

        component.Find(".bit-drm-btn").Click();

        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        Assert.AreEqual(component.Find(".bit-drm").Id, toggle.Arguments[1]);
        Assert.AreEqual(component.Find(".bit-drm-cal").Id, toggle.Arguments[3]);
        Assert.AreEqual(component.Find(".bit-drm-ovl").Id, toggle.Arguments[5]);
        Assert.AreEqual(true, toggle.Arguments[6]);
        Assert.AreEqual(BitResponsiveMode.Panel, toggle.Arguments[7]);
        Assert.AreEqual(BitDropDirection.All, toggle.Arguments[8]);
        Assert.AreEqual(true, toggle.Arguments[9]);
        // MatchWidth is applied after the callout is measured, which is what makes it win over Width.
        Assert.AreEqual(true, toggle.Arguments[14]);
    }

    [TestMethod]
    public void BitDropMenuShouldPassTheNamedScrollContainerToTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.ScrollContainerId, "the-scroller");
        });

        component.Find(".bit-drm-btn").Click();

        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        // Whatever the consumer named as the scrollable part of the content is what the positioning
        // code caps to the room the viewport leaves, instead of the callout itself.
        Assert.AreEqual("the-scroller", toggle.Arguments[10]);
    }

    [TestMethod]
    public void BitDropMenuShouldNotPositionTheCalloutInTheResponsiveModeAgainstTheViewport()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
        });

        component.Find(".bit-drm-btn").Click();

        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        // A responsive drop menu is a panel sized against the screen on exactly the screens where the
        // callout would not have fit, so nothing else has to cap it.
        Assert.AreEqual(string.Empty, toggle.Arguments[10]);
    }

    [TestMethod]
    public async Task BitDropMenuShouldDismissWhenAnotherCalloutTakesOver()
    {
        var dismisses = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.TrapFocus, true);
            parameters.Add(p => p.OnDismiss, () => dismisses++);
        });

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        var before = CountCalloutToggles();

        // The JS side has already hidden the callout to make room for the one taking over, so only the
        // state and the registrations tied to it are left to unwind here.
        await component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened());

        Assert.AreEqual(1, dismisses);
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.disposeFocusTrap"));
        Assert.AreEqual(0, CountCalloutToggles() - before);
        // The focus is left where it is: whatever took the callout over is about to take it.
        Assert.AreEqual(0, CountInvocations("Blazor._internal.domWrapper.focus"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldCloseWhenTheResponsivePanelIsSwipedAway()
    {
        var dismisses = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.OnDismiss, () => dismisses++);
        });

        component.Find(".bit-drm-btn").Click();

        await component.InvokeAsync(() => component.Instance._OnClose());

        Assert.AreEqual(1, dismisses);
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldNotCloseOnAContentClickOfADisabledDropMenuWithAutoClose()
    {
        var isOpen = true;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.AutoClose, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, @"<button class=""item"">Item</button>")));
        });

        var before = CountCalloutToggles();

        component.Find(".bit-drm-cal").Click();

        // The parent owns the state here, so nothing about the callout may move on a click the drop
        // menu is in no state to answer.
        Assert.AreEqual(0, CountCalloutToggles() - before);
    }

    [TestMethod]
    public void BitDropMenuShouldApplyTheSpinnerAndOverlayClassesAndStyles()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Classes, new BitDropMenuClassStyles { Spinner = "cls-spinner", Overlay = "cls-overlay" });
            parameters.Add(p => p.Styles, new BitDropMenuClassStyles { Spinner = "color:olive", Overlay = "color:navy" });
        });

        Assert.IsTrue(component.Find(".bit-drm-spn").ClassList.Contains("cls-spinner"));
        Assert.IsTrue(component.Find(".bit-drm-spn").GetAttribute("style")!.Contains("color:olive"));
        Assert.IsTrue(component.Find(".bit-drm-ovl").ClassList.Contains("cls-overlay"));
        Assert.IsTrue(component.Find(".bit-drm-ovl").GetAttribute("style")!.Contains("color:navy"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldReleaseWhatItRegisteredOnTheJsSideWhenItIsDisposed()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Responsive, true);
        });

        var calloutId = component.Find(".bit-drm-cal").Id;
        var buttonId = component.Find(".bit-drm-btn").Id;

        await component.Instance.DisposeAsync();

        // The callout lives at the end of the body while it is open and every registration is keyed by
        // its id, so a drop menu that goes away without unwinding them leaves both behind.
        Assert.AreEqual(1, Context.JSInterop.Invocations
            .Count(i => i.Identifier == "BitBlazorUI.Callouts.clear" && (string)i.Arguments[0]! == calloutId));
        Assert.AreEqual(1, Context.JSInterop.Invocations
            .Count(i => i.Identifier == "BitBlazorUI.Utils.disposePreventDefaultKeys" && (string)i.Arguments[0]! == buttonId));
        Assert.AreEqual(1, Context.JSInterop.Invocations
            .Count(i => i.Identifier == "BitBlazorUI.Utils.disposeFocusTrap" && (string)i.Arguments[0]! == calloutId));
        Assert.AreEqual(1, Context.JSInterop.Invocations
            .Count(i => i.Identifier == "BitBlazorUI.Swipes.dispose" && (string)i.Arguments[0]! == calloutId));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitDropMenuShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-drm").GetAttribute("style") ?? string.Empty;

        if (visibility is BitVisibility.Visible)
        {
            // The expected style of a visible drop menu is no style at all, which Contains reports for
            // any style there is, so the two hiding declarations are ruled out by name instead.
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
        else
        {
            Assert.IsTrue(style.Contains(expectedStyle));
        }
    }


    [TestMethod]
    public void BitDropMenuParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitDropMenu)}", BitDropMenuParams.ParamName);

        var @params = new BitDropMenuParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitDropMenuParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitDropMenuShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitDropMenuParams
                {
                    Color = BitColor.Success,
                    Size = BitSize.Large,
                    Variant = BitVariant.Outline,
                    FullWidth = true,
                    NoChevron = true,
                    IconName = "Add",
                    Title = "Cascaded title",
                    Background = BitColorKind.Secondary,
                    NoShadow = true
                }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropMenu>(0);
                builder.AddAttribute(1, nameof(BitDropMenu.Text), "Menu");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-drm");

        Assert.IsTrue(root.ClassList.Contains("bit-drm-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-drm-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-drm-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-drm-flw"));
        Assert.AreEqual(0, component.FindAll(".bit-drm-chv").Count);
        Assert.IsTrue(component.Find(".bit-drm-icn").ClassList.Contains("bit-icon--Add"));
        Assert.AreEqual("Cascaded title", component.Find(".bit-drm-btn").GetAttribute("title"));

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-bsg"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-nsh"));
    }

    [TestMethod]
    public void BitDropMenuDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitDropMenuParams
                {
                    Color = BitColor.Success,
                    Size = BitSize.Large,
                    IconName = "Add",
                    Title = "Cascaded title"
                }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropMenu>(0);
                builder.AddAttribute(1, nameof(BitDropMenu.Text), "Menu");
                builder.AddAttribute(2, nameof(BitDropMenu.Color), BitColor.Error);
                builder.AddAttribute(3, nameof(BitDropMenu.Size), BitSize.Small);
                builder.AddAttribute(4, nameof(BitDropMenu.Title), "Direct title");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-drm");

        Assert.IsTrue(root.ClassList.Contains("bit-drm-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-drm-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-drm-suc"));
        Assert.AreEqual("Direct title", component.Find(".bit-drm-btn").GetAttribute("title"));

        // What the markup left alone still comes from the cascade.
        Assert.IsTrue(component.Find(".bit-drm-icn").ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitDropMenuParamsUpdateParametersShouldSetAllProperties()
    {
        var classes = new BitDropMenuClassStyles { Root = "cascaded-root" };
        var styles = new BitDropMenuClassStyles { Root = "color:red" };
        var icon = BitIconInfo.Css("fa-solid fa-house");
        var chevron = BitIconInfo.Css("fa-solid fa-caret-down");

        var @params = new BitDropMenuParams
        {
            Alignment = BitCalloutAlignment.End,
            AriaDescription = "Description",
            AriaHidden = true,
            AutoClose = true,
            AutoFocus = true,
            Background = BitColorKind.Tertiary,
            Border = BitColorKind.Primary,
            ChevronDownIcon = chevron,
            ChevronDownIconName = "ChevronDown",
            Classes = classes,
            Color = BitColor.Warning,
            DropDirection = BitDropDirection.All,
            FullWidth = true,
            HoverCloseDelay = 300,
            HoverOpenDelay = 200,
            Icon = icon,
            IconName = "Home",
            IsLoading = true,
            LazyRender = true,
            MatchWidth = true,
            MaxHeight = "10rem",
            MaxWidth = "30rem",
            MinWidth = "10rem",
            NoChevron = true,
            NoShadow = true,
            OpenOnHover = true,
            PanelPosition = BitPanelPosition.Bottom,
            Responsive = true,
            Size = BitSize.Small,
            Styles = styles,
            Title = "Title",
            Transparent = true,
            TrapFocus = true,
            Variant = BitVariant.Text,
            Width = "20rem",
            AriaLabel = "Label",
            TabIndex = "3"
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropMenu>(0);
                builder.CloseComponent();
            });
        });

        var instance = component.FindComponent<BitDropMenu>().Instance;

        Assert.AreEqual(BitCalloutAlignment.End, instance.Alignment);
        Assert.AreEqual("Description", instance.AriaDescription);
        Assert.IsTrue(instance.AriaHidden);
        Assert.IsTrue(instance.AutoClose);
        Assert.IsTrue(instance.AutoFocus);
        Assert.AreEqual(BitColorKind.Tertiary, instance.Background);
        Assert.AreEqual(BitColorKind.Primary, instance.Border);
        Assert.AreSame(chevron, instance.ChevronDownIcon);
        Assert.AreEqual("ChevronDown", instance.ChevronDownIconName);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual(BitDropDirection.All, instance.DropDirection);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreEqual(300, instance.HoverCloseDelay);
        Assert.AreEqual(200, instance.HoverOpenDelay);
        Assert.AreSame(icon, instance.Icon);
        Assert.AreEqual("Home", instance.IconName);
        Assert.IsTrue(instance.IsLoading);
        Assert.IsTrue(instance.LazyRender);
        Assert.IsTrue(instance.MatchWidth);
        Assert.AreEqual("10rem", instance.MaxHeight);
        Assert.AreEqual("30rem", instance.MaxWidth);
        Assert.AreEqual("10rem", instance.MinWidth);
        Assert.IsTrue(instance.NoChevron);
        Assert.IsTrue(instance.NoShadow);
        Assert.IsTrue(instance.OpenOnHover);
        Assert.AreEqual(BitPanelPosition.Bottom, instance.PanelPosition);
        Assert.IsTrue(instance.Responsive);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreEqual("Title", instance.Title);
        Assert.IsTrue(instance.Transparent);
        Assert.IsTrue(instance.TrapFocus);
        Assert.AreEqual(BitVariant.Text, instance.Variant);
        Assert.AreEqual("20rem", instance.Width);
        Assert.AreEqual("Label", instance.AriaLabel);
        Assert.AreEqual("3", instance.TabIndex);

        var root = component.Find(".bit-drm");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color:red"));
    }

    [TestMethod]
    public void BitDropMenuShouldCarryItsPublicCssVariablesOverToTheCalloutAndTheOverlay()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Style, "color:blue; --bit-DropMenu-callout-background: pink; --bit-DropMenu-radius:0");
            parameters.Add(p => p.Styles, new BitDropMenuClassStyles
            {
                Root = "--bit-DropMenu-callout-radius:2px",
                Callout = "--bit-DropMenu-callout-background:gold"
            });
            parameters.Add(p => p.MinWidth, "8rem");
        });

        var calloutStyle = component.Find(".bit-drm-cal").GetAttribute("style")!;
        var overlayStyle = component.Find(".bit-drm-ovl").GetAttribute("style")!;

        // The callout is rendered outside the root and relocated to the body, so the public variables set on
        // the drop menu reach it only by being copied - and only they are: color:blue belongs to the button.
        Assert.IsTrue(calloutStyle.Contains("--bit-DropMenu-callout-background: pink;"));
        Assert.IsTrue(calloutStyle.Contains("--bit-DropMenu-radius:0;"));
        Assert.IsTrue(calloutStyle.Contains("--bit-DropMenu-callout-radius:2px;"));
        Assert.IsFalse(calloutStyle.Contains("color:blue"));
        Assert.IsTrue(overlayStyle.Contains("--bit-DropMenu-callout-background: pink;"));

        // The sizing parameters come after the copy and Styles.Callout after them, so the most specific wins.
        var copied = calloutStyle.IndexOf("--bit-DropMenu-callout-background: pink", StringComparison.Ordinal);
        var minWidth = calloutStyle.IndexOf("--bit-DropMenu-callout-min-width:8rem", StringComparison.Ordinal);
        var own = calloutStyle.IndexOf("--bit-DropMenu-callout-background:gold", StringComparison.Ordinal);

        Assert.IsTrue(copied >= 0 && copied < minWidth && minWidth < own);
    }

    [TestMethod]
    public void BitDropMenuShouldCarryTheOpenedStylesPublicCssVariablesOnlyWhileOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Styles, new BitDropMenuClassStyles { Opened = "--bit-DropMenu-callout-shadow:none" });
        });

        Assert.IsNull(component.Find(".bit-drm-cal").GetAttribute("style"));

        component.Find(".bit-drm-btn").Click();

        Assert.IsTrue(component.Find(".bit-drm-cal").GetAttribute("style")!.Contains("--bit-DropMenu-callout-shadow:none"));
    }

    [TestMethod]
    public void BitDropMenuShouldSquareOffAButtonThatHoldsOnlyAnIcon()
    {
        var iconOnly = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.IconName, "More");
            parameters.Add(p => p.AriaLabel, "More actions");
        });

        var withText = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.IconName, "More");
            parameters.Add(p => p.Text, "More");
        });

        Assert.IsTrue(iconOnly.Find(".bit-drm").ClassList.Contains("bit-drm-ion"));
        Assert.IsFalse(withText.Find(".bit-drm").ClassList.Contains("bit-drm-ion"));
    }

    [TestMethod]
    public void BitDropMenuShouldHandTheKeyboardBackToThePageWhenItDoesNotTrapIt()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.setupTabOut"));

        component.Find(".bit-drm-btn").Click();

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Utils.setupTabOut");

        Assert.AreEqual(component.Find(".bit-drm-cal").Id, setup.Arguments[0]);
        Assert.AreEqual(component.Find(".bit-drm-btn").Id, setup.Arguments[1]);
        Assert.AreEqual(0, CountInvocations("BitBlazorUI.Utils.disposeTabOut"));

        component.Find(".bit-drm-ovl").Click();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.disposeTabOut"));
    }

    [TestMethod]
    public void BitDropMenuShouldSwitchBetweenTheTabOutAndTheFocusTrapWhileOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();

        component.Render(parameters => parameters.Add(p => p.TrapFocus, true));

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.disposeTabOut"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.setupFocusTrap"));

        component.Render(parameters => parameters.Add(p => p.TrapFocus, false));

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Utils.disposeFocusTrap"));
        Assert.AreEqual(2, CountInvocations("BitBlazorUI.Utils.setupTabOut"));
    }

    [TestMethod]
    public async Task BitDropMenuShouldCloseWhenTheKeyboardTabsOutOfTheCallout()
    {
        var dismissed = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.OnDismiss, () => dismissed++);
        });

        component.Find(".bit-drm-btn").Click();

        Assert.IsTrue(component.Instance.IsOpen);

        await component.InvokeAsync(() => component.Instance._OnTabOut());

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, dismissed);
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    [DataRow(null, "")]
    [DataRow(BitCalloutAlignment.Start, "")]
    [DataRow(BitCalloutAlignment.Center, "center")]
    [DataRow(BitCalloutAlignment.End, "end")]
    public void BitDropMenuShouldHandTheAlignmentToThePositioningCode(BitCalloutAlignment? alignment, string expected)
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.Alignment, alignment);
        });

        component.Find(".bit-drm-btn").Click();

        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        // Argument 22 of Callouts.toggle is the alignment.
        Assert.AreEqual(expected, toggle.Arguments[22]);
    }


    [TestMethod]
    public void BitDropMenuShouldRenderALazyContentOnlyFromTheFirstOpening()
    {
        var opened = 0;

        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.LazyRender, true);
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.AddChildContent("<div class=\"lazy-content\">Body</div>");
        });

        Assert.AreEqual(0, component.FindAll(".lazy-content").Count);

        component.Find(".bit-drm-btn").Click();

        // The callout is placed against its content, so it is only shown once the render put the content in.
        Assert.AreEqual(1, component.FindAll(".lazy-content").Count);
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
        Assert.AreEqual(1, CountCalloutToggles());
        Assert.AreEqual(1, opened);

        component.Find(".bit-drm-ovl").Click();

        // Once rendered, the content stays, so whatever state it holds survives the close.
        Assert.AreEqual(1, component.FindAll(".lazy-content").Count);
        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitDropMenuShouldRenderTheContentUpFrontWithoutLazyRender()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.AddChildContent("<div class=\"eager-content\">Body</div>");
        });

        Assert.AreEqual(1, component.FindAll(".eager-content").Count);
    }

    [TestMethod]
    public void BitDropMenuShouldOpenALazyCalloutDrivenByIsOpen()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.LazyRender, true);
            parameters.AddChildContent("<div class=\"lazy-content\">Body</div>");
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(1, component.FindAll(".lazy-content").Count);
        Assert.AreEqual(1, CountCalloutToggles());
    }

    private int CountCalloutToggles()
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.toggle");
    }

    private int CountInvocations(string identifier)
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier == identifier);
    }
}
