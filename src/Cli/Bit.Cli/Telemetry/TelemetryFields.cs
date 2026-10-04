namespace Bit.Cli.Telemetry;

public static class TelemetryFields
{
    public const string Command = "bit.command";
    public const string ExitCode = "bit.exit_code";
    public const string FirstRun = "bit.first_run";
    public const string CliVersion = "bit.cli.version";
    public const string TemplateVersion = "bit.template.version";
    public const string InstallMethod = "bit.install_method";
    public const string Ci = "bit.ci";
    public const string Interactive = "bit.interactive";
    public const string Terminal = "bit.terminal";
    public const string CodingAgent = "bit.agent";
    public const string OsType = "os.type";
    public const string OsVersion = "os.version";
    public const string Architecture = "host.arch";
    public const string RuntimeVersion = "process.runtime.version";
    public const string SdkVersion = "dotnet.sdk.version";
    public const string Step = "bit.step";
    public const string StepOutcome = "bit.step.outcome";
    public const string ErrorCode = "bit.error.code";
    public const string ResultCode = "bit.result_code";
    public const string ProblemId = "bit.problem_id";
    public const string UserId = "enduser.pseudo.id";
    public const string SessionId = "microsoft.session.id";
    public const string Platforms = "bit.platforms";
    public const string Tools = "bit.tools";
    public const string Ide = "bit.ide";
    public const string Hardware = "bit.hardware";
    public const string TemplatePrefix = "bit.template.";
    public const string TranslateLanguages = "bit.translate.languages";
    public const string TranslateKeys = "bit.translate.keys";
    public const string TranslateBatches = "bit.translate.batches";
    public const string TranslateInputTokens = "bit.translate.input_tokens";
    public const string TranslateOutputTokens = "bit.translate.output_tokens";
    public const string TranslateProvider = "bit.translate.provider";
    public const string HttpStatus = "http.response.status_code";
    public const string ExceptionType = "exception.type";
    public const string ExceptionMessage = "exception.message";
    public const string ExceptionStackTrace = "exception.stacktrace";

    private static readonly HashSet<string> allowed =
    [
        Command, ExitCode, FirstRun, CliVersion, TemplateVersion, InstallMethod, Ci, Interactive, Terminal, CodingAgent,
        OsType, OsVersion, Architecture, RuntimeVersion, SdkVersion, Step, StepOutcome, ErrorCode, ResultCode, ProblemId,
        UserId, SessionId, Platforms, Tools, Ide, Hardware, TranslateLanguages, TranslateKeys, TranslateBatches, TranslateInputTokens,
        TranslateOutputTokens, TranslateProvider, HttpStatus
    ];

    private static readonly HashSet<string> usageOnly =
    [
        Platforms, Tools, Ide, TranslateLanguages, TranslateKeys, TranslateBatches, TranslateInputTokens, TranslateOutputTokens, TranslateProvider
    ];

    public static bool IsAllowed(string name)
    {
        return allowed.Contains(name) || (name.StartsWith(TemplatePrefix, StringComparison.Ordinal) && name.Length > TemplatePrefix.Length && name[TemplatePrefix.Length..].All(char.IsLetterOrDigit));
    }

    public static bool IsUsage(string name)
    {
        return usageOnly.Contains(name) || (name.StartsWith(TemplatePrefix, StringComparison.Ordinal) && name != TemplateVersion);
    }
}
