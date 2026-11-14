using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Bit.Cli.AotProfiling;

public static class AssemblyMethods
{
    internal const string CoreLib = "System.Private.CoreLib";

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
    {
        using var pe = new PEReader(File.OpenRead(path));
        return Read(pe.GetMetadataReader(), unmanagedCallersOnly, instantiated);
    }

    /// <inheritdoc cref="Read(string, bool, IEnumerable{AotProfileClass}?)"/>
    public static IReadOnlyList<AotProfileMethod> Read(MetadataReader metadata, bool unmanagedCallersOnly, IEnumerable<AotProfileClass>? instantiated = null)
        => Read(metadata, (method, _) => unmanagedCallersOnly is false || method.GetCustomAttributes().Any(a => AttributeName(metadata, a) == "UnmanagedCallersOnlyAttribute"), instantiated);

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
    {
        using var pe = new PEReader(File.OpenRead(path));
        return ReadWide(pe.GetMetadataReader(), instantiated);
    }

    /// <inheritdoc cref="ReadWide(string, IEnumerable{AotProfileClass}?)"/>
    public static IReadOnlyList<AotProfileMethod> ReadWide(MetadataReader metadata, IEnumerable<AotProfileClass>? instantiated = null)
        => Read(metadata, (_, parameterCount) => parameterCount > MaxInterpreterEntryArguments, instantiated);

    private static List<AotProfileMethod> Read(MetadataReader metadata, Func<MethodDefinition, int, bool> keep, IEnumerable<AotProfileClass>? instantiated)
    {
        var image = new AotProfileImage(AssemblyName(metadata), Mvid(metadata));
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
                if (ParameterCount(metadata, method) is not { } parameterCount || keep(method, parameterCount) is false)
                    continue;

                owners ??= [(new AotProfileClass(image, typeName), null),
                    .. arity == 0 ? [] : instances[typeName].Where(c => c.Arguments!.Count == arity).Select(c => (c, (IReadOnlyList<string>?)[.. c.Arguments!.Select(TypeText)]))];

                foreach (var (owner, arguments) in owners)
                {
                    // A signature this formatter can't decode is left out: that method stays on the interpreter, which
                    // is better than a profile that is never written.
                    try
                    {
                        methods.Add(ProfileMethod(owner, metadata, type, method, arguments));
                    }
                    catch (Exception exp) when (exp is BadImageFormatException or NotSupportedException or ArgumentOutOfRangeException or InvalidOperationException)
                    {
                    }
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
        var name = type.Image.Name == CoreLib && type.Arguments is null && SignatureFormatter.PrimitiveNames.TryGetValue(type.Name, out var keyword) ? keyword : type.Name;
        return type.Arguments is { Count: > 0 } arguments ? $"{name}<{string.Join(", ", arguments.Select(TypeText))}>" : name;
    }

    public static IReadOnlyList<AotProfileMethod> ReadGenericInstances(IEnumerable<AotProfileMethod> profiled, IEnumerable<string> paths)
    {
        using var assemblies = new AppAssemblies(paths);
        return ReadGenericInstances(profiled, assemblies);
    }

    /// <summary>
    /// The virtual methods of each generic class the profile ran a method of, and of its generic base classes, for that
    /// same instance of the class. Mono's AOT profiler writes an instance only when each of its type arguments is a
    /// class: one over an array, like the <c>Dictionary&lt;string, byte[]&gt;</c> Blazor reads its persisted component
    /// state into, is left out with every method it ran. That instance runs on the code the instances over reference
    /// types share, so when another one is in the profile, the compiled app runs some of that code compiled and the rest
    /// on the interpreter - and a compiled method calling an interpreted override through the vtable can fail with
    /// "function signature mismatch" (System.Text.Json's dictionary converters, calling their
    /// <c>Add(TKey, in TValue, ...)</c>, do). Compiling the virtual methods of the instances the profile has keeps every
    /// such call compiled on both sides.
    /// </summary>
    public static IReadOnlyList<AotProfileMethod> ReadGenericInstances(IEnumerable<AotProfileMethod> profiled, AppAssemblies assemblies)
    {
        var classes = profiled.Select(m => m.Class).Where(c => c.Arguments is not null).ToList();

        // The profile's own image records, so that the base classes added name their images just the way it does.
        var images = new Dictionary<string, AotProfileImage>(StringComparer.Ordinal);
        foreach (var image in classes.SelectMany(Images))
        {
            images.TryAdd(image.Name, image);
        }

        AotProfileImage? ImageOf(string name) => images.TryGetValue(name, out var image) ? image : assemblies.Image(name);

        var methods = new List<AotProfileMethod>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var owner in classes)
        {
            // The class, then each generic base class of it for the type arguments it passes on: a vtable slot can be
            // filled by any of them.
            for (var current = owner; current?.Arguments is not null && visited.Add(current.Key);)
            {
                if (assemblies.TryGetType(current.Image.Name, current.Name, out var metadata, out var typeHandle) is false)
                    break;

                var type = metadata.GetTypeDefinition(typeHandle);
                var arguments = current.Arguments.Select(a => MonoName(a, assemblies)).ToList();
                if (arguments.Any(a => a is null) || type.GetGenericParameters().Count != arguments.Count)
                    break;

                // An instance whose code no other instance shares, like one over value types alone, runs on no code
                // the profiler left out.
                if (IsShared(current.Arguments, assemblies) is not true)
                    break;

                foreach (var methodHandle in type.GetMethods())
                {
                    var method = metadata.GetMethodDefinition(methodHandle);
                    // Virtual methods alone: a call through the vtable is the one that breaks, and the whole class
                    // would make the download almost twice what this adds.
                    if (method.RelativeVirtualAddress == 0 || method.GetGenericParameters().Count > 0 || (method.Attributes & MethodAttributes.Virtual) == 0)
                        continue;

                    methods.Add(ProfileMethod(current, metadata, type, method, arguments!));
                }

                current = type.BaseType.Kind == HandleKind.TypeSpecification
                    ? metadata.GetTypeSpecification((TypeSpecificationHandle)type.BaseType).DecodeSignature(new ProfileClassProvider(assemblies, ImageOf, current.Arguments), null)
                    : null;
            }
        }

        return methods;
    }

