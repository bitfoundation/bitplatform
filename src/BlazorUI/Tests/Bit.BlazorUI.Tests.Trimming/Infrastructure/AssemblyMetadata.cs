using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Bit.BlazorUI.Tests.Trimming.Infrastructure;

/// <summary>
/// What an assembly holds, read off its metadata without loading it: a package as ILLink left it in a trimmed publish, or
/// as the package's build wrote it.
/// </summary>
public sealed class AssemblyMetadata
{
    private readonly Dictionary<string, TypeMembers> _types;

    private AssemblyMetadata(List<string> resources, Dictionary<string, TypeMembers> types)
    {
        Resources = resources;
        _types = types;
    }

    /// <summary>The names of the resources the assembly embeds.</summary>
    public IReadOnlyList<string> Resources { get; }

    /// <summary>What a type of the assembly declares, by its full name ("Bit.BlazorUI.BitButtonParams"); null for a type it does not hold.</summary>
    public TypeMembers? FindType(string fullName) => _types.GetValueOrDefault(fullName);

    public static AssemblyMetadata Read(string path)
    {
        Assert.IsTrue(File.Exists(path), $"No assembly at {path}.");

        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();

        var resources = reader.ManifestResources.Select(h => reader.GetString(reader.GetManifestResource(h).Name)).ToList();

        var types = new Dictionary<string, TypeMembers>(StringComparer.Ordinal);
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            if (type.GetDeclaringType().IsNil is false) continue;

            var name = reader.GetString(type.Name);
            var ns = reader.GetString(type.Namespace);
            types[ns.Length == 0 ? name : $"{ns}.{name}"] = new(
                [.. type.GetMethods().Select(m => reader.GetString(reader.GetMethodDefinition(m).Name))],
                [.. type.GetFields().Select(f => reader.GetString(reader.GetFieldDefinition(f).Name))],
                [.. type.GetProperties().Select(p => reader.GetString(reader.GetPropertyDefinition(p).Name))],
                [.. type.GetNestedTypes().Select(n => reader.GetString(reader.GetTypeDefinition(n).Name))]);
        }

        return new(resources, types);
    }
}

/// <summary>The names of what a type declares itself - its nested types (a closure class of its lambdas) by name only.</summary>
public sealed record TypeMembers(List<string> Methods, List<string> Fields, List<string> Properties, List<string> NestedTypes);
