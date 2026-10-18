using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace Bit.BlazorUI.Build;

/// <summary>
/// The untrimmed assemblies of the bit BlazorUI packages an app references, as one graph of types: a reference one of
/// them makes to another's type (Extras rendering a BitButton of the core) is an edge like any other.
/// </summary>
public sealed class LibraryGraph : IDisposable
{
    private readonly Dictionary<string, LibraryAssembly> _assemblies;

    private LibraryGraph(IEnumerable<LibraryAssembly> assemblies)
    {
        _assemblies = assemblies.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in _assemblies.Values) assembly.Graph = this;
    }

    public IEnumerable<LibraryAssembly> Assemblies => _assemblies.Values;

    public bool Contains(string assemblyName) => _assemblies.ContainsKey(assemblyName);

    public static LibraryGraph Load(IEnumerable<string> paths)
    {
        var loaded = new List<LibraryAssembly>();
        try
        {
            foreach (var path in paths) loaded.Add(LibraryAssembly.Load(path));

            var duplicate = loaded.GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Two copies of {duplicate.Key}: {string.Join(", ", duplicate.Select(a => a.Path))}.");
            }

            return new LibraryGraph(loaded);
        }
        catch
        {
            foreach (var assembly in loaded) assembly.Dispose();
            throw;
        }
    }

    /// <summary>Every library type named this way, the arity left off: a csproj names "BitDropdown", not "BitDropdown`2".</summary>
    public IEnumerable<TypeNode> FindByName(string name)
        => _assemblies.Values.SelectMany(a => a.Types).Where(t => t.FullName.IndexOf('/') < 0 && string.Equals(t.Name, name, StringComparison.Ordinal));

    /// <summary>The types reachable from the given ones, them included.</summary>
    public HashSet<TypeNode> Closure(IEnumerable<TypeNode> roots)
    {
        var reached = new HashSet<TypeNode>();
        var pending = new Stack<TypeNode>(roots);

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (reached.Add(node) is false) continue;

            if (node.Edges is null) node.Assembly.Expand(node);
            foreach (var edge in node.Edges!)
            {
                if (reached.Contains(edge) is false) pending.Push(edge);
            }
        }

        return reached;
    }

    /// <summary>The string literals the code of the given types loads.</summary>
    public static IEnumerable<string> StringsOf(IEnumerable<TypeNode> types)
    {
        foreach (var type in types)
        {
            if (type.Strings is null) type.Assembly.Expand(type);
            foreach (var s in type.Strings!) yield return s;
        }
    }

    /// <summary>Resolves a type reference made by any assembly to a library type, or null for a type of any other assembly.</summary>
    public TypeNode? Resolve(MetadataReader reader, TypeReferenceHandle handle)
    {
        if (TryGetReferencedName(reader, handle, out var assemblyName, out var fullName) is false) return null;
        if (assemblyName is null) return null;

        return _assemblies.TryGetValue(assemblyName, out var assembly) && assembly.TryGetType(fullName, out var node) ? node : null;
    }

    /// <summary>
    /// The assembly a type reference points into and the type's full name there, nested types spelled the way
    /// <see cref="TypeNode.FullName"/> spells them. The assembly is null for a reference within the same module.
    /// </summary>
    public static bool TryGetReferencedName(MetadataReader reader, TypeReferenceHandle handle, out string? assemblyName, out string fullName)
    {
        var reference = reader.GetTypeReference(handle);
        var name = reader.GetString(reference.Name);
        var scope = reference.ResolutionScope;

        switch (scope.Kind)
        {
            case HandleKind.AssemblyReference:
                var ns = reader.GetString(reference.Namespace);
                assemblyName = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name);
                fullName = ns.Length == 0 ? name : $"{ns}.{name}";
                return true;

            case HandleKind.TypeReference:
                if (TryGetReferencedName(reader, (TypeReferenceHandle)scope, out assemblyName, out var outer))
                {
                    fullName = $"{outer}/{name}";
                    return true;
                }
                break;

            case HandleKind.ModuleDefinition:
            case HandleKind.ModuleReference:
                var moduleNs = reader.GetString(reference.Namespace);
                assemblyName = null;
                fullName = moduleNs.Length == 0 ? name : $"{moduleNs}.{name}";
                return true;
        }

        assemblyName = null;
        fullName = name;
        return false;
    }

    public void Dispose()
    {
        foreach (var assembly in _assemblies.Values) assembly.Dispose();
    }
}
