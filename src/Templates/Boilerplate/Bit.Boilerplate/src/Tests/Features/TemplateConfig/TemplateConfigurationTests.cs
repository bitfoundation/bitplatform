using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards the two ways <c>.template.config/template.json</c> silently stops matching the source tree it describes.
/// Neither shows up in a build, in a test run, or in a generated project - the template engine simply does less than
/// it was asked to, and the first sign is a customer's generated project containing a file that should not be there.
/// <list type="number">
/// <item><b>A conditional naming a symbol that does not exist.</b> The engine evaluates an undeclared symbol as
/// false, so the whole block is stripped from every generated project forever. A typo (<c>notifications</c> for
/// <c>notification</c>) deletes a feature from the product with no diagnostic.</item>
/// <item><b>An exclusion rule pointing at a path that no longer exists.</b> Move or rename a file and the rule that
/// used to exclude it keeps sitting in <c>template.json</c> looking effective, while the file it was meant to remove
/// now ships in configurations that must not have it.</item>
/// </list>
/// <para>
/// This runs against the template's own working copy. A generated project has no <c>.template.config</c> directory,
/// so there the test reports inconclusive rather than failing.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class TemplateConfigurationTests
{
    /// <summary>
    /// Not a declared symbol, and deliberately so: <c>//#if (IsInsideProjectTemplate == true)</c> blocks are stripped
    /// from every generated project and exist only so the bitplatform repository's own working copy builds.
    /// </summary>
    private const string TemplateOnlySymbol = "IsInsideProjectTemplate";

    private static readonly string[] skippedDirectories = ["bin", "obj", ".git", ".vs", "node_modules", ".playwright-mcp", "App_Data"];

    /// <summary>
    /// Template conditionals are always written with a parenthesized condition - <c>#if (aspire == true)</c> - in
    /// every host syntax (<c>//#if</c>, <c>@*#if</c>, <c>&lt;!--#if</c>, and bare in <c>.sln</c> / <c>.yml</c>).
    /// C#'s own preprocessor directives never are (<c>#if Android</c>, <c>#if DEBUG</c>), which is what keeps the two
    /// apart without maintaining a list of platform symbols.
    /// <para>
    /// The condition stops at the first <c>)</c> so that whatever follows on the line - <c>*@</c>, <c>--&gt;</c>, an
    /// XML doc tag, a trailing comment - is not read as part of it.
    /// </para>
    /// </summary>
    private static readonly Regex conditionalDirective = new(@"#(?:if|elseif)\s*\((?<condition>[^)\r\n]*)\)", RegexOptions.Compiled);

    private static readonly Regex quotedLiteral = new("\"[^\"]*\"|'[^']*'", RegexOptions.Compiled);

    private static readonly Regex identifier = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    private static readonly string[] conditionKeywords = ["true", "false", "null", "and", "or", "not"];

    /// <summary>
    /// Every symbol a conditional tests must be declared in <c>template.json</c>'s <c>symbols</c>. An undeclared one
    /// is not an error the engine reports - it evaluates to false, so the guarded code is deleted from every
    /// generated project in every configuration.
    /// </summary>
    [TestMethod]
    public void EverySymbolUsedInAConditional_Should_BeDeclaredInTemplateJson()
    {
        var (templateRoot, template) = LoadTemplateJson();

        var declaredSymbols = template.RootElement.GetProperty("symbols")
            .EnumerateObject()
            .Select(symbol => symbol.Name)
            .Append(TemplateOnlySymbol)
            .ToHashSet(StringComparer.Ordinal);

        List<string> undeclared = [];
        var conditionalsSeen = 0;

        foreach (var file in EnumerateTemplateFiles(templateRoot))
        {
            var lineNumber = 0;

            foreach (var line in File.ReadLines(file))
            {
                lineNumber++;

                var match = conditionalDirective.Match(line);
                if (match.Success is false)
                    continue;

                conditionalsSeen++;

                var condition = quotedLiteral.Replace(match.Groups["condition"].Value, " ");

                foreach (Match token in identifier.Matches(condition))
                {
                    if (conditionKeywords.Contains(token.Value, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (declaredSymbols.Contains(token.Value))
                        continue;

                    undeclared.Add($"{Path.GetRelativePath(templateRoot, file)}:{lineNumber} -> '{token.Value}' in `{line.Trim()}`");
                }
            }
        }

        // Non-vacuity: this template has ~900 conditionals. A near-zero count means the scan found nothing to check
        // (wrong root, changed directive syntax, a broken regex) and the assertion below would pass for free.
        Assert.IsGreaterThan(100, conditionalsSeen, $"Only {conditionalsSeen} conditionals were found - the scan is not reaching the source tree.");

        Assert.IsEmpty(undeclared,
            "These conditionals name a symbol template.json does not declare, so the engine treats them as false and " +
            $"strips the guarded code from EVERY generated project:{Environment.NewLine}{string.Join(Environment.NewLine, undeclared)}");
    }

    /// <summary>
    /// Every exclusion / copy-only rule that names a concrete path must still resolve. Only literal paths and plain
    /// <c>some/directory/**</c> entries are checked: the housekeeping globs (<c>**/*.user</c>, <c>**/[Bb]in/**</c>)
    /// legitimately match nothing in a clean tree, and a rule that matches nothing today is only a problem when it
    /// was written for something that used to be there.
    /// </summary>
    [TestMethod]
    public void EveryConcretePathInATemplateRule_Should_StillExist()
    {
        var (templateRoot, template) = LoadTemplateJson();

        List<string> missing = [];
        var pathsChecked = 0;

        foreach (var source in template.RootElement.GetProperty("sources").EnumerateArray())
        {
            if (source.TryGetProperty("modifiers", out var modifiers) is false)
                continue;

            foreach (var modifier in modifiers.EnumerateArray())
            {
                var condition = modifier.TryGetProperty("condition", out var conditionElement) ? conditionElement.GetString() : "<unconditional>";

                foreach (var ruleName in (string[])["exclude", "copyOnly", "include", "rename"])
                {
                    if (modifier.TryGetProperty(ruleName, out var rule) is false || rule.ValueKind is not JsonValueKind.Array)
                        continue;

                    foreach (var pathElement in rule.EnumerateArray())
                    {
                        var path = pathElement.GetString();

                        if (string.IsNullOrWhiteSpace(path))
                            continue;

                        pathsChecked++;

                        if (ResolvesToSomething(templateRoot, path) is false)
                            missing.Add($"{condition} / {ruleName}: '{path}'");
                    }
                }
            }
        }

        // Non-vacuity, as above: template.json carries ~150 rule entries.
        Assert.IsGreaterThan(50, pathsChecked, $"Only {pathsChecked} rule entries were read - template.json is not being parsed as expected.");

        Assert.IsEmpty(missing,
            "These template.json rules name a path that does not exist, so they exclude nothing - the file they were " +
            "written for either moved (and now ships in configurations that must not have it) or is gone and the rule " +
            $"is dead weight:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>
    /// True when the entry is a wildcard glob this test deliberately does not judge, or when it names something that
    /// is really there.
    /// </summary>
    private static bool ResolvesToSomething(string templateRoot, string path)
    {
        var normalized = path.Replace('/', Path.DirectorySeparatorChar);

        if (normalized.EndsWith($"{Path.DirectorySeparatorChar}**", StringComparison.Ordinal))
        {
            var directory = normalized[..^3];

            // Still a glob somewhere else in the entry (src/Server/**/Data/Migrations/**) - not this test's business.
            return directory.Contains('*') || directory.Contains('?')
                || Directory.Exists(Path.Combine(templateRoot, directory));
        }

        if (normalized.Contains('*') || normalized.Contains('?') || normalized.Contains('['))
            return true;

        return File.Exists(Path.Combine(templateRoot, normalized))
            || Directory.Exists(Path.Combine(templateRoot, normalized));
    }

    private static IEnumerable<string> EnumerateTemplateFiles(string templateRoot)
    {
        return Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetRelativePath(templateRoot, file)
                               .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               .Any(segment => skippedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase)) is false)
            .Where(file => Path.GetExtension(file) is not (".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or ".woff" or ".woff2" or ".ttf" or ".dll" or ".pdb" or ".zip" or ".keystore" or ".p12" or ".pfx" or ".webp" or ".mp4"));
    }

    /// <summary>
    /// Finds the template root by walking up from the test binaries. <c>template.json</c> carries <c>//</c> comments,
    /// so it is not strict JSON.
    /// </summary>
    private static (string TemplateRoot, JsonDocument Template) LoadTemplateJson()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No .template.config/template.json above the test binaries - this is a generated project, not the template's own tree.");
            return default;
        }

        var template = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(directory.FullName, ".template.config", "template.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        return (directory.FullName, template);
    }
}
