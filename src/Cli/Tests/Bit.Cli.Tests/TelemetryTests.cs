using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Bit.Cli.Tests.Infrastructure;
using OpenTelemetry.Trace;

namespace Bit.Cli.Tests;

[TestClass]
public class TelemetryTests
{
    [TestMethod]
    public void TheDefault_Should_BeErrorReports()
    {
        using var host = new TestHost();
        var decision = TelemetryDecision.Resolve(host.Environment, host.Services.Settings, "InstrumentationKey=00000000-0000-0000-0000-000000000000");

        Assert.AreEqual(TelemetryLevel.Errors, decision.Level);
        Assert.IsTrue(decision.IsDefault);
        Assert.IsTrue(decision.IsActive);
    }

    [TestMethod]
    public void ABuildWithoutAConnectionString_Should_SendNothing()
    {
        using var host = new TestHost();
        var decision = TelemetryDecision.Resolve(host.Environment, host.Services.Settings, null);

        Assert.AreEqual(TelemetryLevel.Errors, decision.Level);
        Assert.IsFalse(decision.IsActive);
    }

    [TestMethod]
    [DataRow("DO_NOT_TRACK", "1", TelemetryLevel.Off)]
    [DataRow("DOTNET_CLI_TELEMETRY_OPTOUT", "true", TelemetryLevel.Off)]
    [DataRow("BIT_CLI_TELEMETRY", "off", TelemetryLevel.Off)]
    [DataRow("BIT_CLI_TELEMETRY", "all", TelemetryLevel.All)]
    [DataRow("BIT_CLI_TELEMETRY", "errors", TelemetryLevel.Errors)]
    public void Variables_Should_SetTheLevel(string name, string value, TelemetryLevel expected)
    {
        using var host = new TestHost(variables: new Dictionary<string, string> { [name] = value });
        var decision = TelemetryDecision.Resolve(host.Environment, host.Services.Settings, "x");

        Assert.AreEqual(expected, decision.Level);
        Assert.AreEqual(name, decision.Reason);
    }

    [TestMethod]
    public void DoNotTrack_Should_WinOverEverythingElse()
    {
        using var host = new TestHost(variables: new Dictionary<string, string> { ["DO_NOT_TRACK"] = "1", ["BIT_CLI_TELEMETRY"] = "all" });
        host.Services.Settings.Telemetry = "all";

        Assert.AreEqual(TelemetryLevel.Off, TelemetryDecision.Resolve(host.Environment, host.Services.Settings, "x").Level);
    }

    [TestMethod]
    public void LogMode_Should_PrintInsteadOfSending()
    {
        using var host = new TestHost(variables: new Dictionary<string, string> { ["BIT_CLI_TELEMETRY"] = "log" });
        var decision = TelemetryDecision.Resolve(host.Environment, host.Services.Settings, null);

        Assert.IsTrue(decision.LogOnly);
        Assert.IsTrue(decision.IsActive);

        var writer = new StringWriter();
        var telemetry = CliTelemetry.Start(decision, host.Environment, host.Services.Settings, "translate", longRunning: false, writer);
        telemetry.Complete(0);

        StringAssert.Contains(writer.ToString(), "\"name\":\"bit translate\"");
        StringAssert.Contains(writer.ToString(), "bit.command");
    }

    [TestMethod]
    public void TheSetting_Should_ApplyWhenNoVariableOverridesIt()
    {
        using var host = new TestHost();
        host.Services.Settings.Telemetry = "off";
        Assert.IsTrue(host.Services.Settings.Save());

        var reloaded = CliSettings.Load(host.Environment);
        var decision = TelemetryDecision.Resolve(host.Environment, reloaded, "x");

        Assert.AreEqual(TelemetryLevel.Off, decision.Level);
        Assert.AreEqual("bit telemetry", decision.Reason);
    }

    [TestMethod]
    public void OnlyAllowedFields_Should_LeaveTheMachine()
    {
        using var host = new TestHost();
        var (activities, telemetry) = StartWithMemoryExporter(host, TelemetryLevel.Errors);

        telemetry.SetTag(TelemetryFields.ErrorCode, "NU1301");
        telemetry.SetTag("bit.project.name", "Contoso.Shop");
        telemetry.SetTag("file.path", @"C:\Users\jane\Contoso");
        telemetry.SetTag(TelemetryFields.Platforms, "web,android");
        telemetry.SetTag(TelemetryFields.TemplatePrefix + "database", "PostgreSQL");
        telemetry.Complete(0);

        var root = activities.Single(a => a.DisplayName == "bit new");
        var tags = root.TagObjects.Select(t => t.Key).ToList();

        CollectionAssert.Contains(tags, TelemetryFields.ErrorCode);
        CollectionAssert.Contains(tags, TelemetryFields.OsType);
        CollectionAssert.DoesNotContain(tags, "bit.project.name");
        CollectionAssert.DoesNotContain(tags, "file.path");
        CollectionAssert.DoesNotContain(tags, TelemetryFields.Platforms);
        CollectionAssert.DoesNotContain(tags, TelemetryFields.TemplatePrefix + "database");
    }

