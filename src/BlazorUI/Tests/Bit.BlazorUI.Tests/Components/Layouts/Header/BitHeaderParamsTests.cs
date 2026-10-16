using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Header;

/// <summary>
/// Covers the BitParams cascade of the header as a contract rather than as a sample: the name the params object is
/// read by, that every parameter an application could set once for all of its headers has a property on it, that
/// every one of those properties is actually copied across by UpdateParameters, and that a value the header wrote for
/// itself is never overwritten by a cascaded one. What the cascaded values render as is covered by the
/// BitHeaderShouldRespectCascadingParams test of BitHeaderTests.
/// </summary>
[TestClass]
public class BitHeaderParamsTests : BunitTestContext
{
    // What is written per header - its two rows of content and the callbacks a page listens to - has no place on
    // the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitHeader.CascadingParameters),
        nameof(BitHeader.ChildContent),
        nameof(BitHeader.ExtensionContent),
        nameof(BitHeader.OnRevealChanged),
        nameof(BitHeader.OnScrolledChanged),
    ];

    [TestMethod]
    public void BitHeaderParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitHeader)}", BitHeaderParams.ParamName);
        Assert.AreEqual("BitParams.BitHeader", BitHeaderParams.ParamName);
    }

    [TestMethod]
    public void BitHeaderParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitHeaderParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitHeaderParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitHeaderParamsShouldCarryEveryParameterThatIsNotPerHeader()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitHeader).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                          .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length > 10, $"Only {parameters.Length} parameters were found on {nameof(BitHeader)}.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitHeaderParams).GetProperty(name), $"BitHeaderParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitHeaderShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitHeaderParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitHeader>(parameters =>
        {
            parameters.AddCascadingValue(BitHeaderParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitHeaderShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitHeaderParams { Height = 64, Size = BitSize.Large, Hidden = true, Bordered = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitHeader>(0);
                builder.AddAttribute(1, nameof(BitHeader.Height), (int?)40);
                builder.AddAttribute(2, nameof(BitHeader.Size), (BitSize?)BitSize.Small);
                builder.AddAttribute(3, nameof(BitHeader.Hidden), false);
                builder.CloseComponent();
            });
        });

        var header = component.Find(".bit-hdr");

        // Its own height, size and Hidden stand, the cascaded ones never overwrite them.
        StringAssert.Contains(header.GetAttribute("style")!, "height:40px");
        Assert.IsTrue(header.ClassList.Contains("bit-hdr-sm"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-lg"));
        Assert.IsFalse(header.ClassList.Contains("bit-hdr-hdn"));
        Assert.IsFalse(header.HasAttribute("inert"));

        // What it left alone still comes from the cascade.
        Assert.IsTrue(header.ClassList.Contains("bit-hdr-brd"));
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
