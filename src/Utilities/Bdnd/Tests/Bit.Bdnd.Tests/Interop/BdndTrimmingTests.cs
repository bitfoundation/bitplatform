using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Interop;

/// <summary>
/// What keeps the types that cross the JS interop boundary serializable once a published app has been trimmed.
/// System.Text.Json reaches their members through reflection the trimmer cannot follow, so
/// <c>BitBdnd.PreserveInteropContracts</c> roots them. A member it misses is gone only from a published WebAssembly
/// app, where every registration or callback that carries the type then throws, so these tests check the roots.
/// </summary>
[TestClass]
public class BdndTrimmingTests
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [TestMethod]
    public void EveryTypeThatCrossesTheJsBoundaryIsRooted()
    {
        var crossing = CrossingTypes();
        var rooted = Roots().Select(r => r.Type).ToHashSet();

        // the discovery itself finds what it is meant to
        Assert.Contains(typeof(BdndDragSourceOptions), crossing);
        Assert.Contains(typeof(BdndDropTargetOptions), crossing);
        Assert.Contains(typeof(BdndNativeFile), crossing);
        string[] missing = [.. crossing.Where(t => rooted.Contains(t) is false).Select(t => t.Name)];
        Assert.IsEmpty(missing, "not rooted by BitBdnd.PreserveInteropContracts: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void EveryRootedTypeKeepsItsPropertiesAndTheConstructorTheSerializerUses()
    {
        foreach (var (type, members) in Roots())
        {
            Assert.IsTrue(members.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties), $"{type.Name} loses its properties");

            // Without a parameterless constructor (a positional record) the serializer goes through the parameterized
            // one, even to serialize, and needs its parameter names - which only a constructor kept for reflection has.
            var constructor = type.GetConstructor(Type.EmptyTypes) is null
                ? DynamicallyAccessedMemberTypes.PublicConstructors
                : DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
            Assert.IsTrue(members.HasFlag(constructor), $"{type.Name} loses the constructor System.Text.Json uses: it needs {constructor}");
        }
    }

    private static IEnumerable<(Type Type, DynamicallyAccessedMemberTypes Members)> Roots()
    {
        var method = typeof(BitBdnd).GetMethod("PreserveInteropContracts", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "BitBdnd.PreserveInteropContracts is gone");
        return method.GetCustomAttributes<DynamicDependencyAttribute>().Select(a => (a.Type!, a.MemberTypes));
    }

    // The library's types System.Text.Json (de)serializes: what BdndInterop sends to the module and reads back, and
    // what the module passes to the [JSInvokable] callbacks - followed through arrays, nullables, tasks, collections
    // and properties, as the serializer follows them. The calls made on a registration (update, getDragStartRect)
    // carry the same options and BdndDomRect.
    private static HashSet<Type> CrossingTypes()
    {
        var callbacks = typeof(BitBdnd).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(Declared))
            .Where(m => m.IsDefined(typeof(JSInvokableAttribute)));

        var found = new HashSet<Type>();
        foreach (var method in typeof(BdndInterop).GetMethods(Declared).Concat(callbacks))
        {
            Collect(method.ReturnType, found);
            foreach (var parameter in method.GetParameters()) Collect(parameter.ParameterType, found);
        }
        return found;
    }

    private static void Collect(Type type, HashSet<Type> found)
    {
        if (type.IsArray)
        {
            Collect(type.GetElementType()!, found);
            return;
        }
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments()) Collect(argument, found);
        }
        if (type.IsGenericParameter || type.Assembly != typeof(BitBdnd).Assembly || found.Add(type) is false) return;

        foreach (var property in type.GetProperties()) Collect(property.PropertyType, found);
    }
}
