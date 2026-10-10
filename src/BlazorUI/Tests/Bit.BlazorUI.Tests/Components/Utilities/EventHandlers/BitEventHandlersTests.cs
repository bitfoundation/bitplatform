using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.EventHandlers;

[TestClass]
public class BitEventHandlersTests : BunitTestContext
{
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
