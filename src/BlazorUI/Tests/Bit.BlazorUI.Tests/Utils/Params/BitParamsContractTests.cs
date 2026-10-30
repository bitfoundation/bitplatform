using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// Pins every params object the libraries ship to the component that reads it. BitComponentBase puts back a value
/// the cascade stops supplying by matching each property of the params object to the parameter of the same name on
/// the component, and skips one it cannot match without a word - so a property renamed on one side only, or typed
/// apart from its parameter, would be cascaded and then never taken back. This is what fails on it instead.
/// </summary>
[TestClass]
public class BitParamsContractTests
{
    private static readonly Assembly[] Libraries = [typeof(BitButton).Assembly, typeof(BitMessageBox).Assembly, typeof(BitAiChat).Assembly];

    // The name is what the params object is read by, and the attributes are merged into the ones the component
    // collects afresh on every render, so neither is a parameter left behind.
    private static readonly string[] NotParameters = [nameof(IBitComponentParams.Name), nameof(BitComponentBase.HtmlAttributes)];



    [TestMethod]
    public void EveryParamsTypeShouldBeReadByAComponent()
    {
        var read = Readers().Select(r => Definition(r.Cascade.PropertyType)).ToHashSet();

        var unread = ParamsTypes().Where(t => read.Contains(t) is false).Select(t => t.Name).ToArray();

        Assert.AreEqual(0, unread.Length, $"Read by no component: {string.Join(", ", unread)}");
    }

    [TestMethod]
    public void EveryValueAParamsTypeSuppliesShouldBeAParameterTheComponentCanTakeBack()
    {
        var unmatched = new List<string>();

        foreach (var (component, cascade) in Readers())
        {
            var properties = component.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var source in cascade.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (NotParameters.Contains(source.Name)) continue;
                if (source.CanRead is false || source.GetIndexParameters().Length > 0) continue;

                var target = properties.Where(p => p.Name == source.Name
                                                && p.IsDefined(typeof(ParameterAttribute))
                                                && p.GetSetMethod() is not null)
                                       .OrderByDescending(p => Depth(p.DeclaringType))
                                       .FirstOrDefault();

                if (target is null)
                {
                    unmatched.Add($"{component.Name}.{source.Name}: no settable [Parameter] of that name");
                    continue;
                }

                var valueType = Nullable.GetUnderlyingType(source.PropertyType) ?? source.PropertyType;

                if (target.PropertyType.IsAssignableFrom(valueType) is false && target.PropertyType != source.PropertyType)
                {
                    unmatched.Add($"{component.Name}.{source.Name}: a {source.PropertyType.Name} cannot be put in a {target.PropertyType.Name}");
                }
            }
        }

