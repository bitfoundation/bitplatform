using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// Pins how every params object the libraries ship cascades an icon. A component takes one icon through several
/// parameters - an XIcon, its XIconName, its XIconUrl, an XIconTemplate drawn in its place, the XIcons / XIconNames
/// maps, a GetXIcon selector - and the first of them set wins, so they are one setting: a params object supplies
/// none of them to a component that set any one. A cascaded XIcon filled in beside the component's own XIconName
/// would otherwise replace the icon the component asked for. The settings are found by their names, so one a
/// component gains later is covered without a word here; what the names do not tell (a text or a template shown in
/// place of an icon, an icon that replaces another one in a state) is pinned row by row below.
/// </summary>
[TestClass]
public class BitIconCascadeTests : BunitTestContext
{
    private static readonly Assembly[] Libraries = [typeof(BitButton).Assembly, typeof(BitMessageBox).Assembly];

    // The ends of the names that take one icon, longest first.
    private static readonly string[] IconSuffixes = ["IconTemplate", "IconNames", "IconName", "IconUrl", "Icons", "Icon"];

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

    public static IEnumerable<object[]> Settings()
    {
        foreach (var (component, cascade) in Readers())
        {
            var parameters = Parameters(component);

            foreach (var setting in parameters.Keys.GroupBy(SettingOf))
            {
                if (setting.Count() < 2) continue;
                if (setting.Any(name => IsCascaded(component, cascade, name)) is false) continue;

                yield return [Name(component), setting.Key];
            }
        }
    }

