using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// Covers <see cref="BitComponentBase.HasNotBeenSet"/>, which a params object asks before filling in a parameter:
/// it answers for every parameter of the component, whichever class of its hierarchy declares it, and gives the
/// same answer whatever the static type of the reference it is asked through.
/// </summary>
[TestClass]
public class BitHasNotBeenSetTests : BunitTestContext
{
    [TestMethod]
    public void HasNotBeenSetShouldAnswerTheSameThroughEveryReferenceType()
    {
        var component = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Class, "own-class");
            parameters.Add(p => p.IconName, "Add");
        });

        var button = component.Instance;
        BitComponentBase @base = button;

        // A shared parameter and one the button declares itself, each set and unset.
        Assert.IsFalse(button.HasNotBeenSet(nameof(BitButton.Class)));
        Assert.IsFalse(button.HasNotBeenSet(nameof(BitButton.IconName)));
        Assert.IsTrue(button.HasNotBeenSet(nameof(BitButton.Style)));
        Assert.IsTrue(button.HasNotBeenSet(nameof(BitButton.Title)));

        Assert.IsFalse(@base.HasNotBeenSet(nameof(BitButton.Class)));
        Assert.IsFalse(@base.HasNotBeenSet(nameof(BitButton.IconName)));
        Assert.IsTrue(@base.HasNotBeenSet(nameof(BitButton.Style)));
        Assert.IsTrue(@base.HasNotBeenSet(nameof(BitButton.Title)));
    }

    [TestMethod]
    public void HasNotBeenSetShouldAnswerForTheParametersOfTheInputBaseClasses()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Placeholder, "own placeholder");
        });

        var textField = component.Instance;
        BitComponentBase @base = textField;

        // ReadOnly is declared by BitInputBase, Immediate by BitTextInputBase, Placeholder by the text field.
        foreach (var reference in new[] { @base, textField })
        {
            Assert.IsFalse(reference.HasNotBeenSet(nameof(BitTextField.ReadOnly)));
            Assert.IsFalse(reference.HasNotBeenSet(nameof(BitTextField.Immediate)));
            Assert.IsFalse(reference.HasNotBeenSet(nameof(BitTextField.Placeholder)));
            Assert.IsTrue(reference.HasNotBeenSet(nameof(BitTextField.Required)));
            Assert.IsTrue(reference.HasNotBeenSet(nameof(BitTextField.Label)));
        }
    }

    [TestMethod]
    public void ParamsShouldFillInOnlyTheSharedParametersTheComponentLeftUnset()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitButtonParams
            {
                Class = "cascaded-class",
                Style = "margin: 1px;",
                Title = "Cascaded title",
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent<BitButton>(button => button.Add(p => p.Class, "own-class"));
        });

        var button = component.FindComponent<BitButton>().Instance;

        Assert.AreEqual("own-class", button.Class);
        Assert.AreEqual("margin: 1px;", button.Style);
        Assert.AreEqual("Cascaded title", button.Title);

        var root = component.Find(".bit-btn");

        Assert.IsTrue(root.ClassList.Contains("own-class"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-class"));
    }

    [TestMethod]
    public void UpdateParametersShouldKeepTheSharedParametersTheComponentWasGiven()
    {
        var component = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Class, "own-class");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var button = component.Instance;

        new BitButtonParams
        {
            Class = "params-class",
            Dir = BitDir.Ltr,
            Style = "margin: 1px;",
        }.UpdateParameters(button);

        Assert.AreEqual("own-class", button.Class);
        Assert.AreEqual(BitDir.Rtl, button.Dir);
        Assert.AreEqual("margin: 1px;", button.Style);
    }
}
