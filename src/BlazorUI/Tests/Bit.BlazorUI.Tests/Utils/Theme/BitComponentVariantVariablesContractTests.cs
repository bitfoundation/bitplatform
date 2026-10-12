using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the variant tier of the public <c>--bit-&lt;Component&gt;-*</c> variables (#13655): every public variable a
/// component reads in the rules of one of its variants has a twin for that variant
/// (<c>--bit-Button-outline-border-color</c> beside <c>--bit-Button-border-color</c>), read right ahead of it there.
/// </summary>
/// <remarks>
/// Filled, outlined and text looks are styled apart by nearly every design system, so a variable shared by the three
/// cannot re-skin one of them from <c>:root</c> without re-skinning the other two: a neutral hairline around outline
/// buttons would ring every filled one too. The twin is the variant's own say, and the shared variable stays the one
/// that re-skins all three at once - which is why the twin only ever falls back to it, and why a rule shared by two
/// variants, or one no variant qualifies, never reads a shared variable that has twins (it would hand that variant
/// the shared value whatever its twin says). The variant classes are read off the component's C# (the
/// <c>BitVariant.Fill =&gt; "bit-btn-fil"</c> arms), so a part with a variant axis of its own (BitTagsInput's tags,
/// BitTimeline's items) is held to the same rule, and a class that only looks like a variant's is not.
/// </remarks>
[TestClass]
public sealed class BitComponentVariantVariablesContractTests
{
    private static readonly string[] Variants = ["fill", "outline", "text"];

    // The arms mapping BitVariant to the class a component renders for it.
    private static readonly Regex VariantClassArm = new(
        @"BitVariant\.(?<variant>Fill|Outline|Text)\s*=>\s*""\s*(?<class>bit-[a-z]+-[a-z]+)""",
        RegexOptions.Compiled);

    // Names that start with a variant's name without being one of its twins, older than the tier: BitFileInput's
    // on-color of its role is "the text color", not the color of its Text variant.
    private static readonly HashSet<string> NotATwin = new(StringComparer.Ordinal)
    {
        "--bit-FileInput-text-color",
    };

    // Variables that mean the same on every instance whatever asks for them, so no variant has a say of its own: BitCard
    // draws its rule at --bit-Card-border-width whether Outlined, a Border or the Outline variant asks for it, and the
    // root and the two flags read it with no variant to read a tier of.
    private static readonly HashSet<string> SharedByEveryLook = new(StringComparer.Ordinal)
    {
        "--bit-Card-border-width",
    };

    // Components whose unset Variant renders no class at all, so the root rules are a fourth look of their own and read
    // the shared variables for it. Each variant hands its twins in through a private slot the root reads first
    // (BitDropMenu's --bit-drm-rst-brd-p), so a root read of a shared variable is allowed right behind a private read.
    private static readonly HashSet<string> RootOwnsTheUnsetLook = new(StringComparer.Ordinal)
    {
        "DropMenu",
    };

    [TestMethod]
    public void EveryVariantVariableFallsBackToItsSharedOne()
    {
        var offenders = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var sheet in EnumerateVariantStylesheets())
        {
            foreach (var statement in sheet.Statements)
            {
                foreach (var read in ReadsOf(sheet.Component, statement.Text))
                {
                    if (sheet.TwinOf(read.Name) is not { } twin) continue;

                    if (statement.Text.AsSpan(read.End).TrimStart().StartsWith($"var(--bit-{sheet.Component}-{twin.Shared}") is false)
                    {
                        offenders.Add($"{sheet.Component}: {read.Name} does not fall back to --bit-{sheet.Component}-{twin.Shared} right away");
                    }

                    if (statement.Variants.Count != 1 || statement.Variants.Contains(twin.Variant) is false)
                    {
                        offenders.Add($"{sheet.Component}: {read.Name} is read outside the rules of the {twin.Variant} variant ({statement.Header})");
                    }
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders.ToArray(),
            $"A variant's own variable has to fall back to the shared one, in that variant's rules: {string.Join("; ", offenders)}");
    }

    [TestMethod]
    public void AVariantReadsEveryPublicVariableBehindItsOwnTwin()
    {
        var offenders = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var sheet in EnumerateVariantStylesheets())
        {
            foreach (var statement in sheet.Statements)
            {
                foreach (var read in ReadsOf(sheet.Component, statement.Text))
                {
                    if (sheet.TwinOf(read.Name) is not null || SharedByEveryLook.Contains(read.Name)) continue;

                    var shared = read.Name[$"--bit-{sheet.Component}-".Length..];

                    if (statement.Variants.Count == 1)
                    {
                        // The one variant this rule paints reads the variable through its twin, and only through it.
                        var variant = statement.Variants.Single();
                        var expected = $"var(--bit-{sheet.Component}-{variant}-{shared}, ";

                        if (statement.Text.AsSpan(0, read.Start).EndsWith(expected) is false)
                        {
                            offenders.Add($"{sheet.Component}: the {variant} variant reads {read.Name} without --bit-{sheet.Component}-{variant}-{shared} ahead of it ({statement.Header})");
                        }
                    }
                    else if (sheet.HasTwins(shared))
                    {
                        if (statement.Variants.Count == 0 && RootOwnsTheUnsetLook.Contains(sheet.Component) &&
                            PrivateReadAhead.IsMatch(statement.Text.AsSpan(0, read.Start).ToString()))
                        {
                            continue;
                        }


                        // A rule of several variants, or of none, would give a variant the shared value over its twin.
                        var whose = statement.Variants.Count == 0 ? "no variant" : string.Join(" and ", statement.Variants);
                        offenders.Add($"{sheet.Component}: a rule of {whose} reads {read.Name}, which has twins ({statement.Header})");
                    }
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders.ToArray(),
            $"A public variable read by a variant needs that variant's twin ahead of it: {string.Join("; ", offenders)}");
    }

    // A private variable (--bit-<abbr>-*, lower case) whose fallback the text ends in.
    private static readonly Regex PrivateReadAhead = new(@"var\(\s*--bit-[a-z]+-[a-z0-9-]+,\s*$", RegexOptions.Compiled);

    private sealed record Read(string Name, int Start, int End);

    private sealed record Twin(string Variant, string Shared);

    private sealed record Statement(string Text, string Header, IReadOnlySet<string> Variants);

    private sealed record VariantStylesheet(string Component, IReadOnlyList<Statement> Statements, IReadOnlySet<string> PublicNames)
    {
        /// <summary>The variant and shared name <paramref name="name"/> is the twin of, if it is one.</summary>
        public Twin? TwinOf(string name)
        {
            if (NotATwin.Contains(name)) return null;

            var rest = name[$"--bit-{Component}-".Length..];

            foreach (var variant in Variants)
            {
                if (rest.StartsWith($"{variant}-", StringComparison.Ordinal) is false) continue;

                var shared = rest[(variant.Length + 1)..];

                if (PublicNames.Contains($"--bit-{Component}-{shared}")) return new Twin(variant, shared);
            }

            return null;
        }

        public bool HasTwins(string shared) => Variants.Any(v => PublicNames.Contains($"--bit-{Component}-{v}-{shared}") &&
                                                                  TwinOf($"--bit-{Component}-{v}-{shared}") is not null);
    }

    private static IEnumerable<Read> ReadsOf(string component, string text)
    {
        foreach (Match match in Regex.Matches(text, $@"var\(\s*(?<name>--bit-{component}-[a-z0-9-]+)"))
        {
            var name = match.Groups["name"];
            yield return new Read(name.Value, match.Index, name.Index + name.Length + (text.Length > name.Index + name.Length && text[name.Index + name.Length] == ',' ? 1 : 0));
        }
    }

    /// <summary>
    /// The stylesheets of every component that renders a class per variant, each statement of them with the variants
    /// whose classes qualify the rules it sits in.
    /// </summary>
    private static IEnumerable<VariantStylesheet> EnumerateVariantStylesheets()
    {
        var projects = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy" };

        foreach (var componentsDir in projects.Select(p => SourceFiles.GetDirectory(p, "Components")))
        {
            foreach (var stylesheet in Directory.EnumerateFiles(componentsDir, "*.scss", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(stylesheet);
                if (fileName.StartsWith("Bit", StringComparison.Ordinal) is false) continue;

                var classes = ReadVariantClasses(Path.GetDirectoryName(stylesheet)!);
                if (classes.Count == 0) continue;

                var component = fileName["Bit".Length..];
                var text = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(stylesheet));
                var statements = ReadStatements(text, classes);
                var names = Regex.Matches(text, $@"var\(\s*(--bit-{component}-[a-z0-9-]+)")
                                 .Select(m => m.Groups[1].Value)
                                 .ToHashSet(StringComparer.Ordinal);

                yield return new VariantStylesheet(component, statements, names);
            }
        }
    }

    /// <summary>The variant each class a component renders for one stands for: bit-btn-otl is "outline".</summary>
    private static Dictionary<string, string> ReadVariantClasses(string componentDir)
    {
        var classes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(componentDir, "*.cs", SearchOption.TopDirectoryOnly))
        {
            foreach (Match arm in VariantClassArm.Matches(SourceFiles.ReadFullPath(file)))
            {
                classes[arm.Groups["class"].Value] = arm.Groups["variant"].Value.ToLowerInvariant();
            }
        }

        return classes;
    }

    /// <summary>
    /// Every statement of a stylesheet (a declaration or an @include), with the headers of the rules it is nested in and
    /// the variants whose classes those headers name. A header listing several selectors names the variants of all of
    /// them; the braces of an interpolation (#{...}) open no rule.
    /// </summary>
    private static List<Statement> ReadStatements(string text, IReadOnlyDictionary<string, string> classes)
    {
        var classPattern = new Regex($@"\.(?<class>{string.Join("|", classes.Keys.Select(Regex.Escape))})(?![a-z0-9-])");
        var statements = new List<Statement>();
        var headers = new List<string>();
        var current = new StringBuilder();
        var interpolation = 0;

        void Flush()
        {
            var statement = current.ToString().Trim();
            current.Clear();
            if (statement.Length == 0) return;

            var variants = headers.SelectMany(h => classPattern.Matches(h).Select(m => classes[m.Groups["class"].Value]))
                                  .ToHashSet(StringComparer.Ordinal);

            statements.Add(new Statement(statement, string.Join(" > ", headers), variants));
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '{' && (interpolation > 0 || (i > 0 && text[i - 1] == '#')))
            {
                interpolation++;
                current.Append(c);
            }
            else if (interpolation > 0)
            {
                if (c == '}') interpolation--;
                current.Append(c);
            }
            else if (c == '{')
            {
                headers.Add(Regex.Replace(current.ToString().Trim(), @"\s+", " "));
                current.Clear();
            }
            else if (c == '}')
            {
                Flush();
                if (headers.Count > 0) headers.RemoveAt(headers.Count - 1);
            }
            else if (c == ';')
            {
                Flush();
            }
            else
            {
                current.Append(c);
            }
        }

        return statements;
    }
}
