using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// Pins that the BitParams cascade of every component leaves its class and style builders alone when it has nothing
/// new to say. UpdateParameters runs on every render of every component under a BitParams, so a cascade that
/// resets a builder on an unchanged value rebuilds both strings of the root on every render for nothing.
/// </summary>
[TestClass]
public class BitParamsBuilderResetTests
{
    [TestMethod]
    public void EveryParamsShouldNotResetTheBuildersWhenTheCascadeGivesTheSameAgain()
    {
        var failures = new List<string>();
        var tested = 0;

        foreach (var (paramsType, update, componentType) in DiscoverUpdateMethods())
        {
            var cascade = Activator.CreateInstance(paramsType)!;
            Populate(cascade);

            var component = (BitComponentBase)Activator.CreateInstance(componentType)!;
            var classBuilds = 0;
            var styleBuilds = 0;
            component.ClassBuilder.Register(() => { classBuilds++; return null; });
            component.StyleBuilder.Register(() => { styleBuilds++; return null; });

            try
            {
                update.Invoke(cascade, [component]);
                _ = component.ClassBuilder.Value;
                _ = component.StyleBuilder.Value;

                var firstClassBuilds = classBuilds;
                var firstStyleBuilds = styleBuilds;

                update.Invoke(cascade, [component]);
                _ = component.ClassBuilder.Value;
                _ = component.StyleBuilder.Value;

                if (classBuilds != firstClassBuilds)
                {
                    failures.Add($"{paramsType.Name} rebuilt the classes of {componentType.Name} on an unchanged cascade.");
                }

                if (styleBuilds != firstStyleBuilds)
                {
                    failures.Add($"{paramsType.Name} rebuilt the styles of {componentType.Name} on an unchanged cascade.");
                }
            }
            catch (TargetInvocationException ex)
            {
                failures.Add($"{paramsType.Name}.UpdateParameters threw {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}");
            }

            tested++;
        }

        // Guards the discovery itself: a reflection change that found nothing would otherwise pass vacuously.
        Assert.IsTrue(tested > 80, $"Only {tested} params types were discovered.");
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void AChangedCascadedValueShouldStillResetTheBuilder()
    {
        var cascade = new BitButtonParams { Size = BitSize.Small };
        var button = new BitButton();
        var builds = 0;
        button.ClassBuilder.Register(() => { builds++; return null; });

        cascade.UpdateParameters(button);
        _ = button.ClassBuilder.Value;

        cascade.Size = BitSize.Large;
        cascade.UpdateParameters(button);
        _ = button.ClassBuilder.Value;

        Assert.AreEqual(BitSize.Large, button.Size);
        Assert.AreEqual(2, builds);
    }

    [TestMethod]
    public void ANewClassStylesInstanceShouldStillResetTheBuilder()
    {
        var cascade = new BitButtonParams { Styles = new() { Root = "color:red" } };
        var button = new BitButton();
        var builds = 0;
        button.StyleBuilder.Register(() => { builds++; return null; });

        cascade.UpdateParameters(button);
        _ = button.StyleBuilder.Value;

        cascade.Styles = new() { Root = "color:blue" };
        cascade.UpdateParameters(button);
        _ = button.StyleBuilder.Value;

        Assert.AreEqual(2, builds);
    }



    private static IEnumerable<(Type ParamsType, MethodInfo Update, Type ComponentType)> DiscoverUpdateMethods()
    {
        var assemblies = new[] { typeof(BitButtonParams).Assembly, typeof(BitAppShellParams).Assembly };

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type.IsAbstract || type.IsInterface || typeof(IBitComponentParams).IsAssignableFrom(type) is false) continue;

            var found = false;

            // A generic component closes over whichever argument it takes: an int for a numeric one, a string for one
            // constrained to reference types, and some (BitNumberField) only refuse the wrong one in their constructor.
            foreach (var candidate in new[] { typeof(int), typeof(string), typeof(object) })
            {
                var paramsType = type.IsGenericTypeDefinition ? TryClose(() => type.MakeGenericType(Fill(type.GetGenericArguments(), candidate))) : type;
                if (paramsType is null) continue;

                foreach (var method in paramsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.Name != "UpdateParameters") continue;

                    var closed = method.IsGenericMethodDefinition ? TryClose(() => method.MakeGenericMethod(Fill(method.GetGenericArguments(), candidate))) : method;
                    if (closed is null) continue;

                    var parameters = closed.GetParameters();
                    if (parameters.Length != 1) continue;

                    var componentType = parameters[0].ParameterType;
                    if (componentType.IsAbstract || componentType.ContainsGenericParameters) continue;
                    if (typeof(BitComponentBase).IsAssignableFrom(componentType) is false) continue;
                    if (componentType.GetConstructor(Type.EmptyTypes) is null) continue;
                    if (TryClose(() => Activator.CreateInstance(componentType)) is null) continue;

                    found = true;
                    yield return (paramsType, closed, componentType);
                }

                if (found) break;
            }
        }
    }

    private static Type[] Fill(Type[] arguments, Type candidate) => arguments.Select(_ => candidate).ToArray();

    private static T? TryClose<T>(Func<T> close) where T : class
    {
        try
        {
            return close();
        }
        catch (Exception ex) when (ex is ArgumentException or TargetInvocationException)
        {
            return null;
        }
    }

    private static void Populate(object cascade)
    {
        foreach (var property in cascade.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanWrite is false || property.GetIndexParameters().Length > 0) continue;

            var value = SampleOf(property.PropertyType);
            if (value is null) continue;

            property.SetValue(cascade, value);
        }
    }

    private static object? SampleOf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return "x";
        if (underlying == typeof(bool)) return true;
        if (underlying.IsEnum) return Enum.GetValues(underlying).Cast<object>().Last();
        if (underlying == typeof(int)) return 2;
        if (underlying == typeof(double)) return 2d;
        if (underlying == typeof(float)) return 2f;
        if (underlying == typeof(decimal)) return 2m;
        if (underlying == typeof(long)) return 2L;
        if (underlying == typeof(TimeSpan)) return TimeSpan.FromMinutes(1);
        if (underlying == typeof(DateTime)) return new DateTime(2020, 1, 15);
        if (underlying == typeof(DateTimeOffset)) return new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero);
        if (underlying == typeof(TimeOnly)) return new TimeOnly(10, 30);
        if (underlying == typeof(DateOnly)) return new DateOnly(2020, 1, 15);
        if (underlying == typeof(CultureInfo)) return CultureInfo.InvariantCulture;
        if (underlying == typeof(TimeZoneInfo)) return TimeZoneInfo.Utc;

        if (underlying.IsClass && underlying.IsAbstract is false && typeof(Delegate).IsAssignableFrom(underlying) is false
            && underlying.IsGenericTypeDefinition is false && underlying.GetConstructor(Type.EmptyTypes) is not null)
        {
            return Activator.CreateInstance(underlying);
        }

        return null;
    }
}
