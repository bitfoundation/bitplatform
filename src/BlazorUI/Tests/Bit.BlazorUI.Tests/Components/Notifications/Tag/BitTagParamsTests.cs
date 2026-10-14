using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Tag;

/// <summary>
/// Covers the BitParams cascade of the Tag: what a BitTagParams fills in, what it leaves alone because the tag wrote
/// it for itself, and that what the tag derives from its parameters (its classes, its link, its dismiss button, its
/// hidden name and its new-tab hint) follows the cascaded values.
/// </summary>
[TestClass]
public class BitTagParamsTests : BunitTestContext
{
    // Where a tag leads, what it does when activated or dismissed, the selection it keeps and the markup put in its
    // slots are written per tag, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTag.CascadingParameters),
        nameof(BitTag.ChildContent),
        nameof(BitTag.DefaultSelected),
        nameof(BitTag.Href),
        nameof(BitTag.OnChange),
        nameof(BitTag.OnChanging),
        nameof(BitTag.OnClick),
        nameof(BitTag.OnDismiss),
        nameof(BitTag.PrefixTemplate),
        nameof(BitTag.Selected),
        nameof(BitTag.SelectedChanged),
        nameof(BitTag.SuffixTemplate),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitTagParams tagParams, Action<RenderTreeBuilder>? addAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { tagParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTag>(0);
                addAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTagParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitTag)}", BitTagParams.ParamName);
        Assert.AreEqual("BitParams.BitTag", BitTagParams.ParamName);
    }

    [TestMethod]
    public void BitTagParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitTagParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitTagParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitTagParamsShouldCarryEveryParameterThatIsNotPerTag()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitTag).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Select(p => p.Name)
                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitTagParams).GetProperty(name), $"BitTagParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitTagParamsShouldCoverEveryParameterItNames()
    {
        // The other direction: a params property that points at nothing is silently ignored by the cascade.
        foreach (var property in typeof(BitTagParams).GetProperties())
        {
            if (property.Name == nameof(IBitComponentParams.Name)) continue;

            var target = typeof(BitTag).GetProperty(property.Name);

            Assert.IsNotNull(target, $"BitTag has no {property.Name}.");
            Assert.IsTrue(target.IsDefined(typeof(ParameterAttribute), true), $"BitTag.{property.Name} is not a parameter.");
        }
    }

    [TestMethod]
    public void BitTagShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitTagParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Shape = BitShape.Pill,
            Variant = BitVariant.Outline,
            IconName = "Add",
            Text = "Cascaded",
            NoWrap = true,
            FullWidth = true,
            Reversed = true,
            DismissIconName = "ChromeClose",
            DismissLabel = "Cascaded dismiss",
            Title = "Cascaded title",
            Target = "_blank",
            Download = "cascaded.png",
            StopPropagation = true,
            AriaDescription = "Cascaded description"
        }, builder => builder.AddAttribute(1, nameof(BitTag.OnDismiss), EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs e) => { })));

        var root = component.Find(".bit-tag");

        Assert.IsTrue(root.ClassList.Contains("bit-tag-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-cir"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-nwr"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-flw"));
        Assert.IsTrue(root.ClassList.Contains("bit-tag-rvs"));

        Assert.AreEqual("Cascaded", component.Find(".bit-tag-tex").TextContent);
        Assert.IsTrue(component.Find(".bit-tag-icn").ClassList.Contains("bit-icon--Add"));
        Assert.IsTrue(component.Find(".bit-tag-dsi").ClassList.Contains("bit-icon--ChromeClose"));
        Assert.AreEqual("Cascaded dismiss", component.Find(".bit-tag-cls").GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded title", root.GetAttribute("title"));
        Assert.AreEqual($"{root.Id}-dsc", root.GetAttribute("aria-describedby"));
        Assert.AreEqual("Cascaded description", component.Find($"[id='{root.Id}-dsc']").TextContent);
    }

    [TestMethod]
    public void BitTagShouldApplyCascadingLinkParametersFromBitParams()
    {
        var component = RenderWithParams(new BitTagParams
        {
            Target = "_blank",
            Download = "cascaded.png",
            Rel = BitLinkRels.NoFollow
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Text), "Cascaded");
            builder.AddAttribute(2, nameof(BitTag.Href), "https://bitplatform.dev");
        });

        var anchor = component.Find("a.bit-tag-cnt");

        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual("cascaded.png", anchor.GetAttribute("download"));
        // the rel attribute is derived from Href, Rel and Target together, so it has to be recalculated
        // after the cascaded values have filled the last two in - the cascaded Rel and the noopener the
        // cascaded _blank Target adds to it
        Assert.AreEqual("nofollow noopener", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitTagOwnParametersShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitTagParams
        {
            Color = BitColor.Success,
            Text = "Cascaded",
            NoWrap = true,
            Reversed = true,
            HideSelectedIcon = true,
            DismissLabel = "Cascaded dismiss",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Color), BitColor.Error);
            builder.AddAttribute(2, nameof(BitTag.Text), "Own");
            builder.AddAttribute(3, nameof(BitTag.NoWrap), false);
            builder.AddAttribute(4, nameof(BitTag.HideSelectedIcon), false);
            builder.AddAttribute(5, nameof(BitTag.Selected), true);
            builder.AddAttribute(6, nameof(BitTag.DismissLabel), "Own dismiss");
            builder.AddAttribute(7, nameof(BitTag.OnDismiss), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        var root = component.Find(".bit-tag");

        Assert.IsTrue(root.ClassList.Contains("bit-tag-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-tag-suc"));
        Assert.AreEqual("Own", component.Find(".bit-tag-tex").TextContent);
        // A bool written as false by the tag opts it out of the cascaded true.
        Assert.IsFalse(root.ClassList.Contains("bit-tag-nwr"));
        Assert.AreEqual(1, component.FindAll(".bit-tag-sic").Count);
        Assert.AreEqual("Own dismiss", component.Find(".bit-tag-cls").GetAttribute("aria-label"));
        // What the tag left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-tag-rvs"));
    }

    [TestMethod]
    public void BitTagShouldApplyCascadingSelectionAndBaseParametersFromBitParams()
    {
        var component = RenderWithParams(new BitTagParams
        {
            Disabled = true,
            SelectedIconName = "FavoriteStarFill",
            SecondaryIconName = "ChevronDown",
            DismissLabelFormat = "Take {0} off",
            Classes = new() { Root = "cascaded-root", SelectedIcon = "cascaded-sic" },
            Styles = new() { Root = "margin: 1px;" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Text), "Design");
            builder.AddAttribute(2, nameof(BitTag.Selected), true);
            builder.AddAttribute(3, nameof(BitTag.SelectedChanged), EventCallback.Factory.Create<bool>(this, (bool _) => { }));
            builder.AddAttribute(4, nameof(BitTag.OnDismiss), EventCallback.Factory.Create<MouseEventArgs>(this, (MouseEventArgs _) => { }));
        });

        var root = component.Find(".bit-tag");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style")!, "margin: 1px;");
        Assert.IsTrue(component.Find("button.bit-tag-cnt").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-tag-cls").HasAttribute("disabled"));

        var selectedIcon = component.Find(".bit-tag-sic");
        Assert.IsTrue(selectedIcon.ClassList.Contains("bit-icon--FavoriteStarFill"));
        Assert.IsTrue(selectedIcon.ClassList.Contains("cascaded-sic"));
        Assert.IsTrue(component.Find(".bit-tag-sci").ClassList.Contains("bit-icon--ChevronDown"));
        Assert.AreEqual("Take Design off", component.Find(".bit-tag-cls").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTagShouldApplyTheRemainingCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitTagParams
        {
            IconUrl = "/images/avatar.png",
            IconAlt = "Cascaded alt",
            SecondaryText = "Cascaded secondary",
            SecondaryIconName = "ChevronDown",
            AriaCurrent = BitNavAriaCurrent.Page
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Text), "Cascaded");
            builder.AddAttribute(2, nameof(BitTag.Href), "/docs");
            builder.AddAttribute(3, nameof(BitTag.Selected), true);
        });

        Assert.AreEqual("/images/avatar.png", component.Find(".bit-tag-img").GetAttribute("src"));
        Assert.AreEqual("Cascaded alt", component.Find(".bit-tag-img").GetAttribute("alt"));
        Assert.AreEqual("Cascaded secondary", component.Find(".bit-tag-stx").TextContent);
        Assert.IsTrue(component.Find(".bit-tag-sci").ClassList.Contains("bit-icon--ChevronDown"));
        Assert.AreEqual("page", component.Find("a.bit-tag-cnt").GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitTagShouldApplyTheCascadingIconsFromBitParams()
    {
        var component = RenderWithParams(new BitTagParams
        {
            Icon = BitIconInfo.Css("cascaded-icon"),
            IconName = "Calendar",
            SecondaryIcon = BitIconInfo.Bi("chevron-down"),
            SecondaryIconName = "ChevronDown",
            SelectedIcon = BitIconInfo.Css("cascaded-check"),
            SelectedIconName = "FavoriteStarFill",
            DismissIcon = BitIconInfo.Css("cascaded-xmark"),
            DismissIconName = "ChromeClose",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Text), "Cascaded");
            builder.AddAttribute(2, nameof(BitTag.Selected), true);
            builder.AddAttribute(3, nameof(BitTag.OnDismiss), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
        });

        // A cascaded Icon wins over a cascaded IconName, the way it does on the tag itself, for every slot.
        Assert.IsTrue(component.Find(".bit-tag-icn").ClassList.Contains("cascaded-icon"));
        Assert.IsFalse(component.Find(".bit-tag-icn").ClassList.Contains("bit-icon--Calendar"));
        Assert.IsTrue(component.Find(".bit-tag-sci").ClassList.Contains("bi-chevron-down"));
        Assert.IsFalse(component.Find(".bit-tag-sci").ClassList.Contains("bit-icon--ChevronDown"));
        Assert.IsTrue(component.Find(".bit-tag-sic").ClassList.Contains("cascaded-check"));
        Assert.IsFalse(component.Find(".bit-tag-sic").ClassList.Contains("bit-icon--FavoriteStarFill"));
        Assert.IsTrue(component.Find(".bit-tag-dsi").ClassList.Contains("cascaded-xmark"));
        Assert.IsFalse(component.Find(".bit-tag-dsi").ClassList.Contains("bit-icon--ChromeClose"));
    }

    [TestMethod]
    public void BitTagShouldApplyTheCascadingFocusAndNewTabParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTagParams
            {
                AllowDisabledFocus = true,
                NewTabHint = "(new tab)"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTag>(0);
                builder.AddAttribute(1, nameof(BitTag.Text), "Docs");
                builder.AddAttribute(2, nameof(BitTag.Href), "https://bitplatform.dev");
                builder.AddAttribute(3, nameof(BitTag.Target), "_blank");
                builder.CloseComponent();

                builder.OpenComponent<BitTag>(4);
                builder.AddAttribute(5, nameof(BitTag.Text), "Filter");
                builder.AddAttribute(6, nameof(BitTag.Disabled), true);
                builder.AddAttribute(7, nameof(BitTag.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("(new tab)", component.Find("a.bit-tag-cnt .bit-tag-vhd").TextContent);
        Assert.AreEqual("true", component.Find("button.bit-tag-cnt").GetAttribute("aria-disabled"));
        Assert.IsFalse(component.Find("button.bit-tag-cnt").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitTagShouldApplyACascadedNoNewTabHintAndAnEmptyNewTabHint()
    {
        var silenced = RenderWithParams(new BitTagParams { NoNewTabHint = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Text), "Docs");
            builder.AddAttribute(2, nameof(BitTag.Href), "https://bitplatform.dev");
            builder.AddAttribute(3, nameof(BitTag.Target), "_blank");
        });

        Assert.IsNull(silenced.Find("a.bit-tag-cnt").QuerySelector(".bit-tag-vhd"));

        // An empty hint is a value of its own - the one that takes the announcement off - so the cascade hands it over.
        var emptied = RenderWithParams(new BitTagParams { NewTabHint = string.Empty }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTag.Text), "Docs");
            builder.AddAttribute(2, nameof(BitTag.Href), "https://bitplatform.dev");
            builder.AddAttribute(3, nameof(BitTag.Target), "_blank");
        });

        Assert.IsNull(emptied.Find("a.bit-tag-cnt").QuerySelector(".bit-tag-vhd"));
    }

    [TestMethod]
    public void BitTagShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitTagParams
        {
            Dir = BitDir.Rtl,
            Class = "cascaded",
            Id = "cascaded-id",
            Visibility = BitVisibility.Hidden,
            AriaLabel = "Pinned to the top",
            TabIndex = "3",
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        }, builder => builder.AddAttribute(1, nameof(BitTag.IconName), "Pinned"));

        var root = component.Find(".bit-tag");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        StringAssert.Contains(root.GetAttribute("style")!, "visibility:hidden");

        // The name of a plain tag is carried as hidden text and its tab index on the root, cascaded or not.
        Assert.AreEqual("Pinned to the top", component.Find(".bit-tag-vhd").TextContent);
        Assert.AreEqual("3", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitTagParamsUpdateParametersShouldIgnoreNull()
    {
        new BitTagParams { Color = BitColor.Error }.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitTagShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var plain = RenderComponent<BitTag>(parameters => parameters.Add(p => p.Text, "Design"));

        var component = RenderWithParams(new BitTagParams(), builder => builder.AddAttribute(1, nameof(BitTag.Text), "Design"));

        var root = component.Find(".bit-tag");
        var instance = component.FindComponent<BitTag>().Instance;

        Assert.AreEqual(plain.Find(".bit-tag").ClassName, root.ClassName);
        Assert.AreEqual(plain.Find(".bit-tag").GetAttribute("style"), root.GetAttribute("style"));
        Assert.AreEqual("Design", component.Find(".bit-tag-tex").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-tag-cls").Count);

        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Size);
        Assert.IsNull(instance.Shape);
        Assert.IsNull(instance.Variant);
        Assert.IsNull(instance.NewTabHint);
        Assert.AreEqual(BitNavAriaCurrent.True, instance.AriaCurrent);
        Assert.IsFalse(instance.NoWrap);
        Assert.IsFalse(instance.Disabled);
    }
}
