using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// The <c>pe_dotnet</c> debug image of an assembly file: what Sentry's symbolicator needs to find the PDB uploaded for
/// it (<c>sentry-cli debug-files upload</c>) and add the file and line to a frame that carries the method token and the
/// IL offset. The values are written as Sentry's .NET SDK writes them, read from the PE headers by hand: the
/// System.Reflection.Metadata reader would add its weight to every WebAssembly app, which has no assembly file anyway.
/// </summary>
/// <param name="CodeFile">The assembly's path.</param>
/// <param name="CodeId">The COFF time stamp (8 upper-case hex digits) and the image size (lower-case hex).</param>
/// <param name="DebugId">The CodeView GUID and, for a portable PDB, the entry's stamp (8 hex digits), else the age.</param>
/// <param name="DebugFile">The PDB path the compiler wrote.</param>
/// <param name="DebugChecksum">The PDB checksum (<c>SHA256:{hex}</c>), when the image has one.</param>
internal sealed record SentryDebugImage(string CodeFile, string? CodeId, string DebugId, string? DebugFile, string? DebugChecksum)
{
    private sealed class Entry(SentryDebugImage? image)
    {
        public SentryDebugImage? Image { get; } = image;
    }

    // Weak: a module of a collectible AssemblyLoadContext must stay collectible.
    private static readonly ConditionalWeakTable<Module, Entry> _cache = new();

    /// <summary>The debug image of <paramref name="module"/>'s assembly, read once; null without an assembly file or a CodeView entry.</summary>
    public static SentryDebugImage? Of(Module module)
    {
        // WebAssembly loads its assemblies from memory: there is no file to read.
        if (OperatingSystem.IsBrowser()) return null;

        return _cache.GetValue(module, static m => new Entry(Read(m))).Image;
    }

