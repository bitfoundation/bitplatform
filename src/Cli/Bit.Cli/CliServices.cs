using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;

namespace Bit.Cli;

public sealed class CliServices : IDisposable
{
    private readonly bool customPrompter;

    public CliServices(CliEnvironment environment, CliConsole console, IPrompter? prompter = null, IProcessRunner? runner = null, CliLog? log = null)
    {
        Environment = environment;
        Console = console;
        Log = log ?? CliLog.None;
        customPrompter = prompter is not null;
        Prompter = prompter ?? (IsInteractive ? new SpectrePrompter(console.Out) : new NonInteractivePrompter());
        Runner = runner ?? new ProcessRunner(environment, Log);
        Settings = CliSettings.Load(environment);
        Telemetry = CliTelemetry.Disabled(TelemetryDecision.Resolve(environment, Settings, null));
    }

    public CliEnvironment Environment { get; }

    public CliConsole Console { get; }

    public IPrompter Prompter { get; private set; }

    public CliLog Log { get; }

    public IProcessRunner Runner { get; }

    public CliSettings Settings { get; }

    public CliTelemetry Telemetry { get; set; }

    public bool NonInteractive { get; private set; }

    public bool IsInteractive => NonInteractive is false && (customPrompter
        ? Prompter.CanPrompt
        : Environment.IsCI is false && Environment.IsInputRedirected is false && Console.IsInteractive);

    public void DisablePrompts()
    {
        NonInteractive = true;
        Prompter = new NonInteractivePrompter();
    }

    public void Dispose()
    {
        Log.Dispose();
    }
}
