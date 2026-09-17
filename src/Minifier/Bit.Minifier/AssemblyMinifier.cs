using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Bit.Minifier;

/// <summary>
/// Shrinks trimmed assemblies without changing what they do or how they debug: drops attributes only the
/// compiler reads, shortens compiler-generated names (keeping the shape debuggers parse) and rewrites the
/// portable pdb to match. Hand-written names are kept unless <see cref="MinifierOptions.Aggressive"/>.
/// </summary>
internal sealed class AssemblyMinifier(MinifierOptions options)
{
    private const string CompilerGenerated = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

    private static readonly HashSet<string> NullableAttributes =
    [
        "System.Runtime.CompilerServices.NullableAttribute",
        "System.Runtime.CompilerServices.NullableContextAttribute",
        "System.Runtime.CompilerServices.NullablePublicOnlyAttribute",
    ];

    // read by the compiler or analyzers only; nothing at runtime asks for them
    private static readonly HashSet<string> CompileTimeAttributes =
    [
        CompilerGenerated,
        "System.Diagnostics.CodeAnalysis.AllowNullAttribute",
        "System.Diagnostics.CodeAnalysis.DisallowNullAttribute",
        "System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute",
        "System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute",
        "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
        "System.Diagnostics.CodeAnalysis.MaybeNullAttribute",
        "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.MemberNotNullAttribute",
        "System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute",
        "System.Runtime.CompilerServices.RefSafetyRulesAttribute",
        "System.Runtime.CompilerServices.ScopedRefAttribute",
        // Bit.BlazorUI's source generator markers
        "Bit.BlazorUI.CallOnSetAttribute",
        "Bit.BlazorUI.ResetClassBuilderAttribute",
        "Bit.BlazorUI.ResetStyleBuilderAttribute",
        "Bit.BlazorUI.TwoWayBoundAttribute",
    ];

    // attributes that don't make the name of the member they sit on observable
    private static readonly HashSet<string> NameNeutralAttributes =
    [
        .. CompileTimeAttributes,
        .. NullableAttributes,
        "System.Diagnostics.DebuggerBrowsableAttribute",
        "System.Diagnostics.DebuggerHiddenAttribute",
        "System.Diagnostics.DebuggerStepThroughAttribute",
        "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
        "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
        "System.Runtime.CompilerServices.ExtensionAttribute",
        "System.Runtime.CompilerServices.IsReadOnlyAttribute",
        "System.Runtime.CompilerServices.IteratorStateMachineAttribute",
        "System.Runtime.CompilerServices.PreserveBaseOverridesAttribute",
        "System.Runtime.CompilerServices.TupleElementNamesAttribute",
    ];

    private readonly List<string> map = [];
    private AggressiveMinifier literals = default!;

    public IReadOnlyList<MinifiedAssembly> Run()
    {
        using var resolver = new DirectoryResolver(options.Directory);
        var assemblies = Load(resolver);
        if (assemblies.Count == 0) return [];

        var modules = assemblies.Select(a => a.MainModule).ToHashSet();
        var references = ReferenceIndex.Collect(modules);

        var fully = options.FullyMinified.ToHashSet(StringComparer.OrdinalIgnoreCase);
        // EF Core reads [Nullable] itself to tell required columns apart, whatever NullabilityInfoContext says
        var keepNullable = options.KeepNullable || File.Exists(Path.Combine(options.Directory, "Microsoft.EntityFrameworkCore.dll"));

        // every rename keeps the names string literals mention: nameof(...), GetField("..."), [UnsafeAccessor(Name = "...")]
        literals = new AggressiveMinifier(Map);
        literals.CollectLiterals(modules);

        var stats = new Dictionary<ModuleDefinition, (int attributes, int names)>();
        foreach (var module in modules)
        {
            // aggressive: no library gets the benefit of the doubt
            var full = options.Aggressive || fully.Contains(module.Assembly.Name.Name);
            var removable = new HashSet<string>(CompileTimeAttributes);
            if (options.KeepNullable is false && (full || keepNullable is false)) removable.UnionWith(NullableAttributes);
            if (options.Aggressive) removable.UnionWith(AggressiveMinifier.MoreAttributes);

            var attributes = StripAttributes(module, removable, full);
            var names = ShortenGeneratedNames(module, full);
            if (options.Aggressive) names += literals.Run(module, CanRenameInternals(module, modules));
            stats[module] = (attributes, names);
        }

        var retargeted = references.Apply();
        Verify(resolver, modules);

        // an assembly nothing changed in is left as ILLink wrote it
        var changed = assemblies.Where(a => stats[a.MainModule] != (0, 0) || retargeted.Contains(a.MainModule)).ToList();
        return Write(changed, stats);
    }

