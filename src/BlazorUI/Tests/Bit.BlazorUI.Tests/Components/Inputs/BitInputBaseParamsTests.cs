using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// Pins what BitInputBaseParams carries down a BitParams cascade for every input whose params object derives from it,
/// through BitTextFieldParams: ReadOnly and Required are filled in where the input left them unset and left alone
/// where it wrote them - a false included - and nothing else of the base is carried.
/// </summary>
[TestClass]
public class BitInputBaseParamsTests : BunitTestContext
{
    [TestMethod]
    public void UpdateInputBaseParametersShouldFillInWhatTheInputLeftUnset()
    {
        var @params = new BitTextFieldParams { ReadOnly = true, Required = true };

        var component = RenderComponent<BitTextField>();

        @params.UpdateInputBaseParameters(component.Instance);

        Assert.IsTrue(component.Instance.ReadOnly);
        Assert.IsTrue(component.Instance.Required);
    }

    [TestMethod]
    public void UpdateInputBaseParametersShouldLeaveWhatTheInputWroteAlone()
    {
        var @params = new BitTextFieldParams { ReadOnly = true, Required = true };

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, false);
            parameters.Add(p => p.Required, false);
        });

        @params.UpdateInputBaseParameters(component.Instance);

        // A false written on the input is a choice as much as a true is.
        Assert.IsFalse(component.Instance.ReadOnly);
        Assert.IsFalse(component.Instance.Required);
    }

    [TestMethod]
    public void UpdateInputBaseParametersShouldCarryNothingItWasNotGiven()
    {
        var @params = new BitTextFieldParams();

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Required, true);
        });

        @params.UpdateInputBaseParameters(component.Instance);

        Assert.IsFalse(component.Instance.ReadOnly);
        Assert.IsTrue(component.Instance.Required);
    }

    [TestMethod]
    public void UpdateInputBaseParametersShouldIgnoreANullInput()
    {
        var @params = new BitTextFieldParams { ReadOnly = true };

        @params.UpdateInputBaseParameters(null!);
    }

    [TestMethod]
    public void BitInputBaseParamsShouldCarryOnlyTheParametersThatDescribeAnArea()
    {
        // What identifies one field - its value, its default, its name - and what a field writes into or reads
        // before a cascade applies has no place on the params object, and the two that are left are nullable so that
        // an unset one carries nothing.
        var declared = typeof(BitInputBaseParams<string>).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        CollectionAssert.AreEquivalent(new[] { nameof(BitInputBaseParams<string>.ReadOnly), nameof(BitInputBaseParams<string>.Required) }, declared.Select(p => p.Name).ToArray());
        Assert.IsTrue(declared.All(p => p.PropertyType == typeof(bool?)));
    }
}
