using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Telemetry;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class AboutCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var command = new Command("about", "Show where this build of bit comes from, how to verify it, and what a bug report needs.");

        command.SetAction(async (_, cancellationToken) =>
        {
            await WriteAsync(services(), cancellationToken);
            return CliApp.ExitOk;
        });

        return command;
    }

    public static async Task WriteAsync(CliServices cli, CancellationToken cancellationToken, HttpMessageHandler? http = null)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadRight(2)).AddColumn();

        void Row(string name, string value) => grid.AddRow($"[grey]{Markup.Escape(name)}[/]", Markup.Escape(value));

        Row("Version", BuildInfo.Commit is null ? BuildInfo.Version : $"{BuildInfo.Version} (commit {BuildInfo.ShortCommit})");
        Row("Source", $"{BuildInfo.SourceUrl}  (MIT license)");
        Row("Built by", BuildInfo.RunUrl is { } runUrl ? $"GitHub Actions {runUrl}" : "a local build, not GitHub Actions");
        Row("Package", BuildInfo.PackageUrl);

        var assemblyPath = typeof(BuildInfo).Assembly.Location;

        if (OperatingSystem.IsWindows())
        {
            var signature = Provenance.WindowsSignature(assemblyPath);
            Row("Signature", signature.State switch
            {
                SignatureState.Valid => $"bit.dll is signed by {signature.Signer}, and Windows checked the signature",
                SignatureState.Unsigned => BuildInfo.IsOfficialBuild ? "bit.dll isn't signed" : "bit.dll isn't signed, as a local build",
                _ => "bit.dll's signature doesn't check out, so the file was changed after it was signed"
            });
        }

        if (BuildInfo.IsOfficialBuild)
        {
            using var handler = http is null ? new HttpClientHandler() : null;
            Row("Attestation", await Provenance.FindAttestationAsync(http ?? handler!, assemblyPath, cancellationToken) switch
            {
                AttestationState.Found => "GitHub has a build attestation for this exact bit.dll in bitfoundation/bitplatform",
                AttestationState.Missing => "GitHub has no attestation for this bit.dll, so it isn't the file a release built",
                _ => "couldn't reach GitHub to look for this bit.dll's attestation"
            });
            Row("Verify", $"gh attestation verify {ProcessSpec.Quote(assemblyPath)} --repo bitfoundation/bitplatform");
        }

        Row("Runs on", $".NET {Environment.Version} on {OsName(cli.Environment.Os)} {Environment.OSVersion.Version} ({cli.Environment.Architecture.ToString().ToLowerInvariant()})");
        Row("Installed as", CliTelemetry.InstallMethod() switch { "tool" => ".NET global tool", "dnx" => "dnx (not installed)", _ => "a local build" });

        var decision = TelemetryDecision.Resolve(cli.Environment, cli.Settings, BuildInfo.TelemetryConnectionString);
        Row("Telemetry", $"{TelemetryDecision.Name(decision.Level)} ({decision.Reason}){(decision.CanSend ? "" : ", this build has no telemetry endpoint")}");
        Row("Logs", cli.Environment.LogsDirectory);

        cli.Console.Out.Write(new Panel(grid).Header("[bold]bit CLI[/]").Border(BoxBorder.Rounded).BorderStyle(new Style(Color.Grey)).Padding(1, 0, 1, 0));
        cli.Console.Out.MarkupLine("[grey]Everything bit installs or changes is listed before it runs, and each step prints the command it uses.[/]");
    }

    public static string OsName(HostOs os) => os switch
    {
        HostOs.Windows => "Windows",
        HostOs.MacOS => "macOS",
        _ => "Linux"
    };
}
