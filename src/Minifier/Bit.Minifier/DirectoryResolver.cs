using Mono.Cecil;

namespace Bit.Minifier;

/// <summary>
/// Resolves from the trimmed folder only, reading everything into memory so no file stays locked, and hands
/// out the very instances being rewritten for the assemblies being minified.
/// </summary>
internal sealed class DirectoryResolver : BaseAssemblyResolver
{
    private readonly string directory;
    private readonly Dictionary<string, AssemblyDefinition?> cache = new(StringComparer.OrdinalIgnoreCase);
    // the cache may hold one assembly under two names, so what is disposed is kept apart from what is looked up
    private readonly List<AssemblyDefinition> loaded = [];

    public DirectoryResolver(string directory)
    {
        this.directory = directory;
        foreach (var path in GetSearchDirectories()) RemoveSearchDirectory(path);
        AddSearchDirectory(directory);
    }

    public void Register(AssemblyDefinition assembly)
    {
        cache[assembly.Name.Name] = assembly;
        loaded.Add(assembly);
    }

    public AssemblyDefinition? TryLoad(string name)
    {
        if (cache.TryGetValue(name, out var cached)) return cached;
        var path = Path.Combine(directory, name + ".dll");
        AssemblyDefinition? assembly = null;
        if (File.Exists(path))
        {
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = this, InMemory = true });
            }
            catch (BadImageFormatException)
            {
                // not a managed assembly
            }
        }
        cache[name] = assembly;
        if (assembly is not null)
        {
            loaded.Add(assembly);
            // a file need not be named after the assembly in it: whoever asks for that assembly by name gets this
            // very instance rather than a second copy, which would resolve into definitions nothing rewrites
            cache.TryAdd(assembly.Name.Name, assembly);
        }
        return assembly;
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
        => TryLoad(name.Name) ?? throw new AssemblyResolutionException(name);

    protected override void Dispose(bool disposing)
    {
        foreach (var assembly in loaded) assembly.Dispose();
        loaded.Clear();
        cache.Clear();
        base.Dispose(disposing);
    }
}
