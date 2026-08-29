using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>How an untrimmed publish works out which Bit.Butil types the app it is publishing uses.</summary>
public enum ButilScanMode
{
    /// <summary>Do not scan. The default: without ILLink there is then no signal but an explicit list.</summary>
    None,

    /// <summary>
    /// Match Bit.Butil type names against the names in each assembly's <c>#Strings</c> heap. Needs no table
    /// parsing at all, and over-includes whenever an app has a type of its own by the same name - which,
    /// with names like <c>Window</c>, <c>Console</c> and <c>Storage</c> in the library, is often.
    /// </summary>
    TypeNames,

    /// <summary>
    /// Match each assembly's <c>TypeRef</c> rows, which name the namespace as well, so only real references
    /// to <c>Bit.Butil</c> types count. Costs no more at publish than <see cref="TypeNames"/> and is the mode
    /// to use.
    /// </summary>
    TypeReferences,
}

/// <summary>
/// Reads a publish's own assemblies to find the Bit.Butil types the app references, and turns those into the
/// set of JavaScript modules it can reach.
/// </summary>
/// <remarks>
/// This is the untrimmed publish's answer to the question ILLink answers for a trimmed one. It is a coarser
/// answer - a referenced type counts whether or not the call is ever made, and a type reached purely by
/// reflection counts for nothing - and it errs towards shipping too much JavaScript rather than too little.
/// <br/>
/// Two things are deliberately not filtered by name. Which assemblies to read is decided by whether they
/// reference Bit.Butil at all, not by whether they look like framework assemblies: an exclusion list of
/// <c>System.*</c> and <c>Microsoft.*</c> would be fast and would silently skip a consumer's own library
/// that happened to be named that way, and a skipped assembly is a missing module. And Bit.Butil's own
/// assembly is excluded outright, since it names every one of its types and would light up every module.
/// </remarks>
public static class ButilConsumerScan
{
    /// <summary>The Bit.Butil assembly, whose own references say nothing about what a consumer calls.</summary>
    public const string ButilAssemblyFileName = "Bit.Butil.dll";

    private const int TypeRefTable = 0x01;

    /// <summary>What a scan found, in the terms the build needs to report it.</summary>
    public sealed class Result
    {
        /// <summary>The modules the referenced types need, before the manifest closes them over dependencies.</summary>
        public SortedSet<string> Modules { get; } = new(StringComparer.Ordinal);

        /// <summary>The Bit.Butil types the scan matched, for the build log.</summary>
        public SortedSet<string> Types { get; } = new(StringComparer.Ordinal);

        /// <summary>Assemblies that were read and do reference Bit.Butil.</summary>
        public List<string> Scanned { get; } = [];

        /// <summary>Files that are not managed assemblies at all, and were passed over.</summary>
        public List<string> Skipped { get; } = [];
    }

    /// <summary>
    /// Scans the given files. Anything that is not a managed assembly - a native library, a resource file, a
    /// path that no longer exists - is passed over rather than failing the publish: the list a consumer's
    /// build hands in is whatever their references resolved to, and it is not this code's place to insist
    /// every entry be readable.
    /// </summary>
    public static Result Scan(IEnumerable<string> assemblyPaths, ButilTypeModules map, ButilScanMode mode)
    {
        var result = new Result();
        if (mode == ButilScanMode.None) return result;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in assemblyPaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            if (string.Equals(Path.GetFileName(path), ButilAssemblyFileName, StringComparison.OrdinalIgnoreCase)) continue;
            if (seen.Add(Path.GetFullPath(path)) is false) continue;

            PeImage image;
            try
            {
                image = PeImage.Load(path);
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                result.Skipped.Add(path);
                continue;
            }

            try
            {
                if (ScanOne(image, map, mode, result)) result.Scanned.Add(path);
            }
            catch (BadImageFormatException)
            {
                result.Skipped.Add(path);
            }
        }

