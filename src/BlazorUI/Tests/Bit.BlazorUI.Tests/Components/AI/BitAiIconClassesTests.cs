using System;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI;

/// <summary>
/// Pins the one helper the AI components put the glyphs of their icon font on a core component's icon with
/// (BitAiIconClasses): it keeps every class the app gave that component, puts the glyph ahead of the app's own classes
/// of the icon, and hands the component the same instance until the app's classes or the glyph change.
/// </summary>
[TestClass]
public class BitAiIconClassesTests : BitAiTestBase
{
    private const string Glyph = "bit-icon-ai bit-icon-ai--Test";

    [TestMethod]
    public void EveryMergeShouldKeepEveryClassTheAppGave()
    {
        // Internal to the package, so read through reflection: a class the core adds to one of these ClassStyles and the
        // helper does not copy would silently drop what an app gave it.
        var helper = typeof(BitAiChat).Assembly.GetType("Bit.BlazorUI.BitAiIconClasses", throwOnError: true)!;
        var merges = helper.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "With").ToArray();

        Assert.IsGreaterThanOrEqualTo(4, merges.Length);

        foreach (var merge in merges)
        {
            var type = merge.GetParameters()[1].ParameterType;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(string)).ToArray();

            Assert.IsTrue(properties.Any(p => p.Name == "Icon"), $"{type.Name} has no Icon to put the glyph on.");

            var classes = Activator.CreateInstance(type)!;
            foreach (var property in properties) property.SetValue(classes, $"app-{property.Name}");

            var merged = merge.Invoke(null, [Glyph, classes])!;
            var empty = merge.Invoke(null, [Glyph, null])!;

            foreach (var property in properties)
            {
                Assert.AreEqual(property.Name == "Icon" ? $"{Glyph} app-Icon" : $"app-{property.Name}", property.GetValue(merged), $"{type.Name}.{property.Name}");
                Assert.AreEqual(property.Name == "Icon" ? Glyph : null, property.GetValue(empty), $"{type.Name}.{property.Name} without classes of the app");
            }
        }
    }

    [TestMethod]
    public void TheInnerButtonShouldKeepItsClassesUntilTheAppHandsOverOthers()
    {
        var classes = new BitAiScrollToLatestClassStyles { Button = new() { Icon = "app-icon" } };

        var component = RenderComponent<BitAiScrollToLatest>(p => p.Add(c => c.Visible, true).Add(c => c.Classes, classes));
        var first = component.FindComponent<BitButton>().Instance.Classes;

        Assert.AreEqual("bit-icon-ai bit-icon-ai--Down app-icon", first?.Icon);

        component.Render(p => p.Add(c => c.Visible, true).Add(c => c.Classes, classes));

        Assert.AreSame(first, component.FindComponent<BitButton>().Instance.Classes);

        component.Render(p => p.Add(c => c.Visible, true).Add(c => c.Classes, new BitAiScrollToLatestClassStyles { Button = new() { Icon = "other-icon" } }));

        var second = component.FindComponent<BitButton>().Instance.Classes;

        Assert.AreNotSame(first, second);
        Assert.AreEqual("bit-icon-ai bit-icon-ai--Down other-icon", second?.Icon);
    }

    [TestMethod]
    public void AButtonWhoseGlyphChangesShouldBeHandedTheClassesOfTheNewOne()
    {
        var message = Assistant(new BitAiTextPart("answer"));

        var component = RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, message));

        var like = component.FindComponents<BitButton>().Single(b => b.Instance.IconName is "Like");
        Assert.AreEqual("bit-icon-ai bit-icon-ai--Like", like.Instance.Classes?.Icon);

        like.Find("button").Click();

        var liked = component.FindComponents<BitButton>().Single(b => b.Instance.IconName is "LikeSolid");
        Assert.AreEqual("bit-icon-ai bit-icon-ai--LikeSolid", liked.Instance.Classes?.Icon);
    }
}
