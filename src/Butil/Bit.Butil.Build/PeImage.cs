using System;
using System.IO;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>
/// A managed assembly opened for reading: the PE headers walked as far as the metadata root, and the
/// metadata streams (<c>#US</c>, <c>#Strings</c>, <c>#~</c>) located within it.
/// </summary>
/// <remarks>
/// Hand-rolled, rather than through <c>System.Reflection.Metadata</c>, because this code runs inside MSBuild:
/// a task assembly that carries a metadata-reader dependency has to agree with the copy MSBuild already
/// loaded, and the two MSBuild flavours (the dotnet CLI's, and Visual Studio's .NET Framework msbuild.exe with
/// its own binding redirects) ship different versions of it. Depending on nothing keeps the task loadable
/// everywhere. What is parsed here is a fixed, decades-old on-disk layout - PE headers, the CLI header, the
/// metadata root, the stream headers - specified in ECMA-335 II.24-25, so there is no moving target.
/// <br/>
/// Every read is bounds-checked against the file and every malformed input comes out as a
/// <see cref="BadImageFormatException"/> naming the file, because the callers are build tasks: a consumer's
/// publish has to be told which file it could not read, not handed an <c>IndexOutOfRangeException</c>.
/// </remarks>
public sealed class PeImage
{
    private PeImage(string path, byte[] image)
    {
        Path = path;
        Image = image;
    }

    /// <summary>The file the image was read from, used in every error message.</summary>
    public string Path { get; }

    /// <summary>The whole file.</summary>
    public byte[] Image { get; }

    /// <summary>The <c>#US</c> heap: every string literal the method bodies can still reach.</summary>
    public MetadataHeap UserStrings { get; private set; }

    /// <summary>The <c>#Strings</c> heap: every name in the metadata - types, members, namespaces.</summary>
    public MetadataHeap Strings { get; private set; }

    /// <summary>The <c>#~</c> stream: the metadata tables.</summary>
    public MetadataHeap Tables { get; private set; }

    private int _sectionHeaders;

    private int _sections;

    /// <summary>
    /// Reads a managed assembly from disk and locates its metadata streams. A stream the assembly does not
    /// have (an assembly whose method bodies hold no string literal has no <c>#US</c>) comes back empty.
    /// </summary>
    /// <exception cref="BadImageFormatException">The file is not a managed PE image, or is malformed.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static PeImage Load(string path)
    {
        var image = new PeImage(path, File.ReadAllBytes(path));
        image.FindStreams();
        return image;
    }

    /// <summary>Walks PE header -&gt; CLI header -&gt; metadata root -&gt; the stream headers.</summary>
    private void FindStreams()
    {
        // MS-DOS header: the PE signature's file offset lives at 0x3C.
        if (Image.Length < 0x40 || Image[0] != 'M' || Image[1] != 'Z') throw Invalid("not a PE image");

        var peHeader = ReadInt32(0x3C);
        if (ReadUInt32(peHeader) != 0x00004550) throw Invalid("not a PE image"); // "PE\0\0"

        // COFF header: 20 bytes, of which the section count and the size of the optional header matter here.
        var coffHeader = peHeader + 4;
        _sections = ReadUInt16(coffHeader + 2);
        int optionalHeaderSize = ReadUInt16(coffHeader + 16);
        var optionalHeader = coffHeader + 20;

        // The data directories sit at a magic-dependent offset inside the optional header; the CLI header is
        // directory 14 (ECMA-335 II.25.2.3.3).
        var magic = ReadUInt16(optionalHeader);
        var directories = optionalHeader + magic switch
        {
            0x10B => 96,  // PE32
            0x20B => 112, // PE32+
            _ => throw Invalid("unknown PE optional header format")
        };

        _sectionHeaders = optionalHeader + optionalHeaderSize;
        var cliHeaderRva = ReadUInt32(directories + (14 * 8));
        if (cliHeaderRva == 0) throw Invalid("not a managed assembly (no CLI header)");

        // CLI header: the metadata root's RVA is at offset 8 (after Cb and the two runtime version numbers).
        var cliHeader = ResolveRva(cliHeaderRva);
        var metadata = ResolveRva(ReadUInt32(cliHeader + 8));

        if (ReadUInt32(metadata) != 0x424A5342) throw Invalid("the metadata root is not a 'BSJB' signature"); // "BSJB"

        // Metadata root: signature, two version numbers, reserved, then the length-prefixed version string
        // padded to a 4-byte boundary, then flags and the stream count.
        var versionLength = ReadInt32(metadata + 12);
        if (versionLength < 0) throw Invalid("a negative metadata version length");

        // Step by step, so that a hostile length cannot overflow the padding or the offsets built on it back
        // into range: bound the raw length against what is left of the file first, then its padded form.
        if (versionLength > Image.Length - metadata - 16) throw Invalid("a metadata version string that runs past the end of the file");
        var versionSize = Align4(versionLength);
        if (versionSize > Image.Length - metadata - 16) throw Invalid("a metadata version string that runs past the end of the file");

        var streamCount = ReadUInt16(metadata + 16 + versionSize + 2);
        var streamHeader = metadata + 16 + versionSize + 4;

        for (var i = 0; i < streamCount; i++)
        {
            var offset = ReadInt32(streamHeader);
            var size = ReadInt32(streamHeader + 4);
            var name = ReadStreamName(streamHeader + 8, out var nameLength);

            switch (name)
            {
                case "#US": UserStrings = Bound(metadata, offset, size, "#US heap"); break;
                case "#Strings": Strings = Bound(metadata, offset, size, "#Strings heap"); break;
                case "#~": case "#-": Tables = Bound(metadata, offset, size, "metadata table stream"); break;
            }

            streamHeader += 8 + nameLength;
        }
    }

