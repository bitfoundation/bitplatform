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

        Assert.IsTrue(button.HasAttribute("disabled"));
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
    <button type=""button"" aria-haspopup=""true"" aria-expanded=""false"" class=""bit-drm-btn "" id:ignore aria-controls:ignore>
        <span class=""bit-drm-txt "">Menu</span>
        <i aria-hidden=""true"" class=""bit-drm-chv bit-icon bit-icon--ChevronRight bit-ico-r90 ""></i>
    </button>
</div>
<div style=""display:none;"" class=""bit-drm-ovl "" id:ignore></div>
<div tabindex=""-1"" class=""bit-drm-cal"" id:ignore aria-labelledby:ignore>
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
            parameters.Add(p => p.AriaDescription, "described-by-id");
            parameters.Add(p => p.Title, "The tooltip");
        });

        var button = component.Find(".bit-drm-btn");
        var callout = component.Find(".bit-drm-cal");

        Assert.AreEqual("true", button.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
        Assert.AreEqual(callout.Id, button.GetAttribute("aria-controls"));
        Assert.AreEqual(button.Id, callout.GetAttribute("aria-labelledby"));
        Assert.AreEqual("The menu", button.GetAttribute("aria-label"));
        Assert.AreEqual("described-by-id", button.GetAttribute("aria-describedby"));
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
    public void BitDropMenuShouldApplyTheMaxHeight()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.Add(p => p.MaxHeight, "10rem");
        });

        var callout = component.Find(".bit-drm-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-drm-mxh"));
        Assert.IsTrue(callout.GetAttribute("style")!.Contains("--bit-drm-cal-mxh:10rem"));
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
    public void BitDropMenuShouldOpenOnTheArrowKeys()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
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
    public void BitDropMenuShouldCloseOnEscapeFromTheCallout()
    {
        var component = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
        });

        component.Find(".bit-drm-btn").Click();
        Assert.AreEqual("true", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));

        component.Find(".bit-drm-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("false", component.Find(".bit-drm-btn").GetAttribute("aria-expanded"));
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

    private int CountCalloutToggles()
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.toggle");
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

        Assert.IsTrue(style.Contains(expectedStyle));
    }
}