    [TestMethod]
    public void TheSettingsShouldBeFound()
    {
        // A guard on the discovery itself, which would otherwise pass every test below by finding nothing.
        var settings = Settings().Select(s => (Component: (string)s[0], Setting: (string)s[1])).ToArray();

        Assert.IsGreaterThan(150, settings.Length);

        CollectionAssert.AreEquivalent(new[] { "Icon", "IconName", "IconUrl" }, Members(nameof(BitButton), "Icon").Select(m => m.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "Icon", "IconName", "IconTemplate" }, Members(nameof(BitDatePicker), "Icon").Select(m => m.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "Icon", "IconName", "IconTemplate" }, Members(nameof(BitErrorBoundary), "Icon").Select(m => m.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "DividerIcon", "DividerIconName", "DividerIconTemplate" }, Members("BitBreadcrumb", "DividerIcon").Select(m => m.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "SelectedIcon", "SelectedIconName", "GetSelectedIcon" }, Members(nameof(BitRating), "SelectedIcon").Select(m => m.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "PresenceIcon", "PresenceIconName", "PresenceIcons", "PresenceIconNames" }, Members(nameof(BitPersona), "PresenceIcon").Select(m => m.Name).ToArray());
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
    [DynamicData(nameof(Settings))]
    public void AnIconTheComponentPickedShouldNotBeJoinedByACascadedOne(string componentName, string setting)
    {
        var (component, cascade) = Find(componentName);
        var members = Members(componentName, setting);

        foreach (var own in members)
        {
            var cascaded = members.Where(m => m != own && IsCascaded(component, cascade, m.Name)).ToArray();

            if (cascaded.Length == 0) continue;

            var instance = Render(component, Supply(cascade, cascaded), [(own, Value(own, "own"))]);

            Assert.AreEqual(Value(own, "own"), own.GetValue(instance), $"{componentName}.{own.Name}");

            foreach (var member in cascaded)
            {
                Assert.AreEqual(Default(component, member), member.GetValue(instance),
                                $"{componentName}.{member.Name}, with {own.Name} set on the component and the rest cascaded.");
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(Settings))]
    public void AnIconShouldBeCascadedToAComponentThatPickedNone(string componentName, string setting)
    {
        var (component, cascade) = Find(componentName);

        foreach (var member in Members(componentName, setting).Where(m => IsCascaded(component, cascade, m.Name)))
        {
            var instance = Render(component, Supply(cascade, [member]), []);

            Assert.AreEqual(Value(member, "cascaded"), member.GetValue(instance), $"{componentName}.{member.Name}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Settings))]
    public void AnIconTheMarkupLeavesEmptyShouldNotKeepTheCascadedOnesOut(string componentName, string setting)
    {
        // An icon bound to an item that has none is written as null or "", which picks nothing: the cascade still
        // fills the setting in, as it does for a component that does not write the parameter at all.
        var (component, cascade) = Find(componentName);
        var members = Members(componentName, setting);

        foreach (var own in members.Where(m => m.PropertyType.IsValueType is false))
        {
            var cascaded = members.Where(m => m != own && IsCascaded(component, cascade, m.Name)).ToArray();

            if (cascaded.Length == 0) continue;

            var empty = own.PropertyType == typeof(string) ? "" : null;

            var instance = Render(component, Supply(cascade, cascaded), [(own, empty)]);

            foreach (var member in cascaded)
            {
                Assert.AreEqual(Value(member, "cascaded"), member.GetValue(instance),
                                $"{componentName}.{member.Name}, with {own.Name} written empty on the component.");
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(Settings))]
    public void ACascadedIconShouldBeTakenBackOnceTheComponentPicksItsOwn(string componentName, string setting)
    {
        var (component, cascade) = Find(componentName);
        var members = Members(componentName, setting);

        foreach (var icon in members.Where(m => IsCascaded(component, cascade, m.Name)))
        {
            foreach (var own in members.Where(m => m != icon))
            {
                AssertTakenBack(component, cascade, icon, own);
            }
        }
    }

    /// <summary>
    /// The parameters a component shows in place of an icon of another setting, which their names do not tell: a
    /// cascaded one must not replace the icon the component picked, and one it took before the component picked its
    /// own is taken back.
    /// </summary>
    [TestMethod]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.OnIcon), nameof(BitToggleButton.IconName))]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.OnIconName), nameof(BitToggleButton.Icon))]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.OffIcon), nameof(BitToggleButton.IconName))]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.OffIconName), nameof(BitToggleButton.IconName))]
    [DataRow(nameof(BitAccordion), nameof(BitAccordion.ExpandedExpanderIcon), nameof(BitAccordion.ExpanderIconName))]
    [DataRow(nameof(BitAccordion), nameof(BitAccordion.ExpandedExpanderIconName), nameof(BitAccordion.ExpanderIcon))]
    [DataRow("BitAccordionList", "ExpandedExpanderIconName", "ExpanderIconName")]
    [DataRow("BitBreadcrumb", "DividerText", "DividerIconName")]
    [DataRow("BitBreadcrumb", "DividerText", "DividerIcon")]
    [DataRow("BitBreadcrumb", "DividerIconTemplate", "DividerText")]
    [DataRow(nameof(BitTextField), nameof(BitTextField.ClearButtonTemplate), nameof(BitTextField.ClearButtonIconName))]
    [DataRow(nameof(BitTextField), nameof(BitTextField.RevealPasswordTemplate), nameof(BitTextField.RevealPasswordIconName))]
    [DataRow(nameof(BitTextField), nameof(BitTextField.RevealPasswordTemplate), nameof(BitTextField.HidePasswordIcon))]
    [DataRow(nameof(BitPhoneInput), nameof(BitPhoneInput.ClearButtonTemplate), nameof(BitPhoneInput.ClearButtonIcon))]
    [DataRow(nameof(BitSplitter), nameof(BitSplitter.GutterTemplate), nameof(BitSplitter.GutterIconName))]
    [DataRow(nameof(BitRating), nameof(BitRating.GetSelectedIcon), nameof(BitRating.SelectedIconName))]
    [DataRow(nameof(BitRating), nameof(BitRating.GetUnselectedIcon), nameof(BitRating.UnselectedIcon))]
    [DataRow(nameof(BitPersona), nameof(BitPersona.PresenceIcons), nameof(BitPersona.PresenceIconNames))]
    [DataRow(nameof(BitPersona), nameof(BitPersona.PresenceIconNames), nameof(BitPersona.PresenceIconName))]
    [DataRow(nameof(BitDatePicker), nameof(BitDatePicker.IconTemplate), nameof(BitDatePicker.IconName))]
    [DataRow(nameof(BitErrorBoundary), nameof(BitErrorBoundary.IconTemplate), nameof(BitErrorBoundary.Icon))]
    [DataRow(nameof(BitButton), nameof(BitButton.IconUrl), nameof(BitButton.IconName))]
    [DataRow(nameof(BitButton), nameof(BitButton.Icon), nameof(BitButton.IconUrl))]
    [DataRow(nameof(BitTag), nameof(BitTag.IconUrl), nameof(BitTag.Icon))]
    public void APartShownInPlaceOfTheComponentsOwnIconShouldNotBeCascaded(string componentName, string cascadedName, string ownName)
    {
        var (component, cascade) = Find(componentName);
        var cascaded = Parameter(component, cascadedName);
        var own = Parameter(component, ownName);

        var instance = Render(component, Supply(cascade, [cascaded]), [(own, Value(own, "own"))]);

        Assert.AreEqual(Default(component, cascaded), cascaded.GetValue(instance), $"{componentName}.{cascadedName}, with {ownName} set on the component.");
        Assert.AreEqual(Value(own, "own"), own.GetValue(instance), $"{componentName}.{ownName}");

        AssertTakenBack(component, cascade, cascaded, own);
    }

    /// <summary>
    /// An icon that replaces another one only while the component is in a state leaves the other one shown in every
    /// other state, so a component that picked the first still takes the second from the cascade.
    /// </summary>
    [TestMethod]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.IconName), nameof(BitToggleButton.OnIconName))]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.Icon), nameof(BitToggleButton.OffIcon))]
    [DataRow(nameof(BitToggleButton), nameof(BitToggleButton.OffIconName), nameof(BitToggleButton.OnIconName))]
    [DataRow(nameof(BitAccordion), nameof(BitAccordion.ExpanderIconName), nameof(BitAccordion.ExpandedExpanderIconName))]
    [DataRow(nameof(BitTextField), nameof(BitTextField.HidePasswordIconName), nameof(BitTextField.RevealPasswordIconName))]
    public void AnIconOfAnotherStateShouldStillBeCascaded(string componentName, string cascadedName, string ownName)
    {
        var (component, cascade) = Find(componentName);
        var cascaded = Parameter(component, cascadedName);
        var own = Parameter(component, ownName);

        var instance = Render(component, Supply(cascade, [cascaded]), [(own, Value(own, "own"))]);

        Assert.AreEqual(Value(cascaded, "cascaded"), cascaded.GetValue(instance), $"{componentName}.{cascadedName}, with {ownName} set on the component.");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void AnEmptyIconNameShouldNotBeCascadedToAnErrorBoundary(string iconName)
    {
        var instance = (BitErrorBoundary)Render(typeof(BitErrorBoundary), new BitErrorBoundaryParams { IconName = iconName }, []);

        Assert.IsNull(instance.IconName);
    }



    // Takes the cascaded member first, then sets the own one on the component, under the same params object.
    private void AssertTakenBack(Type component, PropertyInfo cascade, PropertyInfo cascaded, PropertyInfo own)
    {
        var @params = Supply(cascade, [cascaded]);

        object? instance = null;

        var host = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [@params]);
            parameters.AddChildContent(Fragment(component, [], i => instance = i));
        });

        Assert.AreEqual(Value(cascaded, "cascaded"), cascaded.GetValue(instance), $"{component.Name}.{cascaded.Name}, before {own.Name} is set.");

        host.Render(parameters => parameters.AddChildContent(Fragment(component, [(own, Value(own, "own"))], i => instance = i)));

        Assert.AreEqual(Default(component, cascaded), cascaded.GetValue(instance), $"{component.Name}.{cascaded.Name}, once {own.Name} is set.");
        Assert.AreEqual(Value(own, "own"), own.GetValue(instance), $"{component.Name}.{own.Name}");
    }

    // The parameters of the component that make up one icon setting, whether the params object can supply them or not.
    private static PropertyInfo[] Members(string componentName, string setting)
    {
        var (component, _) = Find(componentName);

        return Parameters(component).Where(p => SettingOf(p.Key) == setting).Select(p => p.Value).ToArray();
    }

    private static string SettingOf(string name)
    {
        if (name.Length >= 7 && name.StartsWith("Get", StringComparison.Ordinal) && name.EndsWith("Icon", StringComparison.Ordinal)) return name[3..];

        foreach (var suffix in IconSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal)) return name[..^suffix.Length] + "Icon";
        }

        return name;
    }

    private static bool IsCascaded(Type component, PropertyInfo cascade, string name)
    {
        var source = cascade.PropertyType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        if (source is null) return false;

        var target = Parameter(component, name);
        var valueType = Nullable.GetUnderlyingType(source.PropertyType) ?? source.PropertyType;

        return target.CanWrite && target.PropertyType.IsAssignableFrom(valueType);
    }

    private object Render(Type component, IBitComponentParams @params, (PropertyInfo Member, object? Value)[] own)
    {
        object? instance = null;

        RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [@params]);
            parameters.AddChildContent(Fragment(component, own, i => instance = i));
        });

        return instance ?? throw new InvalidOperationException($"{component.Name} was not rendered.");
    }

    private static RenderFragment Fragment(Type component, (PropertyInfo Member, object? Value)[] own, Action<object> capture)
    {
        return builder =>
        {
            builder.OpenComponent(0, component);

            foreach (var (member, value) in own)
            {
                // Boxed, since BitIconInfo converts to a string and would otherwise pick that overload.
                builder.AddAttribute(1, member.Name, value);
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

    // A value per member type and per side, so what a component ends up holding names where it came from. Neither
    // BitIconInfo, a delegate nor a map has value equality, so the same instance is handed out every time.
    private static readonly Dictionary<(Type, string), object> Values = [];

    private static object Value(PropertyInfo member, string side)
    {
        var type = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;

        if (type == typeof(string)) return member.Name.EndsWith("Url") ? $"{side}.png" : $"{side}-{member.Name}";

        lock (Values)
        {
            if (Values.TryGetValue((type, side), out var value)) return value;

            if (type == typeof(BitIconInfo))
            {
                value = BitIconInfo.Css($"{side}-icon");
            }
            else if (typeof(Delegate).IsAssignableFrom(type))
            {
                value = CreateDelegate(type, side);
            }
            else
            {
                value = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"No value of {type} for {member.Name}.");
            }

            Values.Add((type, side), value);

            return value;
        }
    }

    // A RenderFragment that writes the side, a RenderFragment<T> that returns one, and a selector that returns the
    // icon of the side.
    private static object CreateDelegate(Type type, string side)
    {
        var invoke = type.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

        Expression body;

        if (invoke.ReturnType == typeof(void))
        {
            var builder = parameters.Single(p => p.Type == typeof(RenderTreeBuilder));

            body = Expression.Call(builder, typeof(RenderTreeBuilder).GetMethod(nameof(RenderTreeBuilder.AddContent), [typeof(int), typeof(string)])!,
                                   Expression.Constant(0), Expression.Constant(side));
        }
        else if (invoke.ReturnType == typeof(RenderFragment))
        {
            body = Expression.Constant(CreateDelegate(typeof(RenderFragment), side), typeof(RenderFragment));
        }
        else if (invoke.ReturnType.IsAssignableFrom(typeof(BitIconInfo)))
        {
            body = Expression.Constant(BitIconInfo.Css($"{side}-icon"), invoke.ReturnType);
        }
        else
        {
            body = Expression.Default(invoke.ReturnType);
        }

        return Expression.Lambda(type, body, parameters).Compile();
    }

    private static object? Default(Type component, PropertyInfo member) => member.GetValue(Activator.CreateInstance(component));

    private static Dictionary<string, PropertyInfo> Parameters(Type component)
    {
        return component.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.IsDefined(typeof(ParameterAttribute)))
                        .GroupBy(p => p.Name)
                        .ToDictionary(g => g.Key, g => g.OrderByDescending(p => Depth(p.DeclaringType)).First());
    }

    private static PropertyInfo Parameter(Type component, string name) => Parameters(component)[name];

    // A generic component is named without its arity, as it is written in the markup.
    private static string Name(Type component) => component.Name.Split('`')[0];

    private static (Type Component, PropertyInfo Cascade) Find(string componentName)
    {
        return Readers().Single(r => Name(r.Component) == componentName);
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
