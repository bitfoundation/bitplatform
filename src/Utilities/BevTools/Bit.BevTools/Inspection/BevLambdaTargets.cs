using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

[assembly: MetadataUpdateHandler(typeof(Bit.BevTools.BevLambdaTargets))]

namespace Bit.BevTools;

/// <summary>
/// What an inline lambda handler calls, read from its IL: <c>@onclick="() => Load(4000)"</c> is <c>Orders.Load</c>,
/// which the compiled name <c>&lt;BuildRenderTree&gt;b__0_1</c> does not say. Only an answer the IL proves is given:
/// the lambda (or its async state machine) calls exactly one method of application code - framework calls and
/// property or event accessors aside - and it is a method of the component's own types. A second application call (a
/// service, a local function, a base class elsewhere), none, a compiled name that more than one lambda of the type and
/// its base types carries, or IL that cannot be read: null, and the caller keeps "lambda in Orders.BuildRenderTree".
/// </summary>
internal static class BevLambdaTargets
{
    private const int MaxCached = 2000;

    private static readonly ConcurrentDictionary<MethodInfo, MethodInfo?> Targets = new();

    private static readonly ConcurrentDictionary<(string Type, string Method), string?> Titles = new();

    private static readonly Lazy<(OpCode[] OneByte, OpCode[] TwoByte)?> OpCodeTable = new(BuildOpCodeTable);

    /// <summary>
    /// Hot reload: an edited lambda keeps its compiled name while its body changes, so every answer is read again.
    /// Called by the runtime through <see cref="MetadataUpdateHandlerAttribute"/>.
    /// </summary>
    internal static int CachedCount => Targets.Count + Titles.Count;

    internal static void ClearCache(Type[]? updatedTypes)
    {
        Targets.Clear();
        Titles.Clear();
    }

    /// <summary>The one component method <paramref name="lambda"/> calls, or null.</summary>
    public static MethodInfo? CalledMethod(MethodInfo lambda)
    {
        // Only compiled lambdas: a method group is already its own name.
        if (!lambda.Name.StartsWith('<'))
        {
            return null;
        }

        if (Targets.TryGetValue(lambda, out var known))
        {
            return known;
        }

        MethodInfo? target = null;
        try
        {
            target = Find(lambda);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Unreadable IL is no answer, not an error.
        }

        if (Targets.Count < MaxCached)
        {
            Targets[lambda] = target;
        }

        return target;
    }

    /// <summary>
    /// The handler part of a UI event's title for the method name the framework reports: "Orders.Load (lambda)" or
    /// "lambda in Orders.BuildRenderTree" for a compiled lambda name; null for an ordinary method name.
    /// </summary>
    public static string? DescribeEventHandler(string componentTypeName, string methodName)
    {
        if (!methodName.StartsWith('<'))
        {
            return null;
        }

        if (Titles.TryGetValue((componentTypeName, methodName), out var cached))
        {
            return cached;
        }

        var close = methodName.IndexOf('>');
        var outer = close > 1 ? methodName[1..close] : "?";
        string title = "lambda in " + ActivityObserver.Shorten(componentTypeName) + "." + outer;
        try
        {
            if (FindType(componentTypeName) is { } type)
            {
                var shortType = TypeNames.Short(type);
                title = FindLambda(type, methodName) is { } lambda && CalledMethod(lambda) is { } target
                    ? shortType + "." + target.Name + " (lambda)"
                    : "lambda in " + shortType + "." + outer;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
        }

        if (Titles.Count < MaxCached)
        {
            Titles[(componentTypeName, methodName)] = title;
        }

        return title;
    }

    private static Type? FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && assembly.GetType(fullName, throwOnError: false) is { } type)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// The lambda the framework names: compiled into the component (it uses <c>this</c>) or into a nested closure class,
    /// of the component or of a base class (whose lambdas run under the derived type's name). A compiled name is not
    /// unique - closure classes number their lambdas per class, and a base class has its own - so a name more than one
    /// method carries is no answer.
    /// </summary>
    private static MethodInfo? FindLambda(Type component, string name)
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        MethodInfo? found = null;
        var pending = new Stack<Type>();
        for (var type = component; type is not null && type != typeof(object); type = type.BaseType)
        {
            pending.Push(type);
        }

