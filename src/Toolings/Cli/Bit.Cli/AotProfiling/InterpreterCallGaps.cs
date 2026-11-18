using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Bit.Cli.AotProfiling;

/// <summary>
/// The methods that call a compiled method the interpreter can't call: left on the interpreter, such a caller stops the
/// app with a Mono assertion at <c>interp.c</c> (in <c>init_jit_call_info</c>) the first time it makes the call. The
/// interpreter calls compiled code through a wrapper made for the callee's signature (<c>gsharedvt_out_sig</c>), and for
/// two kinds of callee the AOT build has none - Mono's AOT log says <c>AOT NOT FOUND: (wrapper other)
/// object:gsharedvt_out_sig (...)</c> right before the assertion:
/// <list type="bullet">
/// <item>One that passes or returns, by value, a struct holding an <c>[InlineArray]</c>, like System.Private.CoreLib's
/// <c>BitVector256</c> (an <c>InlineArray8&lt;uint&gt;</c>), which every <c>SearchValues</c> of chars builds its state
/// with: Mono describes an inline array by its one element rather than by its size, and finds no wrapper for that.</item>
/// <item>An <c>[UnsafeAccessor]</c> method, like the setters System.Text.Json's source generator writes for init-only
/// properties: the AOT compiler makes a wrapper for the signature of each method it compiles, but not of the ones the
/// runtime writes, which these are.</item>
/// </list>
/// Leaving the callee on the interpreter instead is no way out: the AOT compiler compiles the callees of a method the
/// profile lists anyway. Compiling the caller is: compiled code calls the compiled method directly. Code the profile run
/// never reached is exactly where this hits - a <c>SearchValues</c> made, or a JSON property set, the first time a
/// feature is used. A caller that passes or returns such a struct itself is found the same way, from its own callers'
/// call sites, so the whole chain back to an ordinary signature is compiled.
/// </summary>
public static class InterpreterCallGaps
{
    /// <summary>
    /// The callers in the app's assemblies, listed as <see cref="AssemblyMethods.Read(string, bool, IEnumerable{AotProfileClass}?)"/>
    /// lists methods: a method of a generic type for each instantiation of the type the profile has. A generic method -
    /// <c>IndexOfAnyAsciiSearcher.ComputeUniqueLowNibbleState&lt;T&gt;</c> - is listed for each instantiation the profile
    /// has of a generic method of the same class with as many type parameters (<c>ComputeAsciiState&lt;char&gt;</c>), since
    /// nothing in the assembly says which ones the app makes and the methods of a class are mostly made for the same ones;
    /// one that is generic in a generic type is left out.
    /// </summary>
    public static IReadOnlyList<AotProfileMethod> Callers(AppAssemblies assemblies, IEnumerable<AotProfileMethod> profiled)
    {
        var profiledList = profiled.ToList();
        var instantiated = profiledList.Select(m => m.Class).ToList();

        // The instantiations the profile has of each class's generic methods, by the class and the number of type parameters.
        // The class is the one its profile name finds in the app, so the profiler's short name of a class nested in a nested
        // class finds the same one as its full name, and a short name two classes share finds neither.
        var siblings = new Dictionary<(MetadataReader Metadata, TypeDefinitionHandle Type, int Arity), List<IReadOnlyList<AotProfileClass>>>();
        foreach (var profiledMethod in profiledList)
        {
            if (profiledMethod.Class.Arguments is not null || profiledMethod.Arguments is not { Count: > 0 } arguments
                || assemblies.TryGetType(profiledMethod.Class.Image.Name, profiledMethod.Class.Name, out var classMetadata, out var classHandle) is false)
                continue;

            var key = (classMetadata, classHandle, arguments.Count);
            if (siblings.TryGetValue(key, out var found) is false)
                siblings[key] = found = [];

            if (found.Any(a => AotProfile.ArgumentsKey(a) == AotProfile.ArgumentsKey(arguments)) is false)
                found.Add(arguments);
        }

        var gaps = new CallGaps(assemblies);
        var methods = new List<AotProfileMethod>();

        foreach (var metadata in assemblies.Images)
        {
            var callers = Callers(metadata, assemblies, gaps);
            if (callers.Count == 0)
                continue;

            methods.AddRange(AssemblyMethods.Read(metadata, (handle, _, _) => callers.Contains(handle), instantiated));

            var image = assemblies.Image(AssemblyMethods.AssemblyName(metadata))!;

            foreach (var handle in callers)
            {
                var method = metadata.GetMethodDefinition(handle);
                var arity = method.GetGenericParameters().Count;
                var typeHandle = method.GetDeclaringType();
                var type = metadata.GetTypeDefinition(typeHandle);

                if (arity == 0 || type.GetGenericParameters().Count > 0)
                    continue;

                if (siblings.TryGetValue((metadata, typeHandle, arity), out var instances) is false)
                    continue;

                var owner = new AotProfileClass(image, AssemblyMethods.ClassName(metadata, typeHandle));

                foreach (var arguments in instances)
                {
                    // An instantiation over a class that isn't in the app can't be named in a signature.
                    if (AssemblyMethods.MonoNames(arguments, assemblies) is not { } names)
                        continue;

                    // A signature the formatter can't decode is left out, as AssemblyMethods leaves one out.
                    try
                    {
                        methods.Add(AssemblyMethods.ProfileMethod(owner, metadata, type, method, typeArguments: null, arguments, names));
                    }
                    catch (Exception exp) when (exp is BadImageFormatException or NotSupportedException or ArgumentOutOfRangeException or InvalidOperationException)
                    {
                    }
                }
            }
        }

        return methods;
    }

