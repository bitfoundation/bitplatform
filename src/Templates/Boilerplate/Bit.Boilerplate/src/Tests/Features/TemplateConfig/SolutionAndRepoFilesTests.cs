//-:cnd:noEmit
// Conditional processing is off for this whole file, and the marker above has to stay on the very first line.
// These tests quote the solution files' own #if guards and template.json conditions verbatim; with processing on,
// the engine would read those quotes as real directives and truncate the file in every generated project.

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards the repository-surface files that no build ever validates: the two solution files, the clean scripts and
/// the browser-embedded shared appsettings. Nothing compiles them, so their defects ship silently - four distinct
/// instances were found in one review pass (a solution item whose file the template engine had excluded, a
/// <c>.grafana/README.MD</c> reference to a file stored as <c>README.md</c>, an unmatched <c>EndProjectSection</c>
/// that only parser leniency hid, and a clean script that deleted the two git-tracked service worker sources).
/// <para>
/// Like <see cref="TemplateConfigurationTests"/>, this runs against the template's own working copy and reports
/// inconclusive in a generated project (which has no <c>.template.config</c> directory).
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class SolutionAndRepoFilesTests
{
    /// <summary>Matches a <c>File</c> or <c>Project</c> item's <c>Path</c> attribute in the .slnx.</summary>
    private static readonly Regex slnxItemPath = new(@"<(?:File|Project)\s+Path=""(?<path>[^""]+)""", RegexOptions.Compiled);

    /// <summary>Matches the project path inside a .sln <c>Project("{...}") = "name", "path", "{guid}"</c> line.</summary>
    private static readonly Regex slnProjectPath = new(@"=\s*""[^""]+"",\s*""(?<path>[^""]+\.csproj)"",\s*""\{", RegexOptions.Compiled);

    /// <summary>
    /// Every path either solution file references must exist on disk <b>with exactly the casing it is written in</b>.
    /// Windows and macOS hide a casing mismatch forever - the maintainers' machines and Windows CI resolve
    /// <c>README.MD</c> to <c>README.md</c> happily - while every Linux dev container, Codespace and Rider-on-Linux
    /// user gets a permanently broken solution item. That is exactly how <c>.grafana/README.MD</c> shipped.
    /// </summary>
    [TestMethod]
    public void EverySolutionFilePath_Should_ExistOnDiskWithExactCasing()
    {
        var templateRoot = FindTemplateRoot();

        List<(string Origin, string Path)> referencedPaths = [];

        foreach (var line in File.ReadLines(Path.Combine(templateRoot, "Boilerplate.slnx")))
        {
            if (slnxItemPath.Match(line) is { Success: true } match)
                referencedPaths.Add(("Boilerplate.slnx", match.Groups["path"].Value.Replace("&amp;", "&")));
        }

        foreach (var rawLine in File.ReadLines(Path.Combine(templateRoot, "Boilerplate.sln")))
        {
            var line = rawLine.Trim();

            if (slnProjectPath.Match(line) is { Success: true } project)
            {
                referencedPaths.Add(("Boilerplate.sln", project.Groups["path"].Value));
                continue;
            }

            // Solution items are written as `path = path`. Requiring both sides to be identical (and free of the
            // `|` that configuration rows like `Debug|Any CPU = Debug|Any CPU` carry, and of the `{` that nested
            // project guid pairs start with) is what distinguishes them from every other `lhs = rhs` line.
            var separator = line.IndexOf('=');
            if (separator <= 0 || line.Contains('|') || line.StartsWith('{'))
                continue;

            var left = line[..separator].Trim();
            var right = line[(separator + 1)..].Trim();

            if (left.Length > 0 && left == right && left.Contains('.'))
                referencedPaths.Add(("Boilerplate.sln", left));
        }

        // Non-vacuity: the two files reference ~26 docs, ~14 root items and ~10 projects each.
        Assert.IsGreaterThan(40, referencedPaths.Count,
            $"Only {referencedPaths.Count} referenced paths were parsed out of the solution files - the parsing above is no longer matching their format.");

        var missing = referencedPaths
            .Where(reference => ExistsWithExactCasing(templateRoot, reference.Path) is false)
            .Select(reference => $"{reference.Origin}: '{reference.Path}'")
            .ToList();

        Assert.IsEmpty(missing,
            "These solution entries do not resolve to a file with that exact casing, so on a case-sensitive file " +
            $"system (Linux dev container, Codespaces) they are dead solution items:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>
    /// Every section opened in the .sln must be closed exactly once. Visual Studio and <c>dotnet sln</c> tolerate an
    /// unmatched <c>EndProjectSection</c> - one shipped for months - so leniency, not correctness, is all that keeps
    /// a malformed solution working, and stricter third-party parsers get to be the first to notice.
    /// </summary>
    [TestMethod]
    public void SolutionFileSections_Should_BeBalanced()
    {
        var templateRoot = FindTemplateRoot();

        var lines = File.ReadAllLines(Path.Combine(templateRoot, "Boilerplate.sln")).Select(line => line.Trim()).ToArray();

        int Openers(string keyword) => lines.Count(line => line.StartsWith($"{keyword}(", StringComparison.Ordinal));
        int Closers(string keyword) => lines.Count(line => line == keyword);

        // Non-vacuity: the solution declares several item folders, each with one ProjectSection.
        Assert.IsGreaterThan(3, Openers("ProjectSection"), "Almost no ProjectSection lines were found - the .sln is not being read as expected.");

        Assert.AreEqual(Openers("Project"), Closers("EndProject"),
            "The .sln opens a different number of Project blocks than it closes.");
        Assert.AreEqual(Openers("ProjectSection"), Closers("EndProjectSection"),
            "The .sln opens a different number of ProjectSections than it closes - a stray or missing EndProjectSection.");
        Assert.AreEqual(Openers("GlobalSection"), Closers("EndGlobalSection"),
            "The .sln opens a different number of GlobalSections than it closes.");
    }

    /// <summary>
    /// <c>.docs/25</c> is the one solution item that <c>template.json</c> excludes conditionally, and it is excluded
    /// by TWO rules - one on <c>database</c>, one on <c>module</c>. Each solution file's <c>#if</c> guard around the
    /// entry must mention every symbol those rules test, or the quadrant where the unmentioned rule fires generates a
    /// solution that lists a file the engine deleted. Both halves have shipped: the .sln had no guard at all (so the
    /// DEFAULT Sqlite generation carried the dangling entry), and the .slnx guard tested only <c>database</c> (so
    /// SqlServer + module=None did).
    /// </summary>
    [TestMethod]
    public void ConditionallyExcludedDocsSolutionItem_Should_BeGuardedOnEverySymbolTemplateJsonTestsForIt()
    {
        var (templateRoot, template) = LoadTemplateJson();

        using (template)
        {
            var requiredSymbols = template.RootElement.GetProperty("sources").EnumerateArray()
                .Where(source => source.TryGetProperty("modifiers", out _))
                .SelectMany(source => source.GetProperty("modifiers").EnumerateArray())
                .Where(modifier => modifier.TryGetProperty("exclude", out var exclude)
                                   && exclude.ValueKind is JsonValueKind.Array
                                   && exclude.EnumerateArray().Any(entry => entry.GetString()?.StartsWith(".docs/25", StringComparison.Ordinal) is true))
                .Select(modifier => modifier.GetProperty("condition").GetString()!)
                .Select(condition => Regex.Replace(condition, "\"[^\"]*\"", " "))
                .SelectMany(condition => Regex.Matches(condition, @"[A-Za-z_][A-Za-z0-9_]*").Select(match => match.Value))
                .Where(token => token is not ("true" or "false" or "null"))
                .ToHashSet(StringComparer.Ordinal);

            // Non-vacuity: today those rules test `database` and `module`. Zero means the rules moved or the file was
            // renamed, and this test is checking nothing.
            Assert.IsGreaterThan(1, requiredSymbols.Count,
                "No conditional template.json exclusion for '.docs/25*' was found - either the rules changed shape or the doc was renamed; update this test.");

            foreach (var (solutionFile, entryMarker) in new[] { ("Boilerplate.sln", @".docs\25"), ("Boilerplate.slnx", ".docs/25") })
            {
                var lines = File.ReadAllLines(Path.Combine(templateRoot, solutionFile));
                var entryIndex = Array.FindIndex(lines, line => line.Contains(entryMarker, StringComparison.Ordinal));

                Assert.IsGreaterThan(0, entryIndex, $"{solutionFile} no longer references '{entryMarker}' - if the doc was removed, delete this test's guard for it.");

                var guard = lines[entryIndex - 1];

                Assert.IsTrue(guard.Contains("#" + "if", StringComparison.Ordinal),
                    $"{solutionFile}'s '{entryMarker}' entry is not immediately preceded by a #if guard, so configurations that exclude the file still list it.");

                foreach (var symbol in requiredSymbols)
                {
                    Assert.IsTrue(guard.Contains(symbol, StringComparison.Ordinal),
                        $"{solutionFile}'s guard around '{entryMarker}' does not test '{symbol}', but a template.json rule excludes the file based on it - " +
                        "the configurations where only that rule fires get a dangling solution item.");
                }
            }
        }
    }

    /// <summary>
    /// Both clean scripts delete every <c>*.css</c>/<c>*.js</c>/<c>*.map</c> in the tree, and the repository has
    /// exactly two git-tracked files matching those patterns: <c>service-worker.js</c> and
    /// <c>service-worker.published.js</c> - hand-written PWA/push source that nothing regenerates. The scripts'
    /// behavior is shell-runtime and cannot run inside this suite, so what is pinned here is the presence of the two
    /// guards that keep them from destroying source: the tracked-file check, and the refusal to run the deletion
    /// pass at all outside a git repository (where "tracked" cannot be answered and <c>Clean.bat</c>'s original
    /// guard silently matched nothing - precisely in the state, fresh from <c>dotnet new</c>, with no history to
    /// restore from).
    /// </summary>
    [TestMethod]
    public void CleanScripts_Should_GuardGitTrackedFilesFromDeletion()
    {
        var templateRoot = FindTemplateRoot();

        var cleanSh = File.ReadAllText(Path.Combine(templateRoot, "Clean.sh"));
        var cleanBat = File.ReadAllText(Path.Combine(templateRoot, "Clean.bat"));

        Assert.IsTrue(cleanSh.Contains("git ls-files --error-unmatch", StringComparison.Ordinal),
            "Clean.sh no longer skips git-tracked files before deleting css/js/map files, so it deletes the hand-written service worker sources.");
        Assert.IsTrue(cleanSh.Contains("git rev-parse --is-inside-work-tree", StringComparison.Ordinal),
            "Clean.sh no longer refuses the css/js/map deletion pass outside a git repository, where it cannot tell source files from build output.");

        Assert.IsTrue(cleanBat.Contains("git ls-files", StringComparison.Ordinal),
            "Clean.bat no longer consults git for tracked files before deleting css/js/map files.");
        Assert.IsTrue(cleanBat.Contains("IsNullOrWhiteSpace($trackedFiles)", StringComparison.Ordinal),
            "Clean.bat no longer detects the git-ls-files-failed case (not a git repository), where its tracked-file guard matches nothing and everything gets deleted.");
    }

    /// <summary>
    /// <c>src/Shared/appsettings*.json</c> is embedded into <c>Boilerplate.Shared.dll</c>
    /// (<c>Boilerplate.Shared.csproj</c>'s <c>EmbeddedResource</c> rule), and browsers download that assembly as part
    /// of the Client.Web WebAssembly payload - so anything in these files is world-readable. The
    /// <c>OTEL_EXPORTER_OTLP_*HEADERS</c> stubs are the standard slot for telemetry auth tokens; a value here would
    /// hand that token to every anonymous visitor. Server-side values belong in the Server.Api / Server.Web
    /// appsettings, which override this shared layer.
    /// </summary>
    [TestMethod]
    public void BrowserEmbeddedSharedAppSettings_Should_CarryNoOtlpAuthHeaders()
    {
        var templateRoot = FindTemplateRoot();

        List<string> filled = [];
        var headerKeysSeen = 0;

        foreach (var file in Directory.EnumerateFiles(Path.Combine(templateRoot, "src", "Shared"), "appsettings*.json"))
        {
            using var settings = JsonDocument.Parse(File.ReadAllText(file),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            foreach (var property in settings.RootElement.EnumerateObject())
            {
                if (property.Name.StartsWith("OTEL_EXPORTER_OTLP", StringComparison.Ordinal) is false ||
                    property.Name.EndsWith("HEADERS", StringComparison.Ordinal) is false)
                    continue;

                headerKeysSeen++;

                if (property.Value.ValueKind is not JsonValueKind.Null)
                    filled.Add($"{Path.GetFileName(file)}: {property.Name} = {property.Value}");
            }
        }

        // Non-vacuity: the base file stubs four *_HEADERS keys.
        Assert.IsGreaterThan(2, headerKeysSeen,
            $"Only {headerKeysSeen} OTLP header stubs were found in src/Shared/appsettings*.json - the stubs moved and this test is checking nothing.");

        Assert.IsEmpty(filled,
            "These OTLP auth header values are set in the browser-embedded shared appsettings, which every anonymous " +
            $"visitor downloads. Move them to Server.Api/Server.Web appsettings or environment variables:{Environment.NewLine}{string.Join(Environment.NewLine, filled)}");
    }

    /// <summary>
    /// True when every segment of <paramref name="relativePath"/> matches an on-disk entry with ordinal (case-exact)
    /// comparison - which is what a case-sensitive file system requires, and what <see cref="File.Exists(string)"/>
    /// on Windows cannot answer.
    /// </summary>
    private static bool ExistsWithExactCasing(string root, string relativePath)
    {
        var current = root;

        foreach (var segment in relativePath.Split('/', '\\'))
        {
            var match = Directory.Exists(current)
                ? Directory.EnumerateFileSystemEntries(current).FirstOrDefault(entry => string.Equals(Path.GetFileName(entry), segment, StringComparison.Ordinal))
                : null;

            if (match is null)
                return false;

            current = match;
        }

        return true;
    }

    private static string FindTemplateRoot()
    {
        var (templateRoot, template) = LoadTemplateJson();
        template?.Dispose();
        return templateRoot;
    }

    /// <summary>
    /// Finds the template root by walking up from the test binaries, exactly as
    /// <see cref="TemplateConfigurationTests"/> does. <c>template.json</c> carries <c>//</c> comments, so it is not
    /// strict JSON.
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