        while (pending.TryPop(out var type))
        {
            foreach (var method in type.GetMethods(all))
            {
                if (method.Name == name)
                {
                    if (found is not null)
                    {
                        return null;
                    }

                    found = method;
                }
            }

            foreach (var nested in type.GetNestedTypes(BindingFlags.NonPublic))
            {
                if (nested.IsDefined(typeof(CompilerGeneratedAttribute), false))
                {
                    pending.Push(nested);
                }
            }
        }

        return found;
    }

    private static MethodInfo? Find(MethodInfo lambda)
    {
        var owner = lambda.DeclaringType;
        while (owner is not null && owner.IsDefined(typeof(CompilerGeneratedAttribute), false) && owner.DeclaringType is not null)
        {
            owner = owner.DeclaringType;
        }

        if (owner is null)
        {
            return null;
        }

        // An async lambda's body is its state machine's MoveNext.
        var body = lambda.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ?? (MethodBase)lambda;
        var applicationCalls = new HashSet<MethodBase>();
        foreach (var called in Calls(body))
        {
            // Framework calls (awaiters, builders, StateHasChanged, collections) and accessors say nothing about what
            // the handler does; every other call is application code, and a second one makes the name a guess.
            if (called.IsSpecialName || called.DeclaringType is not { } declaring || IsFramework(declaring.Assembly))
            {
                continue;
            }

            applicationCalls.Add(called);
            if (applicationCalls.Count > 1)
            {
                return null;
            }
        }

        return applicationCalls.SingleOrDefault() is MethodInfo method
            && !method.Name.StartsWith('<')
            && method.DeclaringType is { } owning
            && !owning.IsDefined(typeof(CompilerGeneratedAttribute), false)
            && owning.IsAssignableFrom(owner)
            ? method
            : null;
    }

    private static bool IsFramework(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? "";
        return name is "mscorlib" or "netstandard" or "System"
            || name.StartsWith("System.", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.", StringComparison.Ordinal);
    }

    /// <summary>The methods a body calls: the operands of call and callvirt, decoded with the opcode table.</summary>
    private static IEnumerable<MethodBase> Calls(MethodBase body)
    {
        var il = body.GetMethodBody()?.GetILAsByteArray();
        if (il is null || OpCodeTable.Value is not { } table)
        {
            yield break;
        }

        var typeArgs = body.DeclaringType is { IsGenericType: true } t ? t.GetGenericArguments() : null;
        var methodArgs = body.IsGenericMethod ? body.GetGenericArguments() : null;
        var position = 0;
        while (position < il.Length)
        {
            OpCode code;
            var value = il[position++];
            if (value == 0xFE)
            {
                if (position >= il.Length)
                {
                    yield break;
                }

                code = table.TwoByte[il[position++]];
            }
            else
            {
                code = table.OneByte[value];
            }

            var operandSize = code.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => position + 4 <= il.Length ? 4 + 4 * BitConverter.ToInt32(il, position) : il.Length,
                _ => 4,
            };
            if (code.OperandType == OperandType.InlineMethod && position + 4 <= il.Length && (code == OpCodes.Call || code == OpCodes.Callvirt))
            {
                MethodBase? called = null;
                try
                {
                    called = body.Module.ResolveMethod(BitConverter.ToInt32(il, position), typeArgs, methodArgs);
                }
                catch (Exception ex) when (ex is ArgumentException or BadImageFormatException or TypeLoadException or MissingMethodException)
                {
                }

                if (called is not null)
                {
                    yield return called;
                }
            }

            position += operandSize;
        }
    }

    private static (OpCode[] OneByte, OpCode[] TwoByte)? BuildOpCodeTable()
    {
        var oneByte = new OpCode[256];
        var twoByte = new OpCode[256];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode code)
            {
                var value = (ushort)code.Value;
                if (code.Size == 1)
                {
                    oneByte[value] = code;
                }
                else
                {
                    twoByte[value & 0xFF] = code;
                }
            }
        }

        return (oneByte, twoByte);
    }
}
