using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

var output = args[0];
using var stream = new MemoryStream();
var writer = new BinaryWriter(stream);
var nextId = 0;
int methods = 0;
var inputs = args.Skip(1).ToList();
var baseIndex = inputs.IndexOf("--base");
if (baseIndex >= 0)
{
    var baseBytes = File.ReadAllBytes(inputs[baseIndex + 1]);
    if (baseBytes[^5] != 0) throw new InvalidOperationException("the base profile does not end with its terminator record");
    writer.Write(baseBytes, 0, baseBytes.Length - 5);
    nextId = 10_000_000;
    inputs.RemoveRange(baseIndex, 2);
}
else
{
    writer.Write(Encoding.ASCII.GetBytes("AOTPROFILE"));
    writer.Write((1 << 16) | 0);
}

foreach (var input in inputs)
{
    var onlyUnmanagedCallersOnly = input.StartsWith("uco:");
    var path = input[(input.IndexOf(':') + 1)..];
    using var pe = new PEReader(File.OpenRead(path));
    var md = pe.GetMetadataReader();
    var asmName = md.GetString(md.GetAssemblyDefinition().Name);
    var imageId = nextId++;
    writer.Write((byte)1); writer.Write(imageId);
    WriteString(asmName);
    WriteString(md.GetGuid(md.GetModuleDefinition().Mvid).ToString().ToUpperInvariant());

    foreach (var th in md.TypeDefinitions)
    {
        var td = md.GetTypeDefinition(th);
        var typeName = TypeDefName(md, th);
        if (typeName == ".<Module>") continue;

        int? classId = null;
        foreach (var mh in td.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if (m.RelativeVirtualAddress == 0) continue;
            if (m.GetGenericParameters().Count > 0) continue;
            if (onlyUnmanagedCallersOnly && m.GetCustomAttributes().Any(a => AttributeName(md, a) == "UnmanagedCallersOnlyAttribute") is false) continue;

            var provider = new Provider(md, td, m);
            var sig = m.DecodeSignature(provider, null);
            var sigText = sig.ReturnType.Format() + "(" + string.Join(",", sig.ParameterTypes.Select(p => p.Format())) + ")";

            if (classId is null)
            {
                classId = nextId++;
                writer.Write((byte)2); writer.Write(classId.Value);
                writer.Write((byte)0x12); writer.Write(imageId); writer.Write(-1);
                WriteString(typeName);
            }

            var methodId = nextId++;
            writer.Write((byte)4); writer.Write(methodId);
            writer.Write(classId.Value); writer.Write(-1); writer.Write(sig.ParameterTypes.Length);
            WriteString(md.GetString(m.Name));
            WriteString(sigText);
            methods++;
        }
    }
}

writer.Write((byte)0); writer.Write(nextId);
writer.Flush();
File.WriteAllBytes(output, stream.ToArray());
Console.WriteLine($"{output}: {methods} methods");

static string AttributeName(MetadataReader md, CustomAttributeHandle h)
{
    var ctor = md.GetCustomAttribute(h).Constructor;
    if (ctor.Kind == HandleKind.MemberReference)
    {
        var parent = md.GetMemberReference((MemberReferenceHandle)ctor).Parent;
        return parent.Kind == HandleKind.TypeReference ? md.GetString(md.GetTypeReference((TypeReferenceHandle)parent).Name) : "";
    }
    return md.GetString(md.GetTypeDefinition(md.GetMethodDefinition((MethodDefinitionHandle)ctor).GetDeclaringType()).Name);
}

void WriteString(string s)
{
    var bytes = Encoding.UTF8.GetBytes(s);
    writer.Write(bytes.Length);
    writer.Write(bytes);
}

static string TypeDefName(MetadataReader md, TypeDefinitionHandle h)
{
    var td = md.GetTypeDefinition(h);
    var name = md.GetString(td.Name);
    var declaring = td.GetDeclaringType();
    if (declaring.IsNil) return md.GetString(td.Namespace) + "." + name;
    return TypeDefName(md, declaring) + "/" + name;
}

sealed class Ty(string core, string mods = "", bool byRef = false)
{
    public string Core = core, Mods = mods;
    public bool ByRef = byRef;
    public string Format() => Core + Mods + (ByRef ? "&" : "");
}

sealed class Provider(MetadataReader md, TypeDefinition type, MethodDefinition method) : ISignatureTypeProvider<Ty, object?>
{
    public Ty GetPrimitiveType(PrimitiveTypeCode code) => new(code switch
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
        _ => throw new NotSupportedException(code.ToString()),
    });

    public Ty GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => new(Def(reader, handle));

    public Ty GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => new(Ref(reader, handle));

    public Ty GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public Ty GetSZArrayType(Ty elementType) => new(elementType.Format() + "[]");

    public Ty GetArrayType(Ty elementType, ArrayShape shape) => new(elementType.Format() + "[" + new string(',', shape.Rank - 1) + "]");

    public Ty GetByReferenceType(Ty elementType) => new(elementType.Core + elementType.Mods, "", true);

    public Ty GetPointerType(Ty elementType) => new(elementType.Format() + "*");

    public Ty GetGenericInstantiation(Ty genericType, ImmutableArray<Ty> typeArguments)
        => new(genericType.Format() + "<" + string.Join(", ", typeArguments.Select(a => a.Format())) + ">");

    public Ty GetGenericTypeParameter(object? genericContext, int index)
        => new(md.GetString(md.GetGenericParameter(type.GetGenericParameters()[index]).Name));

    public Ty GetGenericMethodParameter(object? genericContext, int index)
        => new(md.GetString(md.GetGenericParameter(method.GetGenericParameters()[index]).Name));

    public Ty GetFunctionPointerType(MethodSignature<Ty> signature) => new("*()");

    public Ty GetModifiedType(Ty modifier, Ty unmodifiedType, bool isRequired)
        => new(unmodifiedType.Core, unmodifiedType.Mods + (isRequired ? " modreq(" : " modopt(") + modifier.Format() + ")", unmodifiedType.ByRef);

    public Ty GetPinnedType(Ty elementType) => elementType;

    static string Def(MetadataReader r, TypeDefinitionHandle h)
    {
        var td = r.GetTypeDefinition(h);
        var declaring = td.GetDeclaringType();
        if (declaring.IsNil) return Qualified(r.GetString(td.Namespace), r.GetString(td.Name));
        return Def(r, declaring) + "/" + r.GetString(td.Name);
    }

    static string Ref(MetadataReader r, TypeReferenceHandle h)
    {
        var tr = r.GetTypeReference(h);
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference) return Ref(r, (TypeReferenceHandle)tr.ResolutionScope) + "/" + r.GetString(tr.Name);
        return Qualified(r.GetString(tr.Namespace), r.GetString(tr.Name));
    }

    static string Qualified(string ns, string name) => ns.Length > 0 ? ns + "." + name : name;
}
