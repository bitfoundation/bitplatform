using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>
/// Reads the user-string heap (<c>#US</c>) of a managed assembly - every string literal its method bodies can
/// still reach - straight from the file.
/// </summary>
/// <remarks>
/// Hand-rolled, rather than through <c>System.Reflection.Metadata</c>, because this code runs inside MSBuild:
/// a task assembly that carries a metadata-reader dependency has to agree with the copy MSBuild already
/// loaded, and the two MSBuild flavours (the dotnet CLI's, and Visual Studio's .NET Framework msbuild.exe with
/// its own binding redirects) ship different versions of it. Depending on nothing keeps the task loadable
/// everywhere. What is parsed here is a fixed, decades-old on-disk layout - PE headers, the CLI header, the
/// metadata root, the <c>#US</c> stream - specified in ECMA-335 II.24-25, so there is no moving target.
/// </remarks>
public static class UserStringHeap
{
    /// <summary>
    /// Every string in the assembly's user-string heap, in heap order. Empty when the assembly has none.
    /// </summary>
    /// <exception cref="BadImageFormatException">The file is not a managed PE image, or is malformed.</exception>
    public static IEnumerable<string> Read(string assemblyPath)
    {
        var image = File.ReadAllBytes(assemblyPath);
        var heap = FindUserStringHeap(image, assemblyPath);

        // Index 0 of the heap is the single padding byte that stands for "no string"; the first real string
        // starts at 1. The heap is padded to a 4-byte boundary with zero bytes, which decode as empty strings.
        var strings = new List<string>();
        var position = heap.Offset + 1;
        var end = heap.Offset + heap.Size;

        while (position < end)
        {
            var length = ReadCompressedUInt32(image, ref position, assemblyPath);
            if (length == 0) continue;

            if (length > end - position) throw Invalid(assemblyPath, "a string in the #US heap runs past the end of the heap");

            // The blob is UTF-16LE plus one trailing flag byte that says whether any character needs more
            // than the ANSI range - not part of the string. Decoded character by character rather than
            // through Encoding.Unicode, which would replace an unpaired surrogate with U+FFFD: the heap
            // holds whatever the compiler put there, and this has to read it back unchanged.
            var characters = new char[(length - 1) / 2];
            for (var i = 0; i < characters.Length; i++)
            {
                characters[i] = (char)(image[position + (i * 2)] | (image[position + (i * 2) + 1] << 8));
            }

            strings.Add(new string(characters));
            position += (int)length;
        }

        return strings;
    }

    private readonly struct Heap(int offset, int size)
    {
        public int Offset { get; } = offset;

        public int Size { get; } = size;
    }

    /// <summary>Walks PE header -&gt; CLI header -&gt; metadata root -&gt; the <c>#US</c> stream.</summary>
    private static Heap FindUserStringHeap(byte[] image, string path)
    {
        // MS-DOS header: the PE signature's file offset lives at 0x3C.
        if (image.Length < 0x40 || image[0] != 'M' || image[1] != 'Z') throw Invalid(path, "not a PE image");

        var peHeader = ReadInt32(image, 0x3C, path);
        if (ReadUInt32(image, peHeader, path) != 0x00004550) throw Invalid(path, "not a PE image"); // "PE\0\0"

        // COFF header: 20 bytes, of which the section count and the size of the optional header matter here.
        var coffHeader = peHeader + 4;
        int sections = ReadUInt16(image, coffHeader + 2, path);
        int optionalHeaderSize = ReadUInt16(image, coffHeader + 16, path);
        var optionalHeader = coffHeader + 20;

        // The data directories sit at a magic-dependent offset inside the optional header; the CLI header is
        // directory 14 (ECMA-335 II.25.2.3.3).
        var magic = ReadUInt16(image, optionalHeader, path);
        var directories = optionalHeader + magic switch
        {
            0x10B => 96,  // PE32
            0x20B => 112, // PE32+
            _ => throw Invalid(path, "unknown PE optional header format")
        };

        var sectionHeaders = optionalHeader + optionalHeaderSize;
        var cliHeaderRva = ReadUInt32(image, directories + (14 * 8), path);
        if (cliHeaderRva == 0) throw Invalid(path, "not a managed assembly (no CLI header)");

        // CLI header: the metadata root's RVA is at offset 8 (after Cb and the two runtime version numbers).
        var cliHeader = ResolveRva(image, sectionHeaders, sections, cliHeaderRva, path);
        var metadata = ResolveRva(image, sectionHeaders, sections, ReadUInt32(image, cliHeader + 8, path), path);

        if (ReadUInt32(image, metadata, path) != 0x424A5342) throw Invalid(path, "the metadata root is not a 'BSJB' signature"); // "BSJB"

        // Metadata root: signature, two version numbers, reserved, then the length-prefixed version string
        // padded to a 4-byte boundary, then flags and the stream count.
        var versionLength = ReadInt32(image, metadata + 12, path);
        if (versionLength < 0) throw Invalid(path, "a negative metadata version length");
        var streamCount = ReadUInt16(image, metadata + 16 + Align4(versionLength) + 2, path);
        var streamHeader = metadata + 16 + Align4(versionLength) + 4;

        for (var i = 0; i < streamCount; i++)
        {
            var offset = ReadInt32(image, streamHeader, path);
            var size = ReadInt32(image, streamHeader + 4, path);
            var name = ReadStreamName(image, streamHeader + 8, path, out var nameLength);

            if (name == "#US")
            {
                // Step by step, so that a hostile offset or size cannot overflow the sum back into range.
                if (offset < 0 || offset > image.Length - metadata) throw Invalid(path, "the #US heap runs past the end of the file");
                if (size < 0 || size > image.Length - metadata - offset) throw Invalid(path, "the #US heap runs past the end of the file");
                return new Heap(metadata + offset, size);
            }

            streamHeader += 8 + nameLength;
        }

        // An assembly whose method bodies hold no string literal at all simply has no #US stream.
        return new Heap(0, 0);
    }

