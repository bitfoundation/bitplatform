using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Stack;

/// <summary>
/// Covers the BitParams cascade of the stack as a contract rather than as a sample: the name the params object is
/// read by, that every parameter an application could set once for all of its stacks has a property on it, that
/// every one of those properties is actually copied across by UpdateParameters, and that a value the stack wrote for
/// itself is never overwritten by a cascaded one. What the cascaded values render as is covered by the
/// BitStackShouldRespectCascadingParams test of BitStackTests.
/// </summary>
[TestClass]
public class BitStackParamsTests : BunitTestContext
{
    // The content is what is written per stack, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitStack.CascadingParameters),
        nameof(BitStack.ChildContent),
    ];

    [TestMethod]
    public void BitStackParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitStack)}", BitStackParams.ParamName);
        Assert.AreEqual("BitParams.BitStack", BitStackParams.ParamName);
    }

    [TestMethod]
    public void BitStackParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitStackParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitStackParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitStackParamsShouldCarryEveryParameterThatIsNotPerStack()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitStack).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                         .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length > 10, $"Only {parameters.Length} parameters were found on {nameof(BitStack)}.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitStackParams).GetProperty(name), $"BitStackParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitStackShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitStackParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.AddCascadingValue(BitStackParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitStackShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitStackParams { Element = "section", Size = BitSize.Large, Padding = "1rem" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitStack>(0);
                builder.AddAttribute(1, nameof(BitStack.Element), "nav");
                builder.AddAttribute(2, nameof(BitStack.Size), (BitSize?)BitSize.Small);
                builder.CloseComponent();
            });
        });

        var stack = component.Find(".bit-stc");

        // Its own element and size stand, while the cascaded padding it left alone still reaches it.
        Assert.AreEqual("NAV", stack.TagName);
        Assert.IsTrue(stack.ClassList.Contains("bit-stc-sm"));
        Assert.IsFalse(stack.ClassList.Contains("bit-stc-lg"));
        StringAssert.Contains(stack.GetAttribute("style")!, "padding:1rem");
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
        // The element has to be a tag the markup parser accepts, every other string is only read back.
        if (type == typeof(string)) return property.Name == nameof(BitStack.Element) ? "section" : "2rem";
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
