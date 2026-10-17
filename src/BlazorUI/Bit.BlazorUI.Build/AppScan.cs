using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Reads an app's own assemblies for what they use of the bit BlazorUI packages: the library types they reference - an
/// <c>&lt;BitButton&gt;</c> in a .razor file is a reference to Bit.BlazorUI.BitButton - and their string literals,
/// which is where the icon names an app passes end up (BitIconName.Add is a const, so the literal is the app's own).
/// </summary>
/// <remarks>
/// Which assemblies count is never decided by their names: a filter on System.* and Microsoft.* would be fast and would
/// silently pass over an app's own library named that way, and an assembly passed over is a component left out. Nor by
/// whether they reference a library assembly: a project naming only BitIconName consts (inlined, so no reference is
/// emitted), or calling an interop identifier through a plain IJSRuntime, references none and still names what it uses.
/// Every assembly is read but the platform's own, which are told apart by the key they are signed with.
/// </remarks>
public static class AppScan
{
    public sealed class Result
    {
        /// <summary>The library types the app's assemblies reference.</summary>
        public HashSet<TypeNode> Roots { get; } = [];

        /// <summary>The string literals of the assemblies read.</summary>
        public HashSet<string> Strings { get; } = new(StringComparer.Ordinal);

        /// <summary>The assemblies read: every one but the libraries' own and the platform's.</summary>
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

                var references = ReferencesAny(reader, libraryNames);
                if (references is false && IsPlatformAssembly(reader)) continue;

                result.Scanned.Add(path);

                if (graph is not null && references)
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

    /// <summary>
    /// Whether the assembly is one of the .NET platform's own - the runtime, the base class library, ASP.NET Core, the
    /// Microsoft.Extensions libraries - by the public key it is signed with, which no app's own assembly carries.
    /// </summary>
    public static bool IsPlatformAssembly(MetadataReader reader)
    {
        var key = reader.GetAssemblyDefinition().PublicKey;
        if (key.IsNil) return false;

        var blob = reader.GetBlobBytes(key);
        if (blob.Length == 0) return false;

        // A public key's token is the last eight bytes of its SHA-1 hash, in reverse order.
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(blob);
        var token = new StringBuilder(16);
        for (var i = hash.Length - 1; i >= hash.Length - 8; i--) token.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));

        return PlatformKeyTokens.Contains(token.ToString());
    }

    // The ECMA key (the core library), Microsoft's keys (the framework, ASP.NET Core, Microsoft.Extensions) and the .NET
    // Foundation's.
    private static readonly HashSet<string> PlatformKeyTokens = new(StringComparer.Ordinal)
    {
        "b77a5c561934e089", "b03f5f7f11d50a3a", "7cec85d7bea7798e", "adb9793829ddae60", "31bf3856ad364e35", "cc7b13ffcd2ddd51",
    };

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
