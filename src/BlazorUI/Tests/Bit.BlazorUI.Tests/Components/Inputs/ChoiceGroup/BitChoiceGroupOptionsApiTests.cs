using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// The same behaviours as the items API, exercised through the options (ChildContent/Options) API, where
/// the items are registered by the option components themselves instead of coming from an Items collection.
/// </summary>
[TestClass]
public class BitChoiceGroupOptionsApiTests : BunitTestContext
{
    [TestMethod]
    public void BitChoiceGroupShouldDisableAllOptionInputsWhenTheGroupIsDisabled()
    {
        var component = RenderComponent<BitChoiceGroupOptionsTest>(parameters => parameters.Add(p => p.IsEnabled, false));

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.AreEqual(4, inputs.Count);
        Assert.IsTrue(inputs.All(i => i.HasAttribute("disabled")));
    }

    [TestMethod]
    public void BitChoiceGroupShouldDisableOnlyTheDisabledOptionWhenTheGroupIsEnabled()
    {
        var component = RenderComponent<BitChoiceGroupOptionsTest>();

        var inputs = component.FindAll(".bit-chg-icn input");

        Assert.IsFalse(inputs[0].HasAttribute("disabled"));
        Assert.IsFalse(inputs[1].HasAttribute("disabled"));
        Assert.IsFalse(inputs[2].HasAttribute("disabled"));
        Assert.IsTrue(inputs[3].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitChoiceGroupShouldGenerateUniqueInputIdsForTheOptions()
    {
        var component = RenderComponent<BitChoiceGroupOptionsTest>();

        var inputs = component.FindAll(".bit-chg-icn input");
        var labels = component.FindAll(".bit-chg-icn label");

        Assert.AreEqual(4, inputs.Select(i => i.Id).Distinct().Count());

        for (var i = 0; i < inputs.Count; i++)
        {
            Assert.AreEqual(inputs[i].Id, labels[i].GetAttribute("for"));
        }
    }

    [TestMethod]
    public void BitChoiceGroupShouldUpdateTheIsSelectedOfTheOptionsWhenTheValueChanges()
    {
        var component = RenderComponent<BitChoiceGroupOptionsTest>(parameters => parameters.Add(p => p.Value, "first"));

        Assert.AreEqual("First", SelectedOptionText(component));

        component.Render(parameters => parameters.Add(p => p.Value, "second"));

        Assert.AreEqual("Second", SelectedOptionText(component));
    }

    [TestMethod]
    public void BitChoiceGroupShouldKeepTheBoundValueWhenAnOptionIsAddedLater()
    {
        var component = RenderComponent<BitChoiceGroupOptionsTest>(parameters => parameters.Add(p => p.ShowSecond, false));

        // The user picks the second rendered option, which is "Third".
        component.FindAll(".bit-chg-icn input")[1].Change(true);
        Assert.AreEqual("third", component.Instance.SelectedValue);

        // An option that appears afterwards registers itself and must not disturb the selection.
        component.Render(parameters => parameters.Add(p => p.ShowSecond, true));

        Assert.AreEqual("third", component.Instance.SelectedValue);
        Assert.AreEqual("Third", SelectedOptionText(component));
    }

    [TestMethod]
    public void BitChoiceGroupShouldNotReapplyTheDefaultValueWhenAnOptionIsAddedLater()
    {
        var component = RenderComponent<BitChoiceGroupUncontrolledOptionsTest>(parameters =>
        {
            parameters.Add(p => p.ShowSecond, false);
            parameters.Add(p => p.DefaultValue, "first");
        });

        // The user picks the second rendered option, which is "Third".
        component.FindAll(".bit-chg-icn input")[1].Change(true);
        Assert.AreEqual("third", component.Instance.SelectedValue);

        // An option that appears afterwards registers itself and must not revert that choice
        // back to the DefaultValue, which has already been applied once.
        component.Render(parameters => parameters.Add(p => p.ShowSecond, true));

        var selected = component.FindComponents<BitChoiceGroupOption<string>>()
                                .Where(o => o.Instance.IsSelected)
                                .Select(o => o.Instance.Text)
                                .SingleOrDefault();

        Assert.AreEqual("Third", selected);
    }

    [TestMethod]
    public void BitChoiceGroupShouldRemoveAnOptionThatIsNoLongerRendered()
    {
        var component = RenderComponent<BitChoiceGroupOptionsTest>();

        Assert.AreEqual(4, component.FindAll(".bit-chg-icn").Count);

        component.Render(parameters => parameters.Add(p => p.ShowSecond, false));

        Assert.AreEqual(3, component.FindAll(".bit-chg-icn").Count);

        var options = component.FindComponents<BitChoiceGroupOption<string>>();
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, options.Select(o => o.Instance.Index).ToArray());
    }

    private static string? SelectedOptionText(IRenderedComponent<BitChoiceGroupOptionsTest> component)
    {
        return component.FindComponents<BitChoiceGroupOption<string>>()
                        .Where(o => o.Instance.IsSelected)
                        .Select(o => o.Instance.Text)
                        .SingleOrDefault();
    }
}