    /// <summary>The methods of the assembly whose IL calls, or takes a pointer to, a method the interpreter can't call compiled.</summary>
    private static HashSet<MethodDefinitionHandle> Callers(MetadataReader metadata, AppAssemblies assemblies, CallGaps gaps)
    {
        var callers = new HashSet<MethodDefinitionHandle>();
        var known = new Dictionary<EntityHandle, bool>();

        foreach (var handle in metadata.MethodDefinitions)
        {
            if (assemblies.Body(metadata, metadata.GetMethodDefinition(handle)) is not { } body)
                continue;

            foreach (var callee in CallTargets(body))
            {
                if (known.TryGetValue(callee, out var isGap) is false)
                    known[callee] = isGap = gaps.IsGap(metadata, callee);

                if (isGap)
                {
                    callers.Add(handle);
                    break;
                }
            }
        }

        return callers;
    }

    /// <summary>The methods a body calls (<c>call</c>, <c>callvirt</c>, <c>newobj</c>, <c>jmp</c>) or takes a pointer to (<c>ldftn</c>, <c>ldvirtftn</c>).</summary>
    internal static List<EntityHandle> CallTargets(MethodBodyBlock body)
    {
        var targets = new List<EntityHandle>();
        var il = body.GetILReader();

        try
        {
            while (il.RemainingBytes > 0)
            {
                var first = il.ReadByte();
                var opCode = (ILOpCode)(first == 0xFE ? 0xFE00 | il.ReadByte() : first);

                switch (opCode)
                {
                    case ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Jmp or ILOpCode.Ldftn or ILOpCode.Ldvirtftn:
                        targets.Add(MetadataTokens.EntityHandle(il.ReadInt32()));
                        break;
                    case ILOpCode.Switch:
                        // The count is read before the offset is taken: `il.Offset += 4 * il.ReadUInt32()` would take
                        // it first and land 4 bytes short, inside the jump table.
                        var count = il.ReadUInt32();
                        if (count > (uint)il.RemainingBytes / 4)
                            throw new BadImageFormatException();
                        il.Offset += 4 * (int)count;
                        break;
                    default:
                        il.Offset += OperandSize(opCode);
                        break;
                }
            }
        }
        catch (BadImageFormatException)
        {
            // A body this can't read to the end keeps the calls read so far.
        }

        return targets;
    }

    /// <summary>The <c>no.</c> prefix, which <see cref="ILOpCode"/> has no member for; it's followed by a byte.</summary>
    private const ILOpCode NoPrefix = (ILOpCode)0xFE19;

