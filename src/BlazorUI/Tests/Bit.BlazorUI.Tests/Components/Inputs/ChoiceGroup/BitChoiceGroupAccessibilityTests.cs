using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the accessibility contract of the ChoiceGroup: the accessible name of the group, the disabled
/// state of its inputs, and the id/IDREF wiring between each input, its label and its description.
/// </summary>
[TestClass]
public class BitChoiceGroupAccessibilityTests : BunitTestContext
{
    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
    ];

    [TestMethod]
    public void BitChoiceGroupShouldDisableAllInputsWhenTheGroupIsDisabled()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.IsEnabled, false);
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.AreEqual(2, inputs.Count);
        Assert.IsTrue(inputs.All(i => i.HasAttribute("disabled")));
    }

    [TestMethod]
    public void BitChoiceGroupShouldDisableOnlyTheDisabledItemWhenTheGroupIsEnabled()
    {
        var items = GetItems();
        items[1].IsEnabled = false;

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.IsFalse(inputs[0].HasAttribute("disabled"));
        Assert.IsTrue(inputs[1].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldMarkTheItemsOfADisabledGroupAsDisabled()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.FindAll(".bit-chg-icn").All(i => i.ClassList.Contains("bit-chg-ids")));
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotRenderAnEmptyLabelElement()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        // Only the labels of the items, no empty group label.
        Assert.AreEqual(2, component.FindAll("label").Count);
        Assert.IsTrue(component.FindAll("label").All(l => l.HasAttribute("for")));
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotReferenceAMissingLabelWithAriaLabelledBy()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.AriaLabel, "Choose one");
        });

        var root = component.Find(".bit-chg");

        // aria-labelledby wins over aria-label, so it must not be emitted while there is no label element.
        Assert.IsFalse(root.HasAttribute("aria-labelledby"));
        Assert.AreEqual("Choose one", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldReferenceItsLabelWithAriaLabelledBy()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Label, "Pick one");
        });

        var labelledBy = component.Find(".bit-chg").GetAttribute("aria-labelledby");

        Assert.IsNotNull(labelledBy);
        Assert.AreEqual("Pick one", component.Find($"#{labelledBy}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupShouldReferenceItsLabelTemplateWithAriaLabelledBy()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.LabelTemplate, "<span>Templated</span>");
        });

        var labelledBy = component.Find(".bit-chg").GetAttribute("aria-labelledby");

        Assert.IsNotNull(labelledBy);
        Assert.AreEqual("Templated", component.Find($"#{labelledBy}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupShouldGenerateValidInputIdsForValuesWithSpecialCharacters()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "Prefer not to say", Description = "desc" },
            new() { Text = "B", Value = "with \"quotes\" & #hash" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var inputs = component.FindAll(".bit-chg-icn input");
        var labels = component.FindAll(".bit-chg-icn label");

        foreach (var input in inputs)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(input.Id));
            Assert.IsFalse(input.Id!.Any(char.IsWhiteSpace), $"the id '{input.Id}' contains whitespace");
        }

        Assert.AreEqual(inputs.Count, inputs.Select(i => i.Id).Distinct().Count());

        for (var i = 0; i < inputs.Count; i++)
        {
            Assert.AreEqual(inputs[i].Id, labels[i].GetAttribute("for"));
        }

        var describedBy = inputs[0].GetAttribute("aria-describedby");
        Assert.IsNotNull(describedBy);
        Assert.AreEqual(1, component.FindAll($"#{describedBy}").Count);
    }

    [TestMethod]
    public void BitChoiceGroupShouldGenerateUniqueInputIdsForDuplicateValues()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "First", Value = "same" },
            new() { Text = "Second", Value = "same" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var ids = component.FindAll(".bit-chg-icn input").Select(i => i.Id).ToList();

        Assert.AreEqual(2, ids.Distinct().Count());
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotReferenceADescriptionThatIsNotRendered()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Description = "desc A" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.ItemLabelTemplate, (BitChoiceGroupItem<string> item) => builder => builder.AddContent(0, item.Text));
        });

        Assert.AreEqual(0, component.FindAll(".bit-chg-dsc").Count);
        Assert.IsFalse(component.Find(".bit-chg-icn input").HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldReferenceARenderedDescription()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Description = "desc A" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var describedBy = component.Find(".bit-chg-icn input").GetAttribute("aria-describedby");

        Assert.IsNotNull(describedBy);
        Assert.AreEqual("desc A", component.Find($"#{describedBy}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotRenderAGroupDescriptionWhenThereIsNone()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
        });

        Assert.AreEqual(0, component.FindAll(".bit-chg-gds").Count);
        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldReferenceItsGroupDescription()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Description, "pick the one you need");
        });

        var describedBy = component.Find(".bit-chg").GetAttribute("aria-describedby");

        Assert.IsNotNull(describedBy);
        Assert.AreEqual("pick the one you need", component.Find($"#{describedBy}").TextContent);
        Assert.IsTrue(component.Find($"#{describedBy}").ClassList.Contains("bit-chg-gds"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldPreferTheDescriptionTemplateOverTheDescription()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Description, "plain description");
            parameters.Add(p => p.DescriptionTemplate, (RenderFragment)(builder => builder.AddContent(0, "templated description")));
        });

        var describedBy = component.Find(".bit-chg").GetAttribute("aria-describedby");

        Assert.AreEqual("templated description", component.Find($"#{describedBy}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupShouldKeepTheGroupDescriptionAndTheItemDescriptionsSeparate()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Description = "desc A" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.Description, "group description");
        });

        var groupDescribedBy = component.Find(".bit-chg").GetAttribute("aria-describedby");
        var itemDescribedBy = component.Find(".bit-chg-icn input").GetAttribute("aria-describedby");

        Assert.AreNotEqual(groupDescribedBy, itemDescribedBy);
        Assert.AreEqual("group description", component.Find($"#{groupDescribedBy}").TextContent);
        Assert.AreEqual("desc A", component.Find($"#{itemDescribedBy}").TextContent);
    }

    [TestMethod]
    public void BitChoiceGroupShouldKeepAConsumerSuppliedAriaDescribedBy()
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so supply
        // them as raw component attributes (as real markup would) rather than via the builder, which
        // rejects unmatched params on components without [Parameter(CaptureUnmatchedValues)].
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, "Items", GetItems());
            builder.AddAttribute(2, "aria-describedby", "consumer-hint");
            builder.CloseComponent();
        });

        Assert.AreEqual("consumer-hint", component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldPreferAConsumerSuppliedAriaDescribedByOverTheGroupDescription()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, "Items", GetItems());
            builder.AddAttribute(2, "Description", "group description");
            builder.AddAttribute(3, "aria-describedby", "consumer-hint");
            builder.CloseComponent();
        });

        Assert.AreEqual("consumer-hint", component.Find(".bit-chg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldKeepAConsumerSuppliedAriaDescribedByOnTheInputs()
    {
        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "consumer-hint" });
        });

        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-describedby") == "consumer-hint"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldRenderTheTitleOfEachItem()
    {
        var items = new List<BitChoiceGroupItem<string>>
        {
            new() { Text = "A", Value = "A", Title = "the whole story of A" },
            new() { Text = "B", Value = "B" },
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
        });

        var containers = component.FindAll(".bit-chg-icn");

        Assert.AreEqual("the whole story of A", containers[0].GetAttribute("title"));
        Assert.IsFalse(containers[1].HasAttribute("title"));
    }
}
