using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Guards the "FastInvoke targets must be synchronous JavaScript" contract.
///
/// On Blazor WebAssembly, <c>FastInvoke</c>/<c>FastInvokeVoid</c> run through <c>IJSInProcessRuntime</c>
/// synchronously. If the target JavaScript function returns a Promise, the call becomes fire-and-forget.
/// This test links every <c>FastInvoke</c> call site to its TypeScript definition via
/// <see cref="TsPromiseMethodScanner"/> and fails on any match.
///
/// <para>
/// The scanner prioritizes reliable <c>async</c> / <c>: Promise&lt;...&gt;</c> detection; a conservative body
/// scan covers only the residual unannotated-direct-return case. See <see cref="TsPromiseMethodScanner"/> for
/// the full tradeoff and limitations.
/// </para>
/// </summary>
[TestClass]
public class FastInvokeSyncContractTests
{
    // Matches FastInvoke(...) / FastInvokeVoid(...) / FastInvoke<T>(...) capturing the JS identifier passed
    // as the first argument. The identifier can be either a quoted string literal (captured in 'id') or a
    // bare variable reference (captured in 'var'), e.g. when a method does
    // `const string identifier = "BitBlazorUI.X.y"; jsRuntime.FastInvoke<T>(identifier, ...)`. Variable
    // references are resolved separately via ResolveConstStringIdentifier; anything that doesn't resolve to
    // a const string (e.g. the 'this'/'jsRuntime' parameters in the extension definitions themselves) is
    // dropped, so it never produces a false positive.
    private static readonly Regex FastInvokeCallRegex =
        new(@"FastInvoke(?:Void)?\s*(?:<[^>]+>)?\s*\(\s*(?:""(?<id>[^""]+)""|(?<var>[A-Za-z_]\w*))", RegexOptions.Compiled);

    [TestMethod]
    public void FastInvoke_CallSites_ShouldNotTargetAsyncJavaScriptFunctions()
    {
        var blazorUiRoot = TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            // This test reads the .cs/.ts sources from disk. When the tests run from packaged binaries
            // without the source tree present, there is nothing to scan, so report inconclusive rather
            // than failing. In the repo/CI the sources are present and the contract is fully enforced.
            Assert.Inconclusive(
                "Skipped: could not locate the BlazorUI source root (the folder containing 'Bit.BlazorUI' " +
                "and 'Bit.BlazorUI.Extras'). The source tree is required to scan FastInvoke call sites.");
            return;
        }

        var csharpDirs = new[]
        {
            Path.Combine(blazorUiRoot, "Bit.BlazorUI"),
            Path.Combine(blazorUiRoot, "Bit.BlazorUI.Extras"),
        };

        var fastInvokeTargets = new List<(string ClassMethod, string Identifier, string File)>();
        foreach (var dir in csharpDirs)
        {
            foreach (var file in EnumerateSourceFiles(dir, "*.cs"))
            {
                var text = File.ReadAllText(file);

                // Parsed lazily and reused for every variable-identifier resolution in this file.
                CompilationUnitSyntax? syntaxRoot = null;

                foreach (Match match in FastInvokeCallRegex.Matches(text))
                {
                    string? identifier;
                    if (match.Groups["id"].Success)
                    {
                        identifier = match.Groups["id"].Value;
                    }
                    else
                    {
                        // The identifier was passed as a variable (e.g. `const string identifier = "...";`).
                        // Resolve it from the const string declaration that is actually visible at the call
                        // site; skip the call when it can't be resolved (not a contract-relevant literal target).
                        syntaxRoot ??= CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
                        identifier = ResolveConstStringIdentifier(syntaxRoot, match.Groups["var"].Value, match.Index);
                        if (identifier is null) continue;
                    }

                    // Reduce "BitBlazorUI.Utils.getBodyWidth" to "Utils.getBodyWidth" for TS class.method lookup.
                    // This assumes TS class names are unique across the scanned sources: two classes with the
                    // same name in different namespaces would collapse to the same key and could produce a false
                    // positive. That's acceptable for the current single-project layout (one class per file,
                    // distinct class names), so the simpler last-two-segments match is preferred over tracking
                    // full namespaces. Revisit if the TypeScript sources ever introduce duplicate class names.
                    var classMethod = LastTwoSegments(identifier);
                    if (classMethod is null) continue;

                    fastInvokeTargets.Add((classMethod, identifier, file));
                }
            }
        }

        Assert.IsTrue(fastInvokeTargets.Count > 0,
            "Expected to find FastInvoke call sites to validate, but none were found. " +
            "The scanning logic in this test is likely broken or the source layout changed.");

        var asyncJsMethods = CollectAsyncJsMethods(csharpDirs);

        Assert.IsTrue(asyncJsMethods.Count > 0,
            "Expected to find async/Promise-returning TypeScript methods (e.g. BitBlazorUI.PdfReader.renderPage), but none were found. " +
            "The TypeScript parsing in this test is likely broken or the source layout changed.");

