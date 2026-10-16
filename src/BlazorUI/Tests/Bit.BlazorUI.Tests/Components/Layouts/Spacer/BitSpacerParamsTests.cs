using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Spacer;

/// <summary>
/// Covers the BitParams cascade of the spacer as a contract rather than as a sample: the name the params object is
/// read by, that every parameter of the spacer has a property on it (a spacer has no content, so nothing is per
/// spacer), that every one of those properties is actually copied across by UpdateParameters, and that a value the
/// spacer wrote for itself is never overwritten by a cascaded one. What the cascaded values render as is covered by
/// the BitSpacerShouldRespectCascadingParams test of BitSpacerTests.
/// </summary>
[TestClass]
public class BitSpacerParamsTests : BunitTestContext
{
    private static readonly string[] _notCascaded =
    [
        nameof(BitSpacer.CascadingParameters),
    ];

    [TestMethod]
    public void BitSpacerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSpacer)}", BitSpacerParams.ParamName);
        Assert.AreEqual("BitParams.BitSpacer", BitSpacerParams.ParamName);
    }

    [TestMethod]
    public void BitSpacerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSpacerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitSpacerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSpacerParamsShouldCarryEveryParameter()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitSpacer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                          .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length >= 8, $"Only {parameters.Length} parameters were found on {nameof(BitSpacer)}.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitSpacerParams).GetProperty(name), $"BitSpacerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitSpacerShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitSpacerParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.AddCascadingValue(BitSpacerParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitSpacerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitSpacerParams { Width = 10, Height = 20, Element = "span" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSpacer>(0);
                builder.AddAttribute(1, nameof(BitSpacer.Width), (int?)30);
                builder.AddAttribute(2, nameof(BitSpacer.Element), "li");
                builder.CloseComponent();
            });
        });

        var spacer = component.Find(".bit-spc");

        // Its own width and element stand, while the cascaded height it left alone still reaches it.
        Assert.AreEqual("LI", spacer.TagName);
        StringAssert.Contains(spacer.GetAttribute("style")!, "margin-inline-start:30px");
        StringAssert.Contains(spacer.GetAttribute("style")!, "margin-block-start:20px");
    }

    // Gives every property of the params object a value of its own type, and hands back what each of them was given
    // so that the component can be asked for it afterwards.
    private static Dictionary<string, object> Fill(object cascaded)
    {
        var expected = new Dictionary<string, object>();

        foreach (var property in cascaded.GetType()
                                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.DeclaringType == cascaded.GetType() && p.CanWrite))
        {
            var value = ValueFor(property);

            property.SetValue(cascaded, value);

            expected[property.Name] = value;
        }

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(expected.Count >= 8, $"Only {expected.Count} cascading parameters were found on {nameof(BitSpacerParams)}.");

        return expected;
    }

    private static object ValueFor(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return 3;
        // The element has to be a tag the markup parser accepts, every other string is only read back.
        if (type == typeof(string)) return property.Name == nameof(BitSpacer.Element) ? "section" : "2rem";
        if (type.IsEnum) return Enum.GetValues(type).GetValue(1)!;

        return Activator.CreateInstance(type)!;
    }

    private static void AssertWasRead(object component, Dictionary<string, object> expected)
    {
        var type = component.GetType();

        foreach (var (name, value) in expected)
        {
            Assert.AreEqual(value, type.GetProperty(name)!.GetValue(component), $"{name} was not read from the cascading parameters.");
        }
    }
}
