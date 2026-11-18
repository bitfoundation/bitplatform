using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

// RetryBaseAttribute's context and result are marked experimental by MSTest: a change of them is a compile error here,
// in the one file that uses them, which is where it should surface.
#pragma warning disable MSTESTEXP

namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// Runs a failed test once more, and reports one that then passes as flaky rather than letting it pass in silence.
/// Every test class carries it (<see cref="E2ETestBase"/> fails a test of a class that does not).
/// </summary>
/// <remarks>
/// <para>
/// The suite runs some 30,000 browser tests per CI run, and a loaded runner now and then stalls one of them past what it
/// waits for - a press held past the press-and-hold delay, a transient state missed between two polls, a circuit timed
/// out. Each such failure is one test, once, and a different one every run. A second attempt tells that apart from a
/// regression, which fails both times and still fails the job.
/// </para>
/// <para>
/// A retry is never a fix, so none is hidden: the first attempt's failure is written into the output of the attempt
/// that passed (the TRX keeps only that one), and, on GitHub Actions, into the job's summary as a table of the tests
/// that passed only on their second attempt. A test that keeps showing up there is a flaky test to fix.
/// </para>
/// <para>
/// MSTest reads a retry attribute off the test class itself, never off a base class, so it is on every class rather
/// than on <see cref="E2ETestBase"/>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class E2ERetryAttribute : RetryBaseAttribute
{
    // The line E2ETestBase writes into the output of a test that is failing, naming it: a TestResult does not carry the
    // name of its test.
    internal const string TestNameMarker = "[e2e-test] ";

    private static readonly Lock _summaryLock = new();

    protected override async Task<RetryResult> ExecuteAsync(RetryContext retryContext)
    {
        var result = new RetryResult();

        var retried = await retryContext.ExecuteTaskGetter();
        result.AddResult(retried);

        // An inconclusive second attempt (a CDN that could not be reached) verified nothing, so it is no proof of a flake.
        if (retried.Length > 0 && retried.All(r => r.Outcome == UnitTestOutcome.Passed))
        {
            ReportFlaky(retryContext.FirstRunResults, retried);
        }

        return result;
    }

    private static void ReportFlaky(TestResult[] firstRun, TestResult[] retried)
    {
        var name = NameOf(firstRun) ?? NameOf(retried) ?? "(a test that failed before it could name itself)";
        var failure = FailureOf(firstRun);

        foreach (var result in retried)
        {
            result.LogOutput += $"{Environment.NewLine}Passed on its second attempt. The first one failed:{Environment.NewLine}{failure}{Environment.NewLine}";
        }

        var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrEmpty(summary)) return;

        var firstLine = WhatFailed(failure);
        if (firstLine.Length > 300) firstLine = firstLine[..300] + "…";

        try
        {
            lock (_summaryLock)
            {
                var builder = new StringBuilder();
                if (File.Exists(summary) is false || File.ReadAllText(summary).Contains(SummaryHeading, StringComparison.Ordinal) is false)
                {
                    builder.AppendLine();
                    builder.AppendLine(SummaryHeading);
                    builder.AppendLine();
                    builder.AppendLine("A test listed here failed and then passed when it was run again, so it did not fail the job. Its first failure is in its output in the TRX.");
                    builder.AppendLine();
                    builder.AppendLine("| Test | First attempt |");
                    builder.AppendLine("|---|---|");
                }

                builder.AppendLine($"| `{name}` | {Cell(firstLine)} |");

                File.AppendAllText(summary, builder.ToString());
            }
        }
        catch (IOException) { } // the report is a courtesy: a summary that cannot be written never fails the test
        catch (UnauthorizedAccessException) { }
    }

    private const string SummaryHeading = "### E2E tests that passed only on their second attempt";

    private static string? NameOf(TestResult[] results)
    {
        foreach (var result in results)
        {
            var match = Regex.Match(result.TestContextMessages ?? "", Regex.Escape(TestNameMarker) + @"(\S+)");
            if (match.Success) return match.Groups[1].Value;
        }

        return null;
    }

    private static string FailureOf(TestResult[] results)
    {
        var failed = results.FirstOrDefault(r => r.Outcome is not (UnitTestOutcome.Passed or UnitTestOutcome.Inconclusive)) ?? results.FirstOrDefault();

        return failed?.TestFailureException?.Message ?? $"(outcome {failed?.Outcome.ToString() ?? "unknown"}, no exception)";
    }

    // The first line of a failure that says what went wrong: MSTest opens its messages with lines that only say where
    // ("Test method X threw exception:", "TestCleanup method X threw exception. ...") or that something did
    // ("Assertion failed.").
    private static string WhatFailed(string failure)
    {
        var lines = failure.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines.FirstOrDefault(line => line.Contains("threw exception", StringComparison.Ordinal) is false
                                            && line.StartsWith("Assertion failed", StringComparison.Ordinal) is false)
               ?? lines.FirstOrDefault()
               ?? "";
    }

    private static string Cell(string text) => text.Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "'", StringComparison.Ordinal);
}