        var violations = fastInvokeTargets
            .Where(t => asyncJsMethods.Contains(t.ClassMethod))
            .Select(t => $"  - '{t.Identifier}' (async/Promise-returning JS) invoked via FastInvoke in {Path.GetFileName(t.File)}")
            .Distinct()
            .ToList();

        Assert.AreEqual(0, violations.Count,
            "FastInvoke/FastInvokeVoid must only target synchronous JavaScript functions. " +
            "On Blazor WebAssembly these calls run synchronously and discard the returned Promise (fire-and-forget), " +
            "so the following async targets must use the regular asynchronous invocation instead:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static HashSet<string> CollectAsyncJsMethods(IEnumerable<string> roots)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in roots)
        foreach (var file in EnumerateSourceFiles(root, "*.ts"))
        {
            if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var key in TsPromiseMethodScanner.CollectFromSource(File.ReadAllText(file)))
            {
                result.Add(key);
            }
        }

        return result;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root, string pattern)
    {
        if (!Directory.Exists(root)) yield break;

        foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            // Exclude build outputs.
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            yield return file;
        }
    }

    private static string? ResolveConstStringIdentifier(CompilationUnitSyntax root, string variableName, int callPosition)
    {
        // Resolve the variable to the 'const string <variableName> = "...";' declaration that is actually
        // visible at the call site, using the parsed C# syntax tree rather than raw text scanning. Walking the
        // real syntax ancestry means braces, comments, and string literals can never be miscounted, and a
        // same-named const declared in an unrelated method or type can never be bound by mistake.
        var node = root.FindToken(callPosition).Parent;

        for (var current = node; current is not null; current = current.Parent)
        {
            // Local consts: visible from their declaration to the end of the enclosing block, so only a
            // declaration that textually precedes the call site counts.
            foreach (var statement in EnumerateLocalConstStatements(current))
            {
                foreach (var v in statement.Declaration.Variables)
                {
                    if (v.Identifier.ValueText != variableName) continue;
                    if (v.SpanStart >= callPosition) continue;

                    var value = TryGetStringLiteral(v);
                    if (value is not null) return value;
                }
            }

            // Type-level const fields: visible anywhere within the declaring type regardless of source order.
            // Scoping resolution to the containing type (rather than a file-wide name match) prevents binding a
            // same-named const from an unrelated type.
            if (current is TypeDeclarationSyntax typeDecl)
            {
                foreach (var field in typeDecl.Members.OfType<FieldDeclarationSyntax>())
                {
                    if (!field.Modifiers.Any(SyntaxKind.ConstKeyword)) continue;
                    if (!IsStringType(field.Declaration.Type)) continue;

                    foreach (var v in field.Declaration.Variables)
                    {
                        if (v.Identifier.ValueText != variableName) continue;

                        var value = TryGetStringLiteral(v);
                        if (value is not null) return value;
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<LocalDeclarationStatementSyntax> EnumerateLocalConstStatements(SyntaxNode scope)
    {
        // Only inspect statements that belong directly to this scope's own statement list (block or
        // switch-section). Descendant blocks are handled when the walk reaches them as their own scope.
        var statements = scope switch
        {
            BlockSyntax block => block.Statements,
            SwitchSectionSyntax section => section.Statements,
            _ => default
        };

        foreach (var statement in statements)
        {
            if (statement is LocalDeclarationStatementSyntax local &&
                local.IsConst &&
                IsStringType(local.Declaration.Type))
            {
                yield return local;
            }
        }
    }

    private static bool IsStringType(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.StringKeyword),
        IdentifierNameSyntax { Identifier.ValueText: "String" } => true,
        QualifiedNameSyntax { Right.Identifier.ValueText: "String" } => true,
        _ => false
    };

    private static string? TryGetStringLiteral(VariableDeclaratorSyntax declarator)
    {
        if (declarator.Initializer?.Value is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static string? LastTwoSegments(string identifier)
    {
        var segments = identifier.Split('.');
        if (segments.Length < 2) return null;

        return $"{segments[^2]}.{segments[^1]}";
    }

    private static string? TryFindBlazorUiRoot([CallerFilePath] string callerFilePath = "")
    {
        // callerFilePath points at this test file; walk up to the BlazorUI source root, which is the
        // directory that contains both the Bit.BlazorUI and Bit.BlazorUI.Extras projects. Returns null
        // when the source tree isn't present (e.g. running from packaged binaries) so the caller can
        // report the test as inconclusive instead of throwing.
        var directoryName = Path.GetDirectoryName(callerFilePath);
        if (string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName)) return null;

        var dir = new DirectoryInfo(directoryName);

        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Bit.BlazorUI")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Bit.BlazorUI.Extras")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
