using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Bit.Minifier;

/// <summary>
/// References that name a definition by string rather than by token: member and type references between the
/// minified assemblies (friend assemblies reach each other's internals), type forwarders, and types inside
/// custom attributes - <c>typeof</c> values (<c>[AsyncStateMachine(typeof(&lt;M&gt;d__3))]</c>) and the enum
/// types of named and boxed arguments, which the blob spells out. Resolved before anything is renamed,
/// re-pointed after.
/// </summary>
internal sealed class ReferenceIndex
{
    private readonly List<(MemberReference reference, MemberReference definition)> members = [];
    // references of the assemblies that are not rewritten: their rows keep the old names, so they are only checked
    private readonly List<(MemberReference reference, MemberReference definition)> foreign = [];
    private readonly List<(TypeReference reference, TypeDefinition definition)> types = [];
    private readonly List<(ModuleDefinition module, ExportedType forwarder, TypeDefinition definition)> forwarders = [];
    private readonly List<(ModuleDefinition module, TypeDefinition definition, string name, Action fix)> attributeFixes = [];

    public static ReferenceIndex Collect(HashSet<ModuleDefinition> modules, IEnumerable<ModuleDefinition> others)
    {
        var index = new ReferenceIndex();
        var seen = new HashSet<MemberReference>(ReferenceEqualityComparer.Instance);

        foreach (var module in others)
        {
            foreach (var reference in MemberReferences(module))
            {
                if (seen.Add(reference) is false) continue;
                if (Resolve(reference) is { } definition && modules.Contains(definition.Module)) index.foreign.Add((reference, definition));
            }
        }

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

            foreach (var forwarder in module.ExportedTypes)
            {
                if (Resolve(forwarder) is { } definition && modules.Contains(definition.Module)) index.forwarders.Add((module, forwarder, definition));
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

    /// <summary>
    /// The type references and forwarders of <paramref name="module"/> that resolve into <paramref name="modules"/>
    /// now, so that <see cref="AssemblyMinifier"/> can tell afterwards whether one of them stopped resolving.
    /// </summary>
    public static List<(ModuleDefinition module, string name, Func<bool> resolves)> ResolvableTypes(ModuleDefinition module, HashSet<ModuleDefinition> modules)
    {
        var result = new List<(ModuleDefinition, string, Func<bool>)>();
        foreach (var reference in module.GetTypeReferences())
        {
            if (Resolve(reference) is { } definition && modules.Contains(definition.Module))
                result.Add((module, reference.FullName, () => ReferenceEquals(Resolve(reference), definition)));
        }
        foreach (var forwarder in module.ExportedTypes)
        {
            if (Resolve(forwarder) is { } definition && modules.Contains(definition.Module))
                result.Add((module, forwarder.FullName, () => ReferenceEquals(Resolve(forwarder), definition)));
        }
        // attribute blobs name types by string; the argument is read again when checked, since fixes replace it
        foreach (var provider in AssemblyMinifier.Providers(module))
        {
            foreach (var attribute in provider.CustomAttributes)
            {
                var name = attribute.AttributeType.FullName;
                for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                {
                    int index = i;
                    Add(name, () => attribute.ConstructorArguments[index]);
                }
                foreach (var named in new[] { attribute.Fields, attribute.Properties })
                {
                    for (int i = 0; i < named.Count; i++)
                    {
                        int index = i;
                        Add($"{name}.{named[index].Name}", () => named[index].Argument);
                    }
                }
            }
        }
        return result;

        void Add(string name, Func<CustomAttributeArgument> argument)
        {
            var types = NamedTypes(argument()).Select(t => Resolve(t)).ToList();
            if (types.Any(t => t is not null && modules.Contains(t.Module)) && types.All(t => t is not null))
                result.Add((module, $"[{name}] argument", () => NamedTypes(argument()).Select(t => Resolve(t)).SequenceEqual(types)));
        }
    }

    /// <summary>
    /// Every reference that pointed into the rewritten assemblies and no longer reaches the very definition it
    /// did: one whose row could not be re-pointed, or a rename that hid it behind another member.
    /// </summary>
    public IEnumerable<string> Broken()
        => members.Concat(foreign).Where(p => ReferenceEquals(Resolve(p.reference), p.definition) is false)
            .Select(p => $"{p.reference.Module.Assembly.Name.Name}: {p.reference.FullName}");

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
            // a nested reference keeps the empty namespace its declaring type implies
            var @namespace = definition.IsNested ? reference.Namespace : definition.Namespace;
            if (reference.Name == definition.Name && reference.Namespace == @namespace) continue;
            reference.Name = definition.Name;
            reference.Namespace = @namespace;
            changed.Add(reference.Module);
        }
        foreach (var (module, forwarder, definition) in forwarders)
        {
            var @namespace = definition.IsNested ? forwarder.Namespace : definition.Namespace;
            if (forwarder.Name == definition.Name && forwarder.Namespace == @namespace) continue;
            forwarder.Name = definition.Name;
            forwarder.Namespace = @namespace;
            changed.Add(module);
        }
        foreach (var (module, definition, name, fix) in attributeFixes)
        {
            if (NameOf(definition) == name) continue;
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

    public static TypeDefinition? Resolve(ExportedType forwarder) => Try(forwarder.Resolve);

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
        // each fix rebuilds the whole argument, so running it once per renamed target is harmless
        foreach (var target in targets.Distinct()) attributeFixes.Add((module, target, NameOf(target), fix));
    }

    /// <summary>
    /// The name as it is now. TypeReference.FullName is cached, and renaming a type doesn't clear the cache of the
    /// types nested in it.
    /// </summary>
    private static string NameOf(TypeDefinition type)
        => type.DeclaringType is { } declaring ? NameOf(declaring) + "/" + type.Name : type.Namespace + "." + type.Name;

    // a Type value, boxed as object or inside an array, or an enum value, that names a type of the minified set
    private static Func<CustomAttributeArgument>? Retarget(ModuleDefinition module, HashSet<ModuleDefinition> modules, CustomAttributeArgument argument, List<TypeDefinition> targets)
    {
        var type = RetargetArgumentType(module, modules, argument.Type, targets);
        Func<object?>? value = argument.Value switch
        {
            TypeReference reference when RetargetType(module, modules, reference, targets) is { } retargeted => () => retargeted(),
            CustomAttributeArgument boxed when Retarget(module, modules, boxed, targets) is { } inner => () => inner(),
            CustomAttributeArgument[] array => RetargetArray(array),
            _ => null,
        };
        if (type is null && value is null) return null;
        return () => new CustomAttributeArgument(type?.Invoke() ?? argument.Type, value is null ? argument.Value : value());

        Func<object?>? RetargetArray(CustomAttributeArgument[] array)
        {
            var fixes = array.Select(element => Retarget(module, modules, element, targets)).ToArray();
            if (fixes.All(f => f is null)) return null;
            return () => array.Select((element, i) => fixes[i]?.Invoke() ?? element).ToArray();
        }
    }

    // the argument's own type: of the set's types, only an enum can be one, and named and boxed arguments spell it out
    private static Func<TypeReference>? RetargetArgumentType(ModuleDefinition module, HashSet<ModuleDefinition> modules, TypeReference type, List<TypeDefinition> targets)
    {
        if (type is ArrayType array)
        {
            var element = RetargetArgumentType(module, modules, array.ElementType, targets);
            return element is null ? null : () => new ArrayType(element(), array.Rank);
        }
        if (type is TypeSpecification || type.IsGenericParameter || Resolve(type) is not { IsEnum: true }) return null;
        return RetargetType(module, modules, type, targets);
    }

    // a type as a blob spells it out, with every type of the set in it, however deep in arrays and generic arguments
    private static Func<TypeReference>? RetargetType(ModuleDefinition module, HashSet<ModuleDefinition> modules, TypeReference type, List<TypeDefinition> targets)
    {
        switch (type)
        {
            case ArrayType array:
                var element = RetargetType(module, modules, array.ElementType, targets);
                return element is null ? null : () => new ArrayType(element(), array.Rank);

            case GenericInstanceType generic:
                var open = RetargetType(module, modules, generic.ElementType, targets);
                var arguments = generic.GenericArguments.Select(a => RetargetType(module, modules, a, targets)).ToArray();
                if (open is null && arguments.All(a => a is null)) return null;
                return () =>
                {
                    var rebuilt = new GenericInstanceType(open?.Invoke() ?? generic.ElementType);
                    for (int i = 0; i < arguments.Length; i++) rebuilt.GenericArguments.Add(arguments[i]?.Invoke() ?? generic.GenericArguments[i]);
                    return rebuilt;
                };

            // pointers, byrefs and modifiers have no place in a blob
            case TypeSpecification or GenericParameter:
                return null;

            default:
                if (Resolve(type) is not { } definition || modules.Contains(definition.Module) is false) return null;
                targets.Add(definition);
                return () => Import(module, definition);
        }
    }

    private static TypeReference Import(ModuleDefinition module, TypeDefinition definition)
        => definition.Module == module ? definition : module.ImportReference(definition);

    /// <summary>The types an attribute argument names in its blob: its enum type and its Type values.</summary>
    private static IEnumerable<TypeReference> NamedTypes(CustomAttributeArgument argument)
    {
        foreach (var type in Leaves(argument.Type)) yield return type;
        var values = argument.Value switch
        {
            TypeReference reference => Leaves(reference),
            CustomAttributeArgument boxed => NamedTypes(boxed),
            CustomAttributeArgument[] array => array.SelectMany(NamedTypes),
            _ => [],
        };
        foreach (var type in values) yield return type;
    }

    private static IEnumerable<TypeReference> Leaves(TypeReference type) => type switch
    {
        GenericInstanceType generic => Leaves(generic.ElementType).Concat(generic.GenericArguments.SelectMany(Leaves)),
        TypeSpecification specification => Leaves(specification.ElementType),
        GenericParameter => [],
        _ => [type],
    };
}
