using System.Diagnostics;
using Bit.Cli.Telemetry;
using Spectre.Console;

namespace Bit.Cli.Infrastructure;

public sealed record StepResult
{
    public required StepStatus Status { get; init; }

    public required string Title { get; init; }

    public string? Detail { get; init; }

    public string? FollowUp { get; init; }

    public string? Hint { get; init; }

    public IReadOnlyList<string> Codes { get; init; } = [];

    public string? ResultCode { get; init; }

    public static StepResult Succeeded(string title, string? detail = null, string? hint = null) => new() { Status = StepStatus.Succeeded, Title = title, Detail = detail, Hint = hint };

    public static StepResult Warning(string title, string? detail = null, string? followUp = null, string? hint = null, string? resultCode = null)
        => new() { Status = StepStatus.Warning, Title = title, Detail = detail, FollowUp = followUp, Hint = hint, ResultCode = resultCode };

    public static StepResult Skipped(string title, string? detail = null) => new() { Status = StepStatus.Skipped, Title = title, Detail = detail };

    public static StepResult Failed(string title, string? detail = null, string? followUp = null, IReadOnlyList<string>? codes = null, string? resultCode = null, string? hint = null)
        => new() { Status = StepStatus.Failed, Title = title, Detail = detail, FollowUp = followUp, Codes = codes ?? [], ResultCode = resultCode, Hint = hint };

    public static StepResult FromProcess(ProcessResult result, string succeededTitle, string failedTitle, string? followUp, string? succeededDetail = null)
    {
        if (result.Succeeded)
            return Succeeded(succeededTitle, succeededDetail);

        if (result.NotFound)
            return Failed(failedTitle, "the command wasn't found", followUp, resultCode: "tool.missing");

        if (result.TimedOut)
            return Failed(failedTitle, "it took too long and was stopped", followUp, resultCode: "timeout");

        var codes = DiagnosticCodes.Extract(result.Output);
        var firstError = DiagnosticCodes.FirstErrorLine(result.Output);
        var detail = codes.Count > 0 ? string.Join(" · ", codes) : $"exit code {result.ExitCode}";

        return Failed(failedTitle, detail, followUp, codes, hint: firstError);
    }
}

public sealed record StepReport(string Id, StepResult Result, TimeSpan Duration);

public sealed class StepRunner(CliServices cli)
{
    private readonly List<StepReport> reports = [];

    public IReadOnlyList<StepReport> Reports => reports;

    public bool AnyFailed => reports.Any(r => r.Result.Status is StepStatus.Failed);

    public async Task<StepResult> RunAsync(string id, string runningTitle, Func<Action<string>, CancellationToken, Task<StepResult>> work, CancellationToken cancellationToken, bool needsTerminal = false)
    {
        var activity = cli.Telemetry.StartStep(id);
        var stopwatch = Stopwatch.StartNew();
        StepResult result;

        cli.Log.Write($"== {id}: {runningTitle}");

        try
        {
            if (needsTerminal)
            {
                cli.Console.Out.MarkupLine($"  [blue]>[/] {Markup.Escape(runningTitle)}");
                result = await work(_ => { }, cancellationToken);
            }
            else
            {
                result = await cli.Console.RunWithStatusAsync(runningTitle, progress => work(progress, cancellationToken));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CliTelemetry.CompleteStep(activity, StepStatus.Failed, resultCode: "user.canceled");
            throw;
        }
        catch (Exception exp)
        {
            cli.Telemetry.RecordException(activity, exp);
            cli.Log.Write(exp.ToString());
            result = StepResult.Failed(runningTitle, exp.Message, resultCode: "internal." + exp.GetType().Name);
        }

        stopwatch.Stop();
        CliTelemetry.CompleteStep(activity, result.Status, result.Codes, result.ResultCode);

        cli.Console.Step(result.Status, result.Title, result.Detail, result.Status is StepStatus.Skipped ? null : stopwatch.Elapsed);

        if (result.Hint is not null && result.Status is not StepStatus.Succeeded)
        {
            cli.Console.Out.MarkupLine($"      [grey]{Markup.Escape(CliConsole.Truncate(result.Hint, cli.Console.Width - 8))}[/]");
        }

        cli.Log.Write($"== {id}: {result.Status} {result.Detail}");
        reports.Add(new StepReport(id, result, stopwatch.Elapsed));
        return result;
    }

    public void Add(string id, StepResult result)
    {
        cli.Console.Step(result.Status, result.Title, result.Detail);
        reports.Add(new StepReport(id, result, TimeSpan.Zero));
    }
}
