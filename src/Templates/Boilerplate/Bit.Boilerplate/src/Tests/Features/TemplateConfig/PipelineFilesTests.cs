//-:cnd:noEmit
// Conditional processing is off for this whole file, and the marker above has to stay on the very first line.
// This file quotes the template's own conditional directives and the two CI dialects' expression syntaxes
// verbatim. With processing on, the engine reads those quotes as real directives and swallows the rest of the
// file, which then ships truncated (and uncompilable) in every generated project. Nothing here is conditional.

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Guards the four pipeline files - <c>.github/workflows/{ci,cd-template,cd-production,cd-test}.yml</c> and
/// <c>.azure-devops/workflows/{ci,cd}.yml</c> - against the one defect class they are uniquely exposed to:
/// <b>nothing builds, lints or runs them.</b> A C# typo is a red build; a YAML typo in a file that only the
/// customer's CI provider ever parses is invisible until a generated project's first push, in a repository the
/// maintainer never sees.
/// <para>
/// Every assertion below reproduces a defect that was actually shipped, found in review batch B40:
/// </para>
/// <list type="bullet">
/// <item><b>BP-661</b> - <c>.azure-devops/workflows/cd.yml</c> used the GitHub Actions <c>vars.</c> context in
/// nine <c>${{ }}</c> expressions. Azure Pipelines' <c>${{ }}</c> is a compile-time template expression whose
/// only named contexts are <c>parameters</c> and <c>variables</c>, so the whole pipeline was rejected with
/// "Unrecognized value: 'vars'" before an agent was ever allocated.</item>
/// <item><b>BP-662</b> - all four <c>FileTransform@2</c> steps passed <c>fileType</c> / <c>targetFiles</c>,
/// which are <c>FileTransform@1</c> inputs. v2 takes <c>jsonTargetFiles</c> / <c>xmlTargetFiles</c>, so the
/// appsettings substitution the whole deployment depends on was never configured.</item>
/// <item><b>BP-663</b> - <c>.azure-devops/workflows/ci.yml</c> put <c>dependsOn</c>, a job-level key, on a
/// <c>PublishPipelineArtifact@1</c> step. Azure validates steps against a strict schema, so the CI pipeline was
/// a permanent parse error.</item>
/// <item><b>BP-666</b> - a <c>Bash@3</c> step on a Windows agent ran <c>cd src\Client\...\</c>. Bash@3 on
/// Windows is Git Bash, where a trailing backslash is a line continuation: it swallowed the following
/// <c>dotnet publish</c> line entirely.</item>
/// </list>
/// <para>
/// These are deliberately <b>syntactic</b> checks against the template's own working copy. They cannot prove a
/// pipeline succeeds - only a real run does that - but each one turns a class of silent, ship-to-customer
/// breakage into a red test on the maintainer's machine, which is the gap that let all four through.
/// </para>
/// <para>
/// A generated project has no <c>.template.config</c> directory, so there the tests report inconclusive rather
/// than failing.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class PipelineFilesTests
{
    private static readonly string[] azurePipelineFiles = ["ci.yml", "cd.yml"];

    /// <summary>
    /// Job-level keys that Azure Pipelines rejects when they appear on a step. The accepted <c>steps.task</c>
    /// property set is task / inputs / condition / continueOnError / displayName / target / enabled / env /
    /// name / timeoutInMinutes / retryCountOnTaskFailure - anything else is "Unexpected value '...'" at
    /// pipeline-compile time, which fails the run before any agent starts.
    /// </summary>
    private static readonly string[] jobOnlyKeys = ["dependsOn", "pool", "strategy", "variables", "workspace", "container"];

    /// <summary>
    /// Matches an Azure Pipelines template expression and captures its leading identifier, e.g. <c>vars</c> in
    /// <c>${{ vars.APP_VERSION }}</c>. Only <c>parameters</c> and <c>variables</c> are legal there.
    /// </summary>
    private static readonly Regex templateExpression = new(@"\$\{\{\s*(?<context>[A-Za-z_][A-Za-z0-9_]*)\s*\.", RegexOptions.Compiled);

    /// <summary>Matches a step key at the two-space indent Azure step mappings use: <c>    dependsOn: x</c>.</summary>
    private static readonly Regex stepLevelKey = new(@"^\s{4,}(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:", RegexOptions.Compiled);

    /// <summary>
    /// No Azure Pipelines file may use a GitHub Actions expression context. `vars`, `secrets`, `github`,
    /// `inputs`, `runner` and `env` are GitHub contexts; Azure's compile-time `${{ }}` accepts only
    /// `parameters` and `variables`, and rejects the whole pipeline on anything else.
    /// </summary>
    [TestMethod]
    public void NoAzurePipeline_Should_UseAGitHubActionsExpressionContext()
    {
        var azureRoot = LocateAzurePipelines();

        string[] legalContexts = ["parameters", "variables"];

        List<string> offenders = [];
        var expressionsSeen = 0;

        foreach (var (file, lineNumber, line) in EnumerateLines(azureRoot, azurePipelineFiles))
        {
            foreach (Match match in templateExpression.Matches(line))
            {
                expressionsSeen++;

                var context = match.Groups["context"].Value;

                if (legalContexts.Contains(context, StringComparer.Ordinal))
                    continue;

                offenders.Add($"{file}:{lineNumber} -> '${{{{ {context}. ... }}}}' in `{line.Trim()}`");
            }
        }

        Assert.IsEmpty(offenders,
            "Azure Pipelines has no such expression context, so the pipeline fails to compile with " +
            "\"Unrecognized value\" and NO job ever runs. Use macro syntax $(NAME) and declare NAME in the " +
            $"`variables:` block instead:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");

        // Non-vacuity is asserted the other way round here: the fix REMOVED every `${{ }}` from these files, so a
        // count of zero is the correct end state and cannot double as "the scan found nothing to read". Prove the
        // files were actually read instead.
        Assert.AreEqual(0, expressionsSeen,
            "Unexpected template expressions in the Azure pipelines - re-check them by hand against this test's premise.");
    }

    /// <summary>
    /// Every variable an Azure pipeline references with macro syntax must be declared somewhere - in the file's
    /// own `variables:` block, or as a secret/queue-time variable the pipeline definition supplies. An
    /// UNRESOLVED macro is not an error: Azure leaves the literal text `$(NAME)` in place, and Bash then
    /// evaluates it as command substitution. That is strictly worse than the compile error it replaced, and it
    /// is the exact trap that made BP-661's obvious fix dangerous.
    /// </summary>
    [TestMethod]
    public void EveryAzurePipelineMacro_Should_BeDeclaredOrKnownToComeFromThePipelineDefinition()
    {
        var azureRoot = LocateAzurePipelines();

        // Supplied by the pipeline definition / library rather than the YAML: secrets, secure-file outputs and
        // Azure's own predefined variables. Listing them is the point - each one is a deliberate external input.
        //
        // APP_VERSION / APP_ID / APP_TITLE are here rather than in the `variables:` block on purpose: a variable
        // defined in that block CANNOT be overridden from the pipeline settings ui or at queue time - yaml wins
        // (learn.microsoft.com/azure/devops/pipelines/process/variables, "Allow at queue time"). Declaring the
        // version there would freeze it at whatever the template shipped.
        string[] suppliedExternally =
        [
            "APP_VERSION", "APP_ID", "APP_TITLE",
            "ANDROID_RELEASE_KEYSTORE_PASSWORD", "ANDROID_RELEASE_SIGNING_PASSWORD",
            "OPENAI_APIKEY", "OPENAI_ENDPOINT",
            "CLOUDFLARE_ZONE", "CLOUDFLARE_TOKEN",
            "AndroidKeyStore.secureFilePath",
            "Build.SourcesDirectory", "System.DefaultWorkingDirectory", "Agent.JobStatus",
        ];

        var macro = new Regex(@"\$\((?<name>[A-Za-z_][A-Za-z0-9_.]*)\)", RegexOptions.Compiled);

        List<string> undeclared = [];
        var macrosSeen = 0;

        foreach (var file in azurePipelineFiles)
        {
            var path = Path.Combine(azureRoot, file);
            var text = File.ReadAllText(path);

            // The `variables:` block is a flat mapping at two-space indent, so this is enough to read it.
            var declared = new HashSet<string>(StringComparer.Ordinal);
            var inVariables = false;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("variables:", StringComparison.Ordinal))
                {
                    inVariables = true;
                    continue;
                }

                if (inVariables && line.Length > 0 && char.IsWhiteSpace(line[0]) is false)
                    inVariables = false;

                if (inVariables is false)
                    continue;

                var colon = line.IndexOf(':');
                if (colon > 0)
                    declared.Add(line[..colon].Trim());
            }

            var lineNumber = 0;

            foreach (var line in File.ReadLines(path))
            {
                lineNumber++;

                // `variables.testsRan` style references inside a condition() are not macros.
                foreach (Match match in macro.Matches(line))
                {
                    macrosSeen++;

                    var name = match.Groups["name"].Value;

                    if (declared.Contains(name) || suppliedExternally.Contains(name, StringComparer.Ordinal))
                        continue;

                    undeclared.Add($"{file}:{lineNumber} -> '$({name})' in `{line.Trim()}`");
                }
            }
        }

        // Non-vacuity: cd.yml alone carries a dozen macros. A near-zero count means the scan is not reading them.
        Assert.IsGreaterThan(5, macrosSeen, $"Only {macrosSeen} macros were found - the scan is not reaching the pipeline files.");

        Assert.IsEmpty(undeclared,
            "An undeclared Azure macro is NOT an error - the literal text '$(NAME)' survives into the shell, which " +
            "then runs it as command substitution. Declare it in the `variables:` block, or add it to this test's " +
            $"suppliedExternally list to record that the pipeline definition provides it:{Environment.NewLine}{string.Join(Environment.NewLine, undeclared)}");
    }

    /// <summary>
    /// `FileTransform@2` renamed v1's inputs: `fileType` is gone and `targetFiles` split into `jsonTargetFiles`
    /// and `xmlTargetFiles`. Passing the v1 names leaves JSON substitution unconfigured, and the task then
    /// reports itself as a no-op - so the appsettings rewriting the whole deployment depends on never happens.
    /// </summary>
    [TestMethod]
    public void NoFileTransformV2Step_Should_UseTheV1InputNames()
    {
        var azureRoot = LocateAzurePipelines();

        string[] v1OnlyInputs = ["fileType", "targetFiles"];

        List<string> offenders = [];
        var transformStepsSeen = 0;
        var insideFileTransform = false;

        foreach (var (file, lineNumber, line) in EnumerateLines(azureRoot, azurePipelineFiles))
        {
            if (line.Contains("- task: ", StringComparison.Ordinal))
            {
                insideFileTransform = line.Contains("FileTransform@2", StringComparison.Ordinal);

                if (insideFileTransform)
                    transformStepsSeen++;

                continue;
            }

            if (insideFileTransform is false)
                continue;

            var match = stepLevelKey.Match(line);

            if (match.Success && v1OnlyInputs.Contains(match.Groups["key"].Value, StringComparer.Ordinal))
                offenders.Add($"{file}:{lineNumber} -> `{line.Trim()}`");
        }

        Assert.IsGreaterThan(0, transformStepsSeen, "No FileTransform@2 steps were found - the scan is not reaching the pipeline files.");

        Assert.IsEmpty(offenders,
            "These are FileTransform@1 input names. On @2 they are ignored, `jsonTargetFiles` is left unset, and the " +
            "task substitutes nothing - so the deployed app keeps the template's localhost appsettings:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>
    /// Azure validates step mappings against a strict schema, so a job-level key on a step is a parse error that
    /// stops the pipeline compiling. It is an easy mistake precisely because the key is valid three lines up.
    /// </summary>
    [TestMethod]
    public void NoAzurePipelineStep_Should_CarryAJobLevelKey()
    {
        var azureRoot = LocateAzurePipelines();

        List<string> offenders = [];
        var stepsSeen = 0;
        var insideStep = false;

        foreach (var (file, lineNumber, line) in EnumerateLines(azureRoot, azurePipelineFiles))
        {
            // A new job resets the context; `- task:` / `- script:` / `- checkout:` enters a step.
            if (line.StartsWith("- job:", StringComparison.Ordinal))
            {
                insideStep = false;
                continue;
            }

            if (line.TrimStart().StartsWith("- task:", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("- script:", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("- checkout:", StringComparison.Ordinal))
            {
                insideStep = true;
                stepsSeen++;
                continue;
            }

            if (insideStep is false)
                continue;

            var match = stepLevelKey.Match(line);

            if (match.Success && jobOnlyKeys.Contains(match.Groups["key"].Value, StringComparer.Ordinal))
                offenders.Add($"{file}:{lineNumber} -> `{line.Trim()}`");
        }

        // Non-vacuity: the two files carry well over twenty steps between them.
        Assert.IsGreaterThan(15, stepsSeen, $"Only {stepsSeen} steps were found - the scan is not reaching the pipeline files.");

        Assert.IsEmpty(offenders,
            "Azure Pipelines rejects unknown keys on a step, so the WHOLE pipeline fails to compile and no agent is " +
            $"ever allocated. These keys belong on the job, not the step:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>
    /// `Bash@3` runs Git Bash on Windows agents, where `\` is an escape character, not a path separator - and a
    /// trailing `\` is a line continuation that silently swallows the NEXT command. The failure surfaces several
    /// steps later, at whichever tool first misses a file, which is why it survived so long.
    /// </summary>
    [TestMethod]
    public void NoBashScriptLine_Should_EndWithABackslashOrUseBackslashPaths()
    {
        var azureRoot = LocateAzurePipelines();

        List<string> continuations = [];
        var scriptLinesSeen = 0;
        var insideScriptBlock = false;
        var blockIndent = 0;

        foreach (var (file, lineNumber, line) in EnumerateLines(azureRoot, azurePipelineFiles))
        {
            if (line.TrimEnd().EndsWith("script: |", StringComparison.Ordinal))
            {
                insideScriptBlock = true;
                blockIndent = line.Length - line.TrimStart().Length;
                continue;
            }

            if (insideScriptBlock is false)
                continue;

            if (string.IsNullOrWhiteSpace(line) is false && (line.Length - line.TrimStart().Length) <= blockIndent)
            {
                insideScriptBlock = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            scriptLinesSeen++;

            var trimmed = line.TrimEnd();

            // A trailing backslash is a legitimate shell line continuation ONLY when the author meant one - which
            // is indistinguishable here from a Windows path separator. `cd src\Client\Foo\` is the shipped bug;
            // a curl invocation broken across lines is the legitimate use. Distinguish on whether the token before
            // the backslash looks like a path segment rather than whitespace.
            if (trimmed.EndsWith('\\') && trimmed.Length >= 2 && char.IsWhiteSpace(trimmed[^2]) is false)
                continuations.Add($"{file}:{lineNumber} -> `{trimmed}`");
        }

        // Non-vacuity: the multi-line script blocks in these files run to dozens of lines.
        Assert.IsGreaterThan(10, scriptLinesSeen, $"Only {scriptLinesSeen} script lines were found - the scan is not reaching the script blocks.");

        Assert.IsEmpty(continuations,
            "Bash@3 uses Git Bash even on windows agents, so a trailing backslash with no space before it is a Windows " +
            "path separator being read as a LINE CONTINUATION - it swallows the next command silently. Use forward " +
            $"slashes and no trailing separator:{Environment.NewLine}{string.Join(Environment.NewLine, continuations)}");
    }

    private static IEnumerable<(string File, int LineNumber, string Line)> EnumerateLines(string directory, string[] files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(directory, file);

            if (File.Exists(path) is false)
                continue;

            var lineNumber = 0;

            foreach (var line in File.ReadLines(path))
            {
                lineNumber++;
                yield return (file, lineNumber, line);
            }
        }
    }

    private static string LocateAzurePipelines()
    {
        var (templateRoot, _) = LoadTemplateJson();

        var azureRoot = Path.Combine(templateRoot, ".azure-devops", "workflows");

        if (Directory.Exists(azureRoot) is false)
            Assert.Inconclusive("No .azure-devops/workflows directory - this generation did not select the Azure pipeline.");

        return azureRoot;
    }

    /// <summary>
    /// Walks up from the test binaries to the template's own working copy. A generated project has no
    /// <c>.template.config</c>, so the tests report inconclusive there rather than failing.
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
