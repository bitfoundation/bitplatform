using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Collects the library types a signature or a token names, into the edges of the type being expanded. A signature
/// type provider whose "types" are nothing: decoding a signature is only done for the types it visits on the way.
/// </summary>
internal sealed class TypeCollector(LibraryAssembly assembly, HashSet<TypeNode> into) : ISignatureTypeProvider<object?, object?>
{
    private MetadataReader Reader => assembly.Reader;

    public void Add(EntityHandle handle)
    {
        if (handle.IsNil) return;

        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                into.Add(assembly.GetNode((TypeDefinitionHandle)handle));
                break;
            case HandleKind.TypeReference:
                if (assembly.Resolve((TypeReferenceHandle)handle) is { } node) into.Add(node);
                break;
            case HandleKind.TypeSpecification:
                Reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(this, null);
                break;
        }
    }

    /// <summary>A token an instruction names: a type, or the type a method or field it names belongs to.</summary>
    public void AddToken(EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
            case HandleKind.TypeReference:
            case HandleKind.TypeSpecification:
                Add(handle);
                break;
            case HandleKind.MethodDefinition:
                into.Add(assembly.GetNode(Reader.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType()));
                break;
            case HandleKind.FieldDefinition:
                into.Add(assembly.GetNode(Reader.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType()));
                break;
            case HandleKind.MemberReference:
                AddToken(Reader.GetMemberReference((MemberReferenceHandle)handle).Parent);
                break;
            case HandleKind.MethodSpecification:
                var spec = Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                AddToken(spec.Method);
                spec.DecodeSignature(this, null);
                break;
        }
    }

    public object? GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        into.Add(assembly.GetNode(handle));
        return null;
    }

    public object? GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        if (assembly.Resolve(handle) is { } node) into.Add(node);
        return null;
    }

    public object? GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        return null;
    }

    public object? GetSZArrayType(object? elementType) => null;
    public object? GetArrayType(object? elementType, ArrayShape shape) => null;
    public object? GetByReferenceType(object? elementType) => null;
    public object? GetPointerType(object? elementType) => null;
    public object? GetPinnedType(object? elementType) => null;
    public object? GetGenericInstantiation(object? genericType, ImmutableArray<object?> typeArguments) => null;
    public object? GetGenericMethodParameter(object? genericContext, int index) => null;
    public object? GetGenericTypeParameter(object? genericContext, int index) => null;
    public object? GetFunctionPointerType(MethodSignature<object?> signature) => null;
    public object? GetModifiedType(object? modifier, object? unmodifiedType, bool isRequired) => null;
    public object? GetPrimitiveType(PrimitiveTypeCode typeCode) => null;
}