    /// <summary>A stream header's name: null-terminated ASCII, padded to a 4-byte boundary.</summary>
    private static string ReadStreamName(byte[] image, int position, string path, out int paddedLength)
    {
        var length = 0;
        while (position + length < image.Length && image[position + length] != 0) length++;
        if (position + length >= image.Length) throw Invalid(path, "an unterminated metadata stream name");

        paddedLength = Align4(length + 1);
        return Encoding.ASCII.GetString(image, position, length);
    }

    /// <summary>
    /// The compressed unsigned integer of ECMA-335 II.23.2: one, two or four bytes, told apart by the top
    /// bits of the first one.
    /// </summary>
    private static uint ReadCompressedUInt32(byte[] image, ref int position, string path)
    {
        var first = ReadByte(image, position, path);
        position++;

        if ((first & 0x80) == 0) return first;

        if ((first & 0xC0) == 0x80)
        {
            var value = (uint)(((first & 0x3F) << 8) | ReadByte(image, position, path));
            position += 1;
            return value;
        }

        if ((first & 0xE0) == 0xC0)
        {
            var value = (uint)(((first & 0x1F) << 24)
                | (ReadByte(image, position, path) << 16)
                | (ReadByte(image, position + 1, path) << 8)
                | ReadByte(image, position + 2, path));
            position += 3;
            return value;
        }

        throw Invalid(path, "a malformed compressed integer in the #US heap");
    }

    /// <summary>The file offset a relative virtual address points at, through the section table.</summary>
    private static int ResolveRva(byte[] image, int sectionHeaders, int sections, uint rva, string path)
    {
        for (var i = 0; i < sections; i++)
        {
            // Section header: name (8 bytes), virtual size, virtual address, raw size, raw pointer.
            var header = sectionHeaders + (i * 40);
            var virtualSize = ReadUInt32(image, header + 8, path);
            var virtualAddress = ReadUInt32(image, header + 12, path);
            var rawSize = ReadUInt32(image, header + 16, path);
            var rawPointer = ReadUInt32(image, header + 20, path);

            // A section's mapped size can exceed what is stored on disk (zero-filled tail), and can also be
            // rounded up to the file alignment - take the larger of the two so neither case misses.
            if (rva < virtualAddress || rva >= virtualAddress + Math.Max(virtualSize, rawSize)) continue;

            var offset = rawPointer + (rva - virtualAddress);
            if (offset >= (uint)image.Length) throw Invalid(path, "a section points past the end of the file");

            return (int)offset;
        }

        throw Invalid(path, $"the RVA 0x{rva:X} is not inside any section");
    }

    private static int Align4(int value) => (value + 3) & ~3;

    private static byte ReadByte(byte[] image, int position, string path)
    {
        if (position < 0 || position >= image.Length) throw Invalid(path, "a header points past the end of the file");

        return image[position];
    }

    private static ushort ReadUInt16(byte[] image, int position, string path)
        => (ushort)(ReadByte(image, position, path) | (ReadByte(image, position + 1, path) << 8));

    private static uint ReadUInt32(byte[] image, int position, string path)
        => (uint)(ReadByte(image, position, path)
            | (ReadByte(image, position + 1, path) << 8)
            | (ReadByte(image, position + 2, path) << 16)
            | (ReadByte(image, position + 3, path) << 24));

    private static int ReadInt32(byte[] image, int position, string path)
    {
        var value = ReadUInt32(image, position, path);
        if (value > int.MaxValue) throw Invalid(path, "an out-of-range offset");

        return (int)value;
    }

    private static BadImageFormatException Invalid(string path, string reason)
        => new($"'{path}' could not be read as a managed assembly: {reason}.");
}
