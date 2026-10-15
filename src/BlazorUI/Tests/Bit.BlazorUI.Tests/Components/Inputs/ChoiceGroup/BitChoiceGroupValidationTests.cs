using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the ChoiceGroup inside an EditForm: the invalid state the cascading EditContext puts on the group and on its
/// radios, the pick that clears it, and NoValidate keeping the group out of it altogether.
/// </summary>
[TestClass]
public class BitChoiceGroupValidationTests : BunitTestContext
{
    [TestMethod]
    public void BitChoiceGroupValidationShouldFlagAMissingRequiredChoice()
    {
        var component = RenderComponent<BitChoiceGroupValidationTest>();

        Assert.IsFalse(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-invalid"));

        component.Find("form").Submit();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(1, component.FindAll(".validation-message").Count);

        // The invalid state belongs to the choice as a whole, so the group carries it as well as every radio.
        Assert.IsTrue(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-chg").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.GetAttribute("aria-invalid") == "true"));
    }

    [TestMethod]
    public void BitChoiceGroupValidationShouldPassOnceAChoiceIsMade()
    {
        var component = RenderComponent<BitChoiceGroupValidationTest>();

        component.Find("form").Submit();

        Assert.IsTrue(component.Find(".bit-chg").ClassList.Contains("bit-inv"));

        component.FindAll(".bit-chg-icn input")[1].Change(true);

        Assert.AreEqual("B", component.Instance.TestModel.Choice);
        Assert.IsFalse(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-invalid"));
        Assert.IsFalse(component.FindAll(".bit-chg-icn input").Any(i => i.HasAttribute("aria-invalid")));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
    }

    [TestMethod]
    public void BitChoiceGroupNoValidateShouldLeaveTheInvalidStateToTheForm()
    {
        var component = RenderComponent<BitChoiceGroupValidationTest>(parameters => parameters.Add(p => p.NoValidate, true));

        component.Find("form").Submit();

        // The form still rejects the model, but the group neither turns invalid nor says so.
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.IsFalse(component.Find(".bit-chg").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-chg").HasAttribute("aria-invalid"));
        Assert.IsFalse(component.FindAll(".bit-chg-icn input").Any(i => i.HasAttribute("aria-invalid")));
    }

    [TestMethod]
    public void BitChoiceGroupRequiredShouldBeAnnouncedAndEnforcedInsideAForm()
    {
        var component = RenderComponent<BitChoiceGroupValidationTest>(parameters => parameters.Add(p => p.Required, true));

        Assert.AreEqual("true", component.Find(".bit-chg").GetAttribute("aria-required"));
        Assert.IsTrue(component.Find(".bit-chg").ClassList.Contains("bit-chg-req"));
        Assert.IsTrue(component.FindAll(".bit-chg-icn input").All(i => i.HasAttribute("required")));
    }
}