    internal static string AssemblyName(MetadataReader metadata) => metadata.GetString(metadata.GetAssemblyDefinition().Name);

    internal static string Mvid(MetadataReader metadata) => metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString().ToUpperInvariant();

    /// <summary>A class's name the way the AOT profile writes it: the namespace, even an empty one, a dot, and the classes it's nested in.</summary>
    internal static string ClassName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var type = metadata.GetTypeDefinition(handle);
        var declaring = type.GetDeclaringType();
        return declaring.IsNil
            ? $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}"
            : $"{ClassName(metadata, declaring)}/{metadata.GetString(type.Name)}";
    }

    /// <summary>
    /// The name Mono's AOT profiler gives a class nested in a nested class: it writes the class it's nested in directly,
    /// but not the ones that is nested in, so <c>A.B/C/D</c> is <c>.C/D</c>. Null for any other class, which it names in full.
    /// </summary>
    internal static string? NestedProfilerName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var declaringHandle = metadata.GetTypeDefinition(handle).GetDeclaringType();
        if (declaringHandle.IsNil || metadata.GetTypeDefinition(declaringHandle).GetDeclaringType().IsNil)
            return null;

        var declaring = metadata.GetTypeDefinition(declaringHandle);
        return $"{metadata.GetString(declaring.Namespace)}.{metadata.GetString(declaring.Name)}/{metadata.GetString(metadata.GetTypeDefinition(handle).Name)}";
    }

    private static AotProfileMethod ProfileMethod(AotProfileClass owner, MetadataReader metadata, TypeDefinition type, MethodDefinition method, IReadOnlyList<string>? typeArguments)
    {
        var signature = method.DecodeSignature(new SignatureFormatter(metadata, type, method, typeArguments), null);
        return new AotProfileMethod(owner, metadata.GetString(method.Name), signature.ParameterTypes.Length,
            $"{signature.ReturnType.Text}({string.Join(",", signature.ParameterTypes.Select(p => p.Text))})");
    }

    private static IEnumerable<AotProfileImage> Images(AotProfileClass type)
    {
        return type.Arguments is null ? [type.Image] : [type.Image, .. type.Arguments.SelectMany(Images)];
    }

    /// <summary>The name Mono gives a profiled class where it stands in a signature, or null when it isn't in the app.</summary>
    private static string? MonoName(AotProfileClass type, AppAssemblies assemblies)
    {
        if (type.Image.Name == CoreLib && SignatureFormatter.PrimitiveNames.TryGetValue(type.Name, out var primitive))
            return primitive;

        if (assemblies.TryGetType(type.Image.Name, type.Name, out var metadata, out var handle) is false)
            return null;

        // Mono names a class in full there, with every class it's nested in and without a dot for the global namespace.
        var name = SignatureFormatter.DefinitionName(metadata, handle);
        if (type.Arguments is null)
            return name;

        var arguments = type.Arguments.Select(a => MonoName(a, assemblies)).ToList();
        return arguments.Any(a => a is null) ? null : $"{name}<{string.Join(", ", arguments)}>";
    }

    /// <summary>
    /// Whether Mono runs an instance over these type arguments on code other instances share: it shares the code over a
    /// reference type, and over a generic struct of one as well (<c>KeyValuePair&lt;string, string&gt;</c> runs on
    /// <c>KeyValuePair&lt;__Canon, __Canon&gt;</c>). Null when it can't tell, for an argument that isn't in the app.
    /// </summary>
    private static bool? IsShared(IReadOnlyList<AotProfileClass> arguments, AppAssemblies assemblies)
    {
        bool? shared = false;

        foreach (var argument in arguments)
        {
            var argumentShared = IsValueType(argument, assemblies) switch
            {
                false => true,
                true => argument.Arguments is null ? false : IsShared(argument.Arguments, assemblies),
                null => null
            };

            if (argumentShared is true)
                return true;

            if (argumentShared is null)
                shared = null;
        }

        return shared;
    }

    /// <summary>Whether a profiled class is a value type, or null when it isn't in the app.</summary>
    private static bool? IsValueType(AotProfileClass type, AppAssemblies assemblies)
    {
        if (type.Image.Name == CoreLib && SignatureFormatter.PrimitiveNames.ContainsKey(type.Name))
            return type.Name is not ("System.Object" or "System.String");

        if (assemblies.TryGetType(type.Image.Name, type.Name, out var metadata, out var handle) is false)
            return null;

        var baseType = metadata.GetTypeDefinition(handle).BaseType;
        var baseName = baseType.Kind switch
        {
            HandleKind.TypeReference => SignatureFormatter.ReferenceName(metadata, (TypeReferenceHandle)baseType),
            HandleKind.TypeDefinition => SignatureFormatter.DefinitionName(metadata, (TypeDefinitionHandle)baseType),
            _ => null
        };

        return baseName is "System.ValueType" or "System.Enum" && type.Name != "System.Enum";
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

/// <summary>The assemblies of an app, each opened and parsed once, with their classes looked up by the names the AOT profile gives them.</summary>
public sealed class AppAssemblies : IDisposable
{
    private readonly List<PEReader> readers = [];
    private readonly Dictionary<string, MetadataReader> images = new(StringComparer.Ordinal);
    // Built for an image the first time a class of it is looked up: most of the app's images never are.
    private readonly Dictionary<string, Dictionary<string, TypeDefinitionHandle>> classes = new(StringComparer.Ordinal);

    public AppAssemblies(IEnumerable<string> paths)
    {
        try
        {
            foreach (var path in paths)
            {
                var pe = new PEReader(File.OpenRead(path));
                readers.Add(pe);

                if (pe.HasMetadata && pe.GetMetadataReader() is { IsAssembly: true } metadata)
                    images.TryAdd(AssemblyMethods.AssemblyName(metadata), metadata);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public IEnumerable<MetadataReader> Images => images.Values;

    public MetadataReader this[string name] => images[name];

    public AotProfileImage? Image(string name) => images.TryGetValue(name, out var metadata) ? new(name, AssemblyMethods.Mvid(metadata)) : null;

    public bool TryGetType(string image, string name, [NotNullWhen(true)] out MetadataReader? metadata, out TypeDefinitionHandle handle)
    {
        handle = default;
        if (images.TryGetValue(image, out metadata) is false)
            return false;

        if (classes.TryGetValue(image, out var index) is false)
            classes[image] = index = Index(metadata);

        return index.TryGetValue(name, out handle);
    }

    /// <summary>The image and profile name of the class a type reference is to, following type forwarders, or null when it isn't in the app.</summary>
    public (string Image, string Name)? Resolve(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var reference = metadata.GetTypeReference(handle);
        var scope = reference.ResolutionScope;

        return scope.Kind switch
        {
            HandleKind.TypeReference => Resolve(metadata, (TypeReferenceHandle)scope) is { } declaring
                ? (declaring.Image, $"{declaring.Name}/{metadata.GetString(reference.Name)}")
                : null,
            HandleKind.AssemblyReference => Locate(metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)scope).Name), $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}", hops: 0),
            HandleKind.ModuleDefinition or HandleKind.ModuleReference => Locate(AssemblyMethods.AssemblyName(metadata), $"{metadata.GetString(reference.Namespace)}.{metadata.GetString(reference.Name)}", hops: 0),
            _ => null
        };
    }

    public void Dispose() => readers.ForEach(r => r.Dispose());

    private (string Image, string Name)? Locate(string image, string name, int hops)
    {
        if (TryGetType(image, name, out var metadata, out _))
            return (image, name);

        if (metadata is null || hops > 8)
            return null;

        foreach (var handle in metadata.ExportedTypes)
        {
            var exported = metadata.GetExportedType(handle);
            if (exported.Implementation.Kind == HandleKind.AssemblyReference && $"{metadata.GetString(exported.Namespace)}.{metadata.GetString(exported.Name)}" == name)
                return Locate(metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)exported.Implementation).Name), name, hops + 1);
        }

        return null;
    }

    private static Dictionary<string, TypeDefinitionHandle> Index(MetadataReader metadata)
    {
        var index = new Dictionary<string, TypeDefinitionHandle>(StringComparer.Ordinal);
        var nested = new Dictionary<string, TypeDefinitionHandle?>(StringComparer.Ordinal);

        foreach (var handle in metadata.TypeDefinitions)
        {
            index.TryAdd(AssemblyMethods.ClassName(metadata, handle), handle);

            // Two classes can share the name the profiler gives them: then it names neither for sure.
            if (AssemblyMethods.NestedProfilerName(metadata, handle) is { } name)
                nested[name] = nested.ContainsKey(name) ? null : handle;
        }

        foreach (var (name, handle) in nested)
        {
            if (handle is { } found)
                index.TryAdd(name, found);
        }

        return index;
    }
}