        return result;
    }

    /// <summary>True when the assembly references Bit.Butil, which is also what makes its matches count.</summary>
    private static bool ScanOne(PeImage image, ButilTypeModules map, ButilScanMode mode, Result result)
        => mode == ButilScanMode.TypeReferences ? ScanTypeReferences(image, map, result) : ScanTypeNames(image, map, result);

    /// <summary>
    /// Every <c>TypeRef</c> row whose namespace is the library's. Read through the row's own heap index, so a
    /// name the heap stores as a suffix of another is read back whole.
    /// </summary>
    private static bool ScanTypeReferences(PeImage image, ButilTypeModules map, Result result)
    {
        var tables = MetadataTables.Read(image);
        if (tables.TypeRefCount == 0) return false;

        var matches = new List<TypeName>();
        var nested = new List<TypeName>();
        var referencesButil = false;

        for (var row = 1; row <= tables.TypeRefCount; row++)
        {
            var name = tables.TypeRef(row);

            if (string.Equals(name.Namespace, ButilTypeModules.Namespace, StringComparison.Ordinal)
                || name.Namespace.StartsWith(ButilTypeModules.Namespace + ".", StringComparison.Ordinal))
            {
                referencesButil = true;
                matches.Add(name);
            }
            // A nested type's reference carries no namespace of its own - it hangs off the reference to the
            // type enclosing it, which this reader does not follow. Matching those on the name alone is the
            // over-including side of the trade, and only for an assembly that references the library anyway.
            else if (name.Namespace.Length == 0) nested.Add(name);
        }

        if (referencesButil is false) return false;

        foreach (var name in matches)
        {
            Record(result, name.FullName, map.ForFullName(name.FullName));
        }

        foreach (var name in nested)
        {
            Record(result, name.Name, map.ForName(name.Name));
        }

        return true;
    }

    /// <summary>
    /// Every name in the <c>#Strings</c> heap, matched against the Bit.Butil type names. The heap may store
    /// one name as a suffix of another, and a sequential walk only reaches the longer one - so a name that
    /// <em>ends with</em> a known type name counts too, which over-includes rather than missing a module.
    /// </summary>
    private static bool ScanTypeNames(PeImage image, ButilTypeModules map, Result result)
    {
        var names = new List<string>();
        var lastCharacters = new HashSet<char>();
        foreach (var name in map.TypeNames)
        {
            names.Add(name);
            lastCharacters.Add(name[name.Length - 1]);
        }

        if (names.Count == 0) return false;

        var referencesButil = false;
        var matched = new List<string>();

        foreach (var text in ReadStrings(image))
        {
            if (text.Length == 0) continue;

            if (referencesButil is false
                && (string.Equals(text, ButilTypeModules.Namespace, StringComparison.Ordinal)
                    || text.EndsWith("." + ButilTypeModules.Namespace, StringComparison.Ordinal)
                    || text.StartsWith(ButilTypeModules.Namespace + ".", StringComparison.Ordinal)))
            {
                referencesButil = true;
            }

            // The cheap reject first: a name can only match if the heap entry ends in a character some type
            // name ends in, which throws out almost every method and parameter name before any comparison.
            if (lastCharacters.Contains(text[text.Length - 1]) is false) continue;

            foreach (var name in names)
            {
                if (name.Length <= text.Length && text.EndsWith(name, StringComparison.Ordinal)) matched.Add(name);
            }
        }

        if (referencesButil is false) return false;

        foreach (var name in matched)
        {
            Record(result, name, map.ForName(name));
        }

        return true;
    }

    private static void Record(Result result, string type, IReadOnlyCollection<string> modules)
    {
        if (modules.Count == 0) return;

        result.Types.Add(type);
        foreach (var module in modules) result.Modules.Add(module);
    }

    /// <summary>
    /// The <c>#Strings</c> heap walked end to end: null-terminated UTF-8, one entry after another. Entries
    /// the heap only reaches as a suffix of a longer one are not returned - see the caller for how that is
    /// accounted for.
    /// </summary>
    public static IEnumerable<string> ReadStrings(PeImage image)
    {
        var heap = image.Strings;
        var strings = new List<string>();
        if (heap.IsEmpty) return strings;

        // Index 0 of the heap is the empty string; the first real entry starts at 1. Walked in bytes, not in
        // characters: a name outside the ASCII range takes more than one byte per character, and stepping by
        // the decoded length would land in the middle of the next entry.
        var start = heap.Offset + 1;
        var end = heap.Offset + heap.Size;

        for (var position = start; position < end;)
        {
            var terminator = position;
            while (terminator < end && image.Image[terminator] != 0) terminator++;

            if (terminator > position) strings.Add(Encoding.UTF8.GetString(image.Image, position, terminator - position));

            position = terminator + 1;
        }

        return strings;
    }
}
