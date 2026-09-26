using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Persona;

/// <summary>
/// Covers the BitParams cascade of the Persona: what a BitPersonaParams fills in, and what it leaves alone because
/// the persona wrote it for itself.
/// </summary>
[TestClass]
public class BitPersonaParamsTests : BunitTestContext
{
    private static RenderFragment RenderPersona(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitPersona>(0);
            builder.AddAttribute(1, nameof(BitPersona.PrimaryText), "Saleh Khafan");
            builder.AddAttribute(2, nameof(BitPersona.SecondaryText), "Developer");
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitPersonaParams @params, params RenderFragment[] personas)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [@params]);
            parameters.AddChildContent(builder =>
            {
                foreach (var persona in personas)
                {
                    builder.AddContent(0, persona);
                }
            });
        });
    }

    [TestMethod]
    public void BitPersonaParamsShouldHaveCorrectParamName()
    {
        var @params = new BitPersonaParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual("BitParams.BitPersona", @params.Name);
        Assert.AreEqual(BitPersonaParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitPersonaShouldApplyCascadingParametersToEveryPersona()
    {
        var @params = new BitPersonaParams
        {
            Size = BitPersonaSize.Size100,
            Shape = BitPersonaShape.Square,
            CoinColor = BitColor.Success,
            Vertical = true,
            Reversed = true,
            FullWidth = true,
            ShowSecondaryText = true,
            Class = "cascaded",
        };

        var component = RenderWithParams(@params, RenderPersona(), RenderPersona());

        var personas = component.FindAll(".bit-prs");

        Assert.AreEqual(2, personas.Count);

        foreach (var persona in personas)
        {
            foreach (var cls in new[] { "bit-prs-s100", "bit-prs-sqr", "bit-prs-sqs", "bit-prs-suc", "bit-prs-vrt", "bit-prs-rvs", "bit-prs-fwi", "bit-prs-sst", "cascaded" })
            {
                Assert.IsTrue(persona.ClassList.Contains(cls), $"Missing {cls}.");
            }
        }
    }

    [TestMethod]
    public void BitPersonaDirectParametersShouldOverrideCascadingParameters()
    {
        var @params = new BitPersonaParams
        {
            Size = BitPersonaSize.Size100,
            CoinColor = BitColor.Success,
            Vertical = true,
            ActionButtonTitle = "Cascaded title",
        };

        var component = RenderWithParams(@params, RenderPersona(builder =>
        {
            builder.AddAttribute(10, nameof(BitPersona.Size), BitPersonaSize.Size32);
            builder.AddAttribute(11, nameof(BitPersona.CoinColor), BitColor.Error);
            builder.AddAttribute(12, nameof(BitPersona.Vertical), false);
            builder.AddAttribute(13, nameof(BitPersona.ActionButtonTitle), "Own title");
            builder.AddAttribute(14, nameof(BitPersona.OnActionClick), EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => { }));
        }));

        var persona = component.Find(".bit-prs");

        Assert.IsTrue(persona.ClassList.Contains("bit-prs-s32"));
        Assert.IsFalse(persona.ClassList.Contains("bit-prs-s100"));
        Assert.IsTrue(persona.ClassList.Contains("bit-prs-err"));
        Assert.IsFalse(persona.ClassList.Contains("bit-prs-suc"));
        Assert.IsFalse(persona.ClassList.Contains("bit-prs-vrt"));
        Assert.AreEqual("Own title", component.Find(".bit-prs-abt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCascadedPresenceTitlesShouldNameThePresenceOfEveryPersona()
    {
        var @params = new BitPersonaParams
        {
            PresenceTitles = new() { [BitPersonaPresence.Online] = "Disponible", [BitPersonaPresence.Away] = "Absent" },
        };

        var component = RenderWithParams(@params,
            RenderPersona(builder => builder.AddAttribute(10, nameof(BitPersona.Presence), BitPersonaPresence.Online)),
            RenderPersona(builder => builder.AddAttribute(10, nameof(BitPersona.Presence), BitPersonaPresence.Away)));

        var dots = component.FindAll(".bit-prs-pre");

        Assert.AreEqual("Disponible", dots[0].GetAttribute("aria-label"));
        Assert.AreEqual("Absent", dots[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaCascadedTargetShouldProtectTheLinkCoin()
    {
        var @params = new BitPersonaParams { Target = "_blank" };

        var component = RenderWithParams(@params,
            RenderPersona(builder => builder.AddAttribute(10, nameof(BitPersona.Href), "https://bitplatform.dev")));

        var link = component.Find("a.bit-prs-cne");

        Assert.AreEqual("_blank", link.GetAttribute("target"));
        Assert.AreEqual("noopener", link.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitPersonaCascadedRelShouldReachTheLinkCoin()
    {
        var @params = new BitPersonaParams { Rel = BitLinkRels.NoFollow };

        var component = RenderWithParams(@params,
            RenderPersona(builder => builder.AddAttribute(10, nameof(BitPersona.Href), "https://bitplatform.dev")));

        Assert.AreEqual("nofollow", component.Find("a.bit-prs-cne").GetAttribute("rel"));
    }

    [TestMethod]
    public void BitPersonaCascadedImageOverlayTextShouldVeilALinkCoin()
    {
        var @params = new BitPersonaParams { ImageOverlayText = "View profile" };

        var component = RenderWithParams(@params,
            RenderPersona(builder => builder.AddAttribute(10, nameof(BitPersona.Href), "/profile")));

        Assert.AreEqual("View profile", component.Find(".bit-prs-imo").TextContent.Trim());
        Assert.AreEqual("View profile", component.Find("a.bit-prs-cne").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPersonaWithoutCascadedOverlayTextShouldLeaveALinkCoinUnveiled()
    {
        var component = RenderWithParams(new BitPersonaParams { Size = BitPersonaSize.Size72 },
            RenderPersona(builder => builder.AddAttribute(10, nameof(BitPersona.Href), "/profile")));

        Assert.AreEqual(0, component.FindAll(".bit-prs-imo").Count);
    }

    [TestMethod]
    public void BitPersonaCascadedStylesShouldReachTheParts()
    {
        var @params = new BitPersonaParams
        {
            Styles = new() { Root = "margin: 1px;", PrimaryTextContainer = "color: red;" },
            Classes = new() { PrimaryTextContainer = "cascaded-primary" },
        };

        var component = RenderWithParams(@params, RenderPersona());

        StringAssert.Contains(component.Find(".bit-prs").GetAttribute("style"), "margin: 1px;");

        var primary = component.Find(".bit-prs-ptx");

        StringAssert.Contains(primary.GetAttribute("style"), "color: red;");
        Assert.IsTrue(primary.ClassList.Contains("cascaded-primary"));
    }

    [TestMethod]
    public void BitPersonaParamsUpdateParametersShouldSetAllProperties()
    {
        var actionIcon = BitIconInfo.Bit("Camera");
        var coinIcon = BitIconInfo.Bit("Group");
        var unknownIcon = BitIconInfo.Bit("Help");
        var classes = new BitPersonaClassStyles { Root = "custom-root" };
        var styles = new BitPersonaClassStyles { Root = "color: red;" };
        BitColor[] autoCoinColors = [BitColor.Primary];
        var presenceIcons = new Dictionary<BitPersonaPresence, BitIconInfo> { [BitPersonaPresence.Online] = BitIconInfo.Bit("Accept") };
        var presenceIconNames = new Dictionary<BitPersonaPresence, string> { [BitPersonaPresence.Online] = "Accept" };
        var presenceTitles = new Dictionary<BitPersonaPresence, string> { [BitPersonaPresence.Online] = "Here" };

        var @params = new BitPersonaParams
        {
            ActionButtonTitle = "Change",
            ActionIcon = actionIcon,
            ActionIconName = "Camera",
            ActiveAppearance = BitPersonaActiveAppearance.Shadow,
            AllowPhoneInitials = true,
            AutoCoinColor = true,
            AutoCoinColors = autoCoinColors,
            Classes = classes,
            CoinColor = BitColor.Warning,
            CoinIcon = coinIcon,
            CoinIconName = "Group",
            CoinSize = 64,
            CoinVariant = BitVariant.Outline,
            FullWidth = true,
            HidePersonaDetails = true,
            ImageFadeIn = true,
            ImageLoading = BitImageLoading.Lazy,
            ImageOverlayText = "Replace",
            PresenceIcons = presenceIcons,
            PresenceIconNames = presenceIconNames,
            PresenceTitles = presenceTitles,
            Rel = BitLinkRels.NoFollow,
            Reversed = true,
            Shape = BitPersonaShape.Rounded,
            ShowInitialsUntilImageLoads = true,
            ShowOverflowTooltip = false,
            ShowSecondaryText = true,
            Size = BitPersonaSize.Size120,
            Squared = true,
            Styles = styles,
            Target = "_blank",
            UnknownIcon = unknownIcon,
            UnknownIconName = "Help",
            Vertical = true,
        };

        var persona = new BitPersona();

        @params.UpdateParameters(persona);

        Assert.AreEqual("Change", persona.ActionButtonTitle);
        Assert.AreSame(actionIcon, persona.ActionIcon);
        Assert.AreEqual("Camera", persona.ActionIconName);
        Assert.AreEqual(BitPersonaActiveAppearance.Shadow, persona.ActiveAppearance);
        Assert.IsTrue(persona.AllowPhoneInitials);
        Assert.IsTrue(persona.AutoCoinColor);
        Assert.AreSame(autoCoinColors, persona.AutoCoinColors);
        Assert.AreSame(classes, persona.Classes);
        Assert.AreEqual(BitColor.Warning, persona.CoinColor);
        Assert.AreSame(coinIcon, persona.CoinIcon);
        Assert.AreEqual("Group", persona.CoinIconName);
        Assert.AreEqual(64, persona.CoinSize);
        Assert.AreEqual(BitVariant.Outline, persona.CoinVariant);
        Assert.IsTrue(persona.FullWidth);
        Assert.IsTrue(persona.HidePersonaDetails);
        Assert.IsTrue(persona.ImageFadeIn);
        Assert.AreEqual(BitImageLoading.Lazy, persona.ImageLoading);
        Assert.AreEqual("Replace", persona.ImageOverlayText);
        Assert.AreSame(presenceIcons, persona.PresenceIcons);
        Assert.AreSame(presenceIconNames, persona.PresenceIconNames);
        Assert.AreSame(presenceTitles, persona.PresenceTitles);
        Assert.AreEqual(BitLinkRels.NoFollow, persona.Rel);
        Assert.IsTrue(persona.Reversed);
        Assert.AreEqual(BitPersonaShape.Rounded, persona.Shape);
        Assert.IsTrue(persona.ShowInitialsUntilImageLoads);
        Assert.IsFalse(persona.ShowOverflowTooltip);
        Assert.IsTrue(persona.ShowSecondaryText);
        Assert.AreEqual(BitPersonaSize.Size120, persona.Size);
        Assert.IsTrue(persona.Squared);
        Assert.AreSame(styles, persona.Styles);
        Assert.AreEqual("_blank", persona.Target);
        Assert.AreSame(unknownIcon, persona.UnknownIcon);
        Assert.AreEqual("Help", persona.UnknownIconName);
        Assert.IsTrue(persona.Vertical);
    }

    [TestMethod]
    public void BitPersonaParamsShouldCoverEveryParameterItNames()
    {
        // Every property of the params object has to be a parameter of the persona of the same name and a type it
        // can be assigned from - a params property that points at nothing is silently ignored by the cascade.
        foreach (var property in typeof(BitPersonaParams).GetProperties())
        {
            if (property.Name == nameof(IBitComponentParams.Name)) continue;

            var target = typeof(BitPersona).GetProperty(property.Name);

            Assert.IsNotNull(target, $"BitPersona has no {property.Name}.");
            Assert.IsTrue(target.IsDefined(typeof(ParameterAttribute), true), $"BitPersona.{property.Name} is not a parameter.");
        }
    }
}
