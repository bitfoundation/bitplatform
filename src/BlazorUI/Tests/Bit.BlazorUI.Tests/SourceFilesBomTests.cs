using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Pins the charset src/.editorconfig gives every file - utf-8-bom - on the source files of src/BlazorUI.
/// </summary>
/// <remarks>
/// dotnet format enforces the charset on C# alone, and VS Code, dotnet new, an npm script or an agent save a new file
/// without a BOM, so a .razor, .scss or .ts file added without one would go unnoticed until the next sweep. This is what
/// fails on it. The files src/BlazorUI/.editorconfig exempts (batch files, JSON, Apple and Android manifests, served
/// assets) are of none of the extensions read here; a new extension that has to go without a BOM goes in that file,
/// never in this list.
/// </remarks>
[TestClass]
public sealed class SourceFilesBomTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    // The source files: the ones the build, the compilers and the docs read, and an editor writes.
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".razor", ".cshtml", ".scss", ".ts", ".md", ".xaml",
        ".csproj", ".props", ".targets", ".slnf", ".slnx",
    };

    // What the build, npm and the test runner write, and the folders of the tools (.git, .vs, ...).
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", "TestResults",
    };

    [TestMethod]
    public void EverySourceFileStartsWithABom()
    {
        var files = EnumerateSourceFiles(SourceFiles.Root).ToArray();

        Assert.IsTrue(files.Length > 1000, $"Only {files.Length} source files were found under {SourceFiles.Root}.");

        var offenders = files.Where(file => StartsWithBom(file) is false)
                             .Select(file => Path.GetRelativePath(SourceFiles.Root, file).Replace('\\', '/'))
                             .Order(StringComparer.Ordinal)
                             .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        $"These files have no UTF-8 BOM, which src/.editorconfig requires (charset = utf-8-bom); an empty one is " +
                        $"a dead file to delete:\n{string.Join("\n", offenders)}");
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (Extensions.Contains(Path.GetExtension(file))) yield return file;
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(child);

            if (name.StartsWith('.') || SkippedFolders.Contains(name)) continue;

            foreach (var file in EnumerateSourceFiles(child)) yield return file;
        }
    }

    private static bool StartsWithBom(string path)
    {
        Span<byte> head = stackalloc byte[3];

        using var stream = File.OpenRead(path);

        return stream.ReadAtLeast(head, 3, throwOnEndOfStream: false) == 3 && head.SequenceEqual(Bom);
    }
}
