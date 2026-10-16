using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Bit.BlazorUI.Build;

/// <summary>
/// One untrimmed assembly of the bit BlazorUI packages, read for what each of its types reaches - which is how a publish
/// ILLink never touched learns which components, and so which scripts and stylesheets, the types an app references can
/// bring in.
/// </summary>
public sealed class LibraryAssembly : IDisposable
{
    private readonly PEReader _pe;
    private readonly Dictionary<string, TypeNode> _byFullName = new(StringComparer.Ordinal);
    private readonly Dictionary<TypeDefinitionHandle, TypeNode> _byHandle = [];

    private LibraryAssembly(string path, PEReader pe, MetadataReader reader, string name)
    {
        Path = path;
        _pe = pe;
        Reader = reader;
        Name = name;
    }

    public string Path { get; }

    /// <summary>The assembly's simple name, as other assemblies' references name it.</summary>
    public string Name { get; }

    internal MetadataReader Reader { get; }

    internal LibraryGraph? Graph { get; set; }

    public IEnumerable<TypeNode> Types => _byHandle.Values;

    public static LibraryAssembly Load(string path)
    {
        var pe = new PEReader(File.OpenRead(path));
        try
        {
            if (pe.HasMetadata is false) throw new BadImageFormatException($"'{path}' is not a managed assembly.");
            var reader = pe.GetMetadataReader();
            if (reader.IsAssembly is false) throw new BadImageFormatException($"'{path}' is a module, not an assembly.");

            var assembly = new LibraryAssembly(path, pe, reader, reader.GetString(reader.GetAssemblyDefinition().Name));
            foreach (var handle in reader.TypeDefinitions) assembly.Index(handle);
            return assembly;
        }
        catch
        {
            pe.Dispose();
            throw;
        }
    }

    public bool TryGetType(string fullName, out TypeNode node) => _byFullName.TryGetValue(fullName, out node!);

    internal TypeNode GetNode(TypeDefinitionHandle handle) => _byHandle[handle];

    private TypeNode Index(TypeDefinitionHandle handle)
    {
        if (_byHandle.TryGetValue(handle, out var existing)) return existing;

        var definition = Reader.GetTypeDefinition(handle);
        var name = Reader.GetString(definition.Name);
        var declaring = definition.GetDeclaringType();

        string fullName;
        if (declaring.IsNil)
        {
            var ns = Reader.GetString(definition.Namespace);
            fullName = ns.Length == 0 ? name : $"{ns}.{name}";
        }
        else
        {
            fullName = $"{Index(declaring).FullName}/{name}";
        }

        var tick = name.IndexOf('`');
        var node = new TypeNode(this, handle, fullName, tick < 0 ? name : name.Substring(0, tick));
        _byHandle[handle] = node;
        _byFullName[fullName] = node;
        return node;
    }

    /// <summary>
    /// What a type reaches: its base type and nested types (the closures, iterators and async state machines its own
    /// methods are compiled into), the types of its fields and locals, and every type, method and field its method
    /// bodies name - generic arguments included, which is how <c>builder.OpenComponent&lt;BitButton&gt;()</c> reaches
    /// BitButton. Over-inclusion is the direction this errs in: a path that is never walked still counts.
    /// </summary>
    internal void Expand(TypeNode node)
    {
        var edges = new HashSet<TypeNode>();
        var strings = new List<string>();
        var collector = new TypeCollector(this, edges);
        var definition = Reader.GetTypeDefinition(node.Handle);

        // A class that only registers services (Bit.BlazorUI's BitAssetTrimmingRegistrarAttribute) reaches every service,
        // and every service what it renders; registering renders nothing, and an app that uses a service names it itself.
        if (HasAttribute(definition, RegistrarAttribute))
        {
            node.Edges = [];
            node.Strings = [];
            return;
        }

        collector.Add(definition.BaseType);

        foreach (var nested in definition.GetNestedTypes()) edges.Add(GetNode(nested));

        foreach (var fieldHandle in definition.GetFields())
        {
            Reader.GetFieldDefinition(fieldHandle).DecodeSignature(collector, null);
        }

        foreach (var methodHandle in definition.GetMethods())
        {
            var method = Reader.GetMethodDefinition(methodHandle);
            if (method.RelativeVirtualAddress == 0) continue;

            var body = _pe.GetMethodBody(method.RelativeVirtualAddress);
            if (body.LocalSignature.IsNil is false)
            {
                var signature = Reader.GetStandaloneSignature(body.LocalSignature);
                if (signature.GetKind() == StandaloneSignatureKind.LocalVariables) signature.DecodeLocalSignature(collector, null);
            }

            var il = body.GetILReader();
            ILScanner.Scan(ref il,
                token => collector.AddToken(MetadataTokens.EntityHandle(token)),
                stringToken => strings.Add(Reader.GetUserString(MetadataTokens.UserStringHandle(stringToken & 0x00FFFFFF))));
        }

        edges.Remove(node);
        node.Edges = [.. edges];
        node.Strings = strings;
    }

    /// <summary>The name of the attribute marking a class whose references the trimming does not follow.</summary>
    public const string RegistrarAttribute = "BitAssetTrimmingRegistrarAttribute";

    private bool HasAttribute(TypeDefinition definition, string name)
    {
        foreach (var handle in definition.GetCustomAttributes())
        {
            var constructor = Reader.GetCustomAttribute(handle).Constructor;
            var type = constructor.Kind switch
            {
                HandleKind.MethodDefinition => (EntityHandle)Reader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
                HandleKind.MemberReference => Reader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
                _ => default,
            };

            var typeName = type.Kind switch
            {
                HandleKind.TypeDefinition => Reader.GetString(Reader.GetTypeDefinition((TypeDefinitionHandle)type).Name),
                HandleKind.TypeReference => Reader.GetString(Reader.GetTypeReference((TypeReferenceHandle)type).Name),
                _ => null,
            };

            if (string.Equals(typeName, name, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>Resolves a reference this assembly makes to a type of any library assembly, or null for any other type.</summary>
    internal TypeNode? Resolve(TypeReferenceHandle handle) => Graph?.Resolve(Reader, handle);

    public void Dispose() => _pe.Dispose();
}
