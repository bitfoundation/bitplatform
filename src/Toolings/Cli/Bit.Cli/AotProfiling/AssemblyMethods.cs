using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Bit.Cli.AotProfiling;

public static class AssemblyMethods
{
    public static string? ReadName(string path)
    {
        try
        {
            using var pe = new PEReader(File.OpenRead(path));
            if (pe.HasMetadata is false)
                return null;

            var metadata = pe.GetMetadataReader();
            return metadata.IsAssembly ? metadata.GetString(metadata.GetAssemblyDefinition().Name) : null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    public static IReadOnlyList<AotProfileMethod> Read(string path, bool unmanagedCallersOnly)
    {
        using var pe = new PEReader(File.OpenRead(path));
        var metadata = pe.GetMetadataReader();
        var image = new AotProfileImage(metadata.GetString(metadata.GetAssemblyDefinition().Name), metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString().ToUpperInvariant());
        var methods = new List<AotProfileMethod>();

        foreach (var typeHandle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(typeHandle);
            var typeName = ClassName(metadata, typeHandle);
            if (typeName == ".<Module>")
                continue;

            AotProfileClass? owner = null;

            foreach (var methodHandle in type.GetMethods())
            {
                var method = metadata.GetMethodDefinition(methodHandle);
                if (method.RelativeVirtualAddress == 0 || method.GetGenericParameters().Count > 0)
                    continue;

                if (unmanagedCallersOnly && method.GetCustomAttributes().Any(a => AttributeName(metadata, a) == "UnmanagedCallersOnlyAttribute") is false)
                    continue;

                var signature = method.DecodeSignature(new SignatureFormatter(metadata, type, method), null);
                owner ??= new AotProfileClass(image, typeName);
                methods.Add(new AotProfileMethod(owner, metadata.GetString(method.Name), signature.ParameterTypes.Length,
                    $"{signature.ReturnType.Text}({string.Join(",", signature.ParameterTypes.Select(p => p.Text))})"));
            }
        }

        return methods;
    }

    /// <summary>
    /// The virtual methods of each generic class the profile ran a method of, for that same instance of the class.
    /// Mono's AOT profiler writes an instance only when each of its type arguments is a class: one over an array, like
    /// the <c>Dictionary&lt;string, byte[]&gt;</c> Blazor reads its persisted component state into, is left out with
    /// every method it ran. That instance runs on the code the instances over reference types share, so when another
    /// one is in the profile, the compiled app runs some of that code compiled and the rest on the interpreter - and a
    /// compiled method calling an interpreted override through the vtable can fail with "function signature mismatch"
    /// (System.Text.Json's dictionary converters, calling their <c>Add(TKey, in TValue, ...)</c>, do). Compiling the
    /// virtual methods of the instances the profile has keeps every such call compiled on both sides.
    /// </summary>
    public static IReadOnlyList<AotProfileMethod> ReadGenericInstances(IEnumerable<AotProfileMethod> profiled, IEnumerable<string> paths)
    {
        var readers = new List<PEReader>();

        try
        {
            var images = new Dictionary<string, MetadataReader>(StringComparer.Ordinal);
            foreach (var path in paths)
            {
                var pe = new PEReader(File.OpenRead(path));
                readers.Add(pe);

                if (pe.HasMetadata && pe.GetMetadataReader() is { IsAssembly: true } metadata)
                    images.TryAdd(metadata.GetString(metadata.GetAssemblyDefinition().Name), metadata);
            }

            var types = new Dictionary<(string Image, string Name), TypeDefinitionHandle>();
            foreach (var (name, metadata) in images)
            {
                foreach (var handle in metadata.TypeDefinitions)
                {
                    types.TryAdd((name, ClassName(metadata, handle)), handle);
                }
            }

            var methods = new List<AotProfileMethod>();
            var classes = profiled.Select(m => m.Class).Where(c => c.Arguments is not null).DistinctBy(c => c.Key).ToList();

            foreach (var owner in classes)
            {
                if (images.TryGetValue(owner.Image.Name, out var metadata) is false || types.TryGetValue((owner.Image.Name, owner.Name), out var typeHandle) is false)
                    continue;

                var arguments = owner.Arguments!.Select(a => MonoName(a, images, types)).ToList();
                var type = metadata.GetTypeDefinition(typeHandle);
                if (arguments.Any(a => a is null) || type.GetGenericParameters().Count != arguments.Count)
                    continue;

                // An instance over value types alone has code of its own, which no instance the profiler left out runs on.
                if (owner.Arguments!.All(a => IsValueType(a, images, types)))
                    continue;

                foreach (var methodHandle in type.GetMethods())
                {
                    var method = metadata.GetMethodDefinition(methodHandle);
                    // Virtual methods alone: a call through the vtable is the one that breaks, and the whole class
                    // would make the download almost twice what this adds.
                    if (method.RelativeVirtualAddress == 0 || method.GetGenericParameters().Count > 0 || (method.Attributes & MethodAttributes.Virtual) == 0)
                        continue;

                    var signature = method.DecodeSignature(new SignatureFormatter(metadata, type, method, arguments!), null);
                    methods.Add(new AotProfileMethod(owner, metadata.GetString(method.Name), signature.ParameterTypes.Length,
                        $"{signature.ReturnType.Text}({string.Join(",", signature.ParameterTypes.Select(p => p.Text))})"));
                }
            }

            return methods;
        }
        finally
        {
            readers.ForEach(r => r.Dispose());
        }
    }

    /// <summary>The name Mono gives a profiled class where it stands in a signature, or null when it isn't in the app.</summary>
    private static string? MonoName(AotProfileClass type, Dictionary<string, MetadataReader> images, Dictionary<(string Image, string Name), TypeDefinitionHandle> types)
    {
        if (type.Image.Name == "System.Private.CoreLib" && SignatureFormatter.PrimitiveNames.TryGetValue(type.Name, out var primitive))
            return primitive;

        if (images.ContainsKey(type.Image.Name) is false || types.ContainsKey((type.Image.Name, type.Name)) is false)
            return null;

        if (type.Arguments is null)
            return type.Name;

        var arguments = type.Arguments.Select(a => MonoName(a, images, types)).ToList();
        return arguments.Any(a => a is null) ? null : $"{type.Name}<{string.Join(", ", arguments)}>";
    }

    private static bool IsValueType(AotProfileClass type, Dictionary<string, MetadataReader> images, Dictionary<(string Image, string Name), TypeDefinitionHandle> types)
    {
        if (images.TryGetValue(type.Image.Name, out var metadata) is false || types.TryGetValue((type.Image.Name, type.Name), out var handle) is false)
            return false;

        var baseType = metadata.GetTypeDefinition(handle).BaseType;
        if (baseType.IsNil)
            return false;

        var baseName = baseType.Kind switch
        {
            HandleKind.TypeReference => metadata.GetTypeReference((TypeReferenceHandle)baseType) is var reference ? $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}" : null,
            HandleKind.TypeDefinition => ClassName(metadata, (TypeDefinitionHandle)baseType),
            _ => null
        };

        return baseName is "System.ValueType" or "System.Enum" && type.Name != "System.Enum";
    }

    private static string ClassName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var type = metadata.GetTypeDefinition(handle);
        var declaring = type.GetDeclaringType();
        return declaring.IsNil
            ? $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}"
            : $"{ClassName(metadata, declaring)}/{metadata.GetString(type.Name)}";
    }

    private static string AttributeName(MetadataReader metadata, CustomAttributeHandle handle)
    {
        var constructor = metadata.GetCustomAttribute(handle).Constructor;

        if (constructor.Kind == HandleKind.MemberReference)
        {
            var parent = metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent;
            return parent.Kind == HandleKind.TypeReference ? metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)parent).Name) : "";
        }

        return metadata.GetString(metadata.GetTypeDefinition(metadata.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType()).Name);
    }
}

