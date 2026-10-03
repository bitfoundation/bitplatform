using System.Diagnostics;
using System.Text;
using Azure.Monitor.OpenTelemetry.Exporter;
using Bit.Cli.Infrastructure;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bit.Cli.Telemetry;

public sealed class CliTelemetry : IDisposable
{
    public const string SourceName = "Bit.Cli";

    public static readonly ActivitySource Source = new(SourceName, BuildInfo.Version);

    private readonly OpenTelemetrySdk? sdk;
    private bool completed;

    private CliTelemetry(TelemetryDecision decision, OpenTelemetrySdk? sdk)
    {
        Decision = decision;
        this.sdk = sdk;
    }

    public TelemetryDecision Decision { get; }

    public Activity? Root { get; private set; }

    public string? ErrorId => Root?.TraceId.ToHexString();

    public static CliTelemetry Disabled(TelemetryDecision decision) => new(decision, null);

    public static CliTelemetry Start(TelemetryDecision decision, CliEnvironment environment, CliSettings settings, string command, bool longRunning, TextWriter logWriter, Action<TracerProviderBuilder>? configure = null)
    {
        if (decision.IsActive is false && configure is null)
            return Disabled(decision);

        PrepareProcess(environment, longRunning);

        var sdk = OpenTelemetrySdk.Create(builder => builder
            .ConfigureResource(resource => resource.Clear().AddService("bit", serviceVersion: BuildInfo.Version, serviceInstanceId: "cli"))
            .WithTracing(tracing =>
            {
                tracing.AddSource(SourceName).AddProcessor(new AllowListProcessor(decision.Level));

                if (configure is not null)
                {
                    configure(tracing);
                }
                else if (decision.LogOnly)
                {
                    tracing.SetSampler(new AlwaysOnSampler()).AddProcessor(new SimpleActivityExportProcessor(new JsonLinesExporter(logWriter)));
                }
                else
                {
                    tracing.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = BuildInfo.TelemetryConnectionString;
                        options.StorageDirectory = Path.Combine(environment.BitDirectory, "telemetry");
                        options.TracesPerSecond = null;
                        options.SamplingRatio = 1F;
                        options.EnableLiveMetrics = false;
                        options.EnableStandardMetrics = false;
                        options.EnablePerformanceCounters = false;
                    });
                }
            }));

        var telemetry = new CliTelemetry(decision, sdk);
        var installId = settings.InstallId;

        telemetry.Root = Source.StartActivity($"bit {command}", ActivityKind.Server);
        telemetry.Root?
            .SetTag(TelemetryFields.Command, command)
            .SetTag(TelemetryFields.CliVersion, BuildInfo.Version)
            .SetTag(TelemetryFields.FirstRun, settings.IsFirstRun)
            .SetTag(TelemetryFields.UserId, installId)
            .SetTag(TelemetryFields.SessionId, Guid.NewGuid().ToString("N"))
            .SetTag(TelemetryFields.OsType, environment.Os.ToString().ToLowerInvariant())
            .SetTag(TelemetryFields.OsVersion, Environment.OSVersion.Version.ToString())
            .SetTag(TelemetryFields.Architecture, environment.Architecture.ToString().ToLowerInvariant())
            .SetTag(TelemetryFields.RuntimeVersion, Environment.Version.ToString())
            .SetTag(TelemetryFields.InstallMethod, InstallMethod())
            .SetTag(TelemetryFields.Ci, environment.CiName ?? "none")
            .SetTag(TelemetryFields.Interactive, environment.IsInputRedirected is false && environment.IsOutputRedirected is false && environment.IsCI is false)
            .SetTag(TelemetryFields.Terminal, environment.Terminal)
            .SetTag(TelemetryFields.CodingAgent, environment.CodingAgent ?? "none");

        return telemetry;
    }

    public void SetTag(string name, object? value)
    {
        Root?.SetTag(name, value);
    }

    public Activity? StartStep(string step)
    {
        return Source.StartActivity(step, ActivityKind.Internal)?.SetTag(TelemetryFields.Step, step);
    }

    public static void CompleteStep(Activity? activity, StepStatus status, IReadOnlyList<string>? codes = null, string? resultCode = null)
    {
        if (activity is null)
            return;

        activity.SetTag(TelemetryFields.StepOutcome, status.ToString().ToLowerInvariant());

        if (codes is { Count: > 0 })
        {
            activity.SetTag(TelemetryFields.ErrorCode, string.Join(',', codes));
        }

        if (resultCode is not null)
        {
            activity.SetTag(TelemetryFields.ResultCode, resultCode);
        }

        if (status is StepStatus.Failed)
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }

        activity.Dispose();
    }

    public void RecordException(Activity? activity, Exception exception)
    {
        activity ??= Root;

        if (activity is null)
            return;

        var problemId = ProblemId(exception);

        activity.SetStatus(ActivityStatusCode.Error);
        activity.SetTag(TelemetryFields.ProblemId, problemId);
        activity.AddException(exception, new TagList
        {
            { TelemetryFields.ExceptionMessage, $"HResult 0x{exception.HResult:X8}" },
            { TelemetryFields.ExceptionStackTrace, StackWithoutMessages(exception) },
            { TelemetryFields.ProblemId, problemId }
        });
    }

    public void Complete(int exitCode)
    {
        if (completed)
            return;

        completed = true;

        if (Root is not null)
        {
            Root.SetTag(TelemetryFields.ExitCode, exitCode);

            if (exitCode != 0)
            {
                Root.SetStatus(ActivityStatusCode.Error);
            }

            Root.Dispose();
        }

        try
        {
            sdk?.Dispose();
        }
        catch (Exception)
        {
        }
    }

    public void Dispose() => Complete(0);

    public static string StackWithoutMessages(Exception exception)
    {
        var builder = new StringBuilder();

        for (var current = exception; current is not null; current = current.InnerException)
        {
            builder.Append(builder.Length == 0 ? "" : "---> ").AppendLine(current.GetType().FullName);

            if (current.StackTrace is { } stackTrace)
            {
                builder.AppendLine(stackTrace);
            }
        }

        return builder.ToString();
    }

    public static string ProblemId(Exception exception)
    {
        var frames = new StackTrace(exception, false).GetFrames();
        var frame = frames.FirstOrDefault(f => f.GetMethod()?.DeclaringType?.FullName?.StartsWith("Bit.", StringComparison.Ordinal) is true)
            ?? frames.FirstOrDefault();
        var method = frame?.GetMethod();

        return method is null
            ? exception.GetType().FullName ?? "unknown"
            : $"{exception.GetType().FullName} at {method.DeclaringType?.FullName}.{method.Name}";
    }

    public static string InstallMethod()
    {
        var directory = AppContext.BaseDirectory.Replace('\\', '/');

        if (directory.Contains("/.store/", StringComparison.OrdinalIgnoreCase))
            return "tool";

        if (directory.Contains("/.nuget/packages/", StringComparison.OrdinalIgnoreCase) || directory.Contains("dnx", StringComparison.OrdinalIgnoreCase))
            return "dnx";

        return "local";
    }

    private static void PrepareProcess(CliEnvironment environment, bool longRunning)
    {
        AppContext.SetData("Azure.Monitor.OpenTelemetry.Exporter.ShutdownDrainBudgetMilliseconds", longRunning || environment.IsCI ? 2000 : 0);

        if (environment.IsCI)
        {
            AppContext.SetSwitch("Azure.Monitor.OpenTelemetry.Exporter.DisablePersistOnShutdown", true);
        }

        Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", "true");

        foreach (var variable in CliEnvironment.InProcessOnlyVariables.Where(v => v is not "APPLICATIONINSIGHTS_STATSBEAT_DISABLED"))
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }
}
