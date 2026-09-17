using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Bit.Minifier;

/// <summary>
/// References that name a definition by string rather than by token: member and type references between the
/// minified assemblies (friend assemblies reach each other's internals) and <c>typeof</c> values inside
/// custom attributes (<c>[AsyncStateMachine(typeof(&lt;M&gt;d__3))]</c>). Resolved before anything is
/// renamed, re-pointed after.
/// </summary>
internal sealed class ReferenceIndex
{
    private readonly List<(MemberReference reference, MemberReference definition)> members = [];
    private readonly List<(TypeReference reference, TypeDefinition definition)> types = [];
    private readonly List<(ModuleDefinition module, TypeDefinition definition, string name, Action fix)> attributeFixes = [];

    public static ReferenceIndex Collect(HashSet<ModuleDefinition> modules)
    {
        var index = new ReferenceIndex();
        var seen = new HashSet<MemberReference>(ReferenceEqualityComparer.Instance);

        foreach (var module in modules)
        {
            foreach (var reference in MemberReferences(module))
            {
                if (seen.Add(reference) is false) continue;
                if (Resolve(reference) is { } definition && modules.Contains(definition.Module)) index.members.Add((reference, definition));
            }

            foreach (var reference in module.GetTypeReferences())
            {
                if (Resolve(reference) is { } definition && modules.Contains(definition.Module)) index.types.Add((reference, definition));
            }

            foreach (var provider in AssemblyMinifier.Providers(module))
            {
                foreach (var attribute in provider.CustomAttributes)
                {
                    index.CollectArguments(module, modules, attribute.ConstructorArguments);
                    index.CollectNamedArguments(module, modules, attribute.Fields);
                    index.CollectNamedArguments(module, modules, attribute.Properties);
                }
            }
        }
        return index;
    }

    /// <summary>Re-points the references; returns the modules in which one of them actually changed.</summary>
    public HashSet<ModuleDefinition> Apply()
    {
        var changed = new HashSet<ModuleDefinition>();
        foreach (var (reference, definition) in members)
        {
            if (reference.Name == definition.Name) continue;
            reference.Name = definition.Name;
            changed.Add(reference.Module);
        }
        foreach (var (reference, definition) in types)
        {
            if (reference.Name == definition.Name) continue;
            reference.Name = definition.Name;
            changed.Add(reference.Module);
        }
        foreach (var (module, definition, name, fix) in attributeFixes)
        {
            if (definition.Name == name) continue;
            fix();
            changed.Add(module);
        }
        return changed;
    }

    /// <summary>
    /// Member references as the IL, the method impls and the attributes use them - the objects the writer
    /// emits. ModuleDefinition.GetMemberReferences is no substitute: it reads the table afresh on every call.
    /// </summary>
    public static IEnumerable<MemberReference> MemberReferences(ModuleDefinition module)
    {
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                foreach (var implemented in method.Overrides)
                {
                    if (Unwrap(implemented) is { } r) yield return r;
                }
                if (method.HasBody is false) continue;
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is MemberReference operand and (MethodReference or FieldReference) && Unwrap(operand) is { } r) yield return r;
                }
            }
        }

        foreach (var provider in AssemblyMinifier.Providers(module))
        {
            foreach (var attribute in provider.CustomAttributes)
            {
                if (Unwrap(attribute.Constructor) is { } r) yield return r;
            }
        }
    }

    public static MemberReference? Resolve(MemberReference reference) => Try(() => reference switch
    {
        MethodReference method => method.Resolve(),
        FieldReference field => (MemberReference)field.Resolve(),
        _ => null,
    });

    public static TypeDefinition? Resolve(TypeReference reference) => Try(reference.Resolve);

    // an assembly missing from the folder makes Cecil throw rather than return null
    private static T? Try<T>(Func<T?> resolve) where T : class
    {
        try { return resolve(); }
        catch (AssemblyResolutionException) { return null; }
    }

    // definitions are renamed in place, so only references (MemberRef rows) need re-pointing
    private static MemberReference? Unwrap(MemberReference reference)
    {
        if (reference is GenericInstanceMethod generic) reference = generic.ElementMethod;
        return reference is IMemberDefinition ? null : reference;
    }

    private void CollectArguments(ModuleDefinition module, HashSet<ModuleDefinition> modules, Collection<CustomAttributeArgument> arguments)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            int index = i;
            var targets = new List<TypeDefinition>();
            if (Retarget(module, modules, arguments[index], targets) is { } fix)
                AddFix(module, targets, () => arguments[index] = fix());
        }
    }

    private void CollectNamedArguments(ModuleDefinition module, HashSet<ModuleDefinition> modules, Collection<CustomAttributeNamedArgument> arguments)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            int index = i;
            var named = arguments[index];
            var targets = new List<TypeDefinition>();
            if (Retarget(module, modules, named.Argument, targets) is { } fix)
                AddFix(module, targets, () => arguments[index] = new CustomAttributeNamedArgument(named.Name, fix()));
        }
    }

    private void AddFix(ModuleDefinition module, List<TypeDefinition> targets, Action fix)
    {
        foreach (var target in targets) attributeFixes.Add((module, target, target.Name, fix));
    }

    // a Type value, boxed as object or inside an array, that names a type of the minified set
    private static Func<CustomAttributeArgument>? Retarget(ModuleDefinition module, HashSet<ModuleDefinition> modules, CustomAttributeArgument argument, List<TypeDefinition> targets)
    {
        switch (argument.Value)
        {
            case TypeReference type when Resolve(type) is { } definition && modules.Contains(definition.Module):
                targets.Add(definition);
                return () => new CustomAttributeArgument(argument.Type, definition.Module == module ? definition : module.ImportReference(definition));

            case CustomAttributeArgument boxed when Retarget(module, modules, boxed, targets) is { } inner:
                return () => new CustomAttributeArgument(argument.Type, inner());

            case CustomAttributeArgument[] array:
                var fixes = array.Select(element => Retarget(module, modules, element, targets)).ToArray();
                if (fixes.All(f => f is null)) return null;
                return () => new CustomAttributeArgument(argument.Type, array.Select((element, i) => fixes[i]?.Invoke() ?? element).ToArray());

            default:
                return null;
        }
    }
}