    private static int OperandSize(ILOpCode opCode) => opCode switch
    {
        ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Starg_s or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s or ILOpCode.Stloc_s
            or ILOpCode.Ldc_i4_s or ILOpCode.Unaligned or NoPrefix
            or ILOpCode.Br_s or ILOpCode.Brfalse_s or ILOpCode.Brtrue_s or ILOpCode.Beq_s or ILOpCode.Bge_s or ILOpCode.Bgt_s
            or ILOpCode.Ble_s or ILOpCode.Blt_s or ILOpCode.Bne_un_s or ILOpCode.Bge_un_s or ILOpCode.Bgt_un_s or ILOpCode.Ble_un_s
            or ILOpCode.Blt_un_s or ILOpCode.Leave_s => 1,

        ILOpCode.Ldarg or ILOpCode.Ldarga or ILOpCode.Starg or ILOpCode.Ldloc or ILOpCode.Ldloca or ILOpCode.Stloc => 2,

        ILOpCode.Ldc_i8 or ILOpCode.Ldc_r8 => 8,

        ILOpCode.Ldc_i4 or ILOpCode.Ldc_r4 or ILOpCode.Calli
            or ILOpCode.Br or ILOpCode.Brfalse or ILOpCode.Brtrue or ILOpCode.Beq or ILOpCode.Bge or ILOpCode.Bgt or ILOpCode.Ble
            or ILOpCode.Blt or ILOpCode.Bne_un or ILOpCode.Bge_un or ILOpCode.Bgt_un or ILOpCode.Ble_un or ILOpCode.Blt_un or ILOpCode.Leave
            or ILOpCode.Cpobj or ILOpCode.Ldobj or ILOpCode.Ldstr or ILOpCode.Castclass or ILOpCode.Isinst or ILOpCode.Unbox
            or ILOpCode.Ldfld or ILOpCode.Ldflda or ILOpCode.Stfld or ILOpCode.Ldsfld or ILOpCode.Ldsflda or ILOpCode.Stsfld
            or ILOpCode.Stobj or ILOpCode.Box or ILOpCode.Newarr or ILOpCode.Ldelema or ILOpCode.Ldelem or ILOpCode.Stelem
            or ILOpCode.Unbox_any or ILOpCode.Refanyval or ILOpCode.Mkrefany or ILOpCode.Ldtoken
            or ILOpCode.Initobj or ILOpCode.Constrained or ILOpCode.Sizeof => 4,

        _ => 0
    };
}

/// <summary>
/// Which methods the interpreter can't call compiled: an <c>[UnsafeAccessor]</c> one, and one that passes or returns by
/// value a struct holding an <c>[InlineArray]</c>, itself or in a field at any depth.
/// </summary>
internal sealed class CallGaps(AppAssemblies assemblies)
{
    private readonly Dictionary<(MetadataReader Metadata, TypeDefinitionHandle Type), bool> holds = [];

    private AppAssemblies Assemblies => assemblies;

