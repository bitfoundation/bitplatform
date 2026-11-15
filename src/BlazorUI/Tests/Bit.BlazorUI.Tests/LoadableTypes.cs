using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// The types of an assembly that can be loaded, for the contract tests that discover what they check by reflection
/// over the packages. One reading for all of them, so they discover the same set of types.
/// </summary>
/// <remarks>
/// A type whose base sits in an assembly the tests do not ship (Newtonsoft.Json, behind a legacy chart type) cannot be
/// loaded, and is not a type any of these tests looks for anyway. The assemblies do not change while the tests run, so
/// each one is enumerated once per run however many tests read it.
/// </remarks>
internal static class LoadableTypes
{
    private static readonly ConcurrentDictionary<Assembly, Type[]> Cache = new();

    public static IReadOnlyList<Type> Of(Assembly assembly) => Cache.GetOrAdd(assembly, Load);

    private static Type[] Load(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>().ToArray();
        }
    }
}
