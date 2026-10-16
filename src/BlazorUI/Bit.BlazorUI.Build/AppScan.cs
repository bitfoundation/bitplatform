using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Reads an app's own assemblies for what they use of the bit BlazorUI packages: the library types they reference - an
/// <c>&lt;BitButton&gt;</c> in a .razor file is a reference to Bit.BlazorUI.BitButton - and their string literals,
/// which is where the icon names an app passes end up (BitIconName.Add is a const, so the literal is the app's own).
/// </summary>
/// <remarks>
/// Which assemblies count is decided by whether they reference a library assembly at all, never by their names: a filter
/// on System.* and Microsoft.* would be fast and would silently pass over an app's own library named that way, and an
/// assembly passed over is a component left out.
/// </remarks>
public static class AppScan
{
    public sealed class Result
    {
        /// <summary>The library types the app's assemblies reference.</summary>
        public HashSet<TypeNode> Roots { get; } = [];

        /// <summary>The string literals of the assemblies that reference a library assembly.</summary>
        public HashSet<string> Strings { get; } = new(StringComparer.Ordinal);

        /// <summary>The assemblies read that do reference a library assembly.</summary>
        public List<string> Scanned { get; } = [];
    }

    /// <summary>
    /// Scans the given files. Anything that is not a managed assembly - a native library, a satellite resource, a path
    /// that no longer exists - is passed over: the list a build hands in is whatever its references resolved to.
    /// </summary>
    /// <param name="libraryNames">The library assemblies, whose own references say nothing about what an app uses.</param>
    /// <param name="graph">Resolves the references to library types; null to read only the string literals.</param>
    public static Result Scan(IEnumerable<string> paths, ISet<string> libraryNames, LibraryGraph? graph)
    {
        var result = new Result();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (seen.Add(Path.GetFullPath(path)) is false || File.Exists(path) is false) continue;

            try
            {
                using var pe = new PEReader(File.OpenRead(path));
                if (pe.HasMetadata is false) continue;

                var reader = pe.GetMetadataReader();
                if (reader.IsAssembly is false) continue;
                if (libraryNames.Contains(reader.GetString(reader.GetAssemblyDefinition().Name))) continue;
                if (ReferencesAny(reader, libraryNames) is false) continue;

                result.Scanned.Add(path);

                if (graph is not null)
                {
                    foreach (var handle in reader.TypeReferences)
                    {
                        if (graph.Resolve(reader, handle) is { } node) result.Roots.Add(node);
                    }
                }

                foreach (var s in ReadUserStrings(reader)) result.Strings.Add(s);
            }
            catch (BadImageFormatException)
            {
                // Not a managed assembly after all.
            }
        }

        return result;
    }

    public static bool ReferencesAny(MetadataReader reader, ISet<string> assemblyNames)
    {
        foreach (var handle in reader.AssemblyReferences)
        {
            if (assemblyNames.Contains(reader.GetString(reader.GetAssemblyReference(handle).Name))) return true;
        }
        return false;
    }

    /// <summary>Every string literal of an assembly - the #US heap, which holds nothing else.</summary>
    public static IEnumerable<string> ReadUserStrings(MetadataReader reader)
    {
        var size = reader.GetHeapSize(HeapIndex.UserString);
        var handle = MetadataTokens.UserStringHandle(1);

        while (handle.IsNil is false && MetadataTokens.GetHeapOffset(handle) < size)
        {
            yield return reader.GetUserString(handle);
            handle = reader.GetNextHandle(handle);
        }
    }
}
