using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// A <see cref="JsonSerializerOptions"/> instance becomes read-only on first use, so customizing
/// the shared options has to swap in a fresh copy rather than mutate the live one.
/// </summary>
/// <remarks>
/// <see cref="BlexJson.Options"/> is process-wide, so these tests must never overlap with others.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class BlexJsonTests : IDisposable
{
    public void Dispose() => BlexJson.Reset();

    private sealed class UpperCaseStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => reader.GetString() ?? "";

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions o)
            => writer.WriteStringValue(value.ToUpperInvariant());
    }

    [TestMethod]
    public void Configure_AppliesToSubsequentStoreSnapshots()
    {
        // Force the default options into their read-only state first: mutating them in place
        // would throw, which is exactly what Configure exists to avoid.
        _ = new CounterStore().SerializeState();

        BlexJson.Configure(o => o.Converters.Add(new UpperCaseStringConverter()));

        var store = new CounterStore();
        store.Label = "idle";
        Assert.AreEqual("IDLE", store.SerializeState()["Label"]!.GetValue<string>());
    }

    [TestMethod]
    public void Configure_LeavesTheDefaultsIntact()
    {
        BlexJson.Configure(o => o.WriteIndented = true);

        Assert.IsTrue(BlexJson.Options.WriteIndented);
        Assert.IsTrue(BlexJson.Options.PropertyNameCaseInsensitive);
        Assert.Contains(c => c is JsonStringEnumConverter, BlexJson.Options.Converters);
    }

    [TestMethod]
    public void Configure_DoesNotMutateThePreviousInstance()
    {
        var before = BlexJson.Options;
        BlexJson.Configure(o => o.WriteIndented = true);

        Assert.AreNotSame(before, BlexJson.Options);
        Assert.IsFalse(before.WriteIndented);
    }

    [TestMethod]
    public void Configure_RejectsNull()
        => Assert.ThrowsExactly<ArgumentNullException>(() => BlexJson.Configure(null!));

    [TestMethod]
    public void Reset_RestoresTheDefaults()
    {
        BlexJson.Configure(o => o.WriteIndented = true);
        BlexJson.Reset();

        Assert.IsFalse(BlexJson.Options.WriteIndented);
    }

    [TestMethod]
    public void ASourceGeneratedResolver_DrivesGeneratedSnapshots()
    {
        // The trimming/AOT story: generated SerializeState/DeserializeState go through
        // BlexJson.Options, so pointing its TypeInfoResolver at a JsonSerializerContext is all it
        // takes to keep snapshots working in a trimmed Blazor WebAssembly publish, where the
        // reflection-based serializer cannot be relied on.
        BlexJson.Configure(o => o.TypeInfoResolver = BlexTestJsonContext.Default);

        var store = new CounterStore();
        store.Count = 3;
        store.Label = "loaded";

        var snapshot = store.SerializeState();
        Assert.AreEqual(3, snapshot["Count"]!.GetValue<int>());

        var restored = new CounterStore();
        restored.RestoreState(snapshot);
        Assert.AreEqual(3, restored.Count);
        Assert.AreEqual("loaded", restored.Label);
    }
}

/// <summary>Source-generated metadata for the types <see cref="CounterStore"/> puts in its snapshot.</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
internal sealed partial class BlexTestJsonContext : JsonSerializerContext;
