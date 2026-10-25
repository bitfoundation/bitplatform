using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.EventHandlers;

/// <summary>
/// Pins the CSS animation and transition events Bit.BlazorUI.Events.EventHandlers registers: the directives hand
/// their handlers the arguments, the events bubble unless stopped, and the names and the fields the registrations
/// declare are the ones the library's script (Events.ts) fills in on the browser's side.
/// </summary>
[TestClass]
public class BitEventHandlersTests : BunitTestContext
{
    private static string ReadScript() => SourceFiles.Read("Bit.BlazorUI", "Scripts", "Events.ts");

    [TestMethod]
    public void BitEventHandlersShouldHandEveryFieldOfTheArgumentsToTheHandler()
    {
        BitAnimationEventArgs? animation = null;
        BitTransitionEventArgs? transition = null;

        var component = Context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "id", "target");
            builder.AddAttribute(2, "onanimationend", EventCallback.Factory.Create<BitAnimationEventArgs>(this, e => animation = e));
            builder.AddAttribute(3, "ontransitionend", EventCallback.Factory.Create<BitTransitionEventArgs>(this, e => transition = e));
            builder.CloseElement();
        });

        component.Find("#target").TriggerEvent("onanimationend", new BitAnimationEventArgs { AnimationName = "bit-x", ElapsedTime = 1.5, PseudoElement = "::before", TargetId = "target" });
        component.Find("#target").TriggerEvent("ontransitionend", new BitTransitionEventArgs { PropertyName = "opacity", ElapsedTime = 0.25, PseudoElement = "::after", TargetId = "target" });

        Assert.IsNotNull(animation);
        Assert.AreEqual("bit-x", animation.AnimationName);
        Assert.AreEqual(1.5, animation.ElapsedTime);
        Assert.AreEqual("::before", animation.PseudoElement);
        Assert.AreEqual("target", animation.TargetId);

        Assert.IsNotNull(transition);
        Assert.AreEqual("opacity", transition.PropertyName);
        Assert.AreEqual(0.25, transition.ElapsedTime);
        Assert.AreEqual("::after", transition.PseudoElement);
        Assert.AreEqual("target", transition.TargetId);
    }

    [TestMethod]
    public void TheArgumentsShouldStartEmptyRatherThanNull()
    {
        // An event that fires before the script has registered its arguments reaches the handler with the fields
        // empty, which a handler comparing names has to be able to read without a null check.
        var animation = new BitAnimationEventArgs();
        var transition = new BitTransitionEventArgs();

        Assert.AreEqual(string.Empty, animation.AnimationName);
        Assert.AreEqual(string.Empty, animation.PseudoElement);
        Assert.AreEqual(string.Empty, animation.TargetId);
        Assert.AreEqual(0, animation.ElapsedTime);
        Assert.AreEqual(string.Empty, transition.PropertyName);
        Assert.AreEqual(string.Empty, transition.PseudoElement);
        Assert.AreEqual(string.Empty, transition.TargetId);
        Assert.AreEqual(0, transition.ElapsedTime);
    }

    [TestMethod]
    public void TheScriptShouldRegisterTheSameEventsTheDirectivesAreDeclaredFor()
    {
        // The directives are declared with the "on" prefix a razor file writes; the script registers the browser's
        // own names. A name on one side alone is a directive that never gets its arguments, or arguments nothing reads.
        var declared = typeof(Bit.BlazorUI.Events.EventHandlers).GetCustomAttributes<EventHandlerAttribute>()
                                                                  .Select(a => a.AttributeName[2..])
                                                                  .ToArray();

        var scripted = Regex.Matches(ReadScript(), @"_(?:animation|transition)Events\s*=\s*\[(?<names>[^\]]*)\]")
                            .SelectMany(m => Regex.Matches(m.Groups["names"].Value, @"'(?<name>\w+)'").Select(n => n.Groups["name"].Value))
                            .ToArray();

        CollectionAssert.AreEquivalent(declared, scripted);
    }

    [TestMethod,
        DataRow("animationArgs", typeof(BitAnimationEventArgs)),
        DataRow("transitionArgs", typeof(BitTransitionEventArgs))
    ]
    public void TheScriptShouldFillEveryPropertyOfTheArgumentsAndNothingElse(string factory, Type argsType)
    {
        var script = ReadScript();

        var start = script.IndexOf($"const {factory} = ", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{factory} is gone from Events.ts; this contract needs to follow it.");

        var end = script.IndexOf("});", start, StringComparison.Ordinal);
        var body = script[start..end];

        // System.Text.Json binds the camel-cased fields to the properties, so a field named otherwise is dropped and
        // a property the script does not fill stays at its default.
        var filled = Regex.Matches(body, @"^\s*(?<field>\w+):", RegexOptions.Multiline).Select(m => m.Groups["field"].Value).ToArray();
        var expected = argsType.GetProperties().Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..]).ToArray();

        CollectionAssert.AreEquivalent(expected, filled);
    }

    [TestMethod]
    public void TheArgumentsShouldBeKeptFromTrimmingByEveryComponent()
    {
        // The fields are set by System.Text.Json through setters nothing else calls, so a trimmed app would otherwise
        // hand every handler empty arguments. The dependency is declared on what every app renders at least one of.
        var dependencies = typeof(BitComponentBase).GetMethod(nameof(BitComponentBase.SetParametersAsync))!
                                                   .GetCustomAttributes<DynamicDependencyAttribute>()
                                                   .ToDictionary(d => d.Type!, d => d.MemberTypes);

        foreach (var type in new[] { typeof(BitAnimationEventArgs), typeof(BitTransitionEventArgs) })
        {
            Assert.IsTrue(dependencies.TryGetValue(type, out var members), $"{type.Name} is not kept from trimming.");
            Assert.IsTrue(members.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties), $"The properties of {type.Name} are not kept from trimming.");
            Assert.IsTrue(members.HasFlag(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor), $"The constructor of {type.Name} is not kept from trimming.");
        }
    }

    [TestMethod]
    [DataRow("onanimationstart", "animated animationstart bit-x from animated")]
    [DataRow("onanimationend", "animated animationend bit-x from animated")]
    [DataRow("onanimationiteration", "animated animationiteration bit-x from animated")]
    [DataRow("onanimationcancel", "animated animationcancel bit-x from animated")]
    public void BitEventHandlersShouldHandTheAnimationEventsTheirArguments(string eventName, string expected)
    {
        var component = RenderComponent<BitEventHandlersTest>();

        component.Find("#animated").TriggerEvent(eventName, new BitAnimationEventArgs { AnimationName = "bit-x", TargetId = "animated" });

        Assert.AreEqual(expected, component.Instance.Received.First());
    }

    [TestMethod]
    [DataRow("ontransitionrun", "transitioned transitionrun opacity from transitioned")]
    [DataRow("ontransitionstart", "transitioned transitionstart opacity from transitioned")]
    [DataRow("ontransitionend", "transitioned transitionend opacity from transitioned")]
    [DataRow("ontransitioncancel", "transitioned transitioncancel opacity from transitioned")]
    public void BitEventHandlersShouldHandTheTransitionEventsTheirArguments(string eventName, string expected)
    {
        var component = RenderComponent<BitEventHandlersTest>();

        component.Find("#transitioned").TriggerEvent(eventName, new BitTransitionEventArgs { PropertyName = "opacity", TargetId = "transitioned" });

        Assert.AreEqual(expected, component.Instance.Received.First());
    }

    [TestMethod]
    public void BitEventHandlersShouldLetTheEndsBubbleUnlessStopped()
    {
        var component = RenderComponent<BitEventHandlersTest>();

        component.Find("#animated").TriggerEvent("onanimationend", new BitAnimationEventArgs { AnimationName = "bit-x", TargetId = "animated" });
        component.Find("#transitioned").TriggerEvent("ontransitionend", new BitTransitionEventArgs { PropertyName = "opacity", TargetId = "transitioned" });

        // The ancestor hears its content's ends, and the target id is what tells them from its own.
        CollectionAssert.AreEqual(new[]
        {
            "animated animationend bit-x from animated",
            "outer animationend bit-x from animated",
            "transitioned transitionend opacity from transitioned",
            "outer transitionend opacity from transitioned",
        }, component.Instance.Received);

        component.Render(parameters => parameters.Add(p => p.StopPropagation, true));
        component.Instance.Received.Clear();

        component.Find("#animated").TriggerEvent("onanimationend", new BitAnimationEventArgs { AnimationName = "bit-x", TargetId = "animated" });
        component.Find("#transitioned").TriggerEvent("ontransitionend", new BitTransitionEventArgs { PropertyName = "opacity", TargetId = "transitioned" });

        CollectionAssert.AreEqual(new[]
        {
            "animated animationend bit-x from animated",
            "transitioned transitionend opacity from transitioned",
        }, component.Instance.Received);
    }

    [TestMethod]
    public void BitEventHandlersShouldRegisterEveryAnimationAndTransitionEvent()
    {
        var registrations = typeof(Bit.BlazorUI.Events.EventHandlers).GetCustomAttributes<EventHandlerAttribute>().ToDictionary(a => a.AttributeName);

        var expected = new[]
        {
            ("onanimationstart", typeof(BitAnimationEventArgs)),
            ("onanimationend", typeof(BitAnimationEventArgs)),
            ("onanimationiteration", typeof(BitAnimationEventArgs)),
            ("onanimationcancel", typeof(BitAnimationEventArgs)),
            ("ontransitionrun", typeof(BitTransitionEventArgs)),
            ("ontransitionstart", typeof(BitTransitionEventArgs)),
            ("ontransitionend", typeof(BitTransitionEventArgs)),
            ("ontransitioncancel", typeof(BitTransitionEventArgs)),
        };

        Assert.AreEqual(expected.Length, registrations.Count);

        foreach (var (name, argsType) in expected)
        {
            Assert.IsTrue(registrations.TryGetValue(name, out var registration), name);
            Assert.AreEqual(argsType, registration.EventArgsType, name);

            // The events bubble, so they can be stopped. None of them can be canceled, but the Razor compiler only
            // binds :stopPropagation for a registration that enables :preventDefault as well.
            Assert.IsTrue(registration.EnableStopPropagation, name);
            Assert.IsTrue(registration.EnablePreventDefault, name);
        }
    }
}