        Assert.AreEqual(0, unmatched.Count, string.Join(Environment.NewLine, unmatched));
    }

    [TestMethod]
    public void NoValueAParamsTypeSuppliesShouldHaveAnAsyncSetupHook()
    {
        // A params object is applied from the synchronous OnParametersSet, where TakeFromCascade runs the [CallOnSet]
        // hook of what it writes but has no way to await a [CallOnSetAsync] one - nor a [CallOnSet] one that returns
        // a Task or a ValueTask, which the generated code awaits like a [CallOnSetAsync] one and a params object
        // would drop.
        var hooked = new List<string>();

        foreach (var (component, cascade) in Readers())
        {
            var properties = component.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var source in cascade.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var target = properties.Where(p => p.Name == source.Name && p.IsDefined(typeof(ParameterAttribute)))
                                       .OrderByDescending(p => Depth(p.DeclaringType))
                                       .FirstOrDefault();

                if (target is null) continue;

                var attributes = target.GetCustomAttributesData();

                if (attributes.Any(a => a.AttributeType.FullName == "Bit.BlazorUI.CallOnSetAsyncAttribute"))
                {
                    hooked.Add($"{component.Name}.{source.Name}");
                }
                else if (attributes.FirstOrDefault(a => a.AttributeType.FullName == "Bit.BlazorUI.CallOnSetAttribute")
                                   ?.ConstructorArguments.FirstOrDefault().Value is string hook
                         && ReturnsAwaitable(target.DeclaringType!, hook))
                {
                    hooked.Add($"{component.Name}.{source.Name} ([CallOnSet] naming the awaitable {hook})");
                }
            }
        }

        Assert.AreEqual(0, hooked.Count, string.Join(Environment.NewLine, hooked));
    }

    [TestMethod]
    public void EveryParamsTypeShouldBeKeptByItsOwnConstructor()
    {
        // BitParams reads a params object by reflection - copying it, comparing it, matching its properties to the
        // parameters of its component - so a trimmed app has to keep all of it. Its constructor says so, which ILLink
        // follows only where an app creates one: a params type no app creates keeps nothing but the UpdateParameters
        // its component calls, emptied, rather than every property and the two lambdas per property behind it.
        var unkept = new List<string>();

        foreach (var type in ParamsTypes())
        {
            var keepsItself = type.GetConstructor(Type.EmptyTypes)?
                                  .GetCustomAttributes<DynamicDependencyAttribute>()
                                  .Any(d => d.Type == type && d.MemberTypes == DynamicallyAccessedMemberTypes.All) ?? false;

            if (keepsItself is false) unkept.Add(type.Name);
        }

        Assert.AreEqual(0, unkept.Count, $"No public parameterless constructor carrying [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(<itself>))]: {string.Join(", ", unkept)}");
    }

    [TestMethod]
    public void NothingButItsOwnConstructorShouldKeepAParamsType()
    {
        // A dependency on a params type from anywhere else - the component reading it, above all - keeps it in every
        // app that renders the component, constructor and all, so with every lambda its UpdateParameters holds, whether
        // or not the app ever creates one.
        var paramsTypes = ParamsTypes().ToHashSet();
        var keepers = new List<string>();

        foreach (var type in Libraries.SelectMany(a => a.GetTypes()))
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                foreach (var dependency in member.GetCustomAttributes<DynamicDependencyAttribute>())
                {
                    if (dependency.Type is null || paramsTypes.Contains(Definition(dependency.Type)) is false) continue;
                    if (member is ConstructorInfo && Definition(type) == Definition(dependency.Type)) continue;

                    keepers.Add($"{type.Name}.{member.Name} keeps {dependency.Type.Name}");
                }
            }
        }

        Assert.AreEqual(0, keepers.Count, string.Join(Environment.NewLine, keepers));
    }



    private static bool ReturnsAwaitable(Type declaringType, string hook)
    {
        var method = declaringType.GetMethod(hook, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
        var returnType = method?.ReturnType;

        if (returnType is null) return false;

        var definition = returnType.IsGenericType ? returnType.GetGenericTypeDefinition() : returnType;

        return definition == typeof(System.Threading.Tasks.Task) || definition == typeof(System.Threading.Tasks.Task<>)
            || definition == typeof(System.Threading.Tasks.ValueTask) || definition == typeof(System.Threading.Tasks.ValueTask<>);
    }

    private static IEnumerable<Type> ParamsTypes()
    {
        return Libraries.SelectMany(a => a.GetTypes())
                        .Where(t => t.IsClass && t.IsAbstract is false && typeof(IBitComponentParams).IsAssignableFrom(t))
                        .Select(Definition);
    }

    private static IEnumerable<(Type Component, PropertyInfo Cascade)> Readers()
    {
        foreach (var type in Libraries.SelectMany(a => a.GetTypes()))
        {
            if (type.IsClass is false || type.IsAbstract || typeof(IComponent).IsAssignableFrom(type) is false) continue;

            var cascades = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                               .Where(p => p.IsDefined(typeof(CascadingParameterAttribute))
                                        && typeof(IBitComponentParams).IsAssignableFrom(p.PropertyType))
                               .ToArray();

            // The restore reads exactly one params object per component; one that read two would take back neither.
            Assert.IsTrue(cascades.Length <= 1, $"{type.Name} reads {cascades.Length} params objects.");

            if (cascades.Length == 1) yield return (type, cascades[0]);
        }
    }

    private static Type Definition(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    private static int Depth(Type? type)
    {
        var depth = 0;

        for (; type is not null; type = type.BaseType) depth++;

        return depth;
    }
}
