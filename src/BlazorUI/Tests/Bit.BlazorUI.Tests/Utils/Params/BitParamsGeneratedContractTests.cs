using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// Pins the snapshot and the merge the source generator writes into every params type of the packages to the
/// reflection of BitParamsScope they stand in for: the same properties taken apart, a change to any one of them seen,
/// and the same merged copy. The generated code is what a WebAssembly app runs as it starts, and the reflection is what
/// an app's own params types still go through, so a property the two disagreed on would be a change one of them misses.
/// </summary>
[TestClass]
public class BitParamsGeneratedContractTests
{
    private static readonly Assembly[] Libraries = [typeof(BitButton).Assembly, typeof(BitMessageBox).Assembly, typeof(BitAiChat).Assembly];



    [TestMethod]
    public void EveryParamsTypeOfThePackagesShouldHaveItsSnapshotAndMergeGenerated()
    {
        var missing = ParamsTypes().Where(t => Internals.Generated.IsAssignableFrom(t) is false)
                                   .Select(t => t.Name)
                                   .ToArray();

        Assert.AreEqual(0, missing.Length, $"Not declared partial, so taken apart by reflection: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void TheGeneratedSnapshotShouldTakeApartWhatReflectionDoes()
    {
        var failures = new List<string>();

        foreach (var type in ParamsTypes())
        {
            var item = (IBitComponentParams)Activator.CreateInstance(type)!;

            var snapshot = Internals.TakeSnapshot(item);

            if (snapshot is null || snapshot.GetType().Name != "ObjectSnapshot")
            {
                failures.Add($"{type.Name}: no snapshot");
                continue;
            }

            var reflected = Internals.GetSnapshotProperties(type);
            var snapshotType = (Type)snapshot.GetType().GetProperty("Type")!.GetValue(snapshot)!;
            var values = (object?[])snapshot.GetType().GetProperty("Values")!.GetValue(snapshot)!;

            if (snapshotType != type) failures.Add($"{type.Name}: a snapshot of {snapshotType.Name}");
            if (values.Length != reflected.Length) failures.Add($"{type.Name}: {values.Length} values for {reflected.Length} properties");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void TheGeneratedSnapshotShouldSeeAChangeToEveryProperty()
    {
        var failures = new List<string>();
        var checkedCount = 0;

        foreach (var type in ParamsTypes())
        {
            foreach (var property in Internals.GetCopyableProperties(type))
            {
                if (TrySample(property.PropertyType, 0, out var first) is false) continue;
                if (TrySample(property.PropertyType, 1, out var second) is false) continue;

                var item = (IBitComponentParams)Activator.CreateInstance(type)!;

                checkedCount++;
                property.SetValue(item, first);
                var before = Internals.TakeSnapshot(item);

                if (Internals.AreEqual(before, Internals.TakeSnapshot(item)) is false)
                {
                    failures.Add($"{type.Name}.{property.Name}: two snapshots of the same value differ");
                }

                property.SetValue(item, second);

                if (Internals.AreEqual(before, Internals.TakeSnapshot(item)))
                {
                    failures.Add($"{type.Name}.{property.Name}: the change is not seen");
                }
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));

        // Most properties are plain values the samples cover; a run that could build none of them tested nothing.
        Assert.IsTrue(checkedCount > 2000, $"Only {checkedCount} properties were changed.");
    }

    [TestMethod]
    public void TheGeneratedSnapshotShouldSeeAChangeMadeInPlaceInsideANestedObject()
    {
        var classes = new BitButtonClassStyles { Root = "a" };
        var button = new BitButtonParams { Classes = classes, Icon = new BitIconInfo("Add") };

        var before = Internals.TakeSnapshot(button);

        classes.Root = "b";

        Assert.IsFalse(Internals.AreEqual(before, Internals.TakeSnapshot(button)));

        var strings = new BitDataGridStrings { EmptyText = "a" };
        var grid = new BitDataGridParams { Strings = strings };

        before = Internals.TakeSnapshot(grid);

        strings.EmptyText = "b";

        Assert.IsFalse(Internals.AreEqual(before, Internals.TakeSnapshot(grid)));

        var attributes = new Dictionary<string, object> { ["data-a"] = "a" };
        var text = new BitTextFieldParams { HtmlAttributes = attributes };

        before = Internals.TakeSnapshot(text);

        attributes["data-a"] = "b";

        Assert.IsFalse(Internals.AreEqual(before, Internals.TakeSnapshot(text)));
    }

    [TestMethod]
    public void TheGeneratedMergeShouldMakeTheCopyReflectionDoes()
    {
        var failures = new List<string>();

        foreach (var type in ParamsTypes())
        {
            var properties = Internals.GetCopyableProperties(type);
            var basis = (IBitComponentParams)Activator.CreateInstance(type)!;
            var overlay = (IBitComponentParams)Activator.CreateInstance(type)!;

            // The basis sets everything, the overlay every other property, so both halves of the merge are taken.
            for (var i = 0; i < properties.Length; i++)
            {
                if (TrySample(properties[i].PropertyType, 0, out var basisValue)) properties[i].SetValue(basis, basisValue);
                if (i % 2 == 0 && TrySample(properties[i].PropertyType, 1, out var value)) properties[i].SetValue(overlay, value);
            }

            var generated = Internals.MergeOver(overlay, basis);
            var reflected = Internals.MergeByReflection(basis, overlay);

            if (generated is null || generated.GetType() != type)
            {
                failures.Add($"{type.Name}: no copy of its own type");
                continue;
            }

            if (ReferenceEquals(generated, overlay) || ReferenceEquals(generated, basis)) failures.Add($"{type.Name}: not a new object");

            foreach (var property in properties)
            {
                var expected = property.GetValue(reflected);
                var actual = property.GetValue(generated);

                if (AreSameValue(expected, actual) is false) failures.Add($"{type.Name}.{property.Name}: {actual} where reflection merges {expected}");
            }

            // A basis of another type is not merged over.
            var alone = Internals.MergeOver(overlay, new FakeParamsA());
            var reflectedAlone = Internals.MergeByReflection(new FakeParamsA(), overlay);

            foreach (var property in properties)
            {
                if (AreSameValue(property.GetValue(reflectedAlone), property.GetValue(alone)) is false) failures.Add($"{type.Name}.{property.Name}: merged over a basis of another type");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void TheGeneratedCodeShouldLeaveADerivedTypeToReflection()
    {
        var derived = new DerivedButtonParams { Extra = "a", Title = "t" };

        Assert.IsNull(Internals.TakeSnapshot(derived));
        Assert.IsNull(Internals.MergeOver(derived, null));

        var scope = Internals.Create(null, [derived]);
        var copy = (DerivedButtonParams)Internals.All(scope).Values.Cast<object>().Single();

        Assert.AreNotSame(derived, copy);
        Assert.AreEqual("a", copy.Extra);
        Assert.AreEqual("t", copy.Title);
        Assert.IsTrue(Internals.IsCopy(copy));

        derived.Extra = "b";

        Assert.IsFalse(Internals.IsCreatedFrom(scope, null, [derived]));
    }

    [TestMethod]
    public void AScopeShouldMergeThroughTheGeneratedCode()
    {
        var outer = Internals.Create(null, [new BitButtonParams { Title = "outer", Color = BitColor.Error }]);
        var inner = Internals.Create(outer, [new BitButtonParams { Title = "inner" }]);

        var merged = (BitButtonParams)Internals.All(inner).Values.Cast<object>().Single(p => p is BitButtonParams);

        Assert.AreEqual("inner", merged.Title);
        Assert.AreEqual(BitColor.Error, merged.Color);
        Assert.IsTrue(Internals.IsCopy(merged));
    }



    // The internals of BitParamsScope and the generated code, which the tests reach through reflection.
    private static class Internals
    {
        private static readonly Type Scope = typeof(BitButton).Assembly.GetType("Bit.BlazorUI.BitParamsScope", throwOnError: true)!;

        public static readonly Type Generated = typeof(BitButton).Assembly.GetType("Bit.BlazorUI.IBitGeneratedParams", throwOnError: true)!;

        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static object? TakeSnapshot(IBitComponentParams item) => Generated.GetMethod("TakeSnapshot")!.Invoke(item, null);

        public static IBitComponentParams? MergeOver(IBitComponentParams item, IBitComponentParams? basis)
            => (IBitComponentParams?)Generated.GetMethod("MergeOver")!.Invoke(item, [basis]);

        public static IBitComponentParams MergeByReflection(IBitComponentParams? basis, IBitComponentParams overlay)
            => (IBitComponentParams)Scope.GetMethod("MergeByReflection", Static)!.Invoke(null, [basis, overlay])!;

        public static PropertyInfo[] GetCopyableProperties(Type type) => (PropertyInfo[])Scope.GetMethod("GetCopyableProperties", Static)!.Invoke(null, [type])!;

        public static PropertyInfo[] GetSnapshotProperties(Type type) => (PropertyInfo[])Scope.GetMethod("GetSnapshotProperties", Static)!.Invoke(null, [type])!;

        public static bool AreEqual(object? first, object? second) => (bool)Scope.GetMethod("AreEqual", Static)!.Invoke(null, [first, second])!;

        public static bool IsCopy(IBitComponentParams item) => (bool)Scope.GetMethod("IsCopy", Static)!.Invoke(null, [item])!;

        public static object Create(object? parent, IBitComponentParams[] parameters)
            => Scope.GetMethod("Create", Static)!.Invoke(null, [parent, parameters, false, null])!;

        public static bool IsCreatedFrom(object scope, object? parent, IBitComponentParams[] parameters)
            => (bool)Scope.GetMethod("IsCreatedFrom")!.Invoke(scope, [parent, parameters, false])!;

        public static IDictionary All(object scope) => (IDictionary)Scope.GetProperty("All")!.GetValue(scope)!;
    }

    public class DerivedButtonParams : BitButtonParams
    {
        public string? Extra { get; set; }
    }



    private static IEnumerable<Type> ParamsTypes()
    {
        return Libraries.SelectMany(a => a.GetTypes())
                        .Where(t => t.IsClass && t.IsAbstract is false && t.IsNested is false && typeof(IBitComponentParams).IsAssignableFrom(t))
                        .Select(t => t.IsGenericTypeDefinition ? Close(t) : t);

        static Type Close(Type definition)
        {
            if (definition == typeof(BitDropdownParams<,>)) return typeof(BitDropdownParams<BitDropdownItem<string>, string>);

            throw new InvalidOperationException($"{definition.Name} is generic: name the type arguments to test it with here.");
        }
    }

    private static bool AreSameValue(object? first, object? second)
    {
        if (first is IDictionary firstDictionary && second is IDictionary secondDictionary)
        {
            return firstDictionary.Count == secondDictionary.Count
                && firstDictionary.Keys.Cast<object>().All(k => secondDictionary.Contains(k) && Equals(firstDictionary[k], secondDictionary[k]));
        }

        return ReferenceEquals(first, second) || Equals(first, second);
    }

    // Two values of the type that tell apart, shared across calls so a reference type is the same object both times.
    private static readonly Dictionary<(Type, int), object?> _samples = [];

    private static bool TrySample(Type type, int variant, out object? value)
    {
        if (_samples.TryGetValue((type, variant), out value)) return value is not null;

        value = CreateSample(type, variant);
        _samples[(type, variant)] = value;

        return value is not null;
    }

    private static object? CreateSample(Type type, int variant)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return variant == 0 ? "a" : "b";
        if (underlying == typeof(bool)) return variant == 0;
        if (underlying.IsEnum)
        {
            var values = Enum.GetValues(underlying);
            if (values.Length < 2) return null;
            return values.GetValue(variant == 0 ? 0 : values.Length - 1);
        }
        if (underlying == typeof(int)) return variant + 1;
        if (underlying == typeof(uint)) return (uint)(variant + 1);
        if (underlying == typeof(long)) return (long)(variant + 1);
        if (underlying == typeof(float)) return (float)(variant + 1);
        if (underlying == typeof(double)) return (double)(variant + 1);
        if (underlying == typeof(decimal)) return (decimal)(variant + 1);
        if (underlying == typeof(TimeSpan)) return TimeSpan.FromMinutes(variant + 1);
        if (underlying == typeof(DateTime)) return new DateTime(2020, 1, variant + 1, 0, 0, 0, DateTimeKind.Utc);
        if (underlying == typeof(DateTimeOffset)) return new DateTimeOffset(2020, 1, variant + 1, 0, 0, 0, TimeSpan.Zero);
        if (underlying == typeof(CultureInfo)) return variant == 0 ? CultureInfo.InvariantCulture : new CultureInfo("fa-IR");
        if (underlying == typeof(BitIconInfo)) return new BitIconInfo(variant == 0 ? "Add" : "Remove");
        if (underlying == typeof(BitCountry)) return variant == 0 ? BitCountries.Netherlands : BitCountries.Germany;

        if (type == typeof(Dictionary<string, object>) || type == typeof(IDictionary<string, object>))
        {
            return new Dictionary<string, object> { ["data-x"] = variant == 0 ? "a" : "b" };
        }

        if (type.IsGenericType && type.GetGenericArguments() is [var element]
            && typeof(IEnumerable<>).MakeGenericType(element).IsAssignableFrom(typeof(List<>).MakeGenericType(element))
            && type.IsAssignableFrom(typeof(List<>).MakeGenericType(element))
            && TrySample(element, variant, out var item))
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
            list.Add(item);
            return list;
        }

        // A class of the packages with a string to tell two of them apart by: a ClassStyles, a Strings.
        if (underlying.IsClass && underlying.IsAbstract is false && underlying.GetConstructor(Type.EmptyTypes) is not null
            && underlying.Namespace == "Bit.BlazorUI" && typeof(IEnumerable).IsAssignableFrom(underlying) is false)
        {
            var text = underlying.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .FirstOrDefault(p => p.PropertyType == typeof(string) && p.CanWrite && p.SetMethod!.IsPublic);

            if (text is null) return null;

            var instance = Activator.CreateInstance(underlying)!;
            text.SetValue(instance, variant == 0 ? "a" : "b");
            return instance;
        }

        return null;
    }
}