    [TestMethod]
    public void UsageFields_Should_LeaveTheMachineOnlyAtLevelAll()
    {
        using var host = new TestHost();
        var (activities, telemetry) = StartWithMemoryExporter(host, TelemetryLevel.All);

        telemetry.SetTag(TelemetryFields.Platforms, "web,android");
        telemetry.SetTag(TelemetryFields.TemplatePrefix + "database", "PostgreSQL");
        telemetry.Complete(0);

        var tags = activities.Single(a => a.DisplayName == "bit new").TagObjects.ToDictionary(t => t.Key, t => t.Value);

        Assert.AreEqual("web,android", tags[TelemetryFields.Platforms]);
        Assert.AreEqual("PostgreSQL", tags[TelemetryFields.TemplatePrefix + "database"]);
    }

    [TestMethod]
    public void Exceptions_Should_LeaveWithoutTheirMessage()
    {
        const string canary = "canary-7f3a1c";
        using var host = new TestHost();
        var (activities, telemetry) = StartWithMemoryExporter(host, TelemetryLevel.Errors);

        try
        {
            throw new IOException($"Couldn't open C:\\Users\\{canary}\\Contoso\\file.txt", new UnauthorizedAccessException($"denied for {canary}"));
        }
        catch (IOException exp)
        {
            telemetry.RecordException(null, exp);
        }

        var step = telemetry.StartStep("restore");
        CliTelemetry.CompleteStep(step, StepStatus.Failed, ["NU1301"], "restore.failed");
        telemetry.Complete(1);

        var everything = string.Join('\n', activities.SelectMany(a => a.TagObjects.Select(t => $"{t.Key}={t.Value}")
            .Concat(a.Events.SelectMany(e => e.Tags.Select(t => $"{e.Name}.{t.Key}={t.Value}")))
            .Append(a.DisplayName)));

        Assert.IsFalse(everything.Contains(canary, StringComparison.Ordinal), everything);

        var root = activities.Single(a => a.DisplayName == "bit new");
        var exception = root.Events.Single(e => e.Name == "exception");
        var eventTags = exception.Tags.ToDictionary(t => t.Key, t => t.Value?.ToString());

        Assert.AreEqual("System.IO.IOException", eventTags[TelemetryFields.ExceptionType]);
        StringAssert.StartsWith(eventTags[TelemetryFields.ExceptionMessage], "HResult 0x");
        StringAssert.Contains(eventTags[TelemetryFields.ExceptionStackTrace], "System.UnauthorizedAccessException");
        StringAssert.Contains(eventTags[TelemetryFields.ProblemId], "System.IO.IOException at ");
        Assert.AreEqual(ActivityStatusCode.Error, root.Status);

        var stepActivity = activities.Single(a => a.DisplayName == "restore");
        Assert.AreEqual("NU1301", stepActivity.GetTagItem(TelemetryFields.ErrorCode));
        Assert.AreEqual("failed", stepActivity.GetTagItem(TelemetryFields.StepOutcome));
        Assert.AreEqual(root.SpanId, stepActivity.ParentSpanId);
    }

    [TestMethod]
    public void TheRootSpan_Should_BeARequestWithTheMachineFacts()
    {
        using var host = new TestHost();
        var (activities, telemetry) = StartWithMemoryExporter(host, TelemetryLevel.Errors);
        telemetry.Complete(0);

        var root = activities.Single();

        Assert.AreEqual(ActivityKind.Server, root.Kind);
        Assert.AreEqual("new", root.GetTagItem(TelemetryFields.Command));
        Assert.AreEqual(0, root.GetTagItem(TelemetryFields.ExitCode));
        Assert.IsNotNull(root.GetTagItem(TelemetryFields.UserId));
        Assert.IsNotNull(root.GetTagItem(TelemetryFields.RuntimeVersion));
        Assert.AreEqual(32, telemetry.ErrorId!.Length);
    }

    [TestMethod]
    public void ProcessOnlyVariables_Should_NotReachChildProcesses()
    {
        var original = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED");

        try
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", null);
            var environment = CliEnvironment.FromProcess();
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", "true");

            var runner = new ProcessRunner(environment, CliLog.None);
            var spec = OperatingSystem.IsWindows()
                ? new ProcessSpec { FileName = "cmd.exe", Arguments = ["/c", "echo [%APPLICATIONINSIGHTS_STATSBEAT_DISABLED%]"] }
                : new ProcessSpec { FileName = "sh", Arguments = ["-c", "echo [$APPLICATIONINSIGHTS_STATSBEAT_DISABLED]"] };

            var result = runner.RunAsync(spec).GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.Output.Contains("[true]", StringComparison.Ordinal), result.Output);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", original);
        }
    }

    private static (List<Activity> Activities, CliTelemetry Telemetry) StartWithMemoryExporter(TestHost host, TelemetryLevel level)
    {
        var activities = new List<Activity>();
        var decision = new TelemetryDecision(level, false, "test", true);
        var telemetry = CliTelemetry.Start(decision, host.Environment, host.Services.Settings, "new", longRunning: false, TextWriter.Null, tracing => tracing.AddInMemoryExporter(activities));
        return (activities, telemetry);
    }
}
