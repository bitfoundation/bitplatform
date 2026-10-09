using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// Pins how every params object the libraries ship cascades an icon. A component takes one icon through an XIcon,
/// an XIconName and - for some - an IconUrl, and the first of them set wins, so they are one setting: a params object
/// supplies none of them to a component that set any one. A cascaded XIcon filled in beside the component's own
/// XIconName would otherwise replace the icon the component asked for. The icons are found by reflection, so one a
/// params object gains later is covered without a word here.
/// </summary>
[TestClass]
public class BitIconCascadeTests : BunitTestContext
{
    private static readonly Assembly[] Libraries = [typeof(BitButton).Assembly, typeof(BitMessageBox).Assembly];

    // What a generic component is closed over to render it; none is given items, so any type its constraints accept
    // will do.
    private static readonly Dictionary<Type, Type[]> TypeArguments = new()
    {
        [typeof(BitAccordionList<>)] = [typeof(BitAccordionListItem)],
        [typeof(BitBreadcrumb<>)] = [typeof(BitBreadcrumbItem)],
        [typeof(BitDropdown<,>)] = [typeof(BitDropdownItem<string>), typeof(string)],
        [typeof(BitMenuButton<>)] = [typeof(BitMenuButtonItem)],
        [typeof(BitNav<>)] = [typeof(BitNavItem)],
        [typeof(BitNavPanel<>)] = [typeof(BitNavItem)],
        [typeof(BitNumberField<>)] = [typeof(int)],
    };



    [TestInitialize]
    public void Init()
    {
        // The carousel and the swiper pause while the page is hidden, which they ask this service about.
        Services.AddScoped(_ => new BitPageVisibility(new TestJsRuntime()));
    }

    public static IEnumerable<object[]> Icons()
    {
        foreach (var (component, cascade) in Readers())
        {
            foreach (var icon in cascade.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (icon.PropertyType != typeof(BitIconInfo)) continue;

                if (cascade.PropertyType.GetProperty(icon.Name + "Name")?.PropertyType != typeof(string)) continue;

                yield return [component.Name, icon.Name];
            }
        }
    }

    [TestMethod]
    public void TheIconsShouldBeFound()
    {
        // A guard on the discovery itself, which would otherwise pass every test below by finding nothing.
        var icons = Icons().ToArray();

        Assert.IsGreaterThan(150, icons.Length);
        Assert.IsTrue(icons.Any(i => (string)i[0] == nameof(BitButton) && (string)i[1] == nameof(BitButton.Icon)));
        Assert.IsTrue(icons.Any(i => (string)i[0] == nameof(BitErrorBoundary) && (string)i[1] == nameof(BitErrorBoundary.Icon)));
    }

    [TestMethod]
    public void EveryGenericComponentThatCascadesAnIconShouldBeClosed()
    {
        var unclosed = Libraries.SelectMany(a => a.GetTypes())
                                .Where(t => t.IsGenericTypeDefinition && t.IsAbstract is false && typeof(IComponent).IsAssignableFrom(t))
                                .Where(t => TypeArguments.ContainsKey(t) is false)
                                .Where(t => t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                             .Any(p => p.IsDefined(typeof(CascadingParameterAttribute))
                                                    && typeof(IBitComponentParams).IsAssignableFrom(p.PropertyType)
                                                    && p.PropertyType.GetProperties().Any(i => i.PropertyType == typeof(BitIconInfo))))
                                .Select(t => t.Name)
                                .ToArray();

        Assert.AreEqual(0, unclosed.Length, $"Add these to TypeArguments: {string.Join(", ", unclosed)}");
    }

