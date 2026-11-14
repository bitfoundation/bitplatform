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

    /// <summary>
    /// The most arguments a method left on the interpreter can be entered with from compiled code through the wrappers
    /// every AOT build has (Mono's <c>MAX_INTERP_ENTRY_ARGS</c>). One with more needs a wrapper built for its exact
    /// signature, which the AOT compiler builds only for the few calls it sees need one.
    /// </summary>
    public const int MaxInterpreterEntryArguments = 8;

    /// <summary>
    /// The methods of the assembly - every one, or only its <c>[UnmanagedCallersOnly]</c> ones - that have a body and
    /// are not generic themselves. A method of a generic type is listed for its definition, and once more for each
    /// instantiation of the type in <paramref name="instantiated"/> - the classes of the methods the app ran - with its
    /// signature inflated the way Mono's profiler writes it: only an instantiation can be compiled.
    /// </summary>
    public static IReadOnlyList<AotProfileMethod> Read(string path, bool unmanagedCallersOnly, IEnumerable<AotProfileClass>? instantiated = null)
        => Read(path, (metadata, method, _) => unmanagedCallersOnly is false || method.GetCustomAttributes().Any(a => AttributeName(metadata, a) == "UnmanagedCallersOnlyAttribute"), instantiated);

    /// <summary>
    /// The methods with more than <see cref="MaxInterpreterEntryArguments"/> parameters, not counting <c>this</c>. Left
    /// on the interpreter, such a method stops the app with a Mono assertion at <c>interp.c</c> once compiled code needs
    /// a pointer to it - to call it, to make a delegate of it, or to fill an interface slot, which sets up every method
    /// sharing that slot, called or not - since there is no wrapper for its signature. EF Core's
    /// <c>RelationalCommandDiagnosticsLogger</c> alone has a dozen. They are few, so compiling them all costs little.
    /// </summary>
    /// <remarks>
    /// Listed as <see cref="Read(string, bool, IEnumerable{AotProfileClass}?)"/> lists methods: one of a generic type
    /// for each instantiation in <paramref name="instantiated"/>. A generic method is left out - nothing in the
    /// assembly says which instantiations the app makes - and is compiled only when one of them ran.
    /// <para>
    /// Mono's AOT compiler finds a profiled method by its class, name, parameter count and signature text, taking the
    /// first that matches, so overloads are told apart only by their signatures. Those that differ in their parameter
    /// types are each listed and found. A generic type's overloads that one of its instantiations inflates to the same
    /// signature - <c>Log(T, ...)</c> and <c>Log(string, ...)</c> once <c>T</c> is <c>string</c> - are a known
    /// limitation: that instantiation gets a single entry, which compiles the overload declared first, and the other
    /// stays on the interpreter. No signature text written here could name it, so it is kept rather than left out.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<AotProfileMethod> ReadWide(string path, IEnumerable<AotProfileClass>? instantiated = null)
        => Read(path, (_, _, parameterCount) => parameterCount > MaxInterpreterEntryArguments, instantiated);

    private static List<AotProfileMethod> Read(string path, Func<MetadataReader, MethodDefinition, int, bool> keep, IEnumerable<AotProfileClass>? instantiated)
    {
        using var pe = new PEReader(File.OpenRead(path));
        var metadata = pe.GetMetadataReader();
        var image = new AotProfileImage(metadata.GetString(metadata.GetAssemblyDefinition().Name), metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString().ToUpperInvariant());
        var methods = new List<AotProfileMethod>();

        // This assembly's generic types as the profile names their instantiations: by name, with the type arguments.
        var instances = (instantiated ?? [])
            .Where(c => c.Arguments is { Count: > 0 } && c.Image.Name == image.Name)
            .DistinctBy(c => c.Key)
            .ToLookup(c => c.Name);

        foreach (var typeHandle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(typeHandle);
            var typeName = ClassName(metadata, typeHandle);
            if (typeName == ".<Module>")
                continue;

            var arity = type.GetGenericParameters().Count;
            List<(AotProfileClass Class, IReadOnlyList<string>? Arguments)>? owners = null;

            foreach (var methodHandle in type.GetMethods())
            {
                var method = metadata.GetMethodDefinition(methodHandle);
                if (method.RelativeVirtualAddress == 0 || method.GetGenericParameters().Count > 0)
                    continue;

                // The count from the signature's header, so only a method that is kept has its signature decoded.
                if (ParameterCount(metadata, method) is not { } parameterCount || keep(metadata, method, parameterCount) is false)
                    continue;

                owners ??= [(new AotProfileClass(image, typeName), null),
                    .. arity == 0 ? [] : instances[typeName].Where(c => c.Arguments!.Count == arity).Select(c => (c, (IReadOnlyList<string>?)[.. c.Arguments!.Select(TypeText)]))];

                foreach (var (owner, arguments) in owners)
                {
                    // A signature this formatter can't decode is left out: that method stays on the interpreter, which
                    // is better than a profile that is never written.
                    MethodSignature<SignatureType> signature;
                    try
                    {
                        signature = method.DecodeSignature(new SignatureFormatter(metadata, type, method), arguments);
                    }
                    catch (Exception exp) when (exp is BadImageFormatException or NotSupportedException or ArgumentOutOfRangeException or InvalidOperationException)
                    {
                        continue;
                    }

                    methods.Add(new AotProfileMethod(owner, metadata.GetString(method.Name), signature.ParameterTypes.Length,
                        $"{signature.ReturnType.Text}({string.Join(",", signature.ParameterTypes.Select(p => p.Text))})"));
                }
            }
        }

        return methods;
    }

    private static int? ParameterCount(MetadataReader metadata, MethodDefinition method)
    {
        try
        {
            var blob = metadata.GetBlobReader(method.Signature);
            if (blob.ReadSignatureHeader().IsGeneric)
                blob.ReadCompressedInteger();

            return blob.ReadCompressedInteger();
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>A type argument the way Mono prints it in a signature: <c>int</c> for <c>System.Int32</c>.</summary>
    private static string TypeText(AotProfileClass type)
    {
        var name = type.Image.Name == "System.Private.CoreLib" && type.Arguments is null && SignatureFormatter.Keywords.TryGetValue(type.Name, out var keyword) ? keyword : type.Name;
        return type.Arguments is { Count: > 0 } arguments ? $"{name}<{string.Join(", ", arguments.Select(TypeText))}>" : name;
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
    /// <summary>The names Mono prints for System.Private.CoreLib's primitive types, as <see cref="GetPrimitiveType"/> does.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Keywords = new Dictionary<string, string>
    {
        ["System.Void"] = "void",
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
        ["System.TypedReference"] = "typedbyref",
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

    /// <summary>
    /// The type's own parameter by name, as in its definition, or - given an instantiation's type arguments as the
    /// generic context - the argument, as in the inflated signature Mono's profiler writes for the instantiation.
    /// </summary>
    public SignatureType GetGenericTypeParameter(object? genericContext, int index)
        => new(genericContext is IReadOnlyList<string> arguments
            ? arguments[index]
            : metadata.GetString(metadata.GetGenericParameter(type.GetGenericParameters()[index]).Name));

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
