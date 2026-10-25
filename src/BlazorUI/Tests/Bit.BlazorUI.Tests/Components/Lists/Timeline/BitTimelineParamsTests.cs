using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Timeline;

/// <summary>
/// Covers the BitParams cascade of the timeline as a contract rather than as a sample: the name the params object is
/// read by (one name for a timeline of any item type), that every parameter an application could set once for all of
/// its timelines has a property on it, that every one of those properties is actually copied across by
/// UpdateParameters, and that a value the timeline wrote for itself is never overwritten by a cascaded one. What the
/// cascaded values render as is covered by the BitTimelineShouldRespectCascadingParams tests of BitTimelineTests.
/// </summary>
[TestClass]
public class BitTimelineParamsTests : BunitTestContext
{
    // What a timeline is about rather than how it looks - its items, how they are read, drawn and clicked - is written
    // per timeline, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTimeline<BitTimelineItem>.CascadingParameters),
        nameof(BitTimeline<BitTimelineItem>.ChildContent),
        nameof(BitTimeline<BitTimelineItem>.DotTemplate),
        nameof(BitTimeline<BitTimelineItem>.Items),
        nameof(BitTimeline<BitTimelineItem>.ItemTemplate),
        nameof(BitTimeline<BitTimelineItem>.NameSelectors),
        nameof(BitTimeline<BitTimelineItem>.OnItemClick),
        nameof(BitTimeline<BitTimelineItem>.Options),
    ];

    [TestMethod]
    public void BitTimelineParamsShouldHaveCorrectParamName()
    {
        // The params object is not generic, so its name is the bare component name, whatever the timeline is generic over.
        Assert.AreEqual("BitParams.BitTimeline", BitTimelineParams.ParamName);
    }

    [TestMethod]
    public void BitTimelineParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitTimelineParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitTimelineParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitTimelineParamsShouldCarryEveryParameterThatIsNotPerTimeline()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitTimeline<BitTimelineItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                             .Select(p => p.Name)
                                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                             .ToArray();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsTrue(parameters.Length > 10, $"Only {parameters.Length} parameters were found on BitTimeline.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitTimelineParams).GetProperty(name), $"BitTimelineParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitTimelineShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the property is only half of it: one that is never copied across in UpdateParameters is a value an
        // application can set and watch do nothing at all.
        var cascaded = new BitTimelineParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitTimeline<BitTimelineItem>>(parameters =>
        {
            parameters.AddCascadingValue(BitTimelineParams.ParamName, cascaded);
            parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitTimelineShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitTimelineParams { Size = BitSize.Large, Variant = BitVariant.Outline, Horizontal = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimeline<BitTimelineItem>>(0);
                builder.AddAttribute(1, nameof(BitTimeline<BitTimelineItem>.Size), (BitSize?)BitSize.Small);
                builder.AddAttribute(2, nameof(BitTimeline<BitTimelineItem>.Variant), (BitVariant?)BitVariant.Fill);
                builder.AddAttribute(3, nameof(BitTimeline<BitTimelineItem>.Items), new[] { new BitTimelineItem { PrimaryText = "One" } });
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-tln");

        // Its own size and variant stand, while the cascaded orientation it left alone still reaches it.
        Assert.IsTrue(root.ClassList.Contains("bit-tln-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-tln-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-tln-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tln-hrz"));
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
