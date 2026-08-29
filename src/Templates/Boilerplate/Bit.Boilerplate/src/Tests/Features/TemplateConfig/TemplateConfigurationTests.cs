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

    // Build and test output, all of it gitignored. `TestResults` matters as much as `bin`: Playwright writes video
    // recordings there, and a .webm whose bytes happen to contain a directive-opening sequence is reported as a
    // template defect on any machine that has run the UI tests.
    private static readonly string[] skippedDirectories = ["bin", "obj", ".git", ".vs", "node_modules", ".playwright-mcp", "App_Data", "TestResults"];

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
    /// True when the number at <paramref name="index"/> is the argument of a port flag on a documented command line -
    /// <c>-p PORT</c>, <c>--port PORT</c>, <c>--port=PORT</c>. Those really are the app's port and are SUPPOSED to be
    /// rewritten per generation; a calendar year or a quantity in the same sentence is not, and neither is preceded by
    /// a port flag. Kept as a whitelist of flag spellings rather than "any digits after a space", which would exempt
    /// exactly the prose the check exists for.
    /// </summary>
    private static bool IsPortFlagArgument(string line, int index) => portFlag.IsMatch(line[..index]);

    /// <summary>
    /// The flag has to be a command-line token of its own, anchored to the start of the line or preceded by
    /// whitespace - <c>EndsWith("-p")</c> would also accept any word ending in those two characters.
    /// </summary>
    private static readonly Regex portFlag = new(@"(?:^|[ \t])(?:-p|--port|-Port)[ \t]*=?[ \t]*$", RegexOptions.Compiled);

    /// <summary>
    /// Test files that this review's own suite adds live under <c>src/Tests/Features/**</c> and are gated on
    /// <c>advancedTests</c> (plus whatever feature symbol they need), because a generated project should not inherit
    /// them and the packages several of them use are gated on the same symbol. Forget the exclusion entry and the
    /// DEFAULT generation stops compiling - the file ships while the base class, fake or helper it derives from does
    /// not.
    /// <para>
    /// That has happened: <c>AiChatPanelDictationUITests.cs</c> shipped in no exclude list while its base class
    /// <c>AiChatPanelTestBase.cs</c> and the <c>TestChatClient</c> it uses were both removed at
    /// <c>advancedTests != true || signalR != true</c>, so <c>dotnet new bit-bp</c> with no arguments produced a test
    /// project with two CS0246s. Nothing caught it: inside the template every conditional is a comment so the local
    /// build is green, and CI never generates the one combination that breaks - its build-only job passes
    /// <c>--signalR false</c> without <c>--advancedTests</c>, and both of its test jobs pass <c>--advancedTests</c>.
    /// </para>
    /// <para>
    /// The allow-list below is the set that is MEANT to ship in every generated project - the sample tests a
    /// customer is expected to read and extend. Adding to it is a deliberate act; forgetting an exclusion is not.
    /// </para>
    /// <para>
    /// <b>Scope.</b> This proves the DEFAULT configuration only - the one CI never generates, and the one the
    /// failure above shipped in. It does not prove that a file gated on <c>advancedTests</c> alone is also gated on
    /// every symbol its dependencies need, so <c>--advancedTests true --signalR false</c> could still pair a test
    /// with a missing base class. Closing that needs each file's dependencies resolved against each rule's
    /// condition; the generation matrix in CI is the cheaper place to catch it.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryTestFile_Should_BeExcludedFromGeneratedProjects_UnlessItIsADeliberateSample()
    {
        string[] shippedToEveryGeneratedProject =
        [
            "src/Tests/Features/Identity/IntegrationTests.cs",
            "src/Tests/Features/Identity/UITests.cs",
            "src/Tests/Features/Identity/BunitUITests.cs",
            "src/Tests/Features/Identity/TestData.cs",
            // Deliberately kept: adding a language happens in generated projects, and this is the test that keeps
            // CultureInfoManager, MainActivity's DataPathPrefixes and Bit.ResxTranslator.json in step when it does.
            "src/Tests/Features/Culture/SupportedCultureContractTests.cs",
            // Deliberately kept: adding an endpoint that caches happens in generated projects, and this catches an
            // [AppResponseCache] that silently does nothing there. It compiles and passes in the DEFAULT generation -
            // AppTestServer, its two helpers and AppResponseCacheAttribute all ship, and the sitemap endpoints its
            // non-vacuity assertion needs are mapped unconditionally.
            "src/Tests/Features/Caching/ResponseCacheAttributeContractTests.cs"
        ];

        var (templateRoot, template) = LoadTemplateJson();

        HashSet<string> excluded = new(StringComparer.OrdinalIgnoreCase);

        foreach (var source in template.RootElement.GetProperty("sources").EnumerateArray())
        {
            if (source.TryGetProperty("modifiers", out var modifiers) is false)
                continue;

            foreach (var modifier in modifiers.EnumerateArray())
            {
                if (modifier.TryGetProperty("exclude", out var rule) is false || rule.ValueKind is not JsonValueKind.Array)
                    continue;

                // Only rules that actually FIRE in the default configuration count. A rule keeps its file whenever
                // its condition is false, so "named in some rule" is not the same as "removed by default": an
                // exclusion conditioned solely on `(signalR != true)` leaves the file in place as soon as signalR is
                // on. Every rule in this list is an OR-chain that starts with `advancedTests != true`, and
                // advancedTests defaults to false, so requiring that clause is exactly "this rule fires by default"
                // - checked as text rather than by evaluating the expression, which the engine owns.
                var condition = modifier.TryGetProperty("condition", out var conditionElement) ? conditionElement.GetString() : null;

                if (condition is null || condition.Contains("advancedTests != true", StringComparison.Ordinal) is false)
                    continue;

                foreach (var pathElement in rule.EnumerateArray())
                {
                    var path = pathElement.GetString();

                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    excluded.Add(path.Replace('\\', '/').TrimEnd('/'));
                }
            }
        }

        var testsRoot = Path.Combine(templateRoot, "src", "Tests", "Features");
        List<string> unguarded = [];
        var filesChecked = 0;

        foreach (var file in EnumerateTemplateFiles(testsRoot).Where(file => Path.GetExtension(file) is ".cs"))
        {
            var relativePath = Path.GetRelativePath(templateRoot, file).Replace('\\', '/');

            if (shippedToEveryGeneratedProject.Contains(relativePath, StringComparer.OrdinalIgnoreCase))
                continue;

            filesChecked++;

            // Either the file itself is named, or a `some/directory/**` rule above it removes the whole folder.
            var isGated = excluded.Contains(relativePath) ||
                          excluded.Any(entry => entry.EndsWith("/**", StringComparison.Ordinal) &&
                                                relativePath.StartsWith(entry[..^2], StringComparison.OrdinalIgnoreCase));

            if (isGated is false)
            {
                unguarded.Add(relativePath);
            }
        }

        // Non-vacuity: the suite has ~90 files under Features.
        Assert.IsGreaterThan(50, filesChecked, $"Only {filesChecked} test files were scanned - the scan is not reaching src/Tests/Features.");

        Assert.IsEmpty(unguarded,
            "These test files are in no template.json exclude rule that fires in the DEFAULT configuration, so they " +
            "ship into a generated project where the base classes and fakes they depend on have been removed. Add " +
            "each to the exclude list that matches the symbols it actually needs (advancedTests, plus signalR/" +
            $"notification/module/... where relevant):{Environment.NewLine}{string.Join(Environment.NewLine, unguarded)}");
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
    /// A <c>replaces</c> is a plain, unanchored substring substitution over every processed file - the engine has no
    /// idea that <c>2030</c> was meant to be a port. So a port generator quietly rewrites any other occurrence of the
    /// same four digits, and nothing anywhere reports it: the template's own tree is untouched (no substitution
    /// happens there), the generated project still builds, and the damage is a number that is merely wrong.
    /// <para>
    /// Both shapes below have already shipped: one port generator rewrote a calendar year in a security advisory
    /// (a "recommended by NIST until at least ..." sentence came out of one generation naming a year in the past), and
    /// another rewrote a four-digit fragment of an SVG path coordinate in an icon component. Neither number is spelled
    /// out here - a literal in this doc comment would be rewritten by the very generator it describes, and would then
    /// be flagged by the test below.
    /// </para>
    /// <para>
    /// This checks the two contexts a port literal can never legitimately be in, rather than trying to define where it
    /// can - which is what makes it enforceable. It does NOT catch a bare port-shaped number sitting in prose in a
    /// non-markdown file; if that ever bites, tighten it then.
    /// </para>
    /// </summary>
    [TestMethod]
    public void NoNumericReplacesToken_Should_LandInsideALongerNumberOrInMarkdownProse()
    {
        var (templateRoot, template) = LoadTemplateJson();

        using (template)
        {
            var tokens = template.RootElement.GetProperty("symbols")
                .EnumerateObject()
                .Select(symbol => symbol.Value.TryGetProperty("replaces", out var replaces) ? replaces.GetString() : null)
                .Where(replaces => replaces is not null && replaces.All(char.IsAsciiDigit))
                .Select(replaces => replaces!)
                .ToArray();

            // Non-vacuity: the six port generators. A drop to zero means the shape of template.json changed and every
            // assertion below would pass for free.
            Assert.IsGreaterThanOrEqualTo(4, tokens.Length,
                "Expected the port generators' numeric `replaces` tokens; found almost none, so this test checked nothing.");

            List<string> offenders = [];

            foreach (var file in EnumerateTemplateFiles(templateRoot).Where(IsProcessedByTheEngine))
            {
                var isMarkdown = Path.GetExtension(file) is ".md";
                var lineNumber = 0;

                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;

                    foreach (var token in tokens)
                    {
                        for (var index = line.IndexOf(token, StringComparison.Ordinal); index >= 0;
                             index = line.IndexOf(token, index + 1, StringComparison.Ordinal))
                        {
                            var before = index > 0 ? line[index - 1] : '\0';
                            var after = index + token.Length < line.Length ? line[index + token.Length] : '\0';

                            var where = $"{Path.GetRelativePath(templateRoot, file)}:{lineNumber}: {line.Trim()}";

                            // Part of a longer number: the rewrite lands in the middle of a coordinate, a version or an id.
                            if (char.IsAsciiDigit(before) || before is '.' || char.IsAsciiDigit(after) || after is '.')
                            {
                                offenders.Add($"[{token} inside a number] {where}");
                            }
                            // Prose: in markdown a port is written either after a `:` (localhost:PORT, *:PORT) or as
                            // the argument of a port flag in a documented command line (`-p PORT`, `--port PORT`).
                            // Anything else is a sentence that happens to contain the digits - a calendar year, a
                            // quantity - and the rewrite corrupts it.
                            else if (isMarkdown && before is not ':' && IsPortFlagArgument(line, index) is false)
                            {
                                offenders.Add($"[{token} in markdown prose] {where}");
                            }
                        }
                    }
                }
            }

            Assert.IsEmpty(offenders,
                $"""
                 These occurrences of a port generator's `replaces` token are not ports, and every generated project
                 gets them rewritten to a random port. Reword the text, split the number, or narrow the rule:
                 {string.Join(Environment.NewLine, offenders)}
                 """);
        }
    }

    /// <summary>
    /// <c>aspire.config.json</c> is what the <c>aspire</c> CLI reads to find the app host, and it names the app host's
    /// csproj by path. The <c>(aspire == false)</c> rule deletes that project, so unless the same rule deletes this
    /// file too, <c>--aspire false</c> generates a project where <c>aspire run</c> exits non-zero complaining about a
    /// csproj the user never had. Nothing else in the tree references this file, so nothing else would notice.
    /// </summary>
    [TestMethod]
    public void AspireConfigJson_Should_BeExcludedByEveryRuleThatExcludesTheAppHostItPointsAt()
    {
        var (templateRoot, template) = LoadTemplateJson();

        using (template)
        {
            const string aspireConfig = "aspire.config.json";

            var appHostPath = JsonDocument.Parse(File.ReadAllText(Path.Combine(templateRoot, aspireConfig)))
                .RootElement.GetProperty("appHost").GetProperty("path").GetString()!;

            var appHostDirectory = appHostPath[..appHostPath.LastIndexOf('/')];

            // The whole project, not a file inside it: `(database != PostgreSQL)` drops one extension file from this
            // same folder and must not be judged, because the app host it points at is still there.
            var rulesRemovingTheAppHost = template.RootElement.GetProperty("sources")[0].GetProperty("modifiers")
                .EnumerateArray()
                .Where(modifier => modifier.TryGetProperty("exclude", out var exclude)
                                   && exclude.EnumerateArray().Any(entry =>
                                       entry.GetString() == $"{appHostDirectory}/**" || entry.GetString() == appHostPath))
                .ToArray();

            // Non-vacuity: `(aspire == false)` is that rule. Zero means the lookup broke, not that the tree is clean.
            Assert.IsNotEmpty(rulesRemovingTheAppHost,
                $"No exclusion rule removes '{appHostDirectory}' as a whole, so this test checked nothing.");

            foreach (var rule in rulesRemovingTheAppHost)
            {
                var excluded = rule.GetProperty("exclude").EnumerateArray().Select(entry => entry.GetString()).ToArray();

                Assert.Contains(aspireConfig, excluded,
                    $"The rule `{rule.GetProperty("condition").GetString()}` removes {appHostDirectory} but leaves "
                    + $"{aspireConfig} behind, pointing at {appHostPath}.");
            }
        }
    }

    /// <summary>
    /// True when the engine substitutes inside the file. <c>copyOnly</c> files are byte-copied, so no <c>replaces</c>
    /// token in them is ever rewritten and they must not be judged.
    /// </summary>
    private static bool IsProcessedByTheEngine(string file)
    {
        return Path.GetExtension(file) is not (".svg" or ".png" or ".sh");
    }

    /// <summary>
    /// Every conditional region has to be closed before the end of its file. The engine treats EOF as an implicit
    /// <c>#endif</c>, so an unterminated region does not corrupt anything today - it silently swallows everything
    /// from the opening directive to the last line, in every configuration where the condition is false.
    /// <para>
    /// That is what makes it dangerous rather than merely untidy: the template's own working copy shows the
    /// content, the local build shows it, and the only way to notice is to generate a project in the losing
    /// configuration and diff. It already cost <c>.docs/01</c> its trailing footer, and anything appended to the
    /// end of such a file joins the region without anyone touching the directive.
    /// </para>
    /// <para>
    /// Both dialects are counted together - a template conditional and a real C# <c>#if DEBUG</c>
    /// are indistinguishable at their <c>#endif</c> - which is fine, because an unbalanced count is a defect
    /// either way. <c>#else</c> and <c>#elseif</c> neither open nor close. Every directive on a line counts, not
    /// just the first: a C# file may legitimately close a one-line region inline, and only counting the first
    /// would report it as unterminated.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryConditionalRegion_Should_BeTerminatedBeforeEndOfFile()
    {
        var (templateRoot, template) = LoadTemplateJson();
        template?.Dispose();

        List<string> offenders = [];

        foreach (var file in EnumerateTemplateFiles(templateRoot))
        {
            var lines = File.ReadAllLines(file);
            var processingOn = true;
            Stack<int> openedAt = new();

            for (var i = 0; i < lines.Length; i++)
            {
                var marker = processingMarker.Match(lines[i]);
                if (marker.Success)
                {
                    processingOn = marker.Groups["onOff"].Value is "+";
                    continue;
                }

                if (processingOn is false)
                    continue;

                foreach (Match directive in anyDirective.Matches(lines[i]))
                {
                    if (directive.Groups["keyword"].Value is "endif")
                    {
                        if (openedAt.TryPop(out _) is false)
                        {
                            offenders.Add($"{Path.GetRelativePath(templateRoot, file)}:{i + 1}: #endif with no matching directive above it.");
                        }
                    }
                    else if (directive.Groups["keyword"].Value is "if")
                    {
                        openedAt.Push(i + 1);
                    }
                }
            }

            foreach (var line in openedAt)
            {
                offenders.Add($"{Path.GetRelativePath(templateRoot, file)}:{line}: conditional region is never closed - everything from here to the end of the file disappears when the condition is false.");
            }
        }

        Assert.IsEmpty(offenders,
            $"""
             Unbalanced conditional regions. Close each one where the conditional content actually ends, not at
             the end of the file - an #endif placed at EOF keeps the trailing content conditional and changes
             nothing:
             {string.Join(Environment.NewLine, offenders)}
             """);
    }

    /// <summary>
    /// <c>README.md</c> is <c>primaryOutputs[0]</c> and the file the IDE post-action opens after
    /// <c>dotnet new</c>. Its opening code fence exists for exactly one purpose - to record the arguments the
    /// project was generated with, so that the configuration can be reproduced later - and it does that with one
    /// conditional per parameter.
    /// <para>
    /// Nothing connects the two. A parameter added to <c>template.json</c> without a matching branch in
    /// <c>README.md</c> is simply absent from the record, and because the block renders as a shorter but
    /// perfectly well-formed command, the reader has no signal that anything is missing. Six of the twenty-two
    /// parameters had drifted out this way.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryTemplateParameter_Should_BeRecordedInTheGeneratedReadme()
    {
        var (templateRoot, template) = LoadTemplateJson();

        using (template)
        {
            var readme = File.ReadAllText(Path.Combine(templateRoot, "README.md"));

            var missing = template.RootElement.GetProperty("symbols")
                .EnumerateObject()
                .Where(symbol => symbol.Value.TryGetProperty("type", out var type) && type.GetString() is "parameter")
                .Select(symbol => symbol.Name)
                .Where(name => parametersNotWorthRecording.Contains(name) is false)
                // A whole-token match: `--api` must not be satisfied by `--apiServerUrl`.
                .Where(name => Regex.IsMatch(readme, $@"--{Regex.Escape(name)}(?![A-Za-z0-9])") is false)
                .ToArray();

            Assert.IsEmpty(missing,
                $"""
                 These template.json parameters are never named in README.md, so choosing a non-default value for
                 any of them leaves no trace in the one artifact whose job is to record the generating command.
                 Add a branch to the fence at the top of README.md in the existing style:
                 {string.Join(Environment.NewLine, missing)}
                 """);
        }
    }

    /// <summary>
    /// The parameters this check deliberately does not ask <c>README.md</c> to record.
    /// <list type="bullet">
    /// <item><c>helpUrl</c> is a link to the online parameter documentation shown while the template is being
    /// configured. It is not part of the generated project's configuration and there is nothing to reproduce.</item>
    /// <item><c>apiServerUrl</c> and <c>webAppUrl</c> are free-text values, not choices. Unlike every other
    /// parameter they are also <c>replaces</c> tokens, so whatever was passed is already written throughout the
    /// generated tree - the configuration is not lost when the command block omits them, and printing two long
    /// urls in the first code block a user ever sees costs more than it records. Both are
    /// <c>"isVisible": false</c> in <c>ide.host.json</c> for the same reason.</item>
    /// </list>
    /// </summary>
    private static readonly string[] parametersNotWorthRecording = ["helpUrl", "apiServerUrl", "webAppUrl"];

    /// <summary>
    /// The first conditional directive on a line, in any host syntax, with its keyword captured. Deliberately
    /// looser than <see cref="conditionalDirective"/>: this one also has to see <c>#endif</c> and <c>#else</c>,
    /// and it must match a real C# <c>#if DEBUG</c> as well as a parenthesized template conditional.
    /// </summary>
    private static readonly Regex anyDirective = new(@"#(?<keyword>endif|elseif|elif|else|if)\b", RegexOptions.Compiled);

    /// <summary>
    /// The template engine scans every line for the text <c>#if</c> with no idea that it might be inside a C# string
    /// literal, an XML doc comment or a markdown fence. When it finds one with no parenthesized condition after it,
    /// the expression parser indexes past the end of its token list and <b>aborts the entire generation</b> - so one
    /// such line anywhere in the tree means <c>dotnet new bit-bp</c> produces no project at all, and the local build
    /// and the local test run both stay green because inside the template every directive is just a comment.
    /// <para>
    /// This has now happened twice: once in a doc comment that quoted a directive (the file shipped truncated at that
    /// line), and once in <c>Assert.DoesNotContain("#if", ...)</c>, which took the whole generation down. Measured
    /// with one-file throwaway templates: <c>"#if"</c> is fatal, <c>"//#if"</c> is fatal, a bare <c>"#endif"</c> is
    /// harmless, and both <c>"#" + "if"</c> and a <c>-:cnd:noEmit</c> region are safe.
    /// </para>
    /// <para>
    /// The escape hatch is the marker pair - <c>-:cnd:noEmit</c> turns conditional processing off and
    /// <c>+:cnd:noEmit</c> turns it back on - which is what <c>ServerSharedSettings.cs</c> already uses around its
    /// real C# <c>#if Development</c> block, and what the top of this very file uses for the whole file.
    /// </para>
    /// </summary>
    [TestMethod]
    public void NoFileMayCarryALiteralHashIfOutsideAConditionalProcessingOffRegion()
    {
        var (templateRoot, template) = LoadTemplateJson();
        template?.Dispose();

        List<string> offenders = [];

        foreach (var file in EnumerateTemplateFiles(templateRoot))
        {
            var lines = File.ReadAllLines(file);
            var processingOn = true;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // A marker occupies its own line, after the host's comment opener and nothing else - which is what
                // keeps prose that merely NAMES a marker (like the doc comment above) from flipping the state.
                var marker = processingMarker.Match(line);
                if (marker.Success)
                {
                    processingOn = marker.Groups["onOff"].Value is "+";
                    continue;
                }

                if (processingOn is false)
                    continue;

                var firstOccurrence = line.IndexOf(OpenDirectiveText, StringComparison.Ordinal);
                if (firstOccurrence < 0)
                    continue;

                // The engine stops at the FIRST occurrence, so it is that one which has to be a real directive: a line
                // that opens with the literal inside a string and only then carries a valid conditional is still fatal,
                // and the valid one must not excuse it. A real template conditional also carries a non-blank
                // parenthesized condition - an empty one is as fatal as no parentheses at all - while a real C#
                // preprocessor directive is the bare keyword plus a symbol at the start of a line.
                var conditional = conditionalDirective.Match(line, firstOccurrence);
                if (conditional.Success && conditional.Index == firstOccurrence
                    && string.IsNullOrWhiteSpace(conditional.Groups["condition"].Value) is false)
                    continue;

                var preprocessor = csharpPreprocessorDirective.Match(line);
                if (preprocessor.Success && preprocessor.Index + preprocessor.Value.IndexOf(OpenDirectiveText, StringComparison.Ordinal) == firstOccurrence)
                    continue;

                offenders.Add($"{Path.GetRelativePath(templateRoot, file)}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.IsEmpty(offenders,
            $"""
             These lines carry the literal directive-opening text outside a `-:cnd:noEmit` region. Each one either
             aborts `dotnet new` or silently truncates its file from that point on. Wrap the line in a
             `-:cnd:noEmit` / `+:cnd:noEmit` pair, or split the literal so the two characters are never adjacent:
             {string.Join(Environment.NewLine, offenders)}
             """);
    }

    /// <summary>
    /// Assembled at runtime so that this file - which is the one place that must talk about the directive - does not
    /// itself contain the literal text and trip the very rule it enforces. The whole file is already covered by the
    /// <c>-:cnd:noEmit</c> on line 1, but this keeps the guard true even if that marker is ever removed.
    /// </summary>
    private static readonly string OpenDirectiveText = "#" + "if";

    /// <summary>
    /// Matches C#'s own preprocessor directives, which are never parenthesized, unlike template conditionals.
    /// </summary>
    private static readonly Regex csharpPreprocessorDirective = new(@"^\s*#(?:if|elif)\s+[A-Za-z_!(]", RegexOptions.Compiled);

    /// <summary>
    /// A conditional-processing marker on a line of its own: an optional host comment opener, then <c>-</c> or
    /// <c>+</c>, then the marker text. Anchored so that prose mentioning a marker is not mistaken for one.
    /// </summary>
    private static readonly Regex processingMarker = new(@"^\s*(?://|/\*|@\*|<!--|#)?\s*(?<onOff>[-+]):cnd:noEmit\s*(?:\*/|\*@|-->)?\s*$", RegexOptions.Compiled);

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
            .Where(file => Path.GetExtension(file) is not (".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or ".woff" or ".woff2" or ".ttf" or ".dll" or ".pdb" or ".zip" or ".keystore" or ".p12" or ".pfx" or ".webp" or ".mp4" or ".webm"));
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
