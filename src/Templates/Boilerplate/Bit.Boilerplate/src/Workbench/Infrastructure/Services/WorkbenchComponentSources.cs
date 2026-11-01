using System.Reflection.Emit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Boilerplate.Workbench.Infrastructure.Services.WorkbenchComponentSources))]

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Finds the tag in a .razor file that renders a live component. The parent's render tree holds the sequence number the
/// compiled markup gave the tag. The first component up the tree whose compiled code opens that component with that
/// number holds the tag, and the numbers follow the order of the markup, so their rank picks the tag among the file's tags
/// of that component.
/// </summary>
public static class WorkbenchComponentSources
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyDeclared = AnyInstance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly FieldInfo renderHandleField = typeof(ComponentBase).GetField("_renderHandle", AnyInstance)!;
    private static readonly FieldInfo rendererField = typeof(RenderHandle).GetField("_renderer", AnyInstance)!;
    private static readonly FieldInfo componentIdField = typeof(RenderHandle).GetField("_componentId", AnyInstance)!;
    private static readonly MethodInfo componentStateOf = typeof(Renderer).GetMethod("GetComponentState", AnyInstance, [typeof(int)])
                                                          ?? typeof(Renderer).GetMethod("GetRequiredComponentState", AnyInstance, [typeof(int)])!;
    private static readonly MethodInfo framesOf = typeof(Renderer).GetMethod("GetCurrentRenderTreeFrames", AnyInstance, [typeof(int)])!;
    private static readonly PropertyInfo parentOf = typeof(ComponentState).GetProperty("ParentComponentState", AnyInstance)!;

    private static readonly OpCode[] oneByteCodes = new OpCode[0x100];
    private static readonly OpCode[] twoByteCodes = new OpCode[0x100];

    // Per component class, the sequence numbers its code opens each component with, by the component's name, in order.
    private static readonly ConcurrentDictionary<Type, Dictionary<string, List<int>>> sitesByType = new();

    static WorkbenchComponentSources()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode code)
                continue;

            var value = (ushort)code.Value;

            if (value < 0x100)
            {
                oneByteCodes[value] = code;
            }
            else if ((value & 0xFF00) is 0xFE00)
            {
                twoByteCodes[value & 0xFF] = code;
            }
        }
    }

    /// <summary>Raised once hot reload has applied a change to the code, so a picked component can be read again.</summary>
    public static event Action? Updated;

    /// <summary>Hot reload changed some code, and with it the sequence numbers read from it.</summary>
    public static void ClearCache(Type[]? updatedTypes) => sitesByType.Clear();

    public static void UpdateApplication(Type[]? updatedTypes) => Updated?.Invoke();

    /// <summary>The renderer of a component's circuit; null before the component is attached to one.</summary>
    public static Renderer? RendererOf(IComponent component)
    {
        return component is ComponentBase && renderHandleField.GetValue(component) is RenderHandle handle ? rendererField.GetValue(handle) as Renderer : null;
    }

    /// <summary>
    /// The tag that renders the component, or the bit BlazorUI component that draws it itself; both null when neither is found.
    /// Call it on the component's own circuit, as it reads that circuit's render tree.
    /// </summary>
    public static (WorkbenchComponentTag? Tag, string? DrawnBy) Find(ComponentBase component)
    {
        if (renderHandleField.GetValue(component) is not RenderHandle handle || rendererField.GetValue(handle) is not Renderer renderer)
            return (null, null);

        var componentId = (int)componentIdField.GetValue(handle)!;
        var state = (ComponentState)componentStateOf.Invoke(renderer, [componentId])!;

        if (parentOf.GetValue(state) is not ComponentState parent || SequenceIn(renderer, parent.ComponentId, componentId) is not { } sequence)
            return (null, null);

        var name = TagName(component.GetType());

        for (ComponentState? ancestor = parent; ancestor is not null; ancestor = parentOf.GetValue(ancestor) as ComponentState)
        {
            var type = ancestor.Component.GetType();

            if (SitesOf(type).GetValueOrDefault(name) is not { } sites || sites.BinarySearch(sequence) is var rank && rank < 0)
                continue;

            return IsLibrary(type) ? (null, TagName(type)) : (TagIn(type, name, rank, sites.Count), null);
        }

        return (null, null);
    }

    /// <summary>A type's name as a tag writes it, without the generic arity.</summary>
    public static string TagName(Type type) => type.Name.Split('`')[0];

    /// <summary>The sequence number of the frame the parent rendered the component from.</summary>
    private static int? SequenceIn(Renderer renderer, int parentId, int componentId)
    {
        var frames = (ArrayRange<RenderTreeFrame>)framesOf.Invoke(renderer, [parentId])!;

        for (var index = 0; index < frames.Count; index++)
        {
            ref var frame = ref frames.Array[index];

            if (frame.FrameType is RenderTreeFrameType.Component && frame.ComponentId == componentId)
                return frame.Sequence;
        }

        return null;
    }

    /// <summary>The tag at a rank among a .razor file's tags of the component; null when the file's tags and the code's don't add up.</summary>
    private static WorkbenchComponentTag? TagIn(Type type, string name, int rank, int count)
    {
        if (WorkbenchRazorFiles.Find(type) is not { } path)
            return null;

        var text = File.ReadAllText(path);
        var starts = WorkbenchRazorTags.Find(text, name);

        if (starts.Count != count)
            return null;

        var line = text.AsSpan(0, starts[rank]).Count('\n') + 1;

        return new(WorkbenchRazorFiles.Relative(path), rank, line);
    }

    private static bool IsLibrary(Type type) => type.Assembly.GetName().Name is { } name && (name.StartsWith("Bit.", StringComparison.Ordinal) || name.StartsWith("Microsoft.", StringComparison.Ordinal));

    private static Dictionary<string, List<int>> SitesOf(Type type)
    {
        return sitesByType.GetOrAdd(type.IsGenericType ? type.GetGenericTypeDefinition() : type, definition =>
        {
            Dictionary<string, List<int>> sites = [];

            foreach (var (name, sequence) in TypeAndNested(definition).SelectMany(t => t.GetMethods(AnyDeclared).Cast<MethodBase>().Concat(t.GetConstructors(AnyDeclared))).SelectMany(Scan))
            {
                (sites.TryGetValue(name, out var list) ? list : sites[name] = []).Add(sequence);
            }

            foreach (var list in sites.Values)
            {
                list.Sort();
            }

            return sites;
        });
    }

    private static IEnumerable<Type> TypeAndNested(Type type) => [type, .. type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(TypeAndNested)];

    /// <summary>
    /// The components a method opens, each with its sequence number: the constant pushed right after the method's builder,
    /// its first argument, before a call to RenderTreeBuilder.OpenComponent or to the TypeInference.Create method Razor
    /// writes for a generic component.
    /// </summary>
    private static IEnumerable<(string Name, int Sequence)> Scan(MethodBase method)
    {
        byte[]? il;

        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray();
        }
        catch (Exception exception) when (exception is InvalidOperationException or BadImageFormatException)
        {
            il = null;
        }

        if (il is null)
            yield break;

        var typeArguments = method.DeclaringType is { IsGenericType: true } declaring ? declaring.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
        var afterBuilder = false;
        int? sequence = null;

        for (var offset = 0; offset < il.Length;)
        {
            var code = il[offset] is 0xFE && offset + 1 < il.Length ? twoByteCodes[il[++offset]] : oneByteCodes[il[offset]];
            var operand = ++offset;

            offset += OperandSize(code.OperandType, il, operand);

            if (code == OpCodes.Ldarg_1)
            {
                afterBuilder = true;
                sequence = null;
                continue;
            }

            if (afterBuilder)
            {
                sequence = ConstantOf(code, il, operand);
                afterBuilder = false;
            }

            if (sequence is not { } number || (code != OpCodes.Call && code != OpCodes.Callvirt))
                continue;

            MethodBase? target;

            try
            {
                target = method.Module.ResolveMethod(BitConverter.ToInt32(il, operand), typeArguments, methodArguments);
            }
            catch (Exception exception) when (exception is ArgumentException or BadImageFormatException or TypeLoadException)
            {
                continue;
            }

            if (ComponentOpenedBy(target) is { } name)
                yield return (name, number);
        }
    }

    private static string? ComponentOpenedBy(MethodBase? target)
    {
        if (target is MethodInfo { Name: nameof(RenderTreeBuilder.OpenComponent), IsGenericMethod: true } open && open.DeclaringType == typeof(RenderTreeBuilder))
            return TagName(open.GetGenericArguments()[0]);

        // Razor's helper for a generic component whose type is inferred: TypeInference.CreateBitDataGrid_0(__builder, seq, ...).
        if (target?.DeclaringType?.Name is "TypeInference" && target.Name.StartsWith("Create", StringComparison.Ordinal) && target.Name.EndsWith("_CaptureParameters", StringComparison.Ordinal) is false && target.Name.LastIndexOf('_') is > 6 and var end)
            return target.Name[6..end];

        return null;
    }

    private static int? ConstantOf(OpCode code, byte[] il, int operand)
    {
        if (code == OpCodes.Ldc_I4_S)
            return (sbyte)il[operand];

        if (code == OpCodes.Ldc_I4)
            return BitConverter.ToInt32(il, operand);

        if (code == OpCodes.Ldc_I4_M1)
            return -1;

        return code.Value >= OpCodes.Ldc_I4_0.Value && code.Value <= OpCodes.Ldc_I4_8.Value ? code.Value - OpCodes.Ldc_I4_0.Value : null;
    }

    private static int OperandSize(OperandType type, byte[] il, int operand) => type switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, operand),
        _ => 4
    };
}