    /// <summary>A stream's extent as a file offset, checked against the file it claims to sit in.</summary>
    private MetadataHeap Bound(int metadata, int offset, int size, string name)
    {
        // Step by step, so that a hostile offset or size cannot overflow the sum back into range.
        if (offset < 0 || offset > Image.Length - metadata) throw Invalid($"the {name} runs past the end of the file");
        if (size < 0 || size > Image.Length - metadata - offset) throw Invalid($"the {name} runs past the end of the file");

        return new MetadataHeap(metadata + offset, size);
    }

    /// <summary>A stream header's name: null-terminated ASCII, padded to a 4-byte boundary.</summary>
    private string ReadStreamName(int position, out int paddedLength)
    {
        var length = 0;
        while (position + length < Image.Length && Image[position + length] != 0) length++;
        if (position + length >= Image.Length) throw Invalid("an unterminated metadata stream name");

        paddedLength = Align4(length + 1);
        return Encoding.ASCII.GetString(Image, position, length);
    }

    /// <summary>The file offset a relative virtual address points at, through the section table.</summary>
    public int ResolveRva(uint rva)
    {
        for (var i = 0; i < _sections; i++)
        {
            // Section header: name (8 bytes), virtual size, virtual address, raw size, raw pointer.
            var header = _sectionHeaders + (i * 40);
            var virtualSize = ReadUInt32(header + 8);
            var virtualAddress = ReadUInt32(header + 12);
            var rawSize = ReadUInt32(header + 16);
            var rawPointer = ReadUInt32(header + 20);

            // A section's mapped size can exceed what is stored on disk (zero-filled tail), and can also be
            // rounded up to the file alignment - take the larger of the two so neither case misses.
            if (rva < virtualAddress || rva >= virtualAddress + Math.Max(virtualSize, rawSize)) continue;

            var offset = rawPointer + (rva - virtualAddress);
            if (offset >= (uint)Image.Length) throw Invalid("a section points past the end of the file");

            return (int)offset;
        }

        throw Invalid($"the RVA 0x{rva:X} is not inside any section");
    }

    /// <summary>
    /// The UTF-8 string starting at an offset into the <c>#Strings</c> heap, up to its null terminator.
    /// Index 0 is the empty string.
    /// </summary>
    public string ReadHeapString(int index)
    {
        if (index < 0 || index >= Strings.Size) throw Invalid("a name index outside the #Strings heap");

        var start = Strings.Offset + index;
        var end = start;
        var limit = Strings.Offset + Strings.Size;
        while (end < limit && Image[end] != 0) end++;

        return Encoding.UTF8.GetString(Image, start, end - start);
    }

    public static int Align4(int value) => (value + 3) & ~3;

    public byte ReadByte(int position)
    {
        if (position < 0 || position >= Image.Length) throw Invalid("a header points past the end of the file");

        return Image[position];
    }

    public ushort ReadUInt16(int position)
        => (ushort)(ReadByte(position) | (ReadByte(position + 1) << 8));

    public uint ReadUInt32(int position)
        => (uint)(ReadByte(position)
            | (ReadByte(position + 1) << 8)
            | (ReadByte(position + 2) << 16)
            | (ReadByte(position + 3) << 24));

    public int ReadInt32(int position)
    {
        var value = ReadUInt32(position);
        if (value > int.MaxValue) throw Invalid("an out-of-range offset");

        return (int)value;
    }

    public BadImageFormatException Invalid(string reason)
        => new($"'{Path}' could not be read as a managed assembly: {reason}.");
}