    /// <summary>Whether the method a call token of <paramref name="metadata"/> names is one the interpreter can't call compiled.</summary>
    public bool IsGap(MetadataReader metadata, EntityHandle method)
    {
        try
        {
            return IsGap(metadata, method, methodArguments: default);
        }
        catch (Exception exp) when (exp is BadImageFormatException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <param name="methodArguments">For an instance of a generic method, what its type arguments are as far as this asks.</param>
    private bool IsGap(MetadataReader metadata, EntityHandle method, ImmutableArray<Shape> methodArguments)
    {
        var provider = new ShapeProvider(this);
        MethodSignature<Shape> signature;

        switch (method.Kind)
        {
            case HandleKind.MethodSpecification:
                // An instance of a generic method is the method it instantiates, with its type arguments standing for
                // its type parameters: Echo<T>(T) passes one by value as Echo<InlineBuffer>, not as Echo<int>.
                var specification = metadata.GetMethodSpecification((MethodSpecificationHandle)method);
                return IsGap(metadata, specification.Method, specification.DecodeSignature(provider, default));

            case HandleKind.MethodDefinition:
                var definition = metadata.GetMethodDefinition((MethodDefinitionHandle)method);
                if (IsUnsafeAccessor(metadata, definition))
                    return true;

                signature = definition.DecodeSignature(provider, new ShapeContext(default, methodArguments));
                break;

            case HandleKind.MemberReference:
                var reference = metadata.GetMemberReference((MemberReferenceHandle)method);
                if (reference.GetKind() != MemberReferenceKind.Method)
                    return false;

                // Declared in another assembly, or on a generic type, which a call names by a reference.
                if (IsUnsafeAccessor(metadata, reference))
                    return true;

                // A method of a generic type instance, like Box<InlineBuffer>.Echo(T): the instance's type arguments
                // stand for the type's type parameters in its signature.
                var typeArguments = GenericInstance(metadata, reference.Parent) is { } instance ? instance.Arguments(provider) : default;
                signature = reference.DecodeMethodSignature(provider, new ShapeContext(typeArguments, methodArguments));
                break;

            default:
                return false;
        }

        // A struct passed by reference is passed as a pointer.
        return signature.ReturnType.Holds || signature.ParameterTypes.Any(p => p.Holds);
    }

    private static bool IsUnsafeAccessor(MetadataReader metadata, MethodDefinition method)
        => method.GetCustomAttributes().Any(a => AssemblyMethods.AttributeName(metadata, a) == "UnsafeAccessorAttribute");

    /// <summary>Whether a method reference is to an <c>[UnsafeAccessor]</c> method: the one of its type, in the app, with its name and signature.</summary>
    private bool IsUnsafeAccessor(MetadataReader metadata, MemberReference reference)
    {
        var parent = reference.Parent;
        if (GenericInstance(metadata, parent) is { } instance)
            parent = instance.Type;

        MetadataReader? typeMetadata = metadata;
        TypeDefinitionHandle typeHandle;

        if (parent.Kind == HandleKind.TypeDefinition)
            typeHandle = (TypeDefinitionHandle)parent;
        else if (parent.Kind != HandleKind.TypeReference
            || assemblies.Resolve(metadata, (TypeReferenceHandle)parent) is not { } resolved
            || assemblies.TryGetType(resolved.Image, resolved.Name, out typeMetadata, out typeHandle) is false)
            return false;

        var type = typeMetadata.GetTypeDefinition(typeHandle);
        var name = metadata.GetString(reference.Name);

        // The name and the attribute first, so only the signatures of the few methods that can match are read.
        var candidates = type.GetMethods()
            .Select(typeMetadata.GetMethodDefinition)
            .Where(m => typeMetadata.StringComparer.Equals(m.Name, name) && IsUnsafeAccessor(typeMetadata, m))
            .ToList();

        if (candidates.Count == 0)
            return false;

        var typeParameters = GenericNames("!", type.GetGenericParameters().Count);

        foreach (var candidate in candidates)
        {
            var methodParameters = GenericNames("!!", candidate.GetGenericParameters().Count);
            var expected = SignatureText(candidate.DecodeSignature(new SignatureFormatter(typeMetadata, type, candidate, typeParameters, methodParameters), null));
            var actual = SignatureText(reference.DecodeMethodSignature(new SignatureFormatter(metadata, type, candidate, typeParameters, methodParameters), null));

            if (expected == actual)
                return true;
        }

        return false;
    }

    private static List<string> GenericNames(string prefix, int count) => [.. Enumerable.Range(0, count).Select(i => prefix + i)];

    private static string SignatureText(MethodSignature<SignatureType> signature)
        => $"{signature.Header.IsInstance} {signature.GenericParameterCount} {signature.ReturnType.Text}({string.Join(",", signature.ParameterTypes.Select(p => p.Text))})";

    /// <summary>The generic type a type specification instantiates and its type arguments, or null for any other type.</summary>
    private static GenericTypeInstance? GenericInstance(MetadataReader metadata, EntityHandle type)
    {
        if (type.Kind != HandleKind.TypeSpecification)
            return null;

        var blob = metadata.GetBlobReader(metadata.GetTypeSpecification((TypeSpecificationHandle)type).Signature);
        if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
            return null;

        blob.ReadByte(); // CLASS or VALUETYPE
        return new GenericTypeInstance(metadata, blob.ReadTypeHandle(), blob);
    }

    /// <param name="ArgumentsBlob">The signature, read up to the count of type arguments.</param>
    private readonly record struct GenericTypeInstance(MetadataReader Metadata, EntityHandle Type, BlobReader ArgumentsBlob)
    {
        public ImmutableArray<Shape> Arguments(ISignatureTypeProvider<Shape, ShapeContext> provider)
        {
            var blob = ArgumentsBlob;
            var decoder = new SignatureDecoder<Shape, ShapeContext>(provider, Metadata, default);
            var count = blob.ReadCompressedInteger();
            var arguments = ImmutableArray.CreateBuilder<Shape>(count);

            for (var i = 0; i < count; i++)
            {
                arguments.Add(decoder.DecodeType(ref blob));
            }

            return arguments.MoveToImmutable();
        }
    }

    /// <summary>Whether a struct is an inline array or has a field, not static, of a struct that holds one.</summary>
    public bool Holds(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        if (holds.TryGetValue((metadata, handle), out var known))
            return known;

        // Not yet known while its fields are read: a struct can't hold itself, and a cycle the metadata had would end here.
        holds[(metadata, handle)] = false;

        var type = metadata.GetTypeDefinition(handle);
        var result = type.GetCustomAttributes().Any(a => AssemblyMethods.AttributeName(metadata, a) == "InlineArrayAttribute");

        if (result is false)
        {
            var provider = new ShapeProvider(this);
            foreach (var fieldHandle in type.GetFields())
            {
                var field = metadata.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.Static) != 0)
                    continue;

                if (field.DecodeSignature(provider, default).Holds)
                {
                    result = true;
                    break;
                }
            }
        }

        return holds[(metadata, handle)] = result;
    }

