using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Bit.Minifier;

/// <summary>
/// Shrinks trimmed assemblies without changing what they do or how they debug: drops attributes only the
/// compiler reads, shortens compiler-generated names (keeping the shape debuggers parse) and rewrites the
/// portable pdb to match. Hand-written names are kept unless <see cref="MinifierOptions.Aggressive"/>, public
/// ones unless <see cref="MinifierOptions.SuperAggressive"/>.
/// </summary>
internal sealed class AssemblyMinifier(MinifierOptions options)
{
    private const string CompilerGenerated = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

    private const string DynamicallyAccessedMembers = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";

    // the dynamic binder (Microsoft.CSharp) reads it to tell dynamic members apart
    private const string Dynamic = "System.Runtime.CompilerServices.DynamicAttribute";

    // what NullabilityInfoContext reads at runtime, by name, and EF Core through it
    private static readonly HashSet<string> NullableAttributes =
    [
        "System.Diagnostics.CodeAnalysis.AllowNullAttribute",
        "System.Diagnostics.CodeAnalysis.DisallowNullAttribute",
        "System.Diagnostics.CodeAnalysis.MaybeNullAttribute",
        "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullAttribute",
        "System.Runtime.CompilerServices.NullableAttribute",
        "System.Runtime.CompilerServices.NullableContextAttribute",
        "System.Runtime.CompilerServices.NullablePublicOnlyAttribute",
    ];

    // read by the compiler or analyzers only; nothing at runtime asks for them. SetsRequiredMembers is not one of
    // them: System.Text.Json reads it to tell whether a constructor sets the required members.
    private static readonly HashSet<string> CompileTimeAttributes =
    [
        CompilerGenerated,
        "System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute",
        "System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute",
        "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
        "System.Diagnostics.CodeAnalysis.MemberNotNullAttribute",
        "System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute",
        "System.Diagnostics.CodeAnalysis.NotNullWhenAttribute",
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
        Dynamic,
        DynamicallyAccessedMembers,
        "System.ParamArrayAttribute",
        "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute",
        "System.Diagnostics.DebuggerBrowsableAttribute",
        "System.Diagnostics.DebuggerHiddenAttribute",
        "System.Diagnostics.DebuggerStepThroughAttribute",
        "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
        "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
        "System.Runtime.CompilerServices.EnumeratorCancellationAttribute",
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
        // a map is only ever there along with the assemblies it describes
        if (options.MapFile is not null) TryDelete(options.MapFile);

        using var resolver = new DirectoryResolver(options.Directory);
        var assemblies = Load(resolver);
        if (assemblies.Count == 0) return [];

        var modules = assemblies.Select(a => a.Assembly.MainModule).ToHashSet();
        // the other assemblies of the folder: not rewritten, but they may reference or name the rewritten ones
        var others = OtherModules(resolver, assemblies);
        var references = ReferenceIndex.Collect(modules, others);
        var types = others.Concat(modules).SelectMany(m => ReferenceIndex.ResolvableTypes(m, modules)).ToList();

        var fully = options.FullyMinified.ToHashSet(StringComparer.OrdinalIgnoreCase);
        // EF Core tells required columns apart by the nullable metadata and finds backing fields by name, in whatever
        // assembly declares the entities; a model that no longer matches its migrations fails MigrateAsync
        var efCore = File.Exists(Path.Combine(options.Directory, "Microsoft.EntityFrameworkCore.dll"));
        var dynamicBinder = File.Exists(Path.Combine(options.Directory, "Microsoft.CSharp.dll"));

        // every rename keeps the names string literals mention: nameof(...), GetField("..."), [UnsafeAccessor(Name = "...")]
        // Newtonsoft.Json serializes public fields by name
        var keepPublicFields = File.Exists(Path.Combine(options.Directory, "Newtonsoft.Json.dll"));
        literals = new AggressiveMinifier(Map, options.SuperAggressive, keepPublicFields);
        // every assembly of the folder may name what it reads by name, whether this tool rewrites it or not
        literals.Collect(modules, others, wordsOnly: options.Aggressive is false);
        // a satellite assembly is not in the folder itself, and its resource names name the types they belong to
        literals.CollectWords(SatelliteResourceNames());

        // decided before the first rename: a forwarder stops leading to its type once that type is renamed
        var reachable = ReachableFromOthers(modules, others);

        var stats = new Dictionary<ModuleDefinition, (int attributes, int names)>();
        foreach (var module in modules)
        {
            // aggressive: no library gets the benefit of the doubt, unless EF Core may map its types
            var full = fully.Contains(module.Assembly.Name.Name) || (options.Aggressive && efCore is false);
            var removable = new HashSet<string>(CompileTimeAttributes);
            if (options.KeepNullable is false && (full || efCore is false)) removable.UnionWith(NullableAttributes);
            if (options.Aggressive) removable.UnionWith(AggressiveMinifier.MoreAttributes);
            if (options.Aggressive && dynamicBinder is false) removable.Add(Dynamic);
            if (options.SuperAggressive) removable.UnionWith(AggressiveMinifier.SuperAttributes);

            var attributes = StripAttributes(module, removable, full);
            var names = ShortenGeneratedNames(module, full);
            if (options.Aggressive) names += literals.Run(module, Scope(module, modules, reachable), full);
            stats[module] = (attributes, names);
        }

        var retargeted = references.Apply();
        Verify(references, types);

        // an assembly nothing changed in is left as ILLink wrote it
        var changed = assemblies.Where(a => stats[a.Assembly.MainModule] != (0, 0) || retargeted.Contains(a.Assembly.MainModule)).ToList();
        return Write(changed, stats);
    }

