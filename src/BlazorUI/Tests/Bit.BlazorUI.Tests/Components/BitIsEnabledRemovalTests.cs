using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components;

/// <summary>
/// IsEnabled became the Disabled / IsDisabled flag, with the opposite meaning (#5527). What still says
/// IsEnabled would otherwise be read as enabled without a word, so it is refused instead.
/// </summary>
[TestClass]
public class BitIsEnabledRemovalTests : BunitTestContext
{
    [TestMethod]
    [DataRow("IsEnabled")]
    [DataRow("isEnabled")]
    [DataRow("isenabled")]
    public void LeftoverIsEnabledAttributeShouldThrow(string name)
    {
        // Written in markup, a name that is not a parameter arrives as a plain attribute; bUnit's own builder
        // refuses one on a component that captures no unmatched values, so the markup is built by hand.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => Context.Render(builder =>
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddAttribute(1, name, false);
            builder.CloseComponent();
        }));

        StringAssert.Contains(exception.Message, nameof(BitButton));
        StringAssert.Contains(exception.Message, "Disabled");
    }

    [TestMethod]
    public void OtherUnmatchedAttributesShouldStillReachTheElement()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddAttribute(1, "data-enabled", "no");
            builder.CloseComponent();
        });

        Assert.AreEqual("no", component.Find("button").GetAttribute("data-enabled"));
    }

    [TestMethod]
    public void CustomItemWithOnlyIsEnabledShouldThrow()
    {
        var items = new List<LegacyItem> { new() { Text = "One", IsEnabled = false } };

        var nameSelectors = new BitButtonGroupNameSelectors<LegacyItem> { Text = { Name = nameof(LegacyItem.Text) } };

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitButtonGroup<LegacyItem>>(parameters =>
            {
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.NameSelectors, nameSelectors);
            }));

        StringAssert.Contains(exception.Message, nameof(LegacyItem));
    }

    [TestMethod]
    public void CustomItemWithIsEnabledMappedBySelectorShouldBeRead()
    {
        var items = new List<LegacyItem> { new() { Text = "One", IsEnabled = false }, new() { Text = "Two", IsEnabled = true } };
        var nameSelectors = new BitButtonGroupNameSelectors<LegacyItem>
        {
            Text = { Name = nameof(LegacyItem.Text) },
            IsDisabled = { Selector = i => i.IsEnabled is false }
        };

        var component = RenderComponent<BitButtonGroup<LegacyItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, nameSelectors);
        });

        var buttons = component.FindAll("button");
        Assert.IsTrue(buttons[0].HasAttribute("disabled"));
        Assert.IsFalse(buttons[1].HasAttribute("disabled"));
    }

    [TestMethod]
    public void CustomItemWithBothPropertiesShouldReadIsDisabled()
    {
        var items = new List<BothItem> { new() { Text = "One", IsEnabled = true, IsDisabled = true } };

        var nameSelectors = new BitButtonGroupNameSelectors<BothItem> { Text = { Name = nameof(BothItem.Text) } };

        var component = RenderComponent<BitButtonGroup<BothItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.NameSelectors, nameSelectors);
        });

        Assert.IsTrue(component.Find("button").HasAttribute("disabled"));
    }

    public class LegacyItem
    {
        public string? Text { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class BothItem
    {
        public string? Text { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsDisabled { get; set; }
    }
}