/// <summary>The profiled class a type in a signature stands for, given the classes its type parameters stand for; null for anything else.</summary>
internal sealed class ProfileClassProvider(AppAssemblies assemblies, Func<string, AotProfileImage?> image, IReadOnlyList<AotProfileClass> typeArguments) : ISignatureTypeProvider<AotProfileClass?, object?>
{
    public AotProfileClass? GetPrimitiveType(PrimitiveTypeCode typeCode)
        => image(AssemblyMethods.CoreLib) is { } coreLib ? new(coreLib, SignatureFormatter.FullName(typeCode)) : null;

    public AotProfileClass? GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => image(AssemblyMethods.AssemblyName(reader)) is { } definedIn ? new(definedIn, AssemblyMethods.ClassName(reader, handle)) : null;

    public AotProfileClass? GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => assemblies.Resolve(reader, handle) is { } resolved && image(resolved.Image) is { } definedIn ? new(definedIn, resolved.Name) : null;

    public AotProfileClass? GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public AotProfileClass? GetGenericInstantiation(AotProfileClass? genericType, ImmutableArray<AotProfileClass?> typeArguments)
        => genericType is null || typeArguments.Any(a => a is null) ? null : genericType with { Arguments = [.. typeArguments.Select(a => a!)] };

    public AotProfileClass? GetGenericTypeParameter(object? genericContext, int index) => typeArguments[index];

