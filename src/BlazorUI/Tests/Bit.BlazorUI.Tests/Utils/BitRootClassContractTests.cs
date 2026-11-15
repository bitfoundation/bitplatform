using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins that every component of every package has a root class of its own. The root class is also the prefix of every
/// class the component renders and styles (bit-xxx-*), so two components sharing one share every rule written for it:
/// BitStepper's flex row on bit-stp and bit-stp-hrz once reached the BitSwipeTrap inside BitNavPanel, and narrowed and
/// shifted the whole panel. A component that is a part of another one's family carries a root under that one's
/// prefix (BitGridItem's bit-grd-itm), named in <see cref="_familyParts"/>.
/// </summary>
[TestClass]
public class BitRootClassContractTests
{
    // A root that extends another component's root, and the component it is a part of.
    private static readonly Dictionary<string, string> _familyParts = new()
    {
        ["BitGridItem"] = "BitGrid",
    };

    private static readonly Type[] _typeArguments = [typeof(object), typeof(int), typeof(string), typeof(double), typeof(DateTimeOffset)];

    [TestMethod]
    public void EveryComponentShouldHaveARootClassOfItsOwn()
    {
        var roots = ReadRootClasses();

        // Guards the discovery itself: a reflection change that found nothing would otherwise pass vacuously.
        Assert.IsTrue(roots.Count > 150, $"Only {roots.Count} root classes were discovered.");

        var failures = roots.GroupBy(r => r.Class)
                            .Where(g => g.Count() > 1)
                            .Select(g => $"{g.Key} is the root class of {string.Join(", ", g.Select(r => r.Type))}.")
                            .ToList();

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void NoRootClassShouldExtendAnotherComponentsUnlessItIsPartOfItsFamily()
    {
        var roots = ReadRootClasses();
        var failures = new List<string>();

        foreach (var (type, cls) in roots)
        {
            foreach (var (otherType, otherCls) in roots)
            {
                if (cls.StartsWith(otherCls + "-", StringComparison.Ordinal) is false) continue;
                if (_familyParts.TryGetValue(type, out var family) && family == otherType) continue;

                failures.Add($"{type}'s root class {cls} sits under the prefix of {otherType} ({otherCls}).");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    private static List<(string Type, string Class)> ReadRootClasses()
    {
        var assemblies = new[] { typeof(BitButton).Assembly, typeof(BitAppShell).Assembly, typeof(Bit.BlazorUI.Legacy.BitRichTextEditorLegacy).Assembly, typeof(BitAiChat).Assembly };
        var property = typeof(BitComponentBase).GetProperty("RootElementClass", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var roots = new List<(string, string)>();

        foreach (var type in assemblies.SelectMany(LoadableTypes))
        {
            if (type.IsAbstract || typeof(BitComponentBase).IsAssignableFrom(type) is false) continue;

            var closed = Close(type);
            Assert.IsNotNull(closed, $"{type.Name} could not be closed over any of the test's type arguments.");

            // A constructed instance runs the property initializers (BitCalendar's root is one); a constructor that
            // cannot run without a host falls back to an instance with none of them.
            object instance;
            try
            {
                instance = Activator.CreateInstance(closed)!;
            }
            catch (TargetInvocationException)
            {
                instance = RuntimeHelpers.GetUninitializedObject(closed);
            }

            var cls = (string?)property.GetValue(instance);
            Assert.IsFalse(string.IsNullOrWhiteSpace(cls), $"{type.Name} has no root class.");

            roots.Add((NameOf(type), cls!));
        }

        return roots;
    }

    private static Type? Close(Type type)
    {
        if (type.IsGenericTypeDefinition is false) return type;

        // A parameter constrained to a type of the library (BitMap's provider) is tried with the concrete types that
        // satisfy its constraints, ahead of the plain ones.
        IEnumerable<Type[]> combinations = [[]];
        foreach (var parameter in type.GetGenericArguments())
        {
            var constraints = parameter.GetGenericParameterConstraints().Where(c => c != typeof(ValueType)).ToArray();
            var candidates = constraints.Length == 0
                ? _typeArguments
                : LoadableTypes(constraints[0].Assembly)
                    .Where(t => t.IsAbstract is false && t.IsGenericTypeDefinition is false && constraints.All(c => c.IsAssignableFrom(t)))
                    .Concat(_typeArguments)
                    .ToArray();

            combinations = combinations.SelectMany(c => candidates.Select(t => c.Append(t).ToArray()));
        }

        foreach (var arguments in combinations)
        {
            try
            {
                return type.MakeGenericType(arguments);
            }
            catch (ArgumentException)
            {
                // A constraint the arguments do not meet; the next combination is tried.
            }
        }

        return null;
    }

    private static string NameOf(Type type) => type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;

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
}
