using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Layout;

/// <summary>
/// Covers the BitParams cascade of the layout as a contract rather than as a sample: the name the params object is
/// read by, that every parameter an application could set once for all of its layouts has a property on it, that
/// every one of those properties is actually copied across by UpdateParameters, and that a value the layout wrote for
/// itself is never overwritten by a cascaded one. What the cascaded values render as is covered by the
/// BitLayoutShouldRespectCascadingParams test of BitLayoutTests.
/// </summary>
[TestClass]
public class BitLayoutParamsTests : BunitTestContext
{
    // The five sections are what a layout is made of, written per layout, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitLayout.CascadingParameters),
        nameof(BitLayout.Aside),
        nameof(BitLayout.Footer),
        nameof(BitLayout.Header),
        nameof(BitLayout.Main),
        nameof(BitLayout.NavPanel),
    ];

    [TestMethod]
    public void BitLayoutParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitLayout)}", BitLayoutParams.ParamName);
        Assert.AreEqual("BitParams.BitLayout", BitLayoutParams.ParamName);
    }

    [TestMethod]
    public void BitLayoutParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitLayoutParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitLayoutParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitLayoutParamsShouldCarryEveryParameterThatIsNotPerLayout()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitLayout).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                          .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length > 10, $"Only {parameters.Length} parameters were found on {nameof(BitLayout)}.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitLayoutParams).GetProperty(name), $"BitLayoutParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitLayoutShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitLayoutParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.AddCascadingValue(BitLayoutParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitLayoutShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitLayoutParams { HideHeader = true, HideFooter = true, Gap = "1rem" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitLayout>(0);
                builder.AddAttribute(1, nameof(BitLayout.HideHeader), false);
                builder.AddAttribute(2, nameof(BitLayout.Header), (RenderFragment)(b => b.AddContent(0, "Header")));
                builder.AddAttribute(3, nameof(BitLayout.Footer), (RenderFragment)(b => b.AddContent(0, "Footer")));
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-lyt");

        // HideHeader written as false by the layout opts it out of the cascaded one, so its header is rendered, while
        // the cascaded HideFooter it left alone still takes the footer out.
        Assert.AreEqual(1, component.FindAll(".bit-lyt-hdr").Count);
        Assert.IsFalse(root.ClassList.Contains("bit-lyt-nhd"));
        Assert.AreEqual(0, component.FindAll(".bit-lyt-ftr").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-lyt-nft"));
        StringAssert.Contains(root.GetAttribute("style")!, "--bit-lyt-gap:1rem");
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