    public AotProfileClass? GetModifiedType(AotProfileClass? modifier, AotProfileClass? unmodifiedType, bool isRequired) => unmodifiedType;

    public AotProfileClass? GetPinnedType(AotProfileClass? elementType) => elementType;

    // The profiler writes none of these as a class.
    public AotProfileClass? GetGenericMethodParameter(object? genericContext, int index) => null;

    public AotProfileClass? GetSZArrayType(AotProfileClass? elementType) => null;

    public AotProfileClass? GetArrayType(AotProfileClass? elementType, ArrayShape shape) => null;

    public AotProfileClass? GetByReferenceType(AotProfileClass? elementType) => null;

    public AotProfileClass? GetPointerType(AotProfileClass? elementType) => null;

    public AotProfileClass? GetFunctionPointerType(MethodSignature<AotProfileClass?> signature) => null;
}

public sealed record SignatureType(string Core, string Modifiers = "", bool ByReference = false)
{
    public string Text => Core + Modifiers + (ByReference ? "&" : "");
}

/// <param name="typeArguments">The names the type's generic parameters stand for, in a signature of an instance of it; null keeps the parameters' own names.</param>
public sealed class SignatureFormatter(MetadataReader metadata, TypeDefinition type, MethodDefinition method, IReadOnlyList<string>? typeArguments = null) : ISignatureTypeProvider<SignatureType, object?>
{
    /// <summary>The primitive types: their full names, and the names Mono writes them by in a signature.</summary>
    private static readonly IReadOnlyDictionary<PrimitiveTypeCode, (string FullName, string MonoName)> Primitives = new Dictionary<PrimitiveTypeCode, (string, string)>
    {
        [PrimitiveTypeCode.Void] = ("System.Void", "void"),
        [PrimitiveTypeCode.Boolean] = ("System.Boolean", "bool"),
        [PrimitiveTypeCode.Char] = ("System.Char", "char"),
        [PrimitiveTypeCode.SByte] = ("System.SByte", "sbyte"),
        [PrimitiveTypeCode.Byte] = ("System.Byte", "byte"),
        [PrimitiveTypeCode.Int16] = ("System.Int16", "int16"),
        [PrimitiveTypeCode.UInt16] = ("System.UInt16", "uint16"),
        [PrimitiveTypeCode.Int32] = ("System.Int32", "int"),
        [PrimitiveTypeCode.UInt32] = ("System.UInt32", "uint"),
        [PrimitiveTypeCode.Int64] = ("System.Int64", "long"),
        [PrimitiveTypeCode.UInt64] = ("System.UInt64", "ulong"),
        [PrimitiveTypeCode.Single] = ("System.Single", "single"),
        [PrimitiveTypeCode.Double] = ("System.Double", "double"),
        [PrimitiveTypeCode.IntPtr] = ("System.IntPtr", "intptr"),
        [PrimitiveTypeCode.UIntPtr] = ("System.UIntPtr", "uintptr"),
        [PrimitiveTypeCode.Object] = ("System.Object", "object"),
        [PrimitiveTypeCode.String] = ("System.String", "string"),
        [PrimitiveTypeCode.TypedReference] = ("System.TypedReference", "typedbyref")
    };