public sealed record SignatureType(string Core, string Modifiers = "", bool ByReference = false)
{
    public string Text => Core + Modifiers + (ByReference ? "&" : "");
}

/// <param name="typeArguments">The names the type's generic parameters stand for, in a signature of an instance of it; null keeps the parameters' own names.</param>
public sealed class SignatureFormatter(MetadataReader metadata, TypeDefinition type, MethodDefinition method, IReadOnlyList<string>? typeArguments = null) : ISignatureTypeProvider<SignatureType, object?>
{
    /// <summary>The primitive types by their full names, under the names Mono writes them by.</summary>
    public static readonly IReadOnlyDictionary<string, string> PrimitiveNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["System.Boolean"] = "bool",
        ["System.Char"] = "char",
        ["System.SByte"] = "sbyte",
        ["System.Byte"] = "byte",
        ["System.Int16"] = "int16",
        ["System.UInt16"] = "uint16",
        ["System.Int32"] = "int",
        ["System.UInt32"] = "uint",
        ["System.Int64"] = "long",
        ["System.UInt64"] = "ulong",
        ["System.Single"] = "single",
        ["System.Double"] = "double",
        ["System.IntPtr"] = "intptr",
        ["System.UIntPtr"] = "uintptr",
        ["System.Object"] = "object",
        ["System.String"] = "string",
        ["System.TypedReference"] = "typedbyref"
    };

    public SignatureType GetPrimitiveType(PrimitiveTypeCode typeCode) => new(typeCode switch
    {
        PrimitiveTypeCode.Void => "void",
        PrimitiveTypeCode.Boolean => "bool",
        PrimitiveTypeCode.Char => "char",
        PrimitiveTypeCode.SByte => "sbyte",
        PrimitiveTypeCode.Byte => "byte",
        PrimitiveTypeCode.Int16 => "int16",
        PrimitiveTypeCode.UInt16 => "uint16",
        PrimitiveTypeCode.Int32 => "int",
        PrimitiveTypeCode.UInt32 => "uint",
        PrimitiveTypeCode.Int64 => "long",
        PrimitiveTypeCode.UInt64 => "ulong",
        PrimitiveTypeCode.Single => "single",
        PrimitiveTypeCode.Double => "double",
        PrimitiveTypeCode.IntPtr => "intptr",
        PrimitiveTypeCode.UIntPtr => "uintptr",
        PrimitiveTypeCode.Object => "object",
        PrimitiveTypeCode.String => "string",
        PrimitiveTypeCode.TypedReference => "typedbyref",
        _ => throw new NotSupportedException(typeCode.ToString())
    });

    public SignatureType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => new(DefinitionName(reader, handle));

    public SignatureType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => new(ReferenceName(reader, handle));

    public SignatureType GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public SignatureType GetSZArrayType(SignatureType elementType) => new(elementType.Text + "[]");

    public SignatureType GetArrayType(SignatureType elementType, ArrayShape shape) => new(elementType.Text + "[" + new string(',', shape.Rank - 1) + "]");

    public SignatureType GetByReferenceType(SignatureType elementType) => new(elementType.Core + elementType.Modifiers, "", true);

    public SignatureType GetPointerType(SignatureType elementType) => new(elementType.Text + "*");

    public SignatureType GetGenericInstantiation(SignatureType genericType, ImmutableArray<SignatureType> typeArguments)
        => new(genericType.Text + "<" + string.Join(", ", typeArguments.Select(a => a.Text)) + ">");

    public SignatureType GetGenericTypeParameter(object? genericContext, int index)
        => new(typeArguments?[index] ?? metadata.GetString(metadata.GetGenericParameter(type.GetGenericParameters()[index]).Name));

    public SignatureType GetGenericMethodParameter(object? genericContext, int index)
        => new(metadata.GetString(metadata.GetGenericParameter(method.GetGenericParameters()[index]).Name));

    public SignatureType GetFunctionPointerType(MethodSignature<SignatureType> signature) => new("*()");

    public SignatureType GetModifiedType(SignatureType modifier, SignatureType unmodifiedType, bool isRequired)
        => new(unmodifiedType.Core, unmodifiedType.Modifiers + (isRequired ? " modreq(" : " modopt(") + modifier.Text + ")", unmodifiedType.ByReference);

    public SignatureType GetPinnedType(SignatureType elementType) => elementType;

    private static string DefinitionName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        var declaring = definition.GetDeclaringType();
        return declaring.IsNil
            ? Qualified(reader.GetString(definition.Namespace), reader.GetString(definition.Name))
            : DefinitionName(reader, declaring) + "/" + reader.GetString(definition.Name);
    }

    private static string ReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var reference = reader.GetTypeReference(handle);
        return reference.ResolutionScope.Kind == HandleKind.TypeReference
            ? ReferenceName(reader, (TypeReferenceHandle)reference.ResolutionScope) + "/" + reader.GetString(reference.Name)
            : Qualified(reader.GetString(reference.Namespace), reader.GetString(reference.Name));
    }

    private static string Qualified(string ns, string name) => ns.Length > 0 ? ns + "." + name : name;
}
