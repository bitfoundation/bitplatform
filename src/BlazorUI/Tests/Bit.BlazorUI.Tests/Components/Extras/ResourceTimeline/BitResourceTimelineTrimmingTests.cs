using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline.BitResourceTimelineTestInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Bit.BlazorUI.Extras is trimmed in a published app, and System.Text.Json reads what crosses the JS interop by
/// reflection the trimmer cannot see through the params object[] of an interop call. So every type a payload of the
/// BitResourceTimeline reaches is rooted with a [DynamicDependency] on the call that sends it - a method of the internal
/// BitResourceTimelineJsRuntimeExtensions, or a [JSInvokable] callback handing allocations back to the engine.
/// </summary>
/// <remarks>
/// <para>
/// Leaving a type out fails silently at runtime: the trimmer drops the properties nothing assigns from C#, so they are
/// never serialized and the engine quietly falls back to its defaults (measured on a trimmed publish of the original
/// component before it had any roots, BitResourceTimelineOptions kept 6 of its 36 properties). These tests turn that into
/// a failing build, walking the public property graph the way the serializer does, so a new nested model is caught
/// without updating a list.
/// </para>
/// <para>
/// An enum is the one kind of type reachable from a payload that needs no root, and is left out on purpose: the trimmer
/// keeps the enum type through the signature of the property holding it, and marks every field of an enum it keeps (the
/// fields of a value type are kept for its layout, the static literal ones too for an enum), so the names
/// JsonStringEnumConverter writes are all there. The rows the engine reports for the resource template need no root
/// either: they are read off a JsonElement rather than deserialized into a type.
/// </para>
/// <para>
/// The time converters of the library must not be rooted at all. The [JsonConverter] naming one keeps its public
/// parameterless constructor (JsonConverterAttribute annotates the type it takes), which is all the serializer creates it
/// through; rooting it whole keeps every member of its JsonConverter base as well, among them the
/// RequiresUnreferencedCode ConfigureJsonTypeInfoUsingReflection, and the trimmed publish of an app then fails with
/// IL2026 - which the trim analyzers of the library's own build do not see.
/// </para>
/// </remarks>
[TestClass]
public class BitResourceTimelineTrimmingTests
{
    private static readonly Type Extensions = Library.GetType("Bit.BlazorUI.BitResourceTimelineJsRuntimeExtensions", throwOnError: true)!;