    private List<AssemblyDefinition> Load(DirectoryResolver resolver)
    {
        var names = options.Assemblies ?? Directory.EnumerateFiles(options.Directory, "*.dll").Select(Path.GetFileNameWithoutExtension).ToList()!;

        var result = new List<AssemblyDefinition>();
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.Combine(options.Directory, name + ".dll");
            if (File.Exists(path) is false) continue;

            var withSymbols = File.Exists(Path.ChangeExtension(path, ".pdb"));
            AssemblyDefinition assembly;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    InMemory = true,
                    ReadingMode = ReadingMode.Immediate,
                    ReadSymbols = withSymbols,
                    SymbolReaderProvider = withSymbols ? new PortablePdbReaderProvider() : null,
                });
            }
            catch (Exception e) when (options.Assemblies is null && e is BadImageFormatException or SymbolsNotMatchingException or InvalidOperationException)
            {
                // not managed, or a pdb that can't be rewritten along with it: left as it is
                continue;
            }
            // ReadyToRun / mixed-mode images can't be written back
            if ((assembly.MainModule.Attributes & ModuleAttributes.ILOnly) == 0)
            {
                assembly.Dispose();
                continue;
            }
            resolver.Register(assembly);
            result.Add(assembly);
        }
        return result;
    }

    private static int StripAttributes(ModuleDefinition module, HashSet<string> removable, bool full)
    {
        int removed = 0;
        foreach (var provider in Providers(module))
        {
            if (provider.HasCustomAttributes is false) continue;
            var attributes = provider.CustomAttributes;
            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                var name = attributes[i].AttributeType.FullName;
                if (removable.Contains(name) is false) continue;
                // debuggers use it to tell closures and state machines apart from user types, and
                // serializers such as Newtonsoft.Json use it to skip backing fields
                if (name == CompilerGenerated && (provider is TypeDefinition || (full is false && provider is FieldDefinition))) continue;
                attributes.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    /// <summary>
    /// <c>&lt;BuildRenderTree&gt;b__12_0</c> -> <c>&lt;a&gt;b__12_0</c>. Only the member name inside the brackets
    /// changes; the kind marker and ordinals stay, so debuggers still recognize lambdas, local functions,
    /// state machines and backing fields. Fields of generated types (hoisted locals, captured variables) keep
    /// their names because the debugger shows them as the locals they are. Backing fields are renamed in
    /// fully minified assemblies only: EF Core, for one, finds them by name.
    /// </summary>
    private int ShortenGeneratedNames(ModuleDefinition module, bool full)
    {
        // one name map per outermost type keeps every new name unique where the old one was
        var scopes = new Dictionary<TypeDefinition, Dictionary<string, string>>();
        int renamed = 0;

        string Shorten(TypeDefinition owner, string name)
        {
            if (GeneratedName.TryParse(name, out var inner, out var rest) is false) return name;
            var outer = owner;
            while (outer.DeclaringType is not null) outer = outer.DeclaringType;
            if (scopes.TryGetValue(outer, out var scope) is false) scopes[outer] = scope = [];
            if (scope.TryGetValue(inner, out var shortName) is false) scope[inner] = shortName = ShortName.Get(scope.Count);
            return "<" + shortName + rest;
        }

        foreach (var type in module.GetTypes().ToList())
        {
            if (type.IsNested && IsNameNeutral(type) && literals.IsMentioned(type.Name) is false)
            {
                var name = Shorten(type.DeclaringType, type.Name);
                if (name != type.Name) { Map(module, "T", type.FullName, name); type.Name = name; renamed++; }
            }
            foreach (var method in type.Methods)
            {
                if (method.IsVirtual || method.HasOverrides || IsNameNeutral(method) is false || literals.IsMentioned(method.Name)) continue;
                var name = Shorten(type, method.Name);
                if (name != method.Name) { Map(module, "M", $"{type.FullName}::{method.Name}", name); method.Name = name; renamed++; }
            }
            if (full is false) continue;
            foreach (var field in type.Fields)
            {
                if (field.IsPublic || IsNameNeutral(field) is false || literals.IsMentioned(field.Name)) continue;
                var name = Shorten(type, field.Name);
                if (name != field.Name) { Map(module, "F", $"{type.FullName}::{field.Name}", name); field.Name = name; renamed++; }
            }
        }
        return renamed;
    }

    /// <summary>
    /// Internal names are only safe to change when every assembly that can see them is rewritten too:
    /// an InternalsVisibleTo friend that is published but not minified would keep the old names.
    /// </summary>
    private bool CanRenameInternals(ModuleDefinition module, HashSet<ModuleDefinition> modules)
    {
        var minified = modules.Select(m => m.Assembly.Name.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in module.Assembly.CustomAttributes)
        {
            if (attribute.AttributeType.FullName != "System.Runtime.CompilerServices.InternalsVisibleToAttribute") continue;
            var friend = ((string)attribute.ConstructorArguments[0].Value).Split(',')[0].Trim();
            if (minified.Contains(friend) is false && File.Exists(Path.Combine(options.Directory, friend + ".dll"))) return false;
        }
        return true;
    }

    /// <summary>
    /// Every reference into the rewritten assemblies - from each other and from every other assembly in the
    /// folder - must still resolve. Nothing is written otherwise.
    /// </summary>
    private void Verify(DirectoryResolver resolver, HashSet<ModuleDefinition> modules)
    {
        var names = modules.Select(m => m.Assembly.Name.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toCheck = new List<ModuleDefinition>(modules);
        foreach (var path in Directory.EnumerateFiles(options.Directory, "*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (names.Contains(name)) continue;
            var dependent = resolver.TryLoad(name);
            if (dependent is not null && dependent.MainModule.AssemblyReferences.Any(r => names.Contains(r.Name))) toCheck.Add(dependent.MainModule);
        }

        var broken = new List<string>();
        foreach (var module in toCheck)
        {
            foreach (var reference in ReferenceIndex.MemberReferences(module))
            {
                if (reference.DeclaringType is ArrayType) continue; // runtime-provided Get/Set/Address
                if (reference.DeclaringType.Scope is AssemblyNameReference scope && names.Contains(scope.Name) is false) continue;
                if (ReferenceIndex.Resolve(reference.DeclaringType.GetElementType()) is { } declaring && modules.Contains(declaring.Module) is false) continue;
                if (ReferenceIndex.Resolve(reference) is null) broken.Add($"{module.Assembly.Name.Name}: {reference.FullName}");
            }
        }
        if (broken.Count > 0)
            throw new MinifierException($"{broken.Count} reference(s) would no longer resolve, e.g. {string.Join("; ", broken.Take(5))}.");
    }

    private List<MinifiedAssembly> Write(List<AssemblyDefinition> assemblies, Dictionary<ModuleDefinition, (int attributes, int names)> stats)
    {
        // everything is written aside first, so a failure leaves the folder as ILLink produced it
        var staging = Path.Combine(options.Directory, ".bit-minifier");
        Directory.CreateDirectory(staging);
        var results = new List<MinifiedAssembly>();
        try
        {
            foreach (var assembly in assemblies)
            {
                var module = assembly.MainModule;
                // the signature no longer matches; .NET doesn't validate it, so say so instead of lying
                module.Attributes &= ~ModuleAttributes.StrongNameSigned;
                // through streams, so the pdb path the dll records is the pdb's final one, not the staging folder's
                using var dll = File.Create(Path.Combine(staging, assembly.Name.Name + ".dll"));
                using var pdb = module.HasSymbols ? File.Create(Path.Combine(staging, assembly.Name.Name + ".pdb")) : null;
                assembly.Write(dll, new WriterParameters
                {
                    WriteSymbols = module.HasSymbols,
                    SymbolWriterProvider = module.HasSymbols ? new PortablePdbWriterProvider() : null,
                    SymbolStream = pdb,
                    DeterministicMvid = true,
                });
            }

            foreach (var assembly in assemblies)
            {
                var name = assembly.Name.Name;
                var target = Path.Combine(options.Directory, name + ".dll");
                var originalSize = new FileInfo(target).Length;
                File.Move(Path.Combine(staging, name + ".dll"), target, overwrite: true);
                if (assembly.MainModule.HasSymbols)
                    File.Move(Path.Combine(staging, name + ".pdb"), Path.ChangeExtension(target, ".pdb"), overwrite: true);
                var (attributes, names) = stats[assembly.MainModule];
                results.Add(new MinifiedAssembly(name, originalSize, new FileInfo(target).Length, attributes, names));
            }
        }
        finally
        {
            Directory.Delete(staging, recursive: true);
        }

        if (options.MapFile is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.MapFile))!);
            File.WriteAllLines(options.MapFile, map);
        }
        return results;
    }

    private void Map(ModuleDefinition module, string kind, string original, string name)
        => map.Add($"{module.Assembly.Name.Name}\t{kind}\t{original}\t{name}");

    private static bool IsNameNeutral(ICustomAttributeProvider provider)
        => provider.CustomAttributes.All(a => NameNeutralAttributes.Contains(a.AttributeType.FullName));

    internal static IEnumerable<ICustomAttributeProvider> Providers(ModuleDefinition module)
    {
        yield return module.Assembly;
        yield return module;
        foreach (var type in module.GetTypes())
        {
            yield return type;
            foreach (var parameter in type.GenericParameters) yield return parameter;
            foreach (var implementation in type.Interfaces) yield return implementation;
            foreach (var field in type.Fields) yield return field;
            foreach (var property in type.Properties) yield return property;
            foreach (var @event in type.Events) yield return @event;
            foreach (var method in type.Methods)
            {
                yield return method;
                yield return method.MethodReturnType;
                foreach (var parameter in method.Parameters) yield return parameter;
                foreach (var parameter in method.GenericParameters) yield return parameter;
            }
        }
    }
}
