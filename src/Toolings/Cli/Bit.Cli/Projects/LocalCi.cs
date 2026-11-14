using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tools;
using Spectre.Console;

namespace Bit.Cli.Projects;

/// <summary>Where act reaches the container engine from, and where the engine's own socket is for the job's container to mount.</summary>
public sealed record ContainerEngine(string Executable, string? DockerHost, string? DaemonSocket);

/// <summary>
/// Runs the project's .github/workflows/ci.yml on this machine with act, in a container on Docker or Podman. The container
/// starts from an image built from the project's .github/act/Dockerfile, since GitHub's hosted runner comes with a JDK and
/// the Android SDK that act's own images lack.
/// </summary>
public sealed partial class LocalCi(CliServices cli, string directory)
{
    private const string WorkflowPath = ".github/workflows/ci.yml";

    public string Workflow => ProjectContext.CiWorkflow(directory);

    public string Dockerfile => Path.Combine(directory, ".github", "act", "Dockerfile");

    /// <summary>
    /// The image of a runner label, such as ubuntu-24.04: the Dockerfile at that Ubuntu version, tagged with it and the
    /// Dockerfile's hash, so a change to either builds a new image and every project with the same ones shares it.
    /// </summary>
    public static string ImageFor(string dockerfile, string label)
        => $"localhost/bit-ci:{UbuntuVersion(label)}-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dockerfile.ReplaceLineEndings("\n"))))[..12]}";

    private static string UbuntuVersion(string label) => label["ubuntu-".Length..];

    /// <summary>The runner labels of the workflow's jobs that run on Ubuntu, which act runs in the image.</summary>
    public static IReadOnlyList<string> UbuntuLabels(string workflow)
    {
        var labels = RunsOnRegex().Matches(workflow).Select(m => m.Groups["label"].Value).Where(l => l.StartsWith("ubuntu-", StringComparison.Ordinal)).Distinct().ToList();
        return labels.Count == 0 ? ["ubuntu-latest"] : labels;
    }

    [GeneratedRegex("""^\s*runs-on:\s*['"]?(?<label>[\w.-]+)['"]?\s*(#.*)?$""", RegexOptions.Multiline)]
    private static partial Regex RunsOnRegex();

    public async Task<int> RunAsync(IReadOnlyList<string> actArguments, CancellationToken cancellationToken)
    {
        var steps = new StepRunner(cli);
        string? act = null;
        ContainerEngine? engine = null;
        var dockerfile = await File.ReadAllTextAsync(Dockerfile, cancellationToken);
        var images = UbuntuLabels(await File.ReadAllTextAsync(Workflow, cancellationToken)).ToDictionary(label => label, label => ImageFor(dockerfile, label));

        await steps.RunAsync("act", "Looking for act", (_, _) =>
        {
            act = FindAct();
            return Task.FromResult(act is null
                ? StepResult.Failed("act isn't installed", "bit ci runs the workflow with it", followUp: "bit setup --tools act installs it, or see https://nektosact.com/installation/")
                : StepResult.Succeeded("Found act", act));
        }, cancellationToken);

        if (steps.AnyFailed is false)
        {
            await steps.RunAsync("container-engine", "Checking Docker or Podman", async (_, token) =>
            {
                (engine, var problem) = await FindEngineAsync(token);
                return engine is null ? StepResult.Failed("act can't reach Docker or Podman", problem, followUp: "bit doctor checks it and offers to fix it") : StepResult.Succeeded("Docker or Podman answers", engine.DockerHost);
            }, cancellationToken);
        }

        if (steps.AnyFailed is false)
        {
            await steps.RunAsync("ci-image", "Building the CI container's image, once per change to .github/act/Dockerfile", (progress, token) => ImagesAsync(engine!, images, progress, token), cancellationToken);
        }

        if (steps.AnyFailed is false)
        {
            await steps.RunAsync("ci", $"Running {WorkflowPath} with act", async (_, token) =>
            {
                var result = await cli.Runner.RunAsync(new ProcessSpec
                {
                    FileName = act!,
                    Arguments = ActArguments(engine!, images, actArguments),
                    WorkingDirectory = directory,
                    Environment = engine!.DockerHost is null ? null : new Dictionary<string, string?> { ["DOCKER_HOST"] = engine.DockerHost },
                    Interactive = true
                }, token);

                return result.Succeeded
                    ? StepResult.Succeeded("CI passed")
                    : StepResult.Failed("CI failed", result.NotFound ? "act couldn't start" : $"act exited with {result.ExitCode}", followUp: "act's log above says which step failed and why.");
            }, cancellationToken, needsTerminal: true);
        }

        foreach (var followUp in steps.Reports.Select(r => r.Result.FollowUp).OfType<string>())
        {
            cli.Console.Line($"    {Markup.Escape(followUp)}");
        }

        return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
    }

