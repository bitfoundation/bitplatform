//-:cnd:noEmit
// Conditional processing is off for this whole file, and the marker above has to stay on the very first line.
// This file documents the template's own conditional directives, so its doc comments and regexes quote them
// verbatim. With processing on, the engine reads those quotes as real directives and swallows the rest of the
// file, which then ships truncated (and uncompilable) in every generated project. Nothing here is conditional.

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards the three ways <c>.template.config/template.json</c> silently stops matching the source tree it describes.
/// None shows up in a build, in a test run, or in a generated project - the template engine simply does less than
/// it was asked to, and the first sign is a customer's generated project containing a file that should not be there.
/// <list type="number">
/// <item><b>A conditional naming a symbol that does not exist.</b> The engine evaluates an undeclared symbol as
/// false, so the whole block is stripped from every generated project forever. A typo (<c>notifications</c> for
/// <c>notification</c>) deletes a feature from the product with no diagnostic.</item>
/// <item><b>An exclusion rule pointing at a path that no longer exists.</b> Move or rename a file and the rule that
/// used to exclude it keeps sitting in <c>template.json</c> looking effective, while the file it was meant to remove
/// now ships in configurations that must not have it.</item>
/// <item><b>A <c>using</c> guarded more loosely than the folder that declares the namespace.</b> This one does not
/// under-do anything, it breaks the build - and only in the configuration nobody generated. It has happened twice:
/// once when a dto moved into a <c>notification</c>-only namespace and a file that ships in every configuration
/// picked up an unguarded <c>using</c>, and once when a <c>using</c> was guarded
/// <c>(signalR == true || notification == true)</c> for a namespace that exists only under <c>notification</c>.
/// Inside the template every <c>//#if</c> is a comment, so the local build compiles all branches and can never see
/// it.</item>
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

    /// <summary>Matches a file-scoped or block namespace declaration, which is what maps a namespace onto a folder.</summary>
    private static readonly Regex namespaceDeclaration = new(@"^namespace\s+(?<namespace>[A-Za-z_][A-Za-z0-9_.]*)\s*[;{]?\s*$", RegexOptions.Compiled);

    /// <summary>Matches a plain <c>using Some.Namespace;</c> - not an alias, a static or a global using.</summary>
    private static readonly Regex usingDirective = new(@"^using\s+(?<namespace>[A-Za-z_][A-Za-z0-9_.]*)\s*;\s*$", RegexOptions.Compiled);

    /// <summary>Matches the <c>(symbol != true)</c> template.json conditions that delete a whole feature folder.</summary>
    private static readonly Regex singleSymbolIsFalseCondition = new(@"^\(\s*(?<symbol>[A-Za-z_][A-Za-z0-9_]*)\s*!=\s*true\s*\)$", RegexOptions.Compiled);

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
    /// A <c>"generator": "port"</c> symbol must declare an explicit, non-zero <c>fallback</c>.
    /// <para>
    /// The template engine's port macro tries to bind the wildcard address to prove a port is free, and when it cannot
    /// it substitutes <c>fallback</c> - which defaults to <b>0</b>. Nothing warns: <c>dotnet new</c> exits 0 and emits a
    /// project whose launch profile says <c>http://*:0</c> (so Kestrel binds a random ephemeral port) while every client
    /// head's <c>ServerAddress</c> / <c>WebAppUrl</c> and the devcontainer's <c>forwardPorts</c> say <c>0</c> too. The
    /// app starts and no client can reach it. This fires whenever something already holds the port on the wildcard
    /// address - most obviously a previously generated project of this same template, which pins port 5000.
    /// </para>
    /// <para>
    /// Verified against the real engine before this test was written: with <c>{"low":5000,"high":5000,"fallback":5999}</c>
    /// and a <c>TcpListener</c> on <c>0.0.0.0:5000</c>, <c>dotnet new</c> generated <c>"http://*:5999"</c>; with the port
    /// free it generated <c>"http://*:5000"</c>.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryGeneratedPortSymbol_Should_DeclareANonZeroFallback()
    {
        var (_, template) = LoadTemplateJson();

        List<string> offenders = [];
        var portSymbolsChecked = 0;

        foreach (var symbol in template.RootElement.GetProperty("symbols").EnumerateObject())
        {
            if (symbol.Value.TryGetProperty("generator", out var generator) is false ||
                generator.GetString() is not "port")
                continue;

            portSymbolsChecked++;

            if (symbol.Value.TryGetProperty("parameters", out var parameters) is false ||
                parameters.TryGetProperty("fallback", out var fallback) is false ||
                fallback.GetInt32() is 0)
            {
                offenders.Add(symbol.Name);
            }
        }

        // Non-vacuity: the template declares six port symbols (web, api, client web and three aspire ports).
        Assert.IsGreaterThan(1, portSymbolsChecked,
            $"Only {portSymbolsChecked} port symbols were found - the scan is not reading template.json's symbols.");

        Assert.IsEmpty(offenders,
            "These \"generator\": \"port\" symbols have no explicit non-zero \"fallback\", so a machine that already " +
            "has the port bound on the wildcard address generates a project wired to port 0, silently: " +
            $"{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// A <c>using</c> of a namespace that only exists in some configurations has to be guarded at least as narrowly as
    /// the rule that deletes it. A guard that is <b>wider</b> - typically an <c>||</c> that brings in an unrelated
    /// symbol - keeps the line in a configuration where the namespace is gone, and the generated project fails to
    /// compile with CS0246 on a feature the user explicitly turned off.
    /// <para>
    /// The rule enforced here is deliberately blunt: for a namespace that a <c>(sym != true)</c> rule deletes, the
    /// guarding condition must be an AND-chain containing <c>sym == true</c>. Since <c>template.json</c> declares no
    /// constraints between symbols, no other shape can be sound - an <c>||</c> arm is always satisfiable with
    /// <c>sym</c> false.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryUsingOfAConditionalNamespace_Should_BeGuardedAsNarrowlyAsTheRuleThatDeletesIt()
    {
        var (templateRoot, template) = LoadTemplateJson();

        var excludeRules = ReadExcludeRules(template);
        var symbolByExcludedDirectory = ReadSingleSymbolFolderExclusions(excludeRules);
        var symbolByNamespace = MapNamespacesToTheirRequiredSymbol(templateRoot, symbolByExcludedDirectory);

        // Non-vacuity: several whole feature folders are symbol-gated (PushNotification, Tenants, Dashboard, Chatbot...).
        Assert.IsGreaterThan(2, symbolByNamespace.Count,
            $"Only {symbolByNamespace.Count} conditional namespaces were discovered - the scan is not reaching template.json's exclusion rules or the source tree.");

        List<string> unsafeUsings = [];
        var usingsChecked = 0;

        foreach (var file in EnumerateTemplateFiles(templateRoot).Where(file => Path.GetExtension(file) is ".cs" or ".razor"))
        {
            var relativePath = Path.GetRelativePath(templateRoot, file).Replace('\\', '/');

            List<string> openConditions = [];
            var lineNumber = 0;

            foreach (var line in File.ReadLines(file))
            {
                lineNumber++;

                var trimmed = line.Trim();

                if (conditionalDirective.Match(trimmed) is { Success: true } directive && trimmed.Contains("#if"))
                {
                    openConditions.Add(directive.Groups["condition"].Value);
                    continue;
                }

                if (trimmed.Contains("#endif") && openConditions.Count > 0)
                {
                    openConditions.RemoveAt(openConditions.Count - 1);
                    continue;
                }

                if (usingDirective.Match(trimmed) is not { Success: true } usingMatch)
                    continue;

                if (symbolByNamespace.TryGetValue(usingMatch.Groups["namespace"].Value, out var requiredSymbol) is false)
                    continue;

                // A file that is itself removed whenever the symbol is off takes its usings with it - whether that is
                // because it sits in the deleted folder, because template.json names it directly in a rule that fires
                // for the same symbol, or because it is excluded unconditionally.
                if (excludeRules.Any(rule => rule.RemovesWhenSymbolIsOff(requiredSymbol) && rule.Covers(relativePath)))
                    continue;

                usingsChecked++;

                if (openConditions.Any(condition => RequiresSymbol(condition, requiredSymbol)) is false)
                {
                    unsafeUsings.Add($"{relativePath}:{lineNumber} -> `{trimmed}` needs `{requiredSymbol} == true`, but its guard is " +
                                     (openConditions.Count is 0 ? "absent" : $"`{string.Join(" && ", openConditions)}`"));
                }
            }
        }

        Assert.IsGreaterThan(0, usingsChecked, "No using of a conditional namespace was found at all - the scan is not doing anything.");

        Assert.IsEmpty(unsafeUsings,
            "These usings survive into a configuration where template.json has deleted the namespace they name, so that " +
            $"configuration's generated project does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, unsafeUsings)}");
    }

    /// <summary>
    /// True when <paramref name="condition"/> cannot hold unless <paramref name="symbol"/> is true - i.e. it is an
    /// AND-chain one of whose terms is <c>symbol == true</c>. Any <c>||</c> makes it satisfiable without the symbol.
    /// </summary>
    private static bool RequiresSymbol(string condition, string symbol)
    {
        if (condition.Contains("||", StringComparison.Ordinal) || condition.Contains(" or ", StringComparison.OrdinalIgnoreCase))
            return false;

        return condition.Split("&&", StringSplitOptions.RemoveEmptyEntries)
                        .Select(term => term.Replace(" ", "").Replace("(", "").Replace(")", ""))
                        .Any(term => term == $"{symbol}==true");
    }

    /// <summary>
    /// One <c>exclude</c> modifier: the condition under which it fires, and the paths it removes.
    /// </summary>
    private sealed record ExcludeRule(string? Condition, string[] Paths)
    {
        /// <summary>
        /// True when this rule is guaranteed to fire while <paramref name="symbol"/> is off: it has no condition at all
        /// (so it always fires), or one of its <c>||</c> arms is exactly <c>symbol != true</c>.
        /// </summary>
        public bool RemovesWhenSymbolIsOff(string symbol)
        {
            if (string.IsNullOrWhiteSpace(Condition))
                return true;

            return Condition.Split("||", StringSplitOptions.RemoveEmptyEntries)
                            .Select(arm => arm.Replace(" ", "").Replace("(", "").Replace(")", ""))
                            .Any(arm => arm == $"{symbol}!=true");
        }

        public bool Covers(string relativePath)
        {
            return Paths.Any(path => path.EndsWith("/**", StringComparison.Ordinal) && path.TrimEnd('*', '/').Contains('*') is false
                ? relativePath.StartsWith($"{path[..^3]}/", StringComparison.OrdinalIgnoreCase)
                : path.Contains('*')
                    ? Regex.IsMatch(relativePath, $"^{Regex.Escape(path).Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*")}$", RegexOptions.IgnoreCase)
                    : string.Equals(path, relativePath, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static ExcludeRule[] ReadExcludeRules(JsonDocument template)
    {
        List<ExcludeRule> rules = [];

        foreach (var source in template.RootElement.GetProperty("sources").EnumerateArray())
        {
            if (source.TryGetProperty("modifiers", out var modifiers) is false)
                continue;

            foreach (var modifier in modifiers.EnumerateArray())
            {
                if (modifier.TryGetProperty("exclude", out var exclude) is false || exclude.ValueKind is not JsonValueKind.Array)
                    continue;

                var condition = modifier.TryGetProperty("condition", out var conditionElement) ? conditionElement.GetString() : null;

                rules.Add(new ExcludeRule(condition, [.. exclude.EnumerateArray().Select(path => path.GetString()).OfType<string>()]));
            }
        }

        return [.. rules];
    }

    /// <summary>
    /// The <c>"condition": "(sym != true)"</c> rules that delete a whole folder, as directory -> symbol. Those are the
    /// only ones whose "the namespace is gone" consequence is simple enough to check mechanically.
    /// </summary>
    private static Dictionary<string, string> ReadSingleSymbolFolderExclusions(ExcludeRule[] excludeRules)
    {
        Dictionary<string, string> symbolByExcludedDirectory = new(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in excludeRules)
        {
            if (singleSymbolIsFalseCondition.Match(rule.Condition ?? "") is not { Success: true } condition)
                continue;

            foreach (var path in rule.Paths)
            {
                if (path.EndsWith("/**", StringComparison.Ordinal) is false || path.TrimEnd('*', '/').Contains('*'))
                    continue;

                symbolByExcludedDirectory[path[..^3]] = condition.Groups["symbol"].Value;
            }
        }

        return symbolByExcludedDirectory;
    }

    /// <summary>
    /// Namespaces every one of whose declaring files sits inside a symbol-gated folder - those are exactly the
    /// namespaces that stop existing when the symbol is off. A namespace with even one declaration outside such a
    /// folder survives, so it is not this test's business.
    /// </summary>
    private static Dictionary<string, string> MapNamespacesToTheirRequiredSymbol(string templateRoot, Dictionary<string, string> symbolByExcludedDirectory)
    {
        Dictionary<string, HashSet<string>> symbolsByNamespace = new(StringComparer.Ordinal);
        HashSet<string> unconditionalNamespaces = new(StringComparer.Ordinal);

        foreach (var file in EnumerateTemplateFiles(templateRoot).Where(file => Path.GetExtension(file) is ".cs"))
        {
            var relativePath = Path.GetRelativePath(templateRoot, file).Replace('\\', '/');

            var owningDirectory = symbolByExcludedDirectory.Keys
                .FirstOrDefault(directory => relativePath.StartsWith($"{directory}/", StringComparison.OrdinalIgnoreCase));

            foreach (var line in File.ReadLines(file))
            {
                if (namespaceDeclaration.Match(line.Trim()) is not { Success: true } declaration)
                    continue;

                var declaredNamespace = declaration.Groups["namespace"].Value;

                if (owningDirectory is null)
                {
                    unconditionalNamespaces.Add(declaredNamespace);
                }
                else
                {
                    (symbolsByNamespace.TryGetValue(declaredNamespace, out var symbols) ? symbols : symbolsByNamespace[declaredNamespace] = [])
                        .Add(symbolByExcludedDirectory[owningDirectory]);
                }

                break; // One file-scoped namespace per file is this repository's style; the first one is the file's.
            }
        }

        return symbolsByNamespace
            .Where(entry => entry.Value.Count is 1 && unconditionalNamespaces.Contains(entry.Key) is false)
            .ToDictionary(entry => entry.Key, entry => entry.Value.Single(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Local-only directories that exist or not depending on which tools the developer has run. A rule naming one of
    /// them is housekeeping, exactly like <c>**/[Bb]in/**</c>, and its absence says nothing about template.json being
    /// out of date - it only says nobody has opened the solution in that tool on this machine.
    /// </summary>
    private static readonly string[] toolingOnlyDirectories = [".vs", ".vscode", ".idea", ".playwright-mcp", "App_Data", "node_modules"];

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

            if (toolingOnlyDirectories.Contains(directory, StringComparer.OrdinalIgnoreCase))
                return true;

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
