using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Bit.BlazorUI.Legacy;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// A component that disposes its DotNetObjectReference through DisposeAfterAsync keeps it alive until its JS teardown
/// has returned, so a call its script sent in the meantime reaches the component while it is being disposed instead
/// of being refused on the JS side. Each of its [JSInvokable] callbacks has to return early on IsDisposing (or on the
/// disposal flag of its own, for one that is not a BitComponentBase); one that does not raises its events, writes its
/// two-way bound parameters and calls into JS for a component that is gone. Forgetting it breaks nothing a test of the
/// callback itself would see, so the guard is pinned here for every callback of every such component.
/// </summary>
/// <remarks>
/// The check reads the IL: a callback is guarded when it, or the state machine of an async one, reads the disposal
/// flag - directly or through a property getter of the component (BitMap's Gone) - and needs no guard when it never
/// touches the component at all (the swipe callbacks that only complete). It does not check that the flag is read
/// first, which a review of the callback still has to.
/// </remarks>
[TestClass]
public sealed class BitJsInvokableDisposalContractTests
{
    private static readonly Assembly[] Packages =
    [
        typeof(BitButton).Assembly, typeof(BitMap<>).Assembly, typeof(BitAiChat).Assembly, typeof(BitMarkdownEditorLegacy).Assembly
    ];

    private static readonly FieldInfo IsDisposing = typeof(BitComponentBase).GetField("IsDisposing", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo DisposeAfterAsync = typeof(BitButton).Assembly
                                                                           .GetType("Bit.BlazorUI.DotNetObjectReferenceExtensions", throwOnError: true)!
                                                                           .GetMethod("DisposeAfterAsync", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                                                                     .Select(f => (OpCode)f.GetValue(null)!)
                                                                                     .ToDictionary(o => o.Value);

    [TestMethod]
    public void EveryJsInvokableOfAComponentThatKeepsItsReferenceThroughItsTeardownShouldIgnoreACallWhileItIsDisposed()
    {
        var components = FindComponentsDisposingAfterTheirTeardown().ToArray();

        Assert.IsGreaterThanOrEqualTo(16, components.Length, "Found fewer components than the ones known to dispose their reference through DisposeAfterAsync; the scan is broken.");

        var unguarded = components.SelectMany(type => JsInvokables(type).Where(m => TouchesTheComponent(m) && IsGuarded(m, type) is false)
                                                                        .Select(m => $"{type.Name}.{m.Name}"))
                                  .ToArray();

        Assert.IsEmpty(unguarded, "These callbacks belong to components whose reference outlives the start of their disposal, so a call " +
                                  "sent before the teardown reaches the script runs on a component that is going away. Each has to " +
                                  "return early on IsDisposing first: " + string.Join(", ", unguarded));
    }

    [TestMethod]
    [DataRow(typeof(BitButton))]
    [DataRow(typeof(BitMenuButton<>))]
    [DataRow(typeof(_BitMenuButtonItem<>))]
    [DataRow(typeof(BitDateRangePicker))]
    [DataRow(typeof(BitSearchBox))]
    [DataRow(typeof(BitTextField))]
    [DataRow(typeof(BitSticky))]
    [DataRow(typeof(BitInfiniteScrolling<>))]
    [DataRow(typeof(BitMap<>))]
    [DataRow(typeof(BitMarkdownEditor))]
    [DataRow(typeof(BitPdfViewer))]
    [DataRow(typeof(BitRichTextEditor))]
    [DataRow(typeof(BitMarkdownEditorLegacy))]
    public void TheScanShouldFindTheKnownComponents(Type component)
    {
        Assert.Contains(component, FindComponentsDisposingAfterTheirTeardown(), $"{component.Name} disposes its reference through DisposeAfterAsync but the scan did not find it.");
    }

    [TestMethod]
    [DataRow(nameof(Probe.Unguarded), false)]
    [DataRow(nameof(Probe.UnguardedAsync), false)]
    [DataRow(nameof(Probe.Guarded), true)]
    [DataRow(nameof(Probe.GuardedAsync), true)]
    [DataRow(nameof(Probe.GuardedByAProperty), true)]
    [DataRow(nameof(Probe.GuardedByAnExpression), true)]
    public void TheScanShouldTellAGuardedCallbackFromAnUnguardedOne(string name, bool guarded)
    {
        var method = typeof(Probe).GetMethod(name)!;

        Assert.IsTrue(TouchesTheComponent(method));
        Assert.AreEqual(guarded, IsGuarded(method, typeof(Probe)));
    }

    [TestMethod]
    public void TheScanShouldLeaveACallbackThatNeverTouchesTheComponentAlone()
    {
        Assert.IsFalse(TouchesTheComponent(typeof(Probe).GetMethod(nameof(Probe.Completes))!));
    }

    private static IEnumerable<Type> FindComponentsDisposingAfterTheirTeardown()
    {
        return Packages.SelectMany(LoadableTypes)
                       .Where(t => t.IsClass && t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                                .Where(m => m.Name == "DisposeAsync")
                                                .Any(m => Calls(m, DisposeAfterAsync)));
    }

    // The legacy chart's types reference Newtonsoft.Json, which nothing here ships; none of them is a component of this kind.
    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static IEnumerable<MethodInfo> JsInvokables(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.IsDefined(typeof(JSInvokableAttribute)));
    }

    // A callback that never loads `this` (and so captures no `this` into its state machine) cannot reach the component.
    private static bool TouchesTheComponent(MethodInfo method)
    {
        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } stateMachine)
        {
            return stateMachine.StateMachineType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                                .Any(f => f.Name.EndsWith("__this", StringComparison.Ordinal));
        }

