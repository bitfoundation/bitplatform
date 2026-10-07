using System;
using System.Collections.Generic;
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
    private static readonly Assembly[] Libraries = [typeof(BitButton).Assembly, typeof(BitMessageBox).Assembly];

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
