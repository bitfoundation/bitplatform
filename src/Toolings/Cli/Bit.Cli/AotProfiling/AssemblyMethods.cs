using System.Collections.Immutable;
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

public sealed class SignatureFormatter(MetadataReader metadata, TypeDefinition type, MethodDefinition method) : ISignatureTypeProvider<SignatureType, object?>
{
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
        => new(metadata.GetString(metadata.GetGenericParameter(type.GetGenericParameters()[index]).Name));

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
