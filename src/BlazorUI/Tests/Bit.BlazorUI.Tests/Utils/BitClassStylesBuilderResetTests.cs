using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins that a component builds its root class and style strings again when it is handed another Classes or Styles.
/// Their Root is registered in the class and style builders of the root, which are only built again once reset, so a
/// new ClassStyles that did not reset them would leave the root with the one it was first rendered with.
/// </summary>
[TestClass]
public class BitClassStylesBuilderResetTests : BunitTestContext
{
    // Compares what its builders read off Classes and Styles by value in OnParametersSet instead, so that a ClassStyles
    // changed in place through BitModalService.Refresh counts too.
    private static readonly HashSet<string> _comparedByValue = ["BitModal"];

    [TestInitialize]
    public void Init()
    {
        Services.AddScoped(_ => new BitPageVisibility(new TestJsRuntime()));
    }

    [TestMethod]
    public void EveryClassesAndStylesParameterShouldResetItsBuilder()
    {
        var assemblies = new[] { typeof(BitButton).Assembly, typeof(BitAppShell).Assembly, typeof(Bit.BlazorUI.Legacy.BitRichTextEditorLegacy).Assembly };
        var failures = new List<string>();
        var tested = 0;

        foreach (var type in assemblies.SelectMany(LoadableTypes))
        {
            if (type.IsAbstract || typeof(BitComponentBase).IsAssignableFrom(type) is false) continue;
            if (_comparedByValue.Contains(type.Name)) continue;

            foreach (var (name, attribute) in new[] { ("Classes", "ResetClassBuilderAttribute"), ("Styles", "ResetStyleBuilderAttribute") })
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property is null || property.IsDefined(typeof(ParameterAttribute)) is false) continue;
                if (property.PropertyType.Name.EndsWith("ClassStyles", StringComparison.Ordinal) is false) continue;

                tested++;

                if (property.GetCustomAttributes().Any(a => a.GetType().Name == attribute)) continue;

                failures.Add($"{type.Name}.{name} has no [{attribute.Replace("Attribute", "")}].");
            }
        }

        // Guards the discovery itself: a reflection change that found nothing would otherwise pass vacuously.
        Assert.IsTrue(tested > 100, $"Only {tested} Classes/Styles parameters were discovered.");
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    // A type whose base sits in an assembly the tests do not ship (Newtonsoft.Json, behind a chart type) cannot be
    // loaded, and is not a component anyway.
    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    [TestMethod]
    public void ANewClassStylesShouldReachTheRoot()
    {
        var component = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = "first" });
            parameters.Add(p => p.Styles, new() { Root = "color:red" });
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = "second" });
            parameters.Add(p => p.Styles, new() { Root = "color:blue" });
        });

        var button = component.Find("button");

        Assert.IsTrue(button.ClassList.Contains("second"));
        Assert.IsFalse(button.ClassList.Contains("first"));
        Assert.AreEqual("color:blue", button.GetAttribute("style"));
    }

    [TestMethod]
    public void ANewClassStylesShouldReachTheRootOfAnInput()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = "first" });
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = "second" });
        });

        var root = component.Find(".bit-tfl");

        Assert.IsTrue(root.ClassList.Contains("second"));
        Assert.IsFalse(root.ClassList.Contains("first"));
    }

    [TestMethod]
    public void ANewClassStylesShouldReachTheCarouselItems()
    {
        RenderFragment items = builder =>
        {
            builder.OpenComponent<BitCarouselItem>(0);
            builder.CloseComponent();
            builder.OpenComponent<BitCarouselItem>(1);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Item = "first" });
            parameters.Add(p => p.Styles, new() { Item = "color:red" });
            parameters.Add(p => p.ChildContent, items);
        });

        // The items are rendered under a fixed cascade and have no parameters that change, so nothing but the
        // carousel can tell them.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Item = "second" });
            parameters.Add(p => p.Styles, new() { Item = "color:blue" });
            parameters.Add(p => p.ChildContent, items);
        });

        foreach (var item in component.FindAll(".bit-crsi"))
        {
            Assert.IsTrue(item.ClassList.Contains("second"));
            Assert.IsFalse(item.ClassList.Contains("first"));
            Assert.Contains("color:blue", item.GetAttribute("style") ?? "");
        }
    }

    [TestMethod]
    public void ANewClassStylesShouldReachTheSwiperItems()
    {
        RenderFragment items = builder =>
        {
            builder.OpenComponent<BitSwiperItem>(0);
            builder.CloseComponent();
        };

        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Item = "first" });
            parameters.Add(p => p.ChildContent, items);
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Item = "second" });
            parameters.Add(p => p.ChildContent, items);
        });

        var item = component.Find(".bit-swpi");

        Assert.IsTrue(item.ClassList.Contains("second"));
        Assert.IsFalse(item.ClassList.Contains("first"));
    }

    [TestMethod]
    public void AClassStylesChangedInPlaceShouldReachThePivotItems()
    {
        var classes = new BitPivotClassStyles { HeaderItem = "first", SelectedItem = "first-selected" };

        RenderFragment items = builder =>
        {
            builder.OpenComponent<BitPivotItem>(0);
            builder.AddComponentParameter(1, nameof(BitPivotItem.HeaderText), "One");
            builder.CloseComponent();
            builder.OpenComponent<BitPivotItem>(2);
            builder.AddComponentParameter(3, nameof(BitPivotItem.HeaderText), "Two");
            builder.CloseComponent();
        };

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.ChildContent, items);
        });

        // The very same ClassStyles, changed in place: what the tabs read off it is compared by value.
        classes.HeaderItem = "second";
        classes.SelectedItem = "second-selected";
        component.Render(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.ChildContent, items);
        });

        var tabs = component.FindAll(".bit-pvti");

        Assert.AreEqual(2, tabs.Count);
        Assert.IsTrue(tabs.All(t => t.ClassList.Contains("second") && t.ClassList.Contains("first") is false));
        Assert.IsTrue(tabs[0].ClassList.Contains("second-selected"));
    }
}