    [TestMethod]
    [DynamicData(nameof(Icons))]
    public void AnIconTheComponentPickedShouldNotBeJoinedByACascadedOne(string componentName, string iconName)
    {
        var (component, cascade) = Find(componentName);
        var members = Members(component, cascade, iconName);

        foreach (var own in members)
        {
            var @params = Supply(cascade, members.Where(m => m != own));

            var instance = Render(component, @params, [own]);

            foreach (var member in members)
            {
                var expected = member == own ? Value(member, "own") : Default(component, member);

                Assert.AreEqual(expected, member.GetValue(instance),
                                $"{component.Name}.{member.Name}, with {own.Name} set on the component and the rest cascaded.");
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(Icons))]
    public void AnIconShouldBeCascadedToAComponentThatPickedNone(string componentName, string iconName)
    {
        var (component, cascade) = Find(componentName);

        foreach (var member in Members(component, cascade, iconName))
        {
            var instance = Render(component, Supply(cascade, [member]), []);

            Assert.AreEqual(Value(member, "cascaded"), member.GetValue(instance), $"{component.Name}.{member.Name}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Icons))]
    public void ACascadedIconShouldBeTakenBackOnceTheComponentPicksItsOwn(string componentName, string iconName)
    {
        var (component, cascade) = Find(componentName);
        var members = Members(component, cascade, iconName);
        var icon = members[0];

        foreach (var own in members.Skip(1))
        {
            var @params = Supply(cascade, [icon]);

            object? instance = null;

            var host = RenderComponent<BitParams>(parameters =>
            {
                parameters.Add(p => p.Parameters, [@params]);
                parameters.AddChildContent(Fragment(component, [], i => instance = i));
            });

            Assert.AreEqual(Value(icon, "cascaded"), icon.GetValue(instance), $"{component.Name}.{icon.Name}, before {own.Name} is set.");

            host.Render(parameters => parameters.AddChildContent(Fragment(component, [own], i => instance = i)));

            Assert.AreEqual(Default(component, icon), icon.GetValue(instance), $"{component.Name}.{icon.Name}, once {own.Name} is set.");
            Assert.AreEqual(Value(own, "own"), own.GetValue(instance), $"{component.Name}.{own.Name}");
        }
    }



    // The parameters of the component that make up the icon: the BitIconInfo first, then its name, then the url of
    // the picture shown when neither is set, which the default icon of a component has too.
    private static PropertyInfo[] Members(Type component, PropertyInfo cascade, string iconName)
    {
        var names = new List<string> { iconName, iconName + "Name" };

        if (iconName == "Icon" && cascade.PropertyType.GetProperty("IconUrl") is not null)
        {
            names.Add("IconUrl");
        }

        return names.Select(n => Parameter(component, n)).ToArray();
    }

    private object Render(Type component, IBitComponentParams @params, PropertyInfo[] own)
    {
        object? instance = null;

        RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [@params]);
            parameters.AddChildContent(Fragment(component, own, i => instance = i));
        });

        return instance ?? throw new InvalidOperationException($"{component.Name} was not rendered.");
    }

    private static RenderFragment Fragment(Type component, PropertyInfo[] own, Action<object> capture)
    {
        return builder =>
        {
            builder.OpenComponent(0, component);

            foreach (var member in own)
            {
                // Boxed, since BitIconInfo converts to a string and would otherwise pick that overload.
                builder.AddAttribute(1, member.Name, Value(member, "own"));
            }

            builder.AddComponentReferenceCapture(2, capture);
            builder.CloseComponent();
        };
    }

    private static IBitComponentParams Supply(PropertyInfo cascade, IEnumerable<PropertyInfo> members)
    {
        var @params = Activator.CreateInstance(cascade.PropertyType)!;

        foreach (var member in members)
        {
            cascade.PropertyType.GetProperty(member.Name)!.SetValue(@params, Value(member, "cascaded"));
        }

        return (IBitComponentParams)@params;
    }

    // A value per member and per side, so what a component ends up holding names where it came from. BitIconInfo has
    // no value equality, so the same instance is handed out every time.
    private static readonly Dictionary<string, BitIconInfo> IconValues = new()
    {
        ["own"] = BitIconInfo.Css("own-icon"),
        ["cascaded"] = BitIconInfo.Css("cascaded-icon"),
    };

    private static object Value(PropertyInfo member, string side)
    {
        if (member.PropertyType == typeof(BitIconInfo)) return IconValues[side];

        return member.Name.EndsWith("Url") ? $"{side}.png" : $"{side}-icon-name";
    }

    private static object? Default(Type component, PropertyInfo member) => member.GetValue(Activator.CreateInstance(component));

    private static PropertyInfo Parameter(Type component, string name)
    {
        return component.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.Name == name && p.IsDefined(typeof(ParameterAttribute)))
                        .OrderByDescending(p => Depth(p.DeclaringType))
                        .First();
    }

    private static (Type Component, PropertyInfo Cascade) Find(string componentName)
    {
        return Readers().Single(r => r.Component.Name == componentName);
    }

    private static IEnumerable<(Type Component, PropertyInfo Cascade)> Readers()
    {
        foreach (var type in Libraries.SelectMany(a => a.GetTypes()))
        {
            if (type.IsClass is false || type.IsAbstract || typeof(IComponent).IsAssignableFrom(type) is false) continue;

            var component = type;

            if (type.IsGenericTypeDefinition)
            {
                if (TypeArguments.TryGetValue(type, out var arguments) is false) continue;

                component = type.MakeGenericType(arguments);
            }

            var cascade = component.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                   .FirstOrDefault(p => p.IsDefined(typeof(CascadingParameterAttribute))
                                                     && typeof(IBitComponentParams).IsAssignableFrom(p.PropertyType));

            if (cascade is not null) yield return (component, cascade);
        }
    }

    private static int Depth(Type? type)
    {
        var depth = 0;

        for (; type is not null; type = type.BaseType) depth++;

        return depth;
    }
}