    /// <summary>A type as far as this asks: whether it's a struct, and whether it holds an inline array where it's passed by value.</summary>
    internal readonly record struct Shape(bool ValueType, bool Holds);

    /// <summary>What the type parameters of a method's type and of the method stand for, where that is known.</summary>
    internal readonly record struct ShapeContext(ImmutableArray<Shape> TypeArguments, ImmutableArray<Shape> MethodArguments);

    private sealed class ShapeProvider(CallGaps gaps) : ISignatureTypeProvider<Shape, ShapeContext>
    {
        private static readonly Shape Reference = new(false, false);

        public Shape GetPrimitiveType(PrimitiveTypeCode typeCode)
            => new(typeCode is not (PrimitiveTypeCode.Object or PrimitiveTypeCode.String), false);

        public Shape GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            => rawTypeKind == (byte)SignatureTypeKind.ValueType ? new(true, gaps.Holds(reader, handle)) : Reference;

        public Shape GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            if (rawTypeKind != (byte)SignatureTypeKind.ValueType)
                return Reference;

            // A struct that isn't in the app can't be told apart, and is taken for one without an inline array.
            return gaps.Assemblies.Resolve(reader, handle) is { } resolved && gaps.Assemblies.TryGetType(resolved.Image, resolved.Name, out var metadata, out var definition)
                ? new(true, gaps.Holds(metadata, definition))
                : new(true, false);
        }

        public Shape GetTypeFromSpecification(MetadataReader reader, ShapeContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

        // A generic struct holds one when its definition does (InlineArray8<T> is one), or - taken broadly, as compiling a
        // method too many costs little - when a struct it's instantiated over does.
        public Shape GetGenericInstantiation(Shape genericType, ImmutableArray<Shape> typeArguments)
            => genericType.ValueType ? new(true, genericType.Holds || typeArguments.Any(a => a.ValueType && a.Holds)) : Reference;

        public Shape GetByReferenceType(Shape elementType) => Reference;

        public Shape GetPointerType(Shape elementType) => Reference;

        public Shape GetSZArrayType(Shape elementType) => Reference;

        public Shape GetArrayType(Shape elementType, ArrayShape shape) => Reference;

        public Shape GetFunctionPointerType(MethodSignature<Shape> signature) => Reference;

        // A type parameter is what it stands for in an instance, and taken for a class where that isn't known.
        public Shape GetGenericTypeParameter(ShapeContext genericContext, int index) => Argument(genericContext.TypeArguments, index);

        public Shape GetGenericMethodParameter(ShapeContext genericContext, int index) => Argument(genericContext.MethodArguments, index);

        private static Shape Argument(ImmutableArray<Shape> arguments, int index)
            => arguments.IsDefault || index >= arguments.Length ? Reference : arguments[index];

        public Shape GetModifiedType(Shape modifier, Shape unmodifiedType, bool isRequired) => unmodifiedType;

        public Shape GetPinnedType(Shape elementType) => elementType;
    }
}