    /// <summary>The primitive types by their full names, under the names Mono writes them by.</summary>
    public static readonly IReadOnlyDictionary<string, string> PrimitiveNames = Primitives.Values.ToDictionary(p => p.FullName, p => p.MonoName, StringComparer.Ordinal);

    public static string FullName(PrimitiveTypeCode typeCode) => Primitive(typeCode).FullName;

    public SignatureType GetPrimitiveType(PrimitiveTypeCode typeCode) => new(Primitive(typeCode).MonoName);

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

    /// <summary>A class's name the way Mono writes it in a signature: with every class it's nested in, and no dot for the global namespace.</summary>
    internal static string DefinitionName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        var declaring = definition.GetDeclaringType();
        return declaring.IsNil
            ? Qualified(reader.GetString(definition.Namespace), reader.GetString(definition.Name))
            : DefinitionName(reader, declaring) + "/" + reader.GetString(definition.Name);
    }

    internal static string ReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var reference = reader.GetTypeReference(handle);
        return reference.ResolutionScope.Kind == HandleKind.TypeReference
            ? ReferenceName(reader, (TypeReferenceHandle)reference.ResolutionScope) + "/" + reader.GetString(reference.Name)
            : Qualified(reader.GetString(reference.Namespace), reader.GetString(reference.Name));
    }

    private static (string FullName, string MonoName) Primitive(PrimitiveTypeCode typeCode)
        => Primitives.TryGetValue(typeCode, out var primitive) ? primitive : throw new NotSupportedException(typeCode.ToString());

    private static string Qualified(string ns, string name) => ns.Length > 0 ? ns + "." + name : name;
}