        return Instructions(method).Any(i => i.OpCode == OpCodes.Ldarg_0 || i.OpCode == OpCodes.Ldarga_S || i.OpCode == OpCodes.Ldarg_S);
    }

    private static bool IsGuarded(MethodBase method, Type component, int depth = 2)
    {
        var body = Body(method);

        foreach (var (opCode, token) in Instructions(body))
        {
            if (opCode == OpCodes.Ldfld && Resolve(body, token) is FieldInfo field && IsDisposalFlag(field, component)) return true;

            if (depth > 0 && (opCode == OpCodes.Call || opCode == OpCodes.Callvirt) &&
                Resolve(body, token) is MethodInfo { IsSpecialName: true } getter &&
                getter.Name.StartsWith("get_", StringComparison.Ordinal) &&
                IsDeclaredOn(getter, component) &&
                IsGuarded(getter, component, depth - 1)) return true;
        }

        return false;
    }

    // Declared on the component or one of its base classes, so the flag it reads is the component's own.
    private static bool IsDeclaredOn(MethodInfo method, Type component)
    {
        if (method.DeclaringType is not { } owner) return false;

        for (var type = component; type is not null; type = type.BaseType)
        {
            if (Definition(type) == Definition(owner)) return true;
        }

        return false;
    }

    // IsDisposing for a BitComponentBase, the _disposed of its own for a component that is not one.
    private static bool IsDisposalFlag(FieldInfo field, Type component)
    {
        if (SameMember(field, IsDisposing)) return true;

        return typeof(BitComponentBase).IsAssignableFrom(Definition(component)) is false &&
               field.Name == "_disposed" &&
               Definition(field.DeclaringType!) == Definition(component);
    }

    private static bool Calls(MethodBase method, MethodInfo target)
    {
        var body = Body(method);

        return Instructions(body).Any(i => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                                           Resolve(body, i.Token) is MethodInfo callee &&
                                           SameMember(callee.IsGenericMethod ? callee.GetGenericMethodDefinition() : callee, target));
    }

    // The body that actually runs: the MoveNext of the state machine for an async method.
    private static MethodBase Body(MethodBase method)
    {
        return method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } stateMachine
            ? stateMachine.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            : method;
    }

    private static MemberInfo? Resolve(MethodBase method, int token)
    {
        try
        {
            var typeArguments = method.DeclaringType is { IsGenericType: true } declaringType ? declaringType.GetGenericArguments() : null;
            var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

            return method.Module.ResolveMember(token, typeArguments, methodArguments);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static IEnumerable<(OpCode OpCode, int Token)> Instructions(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        var offset = 0;

        while (offset < il.Length)
        {
            short value = il[offset++];
            if (value == 0xFE) value = unchecked((short)(0xFE00 | il[offset++]));

            var opCode = OpCodesByValue[value];
            var token = 0;

            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar:
                    offset += 1;
                    break;
                case OperandType.InlineVar:
                    offset += 2;
                    break;
                case OperandType.InlineI8 or OperandType.InlineR:
                    offset += 8;
                    break;
                case OperandType.InlineSwitch:
                    offset += 4 + BitConverter.ToInt32(il, offset) * 4;
                    break;
                default:
                    token = BitConverter.ToInt32(il, offset);
                    offset += 4;
                    break;
            }

            yield return (opCode, token);
        }
    }

    private static Type Definition(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    private static bool SameMember(MemberInfo a, MemberInfo b) => a.Module == b.Module && a.MetadataToken == b.MetadataToken;



    private sealed class Probe : BitComponentBase
    {
        private int _calls;

        private bool Gone => IsDisposing || IsDisposed;

        protected override string RootElementClass => "probe";

        [JSInvokable] public Task Completes() => Task.CompletedTask;

        [JSInvokable]
        public void Unguarded()
        {
            _calls++;
        }

        [JSInvokable]
        public async Task UnguardedAsync()
        {
            await Task.Yield();
            _calls++;
        }

        [JSInvokable]
        public void Guarded()
        {
            if (IsDisposing) return;

            _calls++;
        }

        [JSInvokable]
        public async Task GuardedAsync()
        {
            if (IsDisposing) return;

            await Task.Yield();
            _calls++;
        }

        [JSInvokable]
        public void GuardedByAProperty()
        {
            if (Gone) return;

            _calls++;
        }

        [JSInvokable]
        public Task GuardedByAnExpression() => IsDisposing ? Task.CompletedTask : Task.FromResult(_calls++);
    }
}
