using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Footer;

/// <summary>
/// Covers the BitParams cascade of the footer as a contract rather than as a sample: the name the params object is
/// read by, that every parameter an application could set once for all of its footers has a property on it, that
/// every one of those properties is actually copied across by UpdateParameters, and that a value the footer wrote for
/// itself is never overwritten by a cascaded one. What the cascaded values render as is covered by the
/// BitFooterShouldRespectCascadingParams tests of BitFooterTests.
/// </summary>
[TestClass]
public class BitFooterParamsTests : BunitTestContext
{
    // What is written per footer - its content and the callbacks a page listens to - has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitFooter.CascadingParameters),
        nameof(BitFooter.ChildContent),
        nameof(BitFooter.OnOverlapChanged),
        nameof(BitFooter.OnRevealChanged),
    ];

    [TestMethod]
    public void BitFooterParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitFooter)}", BitFooterParams.ParamName);
        Assert.AreEqual("BitParams.BitFooter", BitFooterParams.ParamName);
    }

    [TestMethod]
    public void BitFooterParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitFooterParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitFooterParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitFooterParamsShouldCarryEveryParameterThatIsNotPerFooter()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitFooter).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                          .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length > 10, $"Only {parameters.Length} parameters were found on {nameof(BitFooter)}.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitFooterParams).GetProperty(name), $"BitFooterParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitFooterShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitFooterParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitFooter>(parameters =>
        {
            parameters.AddCascadingValue(BitFooterParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitFooterShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitFooterParams { Hidden = true, Height = 56, Elevated = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFooter>(0);
                builder.AddAttribute(1, nameof(BitFooter.Hidden), false);
                builder.AddAttribute(2, nameof(BitFooter.Height), (int?)40);
                builder.CloseComponent();
            });
        });

        var footer = component.Find(".bit-ftr");

        // Hidden written as false by the footer opts it out of the cascaded Hidden, and its own Height stands.
        Assert.IsFalse(footer.ClassList.Contains("bit-ftr-hdn"));
        Assert.IsFalse(footer.HasAttribute("inert"));
        StringAssert.Contains(footer.GetAttribute("style")!, "height:40px");

        // What it left alone still comes from the cascade.
        Assert.IsTrue(footer.ClassList.Contains("bit-ftr-elv"));
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

        return expected;
    }

    private static object ValueFor(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return 3;
        if (type == typeof(string)) return "2rem";
        if (type.IsEnum) return Enum.GetValues(type).GetValue(1)!;

        return Activator.CreateInstance(type)!;
    }

    private static void AssertWasRead(object component, Dictionary<string, object> expected)
    {
        var type = component.GetType();

        Assert.IsTrue(expected.Count > 10, $"Only {expected.Count} cascading parameters were found on {type.Name}.");

        foreach (var (name, value) in expected)
        {
            Assert.AreEqual(value, type.GetProperty(name)!.GetValue(component), $"{name} was not read from the cascading parameters.");
        }
    }
}
