using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Container;

/// <summary>
/// Covers the BitParams cascade of the container as a contract rather than as a sample: the name the params object is
/// read by, that every parameter of the container but its content has a property on it, that every one of those
/// properties is actually copied across by UpdateParameters, and that a value the container wrote for itself is never
/// overwritten by a cascaded one. What the cascaded values render as is covered by the
/// BitContainerShouldRespectCascadingParams test of BitContainerTests.
/// </summary>
[TestClass]
public class BitContainerParamsTests : BunitTestContext
{
    private static readonly string[] _notCascaded =
    [
        nameof(BitContainer.CascadingParameters),
        nameof(BitContainer.ChildContent),
    ];

    [TestMethod]
    public void BitContainerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitContainer)}", BitContainerParams.ParamName);
        Assert.AreEqual("BitParams.BitContainer", BitContainerParams.ParamName);
    }

    [TestMethod]
    public void BitContainerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitContainerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitContainerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitContainerParamsShouldCarryEveryParameter()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitContainer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                             .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length >= 3, $"Only {parameters.Length} parameters were found on {nameof(BitContainer)}.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitContainerParams).GetProperty(name), $"BitContainerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitContainerShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitContainerParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitContainer>(parameters =>
        {
            parameters.AddCascadingValue(BitContainerParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitContainerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitContainerParams { MaxWidth = "40rem", NoGutter = true, Element = "section" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitContainer>(0);
                builder.AddAttribute(1, nameof(BitContainer.MaxWidth), "30rem");
                builder.AddAttribute(2, nameof(BitContainer.Element), "main");
                builder.CloseComponent();
            });
        });

        var container = component.Find(".bit-cnt");

        // Its own width and element stand, while the cascaded NoGutter it left alone still reaches it.
        Assert.AreEqual("MAIN", container.TagName);
        Assert.AreEqual("--bit-cnt-max-width:30rem", container.GetAttribute("style"));
        Assert.IsTrue(container.ClassList.Contains("bit-cnt-ngt"));
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
        Assert.IsTrue(expected.Count >= 3, $"Only {expected.Count} cascading parameters were found on {nameof(BitContainerParams)}.");

        return expected;
    }

    private static object ValueFor(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(bool)) return true;
        // The element has to be a tag the markup parser accepts, every other string is only read back.
        if (type == typeof(string)) return property.Name == nameof(BitContainer.Element) ? "section" : "2rem";
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
