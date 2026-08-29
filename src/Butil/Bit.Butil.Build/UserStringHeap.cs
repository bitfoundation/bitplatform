using System.Collections.Generic;

namespace Bit.Butil.Build;

/// <summary>
/// Reads the user-string heap (<c>#US</c>) of a managed assembly - every string literal its method bodies can
/// still reach - straight from the file.
/// </summary>
/// <remarks>
/// The PE and metadata walking this rests on lives in <see cref="PeImage"/>; see there for why it is
/// hand-rolled rather than done through <c>System.Reflection.Metadata</c>.
/// </remarks>
public static class UserStringHeap
{
    /// <summary>
    /// Every string in the assembly's user-string heap, in heap order. Empty when the assembly has none.
    /// </summary>
    /// <exception cref="System.BadImageFormatException">The file is not a managed PE image, or is malformed.</exception>
    public static IEnumerable<string> Read(string assemblyPath) => Read(PeImage.Load(assemblyPath));

    /// <summary>
    /// Every string in an already-loaded image's user-string heap, in heap order - the same computation as
    /// <see cref="Read(string)"/> for a caller that has other reasons to hold the image open.
    /// </summary>
    public static IEnumerable<string> Read(PeImage image)
    {
        var heap = image.UserStrings;

        // Index 0 of the heap is the single padding byte that stands for "no string"; the first real string
        // starts at 1. The heap is padded to a 4-byte boundary with zero bytes, which decode as empty strings.
        var strings = new List<string>();
        if (heap.IsEmpty) return strings;

        var position = heap.Offset + 1;
        var end = heap.Offset + heap.Size;

        while (position < end)
        {
            var length = ReadCompressedUInt32(image, ref position);
            if (length == 0) continue;

            if (length > end - position) throw image.Invalid("a string in the #US heap runs past the end of the heap");

            strings.Add(Decode(image, position, (int)length));
            position += (int)length;
        }

        return strings;
    }

    /// <summary>
    /// The string a <c>ldstr</c> token names: its operand is a byte offset into the <c>#US</c> heap rather
    /// than a row number, so the same length-prefixed blob is read from there directly.
    /// </summary>
    public static string ReadAt(PeImage image, int offset)
    {
        var heap = image.UserStrings;
        if (heap.IsEmpty || offset <= 0 || offset >= heap.Size) throw image.Invalid("a string token outside the #US heap");

        var position = heap.Offset + offset;
        var length = ReadCompressedUInt32(image, ref position);
        if (length == 0) return string.Empty;
        if (length > heap.Offset + heap.Size - position) throw image.Invalid("a string in the #US heap runs past the end of the heap");

        return Decode(image, position, (int)length);
    }

    /// <summary>
    /// The blob is UTF-16LE plus one trailing flag byte that says whether any character needs more than the
    /// ANSI range - not part of the string. Decoded character by character rather than through
    /// <c>Encoding.Unicode</c>, which would replace an unpaired surrogate with U+FFFD: the heap holds
    /// whatever the compiler put there, and this has to read it back unchanged.
    /// </summary>
    private static string Decode(PeImage image, int position, int length)
    {
        var characters = new char[(length - 1) / 2];
        for (var i = 0; i < characters.Length; i++)
        {
            characters[i] = (char)(image.Image[position + (i * 2)] | (image.Image[position + (i * 2) + 1] << 8));
        }

        return new string(characters);
    }

    /// <summary>
    /// The compressed unsigned integer of ECMA-335 II.23.2: one, two or four bytes, told apart by the top
    /// bits of the first one.
    /// </summary>
    private static uint ReadCompressedUInt32(PeImage image, ref int position)
    {
        var first = image.ReadByte(position);
        position++;

        if ((first & 0x80) == 0) return first;

        if ((first & 0xC0) == 0x80)
        {
            var value = (uint)(((first & 0x3F) << 8) | image.ReadByte(position));
            position += 1;
            return value;
        }

        if ((first & 0xE0) == 0xC0)
        {
            var value = (uint)(((first & 0x1F) << 24)
                | (image.ReadByte(position) << 16)
                | (image.ReadByte(position + 1) << 8)
                | image.ReadByte(position + 2));
            position += 3;
            return value;
        }

        throw image.Invalid("a malformed compressed integer in the #US heap");
    }
}
