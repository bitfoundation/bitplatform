using System.Text;

namespace Bit.Cli.AotProfiling;

public sealed record AotProfileImage(string Name, string Mvid);

public sealed record AotProfileClass(AotProfileImage Image, string Name, IReadOnlyList<AotProfileClass>? Arguments = null)
{
    public string Key => $"[{Image.Name}]{Name}{AotProfile.ArgumentsKey(Arguments)}";
}

public sealed record AotProfileMethod(AotProfileClass Class, string Name, int ParameterCount, string Signature, IReadOnlyList<AotProfileClass>? Arguments = null)
{
    public string Key => $"{Class.Key}::{Name}{AotProfile.ArgumentsKey(Arguments)}({Signature})";
}

public sealed class AotProfile
{
    private const string Magic = "AOTPROFILE";
    private const int Version = 1 << 16;
    private const byte NoneRecord = 0, ImageRecord = 1, TypeRecord = 2, GenericInstanceRecord = 3, MethodRecord = 4;
    private const byte ClassType = 0x12;

    private readonly List<AotProfileMethod> methods = [];
    private readonly HashSet<string> keys = [];

    public IReadOnlyList<AotProfileMethod> Methods => methods;

    public bool Add(AotProfileMethod method)
    {
        if (keys.Add(method.Key) is false)
            return false;

        methods.Add(method);
        return true;
    }

    public int AddRange(IEnumerable<AotProfileMethod> added) => added.Count(Add);

    public static AotProfile Read(byte[] data)
    {
        using var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);

        if (data.Length < Magic.Length + 4 || Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length)) != Magic)
            throw new InvalidDataException("This isn't an AOT profile: it doesn't start with AOTPROFILE.");

        if (reader.ReadInt32() is var version && version != Version)
            throw new InvalidDataException($"This AOT profile is version 0x{version:x}, not 0x{Version:x}.");

        var profile = new AotProfile();
        var images = new Dictionary<int, AotProfileImage>();
        var classes = new Dictionary<int, AotProfileClass>();
        var instances = new Dictionary<int, IReadOnlyList<AotProfileClass>>();

        while (true)
        {
            var type = reader.ReadByte();
            var id = reader.ReadInt32();

            switch (type)
            {
                case NoneRecord:
                    return profile;
                case ImageRecord:
                    images[id] = new AotProfileImage(ReadString(reader), ReadString(reader));
                    break;
                case GenericInstanceRecord:
                    var count = reader.ReadInt32();
                    instances[id] = [.. Enumerable.Range(0, count).Select(_ => classes[reader.ReadInt32()])];
                    break;
                case TypeRecord:
                    if (reader.ReadByte() != ClassType)
                        throw new InvalidDataException($"Type record {id} isn't a class.");
                    var image = images[reader.ReadInt32()];
                    var classInstance = reader.ReadInt32();
                    classes[id] = new AotProfileClass(image, ReadString(reader), classInstance == -1 ? null : instances[classInstance]);
                    break;
                case MethodRecord:
                    var owner = classes[reader.ReadInt32()];
                    var methodInstance = reader.ReadInt32();
                    var parameterCount = reader.ReadInt32();
                    profile.Add(new AotProfileMethod(owner, ReadString(reader), parameterCount, ReadString(reader), methodInstance == -1 ? null : instances[methodInstance]));
                    break;
                default:
                    throw new InvalidDataException($"Unknown record type {type} in the AOT profile.");
            }
        }
    }

    public byte[] ToArray()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        var nextId = 0;
        var images = new Dictionary<string, int>();
        var classes = new Dictionary<string, int>();
        var instances = new Dictionary<string, int>();

        writer.Write(Encoding.ASCII.GetBytes(Magic));
        writer.Write(Version);

        int WriteImage(AotProfileImage image)
        {
            if (images.TryGetValue(image.Name, out var id))
                return id;

            images[image.Name] = id = nextId++;
            writer.Write(ImageRecord);
            writer.Write(id);
            WriteString(writer, image.Name);
            WriteString(writer, image.Mvid);
            return id;
        }

        int WriteInstance(IReadOnlyList<AotProfileClass>? arguments)
        {
            if (arguments is null)
                return -1;

            var key = ArgumentsKey(arguments);
            if (instances.TryGetValue(key, out var id))
                return id;

            var ids = arguments.Select(WriteClass).ToList();
            instances[key] = id = nextId++;
            writer.Write(GenericInstanceRecord);
            writer.Write(id);
            writer.Write(ids.Count);
            ids.ForEach(writer.Write);
            return id;
        }

        int WriteClass(AotProfileClass type)
        {
            if (classes.TryGetValue(type.Key, out var id))
                return id;

            var imageId = WriteImage(type.Image);
            var instanceId = WriteInstance(type.Arguments);
            classes[type.Key] = id = nextId++;
            writer.Write(TypeRecord);
            writer.Write(id);
            writer.Write(ClassType);
            writer.Write(imageId);
            writer.Write(instanceId);
            WriteString(writer, type.Name);
            return id;
        }

        foreach (var method in methods)
        {
            var classId = WriteClass(method.Class);
            var instanceId = WriteInstance(method.Arguments);
            writer.Write(MethodRecord);
            writer.Write(nextId++);
            writer.Write(classId);
            writer.Write(instanceId);
            writer.Write(method.ParameterCount);
            WriteString(writer, method.Name);
            WriteString(writer, method.Signature);
        }

        writer.Write(NoneRecord);
        writer.Write(0);
        writer.Flush();
        return stream.ToArray();
    }

    internal static string ArgumentsKey(IReadOnlyList<AotProfileClass>? arguments)
    {
        return arguments is null ? "" : $"<{string.Join(",", arguments.Select(a => a.Key))}>";
    }

    private static string ReadString(BinaryReader reader)
    {
        return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
