using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
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

    private static readonly Assembly Library = typeof(BitBdnd).Assembly;

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value);

    [TestMethod]
    public void EveryTypeThatCrossesTheJsBoundaryIsRooted()
    {
        var crossing = CrossingTypes();
        var roots = Roots();

        // the discovery itself finds what it is meant to - also where a component calls into JS on its own: a
        // draggable reads the rectangle of its drag start off its registration
        Assert.Contains(typeof(BdndDragSourceOptions), crossing);
        Assert.Contains(typeof(BdndDropTargetOptions), crossing);
        Assert.Contains(typeof(BdndDomRect), crossing);
        Assert.Contains(typeof(BdndNativeFile), crossing);
        Assert.Contains(typeof(BdndDomRect), JsCalls().Where(c => Owner(c.Caller) == typeof(BdndDraggable<>)).SelectMany(c => c.Callee.GetGenericArguments()));

        string[] missing = [.. crossing.Where(t => roots.ContainsKey(t) is false).Select(t => t.Name)];
        Assert.IsEmpty(missing, "not rooted by BitBdnd.PreserveInteropContracts: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void EveryCrossingTypeKeepsItsPropertiesAndTheConstructorTheSerializerUses()
    {
        var roots = Roots();
        foreach (var type in CrossingTypes().Where(roots.ContainsKey))
        {
            var (members, signatures) = roots[type];
            Assert.IsTrue(members.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties) || type.GetProperties().All(p => signatures.Contains(p.Name)),
                $"{type.Name} loses its properties");

            // The serializer creates a struct without a constructor, and a class through its public parameterless one.
            // Without that it goes through a parameterized constructor - even to serialize - and matches its parameters to
            // the properties by name, names the trimmer strips from a constructor it does not keep for reflection.
            if (type.IsValueType) continue;
            Assert.IsNotNull(type.GetConstructor(Type.EmptyTypes),
                $"{type.Name} has no public parameterless constructor: give it one (a positional record: init-only properties instead)");
            Assert.IsTrue(members.HasFlag(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor) || signatures.Contains("#ctor") || signatures.Contains("#ctor()"),
                $"{type.Name} loses its parameterless constructor");
        }
    }

    [TestMethod]
    public void OnlyTheInteropLayerPassesArgumentsToJs()
    {
        // The arguments of a call go to JS as an object[], so their types are in no signature the discovery can read.
        // BdndInterop's parameters name them, and so do the options of a BdndJsRegistration; a call made anywhere else
        // has to pass none, or what it sends would go unchecked.
        var calls = JsCalls().ToList();
        string[] unseen = [.. calls
            .Where(c => c.PassesArguments && Owner(c.Caller) != typeof(BdndInterop) && Owner(c.Caller) != typeof(BdndJsRegistration<>))
            .Select(c => $"{Owner(c.Caller).Name} -> {c.Callee.Name}")
            .Distinct()];

        // the scan tells the two apart
        Assert.IsTrue(calls.Any(c => c.PassesArguments && Owner(c.Caller) == typeof(BdndInterop)), "the calls BdndInterop makes pass arguments");
        Assert.IsTrue(calls.Any(c => c.PassesArguments is false && Owner(c.Caller) == typeof(BdndDraggable<>)), "a draggable's getDragStartRect passes none");
        Assert.IsEmpty(unseen, "pass the arguments through a BdndInterop method whose parameters name their types: " + string.Join(", ", unseen));
    }

    [TestMethod]
    public void TheRootsAreReachedWithoutTheRegistration()
    {
        // An app that lazy loads Bit.Bdnd on WebAssembly never calls AddBitBdndServices, so the components have to reach
        // the roots themselves: through GetBdndInterop, on their way to the interop.
        var calls = Calls().ToList();

        Assert.IsTrue(calls.Any(c => c.Caller.Name == nameof(BitBdnd.GetBdndInterop) && c.Callee.Name == "PreserveInteropContracts"),
            "GetBdndInterop calls PreserveInteropContracts");
        foreach (var component in new[] { typeof(BdndDraggable<>), typeof(BdndDropTarget<>) })
            Assert.IsTrue(calls.Any(c => Owner(c.Caller) == component && c.Callee.Name == nameof(BitBdnd.GetBdndInterop)),
                $"{component.Name} takes its interop from GetBdndInterop");
    }

    // What PreserveInteropContracts keeps of each type, whichever DynamicDependency overload names it: the member types,
    // and the members a signature names (a property by its name, a constructor as #ctor(...)).
    private static Dictionary<Type, (DynamicallyAccessedMemberTypes Members, HashSet<string> Signatures)> Roots()
    {
        var method = typeof(BitBdnd).GetMethod("PreserveInteropContracts", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "BitBdnd.PreserveInteropContracts is gone");

        var roots = new Dictionary<Type, (DynamicallyAccessedMemberTypes Members, HashSet<string> Signatures)>();
        foreach (var root in method.GetCustomAttributes<DynamicDependencyAttribute>())
        {
            // a type name is written the way a documentation comment ID writes it (Outer.Nested, Generic`1), and a bare
            // signature names a member of the method's own type
            var type = root.Type
                ?? (root.TypeName is { } name
                    ? Assembly.Load(root.AssemblyName!).GetTypes().SingleOrDefault(t => t.FullName?.Replace('+', '.') == name)
                    : method.DeclaringType);
            Assert.IsNotNull(type, $"a DynamicDependency names {root.TypeName} in {root.AssemblyName}, which does not exist");

            var (members, signatures) = roots.TryGetValue(type, out var kept) ? kept : (default, new HashSet<string>(StringComparer.Ordinal));
            if (root.MemberSignature is { } signature) signatures.Add(signature);
            roots[type] = (members | root.MemberTypes, signatures);
        }
        return roots;
    }

    // The library's types System.Text.Json (de)serializes, followed through arrays, by-refs, nullables, tasks,
    // collections and properties, as the serializer follows them:
    //  - what BdndInterop sends to the module and reads back,
    //  - what any call into JS reads back (InvokeAsync<T>, Invoke<T>), wherever the library makes it,
    //  - the options of every BdndJsRegistration<TOptions> the library uses, which it registers and pushes to JS (update),
    //  - and what the module passes to the [JSInvokable] callbacks.
    // The arguments any other call passes are not seen here - OnlyTheInteropLayerPassesArgumentsToJs keeps there being none.
    private static HashSet<Type> CrossingTypes()
    {
        var calls = Calls().ToList();
        var callbacks = Library.GetTypes()
            .SelectMany(t => t.GetMethods(Declared))
            .Where(m => m.IsDefined(typeof(JSInvokableAttribute)));
        var registrationOptions = calls
            .Select(c => c.Callee.DeclaringType)
            .Where(t => t is { IsGenericType: true } && t.GetGenericTypeDefinition() == typeof(BdndJsRegistration<>))
            .SelectMany(t => t!.GetGenericArguments());

        var found = new HashSet<Type>();
        foreach (var method in typeof(BdndInterop).GetMethods(Declared).Concat(callbacks))
        {
            Collect(method.ReturnType, found);
            foreach (var parameter in method.GetParameters()) Collect(parameter.ParameterType, found);
        }
        foreach (var type in JsCalls(calls).SelectMany(c => c.Callee.GetGenericArguments()).Concat(registrationOptions))
            Collect(type, found);
        return found;
    }

    private static void Collect(Type type, HashSet<Type> found)
    {
        if (type.HasElementType)
        {
            // an array, or the by-ref of an in/ref/out parameter
            Collect(type.GetElementType()!, found);
            return;
        }
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments()) Collect(argument, found);
            // what a root names (typeof(X<>)), and keeps for every instantiation
            type = type.GetGenericTypeDefinition();
        }
        // an enum needs no root: the trimmer keeps the fields of every enum it keeps, and the serializer reads nothing else
        if (type.IsGenericParameter || type.IsEnum || type.Assembly != Library || found.Add(type) is false) return;

        foreach (var property in type.GetProperties()) Collect(property.PropertyType, found);
    }

    // The calls into JS - the methods of IJSRuntime, IJSObjectReference, their in-process forms and their extensions, but
    // not DotNetObjectReference, which hands JS a reference rather than a serialized value - and whether each passes
    // arguments: for an omitted params array the compiler passes the Array.Empty<object>() it calls just before.
    private static IEnumerable<(MethodBase Caller, MethodInfo Callee, bool PassesArguments)> JsCalls(
        IEnumerable<(MethodBase Caller, MethodBase Callee, MethodBase? Previous)>? calls = null) =>
        from call in calls ?? Calls()
        where call.Callee is MethodInfo
            && call.Callee.DeclaringType is { } type
            && type.Assembly == typeof(IJSRuntime).Assembly
            && type != typeof(DotNetObjectReference)
            && (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DotNetObjectReference<>)) is false
        let passesArguments = call.Callee.GetParameters().Any(p => p.ParameterType == typeof(object[])) && IsArrayEmpty(call.Previous) is false
        select (call.Caller, (MethodInfo)call.Callee, passesArguments);

    private static bool IsArrayEmpty(MethodBase? method) => method is MethodInfo { Name: nameof(Array.Empty) } && method.DeclaringType == typeof(Array);

    // Every method the library's code calls, creates an instance through or takes a delegate to - compiler-generated
    // state machines and closures included, which is where an async method's code is - with the method the instruction
    // just before called, if it was a call.
    private static IEnumerable<(MethodBase Caller, MethodBase Callee, MethodBase? Previous)> Calls()
    {
        foreach (var type in Library.GetTypes())
        {
            var typeArguments = type.IsGenericType ? type.GetGenericArguments() : null;
            foreach (var caller in type.GetMethods(Declared).Concat<MethodBase>(type.GetConstructors(Declared)))
            {
                if (caller.GetMethodBody()?.GetILAsByteArray() is not { } il) continue;
                var methodArguments = caller.IsGenericMethod ? caller.GetGenericArguments() : null;

                MethodBase? previous = null;
                for (var at = 0; at < il.Length;)
                {
                    var code = il[at] == 0xFE ? OpCodesByValue[unchecked((short)(0xFE00 | il[at + 1]))] : OpCodesByValue[il[at]];
                    at += code.Size;

                    var callee = code.OperandType == OperandType.InlineMethod
                        ? type.Module.ResolveMethod(BitConverter.ToInt32(il, at), typeArguments, methodArguments)
                        : null;
                    if (callee is not null) yield return (caller, callee, previous);
                    previous = callee;

                    at += code.OperandType switch
                    {
                        OperandType.InlineNone => 0,
                        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                        OperandType.InlineVar => 2,
                        OperandType.InlineI8 or OperandType.InlineR => 8,
                        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, at),
                        _ => 4,
                    };
                }
            }
        }
    }

    // The type a method's code belongs to, for a state machine or a closure the one the compiler nested it in.
    private static Type Owner(MethodBase method)
    {
        var type = method.DeclaringType!;
        while (type.DeclaringType is { } outer) type = outer;
        return type;
    }
}