    /// <summary>
    /// An assembly and the file it was read from. The two are told apart throughout: a file need not be named
    /// after the assembly inside it, and it is the file the folder holds and this tool replaces.
    /// </summary>
    private sealed record Loaded(AssemblyDefinition Assembly, string File);

    private List<Loaded> Load(DirectoryResolver resolver)
    {
        var names = options.Assemblies ?? Directory.EnumerateFiles(options.Directory, "*.dll").Select(Path.GetFileNameWithoutExtension).ToList()!;

        var result = new List<Loaded>();
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.Combine(options.Directory, name + ".dll");
            if (File.Exists(path) is false) continue;

            AssemblyDefinition assembly;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    InMemory = true,
                    ReadingMode = ReadingMode.Immediate,
                    // the pdb next to it, or the one embedded in it
                    ReadSymbols = true,
                    SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: false),
                });
                // a Windows pdb, which only a reader this tool doesn't ship understands
                if (assembly.MainModule.HasSymbols is false && File.Exists(Path.ChangeExtension(path, ".pdb")))
                {
                    assembly.Dispose();
                    throw new InvalidOperationException($"{name}.pdb is not a portable pdb.");
                }
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
            result.Add(new Loaded(assembly, name));
        }
        return result;
    }

    /// <summary>The other managed assemblies of the folder, as the resolver hands them out.</summary>
    private List<ModuleDefinition> OtherModules(DirectoryResolver resolver, List<Loaded> assemblies)
    {
        // by file name rather than by assembly name: a file holding an assembly of another name is still one
        // of the files just loaded, and reading it a second time would leave two copies of the same assembly
        var files = assemblies.Select(a => a.File).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var others = new List<ModuleDefinition>();
        foreach (var path in Directory.EnumerateFiles(options.Directory, "*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (files.Contains(name) is false && resolver.TryLoad(name) is { } other) others.Add(other.MainModule);
        }
        return others;
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
                // a trimmed app's DI checks that an open generic implementation asks no more of its type arguments
                // than its service does, so both keep what they ask
                if (name == DynamicallyAccessedMembers && provider is GenericParameter) continue;
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

        // a name that is skipped may already be what another one shortens to (<a>k__BackingField)
        static bool Clashes(IEnumerable<IMemberDefinition> siblings, string name) => siblings.Any(s => s.Name == name);

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
                if (name != type.Name && Clashes(type.DeclaringType.NestedTypes, name) is false) { Map(module, "T", type.FullName, name); type.Name = name; renamed++; }
            }
            foreach (var method in type.Methods)
            {
                if (method.IsVirtual || method.HasOverrides || IsNameNeutral(method) is false || literals.IsMentioned(method.Name)) continue;
                var name = Shorten(type, method.Name);
                if (name != method.Name && Clashes(type.Methods, name) is false) { Map(module, "M", $"{type.FullName}::{method.Name}", name); method.Name = name; renamed++; }
            }
            if (full is false) continue;
            foreach (var field in type.Fields)
            {
                if (field.IsPublic || IsNameNeutral(field) is false || literals.IsMentioned(field.Name)) continue;
                var name = Shorten(type, field.Name);
                if (name != field.Name && Clashes(type.Fields, name) is false) { Map(module, "F", $"{type.FullName}::{field.Name}", name); field.Name = name; renamed++; }
            }
        }
        return renamed;
    }

    /// <summary>
    /// Names are only safe to change when every assembly that can see them is rewritten too: an
    /// InternalsVisibleTo friend that is published but not minified would keep the old internal names, and an
    /// assembly that references this one but is not minified would keep the old public ones.
    /// </summary>
    private RenameScope Scope(ModuleDefinition module, HashSet<ModuleDefinition> modules, HashSet<string> reachable)
    {
        if (CanRenameInternals(module, modules) is false) return RenameScope.Private;
        if (options.SuperAggressive is false) return RenameScope.Internal;
        return reachable.Contains(module.Assembly.Name.Name) ? RenameScope.Internal : RenameScope.Public;
    }

    /// <summary>
    /// The assemblies an assembly this tool doesn't rewrite may name: the ones it references, and the ones a
    /// forwarder of those leads it to, which is how an assembly reaches a type through a facade.
    /// </summary>
    private static HashSet<string> ReachableFromOthers(HashSet<ModuleDefinition> modules, List<ModuleDefinition> others)
    {
        var forwarded = modules.ToDictionary(m => m.Assembly.Name.Name, m => m.ExportedTypes
            .Select(e => e.Scope).OfType<AssemblyNameReference>().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var reachable = others.SelectMany(o => o.AssemblyReferences.Select(r => r.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(reachable);
        while (queue.Count > 0)
        {
            if (forwarded.TryGetValue(queue.Dequeue(), out var targets) is false) continue;
            foreach (var target in targets)
            {
                if (reachable.Add(target)) queue.Enqueue(target);
            }
        }
        return reachable;
    }

    /// <summary>The resource names of the satellite assemblies, which sit in a folder per culture.</summary>
    private List<string> SatelliteResourceNames()
    {
        var names = new List<string>();
        foreach (var path in Directory.EnumerateFiles(options.Directory, "*.resources.dll", SearchOption.AllDirectories))
        {
            try
            {
                using var satellite = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
                names.AddRange(satellite.MainModule.Resources.Select(r => r.Name));
            }
            catch (Exception e) when (e is BadImageFormatException or IOException)
            {
                // not a managed assembly, or not readable: nothing to learn from it
            }
        }
        return names;
    }

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
    /// folder - must still reach the definition it reached before: the member references (<paramref name="references"/>),
    /// and the type references, forwarders and attribute arguments that resolved before (<paramref name="types"/>).
    /// Nothing is written otherwise.
    /// </summary>
    private static void Verify(ReferenceIndex references, List<(ModuleDefinition module, string name, Func<bool> resolves)> types)
    {
        var broken = types.Where(t => t.resolves() is false).Select(t => $"{t.module.Assembly.Name.Name}: {t.name}").ToList();
        broken.AddRange(references.Broken());
        if (broken.Count > 0)
            throw new MinifierException($"{broken.Count} reference(s) would no longer resolve, e.g. {string.Join("; ", broken.Take(5))}.");
    }

    private List<MinifiedAssembly> Write(List<Loaded> assemblies, Dictionary<ModuleDefinition, (int attributes, int names)> stats)
    {
        // everything is written aside first, so a failure leaves the folder as ILLink produced it
        var staging = Path.Combine(options.Directory, ".bit-minifier");
        if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        Directory.CreateDirectory(staging);
        // decided before anything is written: an embedded pdb goes back into the dll, and nothing next to it
        var withPdbFile = assemblies.Where(a => a.Assembly.MainModule.HasSymbols && a.Assembly.MainModule.SymbolReader is not EmbeddedPortablePdbReader).ToHashSet();
        var results = new List<MinifiedAssembly>();
        try
        {
            foreach (var loaded in assemblies)
            {
                var module = loaded.Assembly.MainModule;
                // the signature no longer matches; .NET doesn't validate it, so say so instead of lying
                module.Attributes &= ~ModuleAttributes.StrongNameSigned;
                // through streams, so the dll names its pdb by file name rather than by its path in the staging folder
                using var dll = File.Create(Path.Combine(staging, loaded.File + ".dll"));
                using var pdb = withPdbFile.Contains(loaded) ? File.Create(Path.Combine(staging, loaded.File + ".pdb")) : null;
                loaded.Assembly.Write(dll, new WriterParameters
                {
                    WriteSymbols = module.HasSymbols,
                    SymbolWriterProvider = pdb is not null ? new PortablePdbWriterProvider() : module.HasSymbols ? new EmbeddedPortablePdbWriterProvider() : null,
                    SymbolStream = pdb,
                    DeterministicMvid = true,
                });
            }

            if (options.MapFile is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.MapFile))!);
                File.WriteAllLines(options.MapFile, map);
            }

            Swap(assemblies, withPdbFile, staging, stats, results);
        }
        finally
        {
            try { Directory.Delete(staging, recursive: true); }
            catch (IOException) { /* a leftover is removed by the next run */ }
            catch (UnauthorizedAccessException) { }
        }
        return results;
    }

    /// <summary>
    /// Moves the originals aside rather than overwriting them, so a failure halfway can put them all back. If one
    /// can't be, the folder is half minified: ILLink's semaphore goes, so the next publish trims it afresh.
    /// </summary>
    private void Swap(List<Loaded> assemblies, HashSet<Loaded> withPdbFile, string staging, Dictionary<ModuleDefinition, (int attributes, int names)> stats, List<MinifiedAssembly> results)
    {
        var replaced = new List<(string original, string target)>();
        var added = new List<string>();
        try
        {
            foreach (var loaded in assemblies)
            {
                var name = loaded.File;
                var target = Path.Combine(options.Directory, name + ".dll");
                var originalSize = new FileInfo(target).Length;
                foreach (var extension in withPdbFile.Contains(loaded) ? new[] { ".dll", ".pdb" } : [".dll"])
                {
                    var file = Path.ChangeExtension(target, extension);
                    if (File.Exists(file))
                    {
                        var original = Path.Combine(staging, name + extension + ".original");
                        File.Move(file, original);
                        replaced.Add((original, file));
                    }
                    else
                    {
                        added.Add(file);
                    }
                    File.Move(Path.Combine(staging, name + extension), file);
                }
                var (attributes, names) = stats[loaded.Assembly.MainModule];
                results.Add(new MinifiedAssembly(name, originalSize, new FileInfo(target).Length, attributes, names));
            }
        }
        catch (Exception e)
        {
            results.Clear();
            var failed = new List<string>();
            foreach (var (original, target) in replaced)
            {
                try { File.Move(original, target, overwrite: true); }
                catch (Exception restore) when (restore is IOException or UnauthorizedAccessException) { failed.Add(Path.GetFileName(target)); }
            }
            foreach (var file in added) TryDelete(file);
            if (options.MapFile is not null) TryDelete(options.MapFile);
            if (failed.Count == 0) throw;

            TryDelete(Path.Combine(options.Directory, "Link.semaphore"));
            throw new MinifierException(
                $"Replacing the assemblies failed ({e.Message}) and {string.Join(", ", failed)} could not be restored, so the folder is partly minified. Publish again: the assemblies will be trimmed afresh.",
                folderUntouched: false);
        }
    }

    private static void TryDelete(string file)
    {
        try { File.Delete(file); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private void Map(ModuleDefinition module, string kind, string original, string name)
        => map.Add($"{module.Assembly.Name.Name}\t{kind}\t{original}\t{name}");

    /// <summary>Whether no attribute on it is one that something may find it by.</summary>
    internal static bool IsNameNeutral(ICustomAttributeProvider provider)
        => provider.CustomAttributes.All(a => NameNeutralAttributes.Contains(a.AttributeType.FullName) || a.AttributeType.Name.EndsWith("StateMachineAttribute", StringComparison.Ordinal));

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