    [TestMethod]
    public void EveryTypeAPayloadReachesShouldBeRootedOnTheCallThatSendsIt()
    {
        var missing = InteropCalls().SelectMany(call => Missing(call).Select(type => $"  {call.Name}: [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof({type.Name}))]"))
                                    .ToList();

        Assert.IsEmpty(missing, "These types cross the JS interop through these calls but are not rooted on them, so a trimmed app would " +
                                "silently drop their properties:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [TestMethod]
    public void EveryTypeACallbackHandsBackShouldBeRootedOnTheCallback()
    {
        // _OnAllocationsCopying and _OnAllocationCreating hand back the same allocation graph setData sends, so they root
        // the same types the calls sending it root.
        var missing = Callbacks().SelectMany(callback => Missing(callback).Select(type => $"  {callback.Name}: [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof({type.Name}))]"))
                                 .ToList();

        Assert.IsEmpty(missing, "These types are handed back to the engine by these callbacks but are not rooted on them, so a trimmed app " +
                                "would silently drop their properties:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [TestMethod]
    public void EveryRootShouldBeAWholeTypeOfThisAssembly()
    {
        // A root naming its type by a string (renamed or misspelled), naming only some of its members, or naming a type of
        // another assembly keeps nothing here, and the trimmer does not complain, so the attribute would look right while
        // doing nothing.
        var wrong = InteropCalls().Concat(Callbacks())
                                  .SelectMany(method => method.GetCustomAttributes<DynamicDependencyAttribute>().Select(root => (Method: method, Root: root)))
                                  .Where(x => x.Root.Type is null
                                              || x.Root.MemberTypes != DynamicallyAccessedMemberTypes.All
                                              || x.Root.MemberSignature is not null
                                              || x.Root.Type.Assembly != Library)
                                  .Select(x => $"  {x.Method.Name}: {x.Root.Type?.FullName ?? x.Root.TypeName} ({x.Root.MemberTypes})")
                                  .ToList();

        Assert.IsEmpty(wrong, string.Join(Environment.NewLine, wrong));
    }

    [TestMethod]
    public void NoConverterShouldBeRootedAndEveryOneShouldBeKeptByItsAttribute()
    {
        var converters = InteropCalls().Concat(Callbacks()).SelectMany(method => Walk(method).Converters).ToHashSet();

        Assert.HasCount(3, converters, "The scan did not find the three time converters of the payloads.");

        var rooted = InteropCalls().Concat(Callbacks())
                                   .SelectMany(method => Rooted(method).Where(converters.Contains).Select(type => $"  {method.Name}: {type.Name}"))
                                   .ToList();

        Assert.IsEmpty(rooted, "A converter rooted whole keeps the RequiresUnreferencedCode members of JsonConverter, which fails the " +
                               "trimmed publish of an app (IL2026):" + Environment.NewLine + string.Join(Environment.NewLine, rooted));

        // What keeps them instead: the [JsonConverter] naming each, whose type argument the trimmer is told to keep the
        // public parameterless constructor of - which the converter has.
        var converterType = typeof(JsonConverterAttribute).GetConstructor([typeof(Type)])!.GetParameters().Single();
        var kept = converterType.GetCustomAttribute<DynamicallyAccessedMembersAttribute>()?.MemberTypes ?? DynamicallyAccessedMemberTypes.None;

        Assert.IsTrue(kept.HasFlag(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor), "JsonConverterAttribute no longer keeps the constructor of the converter it names.");

        foreach (var converter in converters)
        {
            Assert.IsNotNull(converter.GetConstructor(Type.EmptyTypes), $"{converter.Name} has no public parameterless constructor for the serializer to create it through.");
        }
    }

    [TestMethod]
    public void EveryRootShouldBeReachableFromTheCallItIsOn()
    {
        // A root no payload of its call reaches any more is left over from a type that moved or went away.
        var stale = InteropCalls().Concat(Callbacks())
                                  .SelectMany(method => Rooted(method).Except(Required(method)).Select(type => $"  {method.Name}: {type.Name}"))
                                  .ToList();

        Assert.IsEmpty(stale, string.Join(Environment.NewLine, stale));
    }

    [TestMethod]
    public void TheScanShouldFindEveryPayloadOfTheTimeline()
    {
        var reached = InteropCalls().Concat(Callbacks()).SelectMany(Reach).ToHashSet();

        // What the engine is configured with, the data it is given and the layout it answers with; the scan is broken
        // when it does not find them.
        Type[] expected =
        [
            typeof(BitResourceTimelineOptions),
            typeof(BitResourceTimelineColors),
            typeof(BitResourceTimelineSurface),
            typeof(BitResourceTimelineSurfaceBand),
            typeof(BitResourceTimelineColumnStripes),
            typeof(BitResourceTimelineRowStripes),
            typeof(BitResourceTimelineAxisLines),
            typeof(BitResourceTimelineTexts),
            typeof(BitResourceTimelineResource),
            typeof(BitResourceTimelineAllocation),
            typeof(BitResourceTimelineEdgeBar),
            typeof(BitResourceTimelineBarIcon),
            typeof(BitResourceTimelineBarSection),
            LayoutType,
            ConverterType("BitResourceTimelineUnixMillisecondsConverter"),
            ConverterType("BitResourceTimelineNullableUnixMillisecondsConverter"),
            ConverterType("BitResourceTimelineMillisecondsTimeSpanConverter"),
        ];

        var notFound = expected.Where(type => reached.Contains(type) is false).Select(type => type.Name).ToList();

        Assert.IsEmpty(notFound, "The scan did not reach: " + string.Join(", ", notFound));

        // Only the data of the timeline crosses the boundary: the arguments of its events travel in process.
        Assert.IsFalse(reached.Contains(typeof(BitResourceTimelineConfig)), "The Config is sent in its parts, never as a whole.");
        Assert.IsFalse(reached.Contains(typeof(BitResourceTimelinePointerArgs)));
        Assert.IsFalse(reached.Contains(typeof(BitResourceTimelineRowContext)));
        Assert.IsFalse(reached.Contains(typeof(BitResourceTimelineWindow)));
    }

    [TestMethod]
    public void TheEnumsAPayloadReachesShouldBeKeptWithTheirFieldsAndWrittenByName()
    {
        var enums = InteropCalls().Concat(Callbacks()).SelectMany(method => Walk(method).Enums).Distinct().OrderBy(t => t.Name, StringComparer.Ordinal).ToList();

        // A new enum reaching a payload is a decision: it needs no root (see the remarks of the class) only while it is
        // reached through the type of a property, and the engine reads it only while it is written by its name.
        CollectionAssert.AreEqual(new[]
        {
            typeof(BitResourceTimelineBarIconPosition),
            typeof(BitResourceTimelineBarSectionPosition),
            typeof(BitResourceTimelineEmptyDragAction),
            typeof(BitResourceTimelineRendererType),
            typeof(BitResourceTimelineStripeAlign),
            typeof(BitResourceTimelineSurfaceCombine),
            typeof(BitResourceTimelineSurfaceUnit),
        }, enums);

        foreach (var type in enums)
        {
            Assert.AreEqual(typeof(JsonStringEnumConverter<>).MakeGenericType(type), type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType,
                            $"{type.Name} is not written by its name.");
        }
    }

    [TestMethod]
    public void TheRowsTheEngineReportsShouldBeReadWithoutReflection()
    {
        var rowsChanged = typeof(BitResourceTimeline).GetMethod(nameof(BitResourceTimeline._OnResourceRowsChanged))!;

        Assert.AreEqual(typeof(JsonElement), rowsChanged.GetParameters().Single().ParameterType);

        var row = Library.GetType("Bit.BlazorUI.BitResourceTimelineResourceRow", throwOnError: true)!;

        Assert.IsFalse(InteropCalls().Concat(Callbacks()).SelectMany(Reach).Contains(row), "The rows are deserialized into a type, which a trimmed app would have to root.");
    }



    private static IEnumerable<MethodInfo> InteropCalls()
    {
        var calls = Extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.IsGreaterThanOrEqualTo(30, calls.Length, "The calls of BitResourceTimelineJsRuntimeExtensions were not found; the scan is broken.");

        return calls;
    }

    private static IEnumerable<MethodInfo> Callbacks()
    {
        var callbacks = typeof(BitResourceTimeline).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                   .Where(m => m.IsDefined(typeof(JSInvokableAttribute)))
                                                   .ToArray();

        Assert.IsGreaterThanOrEqualTo(15, callbacks.Length, "The [JSInvokable] callbacks of BitResourceTimeline were not found; the scan is broken.");

        return callbacks;
    }

    private static Type ConverterType(string name) => Library.GetType("Bit.BlazorUI." + name, throwOnError: true)!;

    private static HashSet<Type> Rooted(MethodInfo method) =>
        method.GetCustomAttributes<DynamicDependencyAttribute>().Where(a => a.Type is not null).Select(a => a.Type!).ToHashSet();

    // The classes and structs of the library a call carries across, which are what has to be rooted on it (its converters
    // are kept by their attributes instead, see the remarks of the class).
    private static HashSet<Type> Required(MethodInfo method) => Walk(method).Types;

    private static IEnumerable<Type> Missing(MethodInfo method) => Required(method).Except(Rooted(method)).OrderBy(t => t.Name, StringComparer.Ordinal);

    private static IEnumerable<Type> Reach(MethodInfo method)
    {
        var (types, converters, enums) = Walk(method);

        return types.Concat(converters).Concat(enums);
    }

    // What crosses the boundary through a call: its arguments - less the runtime it is called on and the reference the
    // engine calls back through, which the interop marshals as a handle - and its result.
    private static IEnumerable<Type> Payloads(MethodInfo method)
    {
        foreach (var parameter in method.GetParameters())
        {
            var type = parameter.ParameterType;

            if (type == typeof(IJSRuntime) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DotNetObjectReference<>))) continue;

            yield return type;
        }

        yield return method.ReturnType;
    }

    // Walks the public property graph of the payloads of a call, keeping the types the library declares itself: the
    // types of other assemblies (string, DateTimeOffset, JsonElement, ElementReference) are their own concern, as the
    // framework roots its own serializable surface.
    private static (HashSet<Type> Types, HashSet<Type> Converters, HashSet<Type> Enums) Walk(MethodInfo method)
    {
        var types = new HashSet<Type>();
        var converters = new HashSet<Type>();
        var enums = new HashSet<Type>();
        var queue = new Queue<Type>(Payloads(method));

        while (queue.Count > 0)
        {
            foreach (var type in Unwrap(queue.Dequeue()))
            {
                if (type.Assembly != Library || types.Contains(type) || enums.Contains(type)) continue;

                AddConverter(type.GetCustomAttribute<JsonConverterAttribute>());

                if (type.IsEnum)
                {
                    enums.Add(type);
                    continue;
                }

                types.Add(type);

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.GetCustomAttribute<JsonIgnoreAttribute>() is { Condition: JsonIgnoreCondition.Always }) continue;

                    AddConverter(property.GetCustomAttribute<JsonConverterAttribute>());
                    queue.Enqueue(property.PropertyType);
                }
            }
        }

        return (types, converters, enums);

        void AddConverter(JsonConverterAttribute? attribute)
        {
            // A converter of the framework (JsonStringEnumConverter<T>) is kept by the [JsonConverter] naming it.
            if (attribute?.ConverterType is { } converter && converter.Assembly == Library)
            {
                converters.Add(converter);
            }
        }
    }

    // Reduces a type to the types actually serialized: unwraps nullables, arrays, tasks and the element types of the
    // collections and dictionaries.
    private static IEnumerable<Type> Unwrap(Type type)
    {
        if (type.IsArray) return Unwrap(type.GetElementType()!);

        if (type.IsGenericType)
        {
            return Nullable.GetUnderlyingType(type) is { } underlying ? Unwrap(underlying) : type.GetGenericArguments().SelectMany(Unwrap);
        }

        return [type];
    }
}