    public IReadOnlyList<string> ActArguments(ContainerEngine engine, IReadOnlyDictionary<string, string> images, IReadOnlyList<string> extra)
    {
        return
        [
            "workflow_dispatch",
            "--workflows", WorkflowPath,
            .. images.SelectMany(image => new[] { "--platform", $"{image.Key}={image.Value}" }),
            // As GitHub's runners are: the JDK's folder and the Android SDK's Linux tools are x64 ones.
            "--container-architecture", "linux/amd64",
            // The images are built here, and the actions are fetched once: neither is pulled again on every run.
            "--pull=false",
            "--action-offline-mode",
            .. (engine.DaemonSocket is null ? [] : new[] { "--container-daemon-socket", engine.DaemonSocket }),
            .. extra
        ];
    }

    public async Task<(ContainerEngine? Engine, string? Problem)> FindEngineAsync(CancellationToken cancellationToken)
    {
        var runtime = await ToolCatalog.ContainerRuntimeAsync(cli.Runner, cli.Environment, cancellationToken);

        // Docker Desktop, Colima and OrbStack run the engine in a VM, where its socket is /var/run/docker.sock.
        if (runtime is "docker")
            return (new ContainerEngine("docker", null, cli.Environment.IsLinux ? null : "/var/run/docker.sock"), null);

        if (runtime is not "podman" || ToolCatalog.FindPodman(cli.Runner, cli.Environment) is not { } podman)
            return (null, "Neither Docker nor Podman answers.");

        // Where Podman's service listens, in its machine on Windows and macOS: act mounts it into the job's container, where
        // the tests start their own containers.
        var info = await cli.Runner.RunAsync(new ProcessSpec { FileName = podman, Arguments = ["info", "--format", "{{.Host.RemoteSocket.Path}}"], Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);
        var socket = info.Succeeded ? info.OutputLines.FirstOrDefault()?.Trim() : null;
        var socketPath = socket?.StartsWith("unix://", StringComparison.Ordinal) is true ? socket["unix://".Length..] : null;

        if (cli.Environment.IsWindows)
        {
            return await ToolCatalog.PodmanPipeAsync(cli.Runner, podman, cancellationToken) is { } pipe && cli.Environment.NamedPipeExists(pipe)
                ? (new ContainerEngine(podman, "npipe://" + pipe.Replace('\\', '/'), socketPath), null)
                : (null, "Podman's Docker API is down, which act connects to. Restarting Podman brings it back (podman machine stop, then podman machine start), and stops its running containers.");
        }

        if (cli.Environment.IsMacOS)
        {
            var inspect = await cli.Runner.RunAsync(new ProcessSpec { FileName = podman, Arguments = ["machine", "inspect", "--format", "{{.ConnectionInfo.PodmanSocket.Path}}"], Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);
            return inspect.Succeeded && inspect.OutputLines.FirstOrDefault()?.Trim() is { Length: > 0 } hostSocket
                ? (new ContainerEngine(podman, "unix://" + hostSocket, socketPath), null)
                : (null, "Podman's machine has no socket for act to connect to.");
        }

        // Podman runs here itself, and its service is the same socket act connects to.
        return socket is null
            ? (null, "Podman's service doesn't answer. systemctl --user enable --now podman.socket starts it.")
            : (new ContainerEngine(podman, socket, socketPath), null);
    }

    private async Task<StepResult> ImagesAsync(ContainerEngine engine, IReadOnlyDictionary<string, string> images, Action<string> progress, CancellationToken cancellationToken)
    {
        var built = false;

        foreach (var (label, image) in images)
        {
            if ((await cli.Runner.RunAsync(new ProcessSpec { FileName = engine.Executable, Arguments = ["image", "inspect", image], Timeout = TimeSpan.FromSeconds(30) }, cancellationToken)).Succeeded)
                continue;

            var build = await cli.Runner.RunAsync(new ProcessSpec
            {
                FileName = engine.Executable,
                Arguments = ["build", "--platform", "linux/amd64", "--build-arg", $"UBUNTU_VERSION={UbuntuVersion(label)}", "--tag", image, "--file", Dockerfile, Path.GetDirectoryName(Dockerfile)!],
                Timeout = TimeSpan.FromMinutes(60),
                OnOutputLine = progress
            }, cancellationToken);

            if (build.Succeeded is false)
                return StepResult.FromProcess(build, "Built the CI container's image", "Couldn't build the CI container's image", followUp: null);

            built = true;
        }

        return StepResult.Succeeded(built ? "Built the CI container's image" : "The CI container's image is ready", string.Join(", ", images.Values));
    }

    private string? FindAct()
    {
        if (cli.Runner.FindExecutable("act") is { } act)
            return act;

        // winget puts act's link here, which a terminal opened before it was installed doesn't have on its PATH.
        var linked = Path.Combine(cli.Environment.GetVariable("LOCALAPPDATA") ?? Path.Combine(cli.Environment.HomeDirectory, "AppData", "Local"), "Microsoft", "WinGet", "Links", "act.exe");
        return cli.Environment.IsWindows && File.Exists(linked) ? linked : null;
    }
}