    [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "An assembly bundled into a single-file app has no path of its own (empty): it gets no debug image, and its frames keep what the stack trace tells.")]
    private static SentryDebugImage? Read(Module module)
    {
        try
        {
            var location = module.Assembly.Location;
            if (string.IsNullOrEmpty(location) || File.Exists(location) is false) return null;

            using var stream = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Read(stream, location);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Reads the debug image of the PE file in <paramref name="stream"/>; null when it is not one or has no CodeView entry.</summary>
    internal static SentryDebugImage? Read(Stream stream, string codeFile)
    {
        // PE/COFF: the DOS header points at the PE signature, followed by the COFF header and the optional header,
        // whose data directory 6 is the debug directory; RVAs map to file offsets through the section table.
        Span<byte> header = stackalloc byte[64];
        if (ReadAt(stream, 0, header) is false || BinaryPrimitives.ReadUInt16LittleEndian(header) != 0x5A4D) return null;

        var peOffset = BinaryPrimitives.ReadInt32LittleEndian(header[0x3C..]);
        Span<byte> coff = stackalloc byte[24];
        if (peOffset <= 0 || ReadAt(stream, peOffset, coff) is false || BinaryPrimitives.ReadUInt32LittleEndian(coff) != 0x00004550) return null;

        var sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(coff[6..]);
        var timeDateStamp = BinaryPrimitives.ReadUInt32LittleEndian(coff[8..]);
        var optionalHeaderSize = BinaryPrimitives.ReadUInt16LittleEndian(coff[20..]);
        if (sectionCount is 0 or > 96 || optionalHeaderSize < 96) return null;

        var optionalHeader = new byte[optionalHeaderSize];
        var optionalOffset = peOffset + 24L;
        if (ReadAt(stream, optionalOffset, optionalHeader) is false) return null;

        var magic = BinaryPrimitives.ReadUInt16LittleEndian(optionalHeader);
        var directories = magic switch { 0x10B => 96, 0x20B => 112, _ => -1 };
        if (directories < 0) return null;

        var sizeOfImage = BinaryPrimitives.ReadUInt32LittleEndian(optionalHeader.AsSpan(56));
        var directoryCount = BinaryPrimitives.ReadUInt32LittleEndian(optionalHeader.AsSpan(directories - 4));
        const int debugDirectory = 6;
        if (directoryCount <= debugDirectory || directories + (debugDirectory + 1) * 8 > optionalHeaderSize) return null;

        var debugRva = BinaryPrimitives.ReadUInt32LittleEndian(optionalHeader.AsSpan(directories + debugDirectory * 8));
        var debugSize = BinaryPrimitives.ReadUInt32LittleEndian(optionalHeader.AsSpan(directories + debugDirectory * 8 + 4));
        if (debugRva == 0 || debugSize == 0) return null;

        var sections = new byte[sectionCount * 40];
        if (ReadAt(stream, optionalOffset + optionalHeaderSize, sections) is false) return null;

        var debugOffset = ToFileOffset(sections, debugRva);
        var entryCount = (int)Math.Min(debugSize / 28, 16);
        if (debugOffset < 0 || entryCount == 0) return null;

        var entries = new byte[entryCount * 28];
        if (ReadAt(stream, debugOffset, entries) is false) return null;

        string? debugId = null, debugFile = null, debugChecksum = null;
        for (var i = 0; i < entryCount; i++)
        {
            var entry = entries.AsSpan(i * 28, 28);
            var stamp = BinaryPrimitives.ReadUInt32LittleEndian(entry[4..]);
            var minorVersion = BinaryPrimitives.ReadUInt16LittleEndian(entry[10..]);
            var type = BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            var dataSize = BinaryPrimitives.ReadUInt32LittleEndian(entry[16..]);
            var dataPointer = BinaryPrimitives.ReadUInt32LittleEndian(entry[24..]);
            if (dataSize is 0 or > 4096 || dataPointer == 0) continue;

            var data = new byte[dataSize];
            if (ReadAt(stream, dataPointer, data) is false) continue;

            if (type == 2 && debugId is null && dataSize >= 24 && BinaryPrimitives.ReadUInt32LittleEndian(data) == 0x53445352)
            {
                // CodeView "RSDS": the PDB's GUID, its age, and its path. A portable PDB (minor version "PM") is named by
                // the GUID and the entry's stamp, which together are its PDB id; a Windows PDB by the GUID and the age.
                var guid = new Guid(data.AsSpan(4, 16));
                var age = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(20));
                debugId = minorVersion == 0x504D
                    ? guid.ToString("D") + "-" + stamp.ToString("x8", CultureInfo.InvariantCulture)
                    : guid.ToString("D") + "-" + age.ToString("x", CultureInfo.InvariantCulture);
                debugFile = NullTerminated(data.AsSpan(24));
            }
            else if (type == 19 && debugChecksum is null)
            {
                // PdbChecksum: the algorithm's name, then the checksum of the PDB.
                var end = data.AsSpan().IndexOf((byte)0);
                if (end > 0 && end < data.Length - 1)
                {
                    debugChecksum = Encoding.UTF8.GetString(data.AsSpan(0, end)) + ":" + Convert.ToHexStringLower(data.AsSpan(end + 1));
                }
            }
        }

        if (debugId is null) return null;

        var codeId = timeDateStamp.ToString("X8", CultureInfo.InvariantCulture) + sizeOfImage.ToString("x", CultureInfo.InvariantCulture);
        return new SentryDebugImage(codeFile, codeId, debugId, debugFile, debugChecksum);
    }

    /// <summary>The file offset of <paramref name="rva"/>, through the section that holds it; -1 when none does.</summary>
    private static long ToFileOffset(ReadOnlySpan<byte> sections, uint rva)
    {
        for (var i = 0; i + 40 <= sections.Length; i += 40)
        {
            var section = sections.Slice(i, 40);
            var virtualSize = BinaryPrimitives.ReadUInt32LittleEndian(section[8..]);
            var virtualAddress = BinaryPrimitives.ReadUInt32LittleEndian(section[12..]);
            var rawSize = BinaryPrimitives.ReadUInt32LittleEndian(section[16..]);
            var rawPointer = BinaryPrimitives.ReadUInt32LittleEndian(section[20..]);
            if (rva >= virtualAddress && rva < (long)virtualAddress + Math.Max(virtualSize, rawSize)) return rawPointer + (long)(rva - virtualAddress);
        }
        return -1;
    }

    private static bool ReadAt(Stream stream, long offset, Span<byte> buffer)
    {
        if (offset < 0 || offset + buffer.Length > stream.Length) return false;
        stream.Position = offset;
        stream.ReadExactly(buffer);
        return true;
    }

    private static string? NullTerminated(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        var text = Encoding.UTF8.GetString(end >= 0 ? bytes[..end] : bytes);
        return text.Length > 0 ? text : null;
    }
}
