using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Keys;
using Bit.Cli.Projects;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class KeysCommand
{
    public static Command Create(Func<CliServices> services)
    {
        return new Command("keys", "Create the keys and certificates a bit Boilerplate project signs and encrypts with.")
        {
            Vapid(services),
            Certificate(services),
            Android(services)
        };
    }

    private static Command Vapid(Func<CliServices> services)
    {
        var command = new Command("vapid", "Create a VAPID key pair for web push notifications.");

        command.SetAction(_ =>
        {
            var cli = services();
            var keys = VapidKeys.Create();

            cli.Console.Out.MarkupLine($"  [grey]Public key[/]   {keys.PublicKey}");
            cli.Console.Out.MarkupLine($"  [grey]Private key[/]  {keys.PrivateKey}");
            cli.Console.Out.MarkupLine("[grey]The public key goes in AdsPushVapid:PublicKey of Client.Web, the private key in AdsPushVapid:PrivateKey of the server. Keep the private key out of the repository.[/]");
            return CliApp.ExitOk;
        });

        return command;
    }

    private static Command Certificate(Func<CliServices> services)
    {
        var outputOption = new Option<string?>("--output", "-o") { Description = "The folder for the certificate. Default: Server.Api's folder for a development one, the current folder otherwise." };
        var productionOption = new Option<bool>("--production") { Description = "Create one for a deployed app (OU=Production, valid for a year) rather than for development." };
        var rotateOption = new Option<bool>("--rotate") { Description = "Keep the current AppCertificate as a retired one, which still validates what it signed, and create a new one next to it." };
        var pfxOption = new Option<bool>("--pfx") { Description = "Also write AppCertificate.pfx, protected by a password bit creates." };
        var listOption = new Option<bool>("--list") { Description = "List the certificates in the folder: which one is active, their key ids and when they expire." };

        var command = new Command("certificate", "Create, rotate or list the AppCertificate the server signs tokens and encrypts its Data Protection keys with.")
        {
            outputOption,
            productionOption,
            rotateOption,
            pfxOption,
            listOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var production = parseResult.GetValue(productionOption);
            var project = FindProject(cli.Environment.CurrentDirectory);
            var folder = parseResult.GetValue(outputOption) is { } output
                ? Path.GetFullPath(output, cli.Environment.CurrentDirectory)
                : production is false && project is { } found ? Path.Combine(found.Directory, "src", "Server", $"{found.Name}.Server.Api") : cli.Environment.CurrentDirectory;

            if (parseResult.GetValue(listOption))
                return List(cli, folder);

            var existing = AppCertificates.Read(folder, AppCertificates.ActiveName);
            var rotate = parseResult.GetValue(rotateOption);

            if (existing is not null && rotate is false)
                return cli.UsageError("certificate-exists", $"{Path.Combine(folder, AppCertificates.ActiveName)}.crt already exists. --rotate keeps it as a retired certificate and creates a new one.");

            if (rotate && existing is null)
                return cli.UsageError("certificate-missing", $"There's no AppCertificate.crt and AppCertificate.key in {folder} to rotate.");

            var forDeployment = production || (existing is not null && AppCertificates.Subject(existing).Contains("OU=Development", StringComparison.Ordinal) is false);

            if (forDeployment && await KeyFolder.TrackedRepositoryAsync(cli.Runner, Path.Combine(folder, $"{AppCertificates.ActiveName}.key"), cancellationToken) is { } repository)
                return cli.UsageError("key-in-repository", $"The private key would be in the git repository {repository}, where a commit takes it along. Pick a folder outside it with -o, or let bit pipeline keep it in a GitHub secret.");

            Directory.CreateDirectory(folder);

            var pair = forDeployment ? AppCertificates.CreateProduction() : AppCertificates.CreateDevelopment();

            if (rotate)
            {
                var retired = AppCertificates.Retire(folder, DateTimeOffset.UtcNow);
                cli.Console.Step(StepStatus.Succeeded, $"Retired the current certificate as {retired}", "it still validates the tokens it signed and decrypts the keys it protected");
            }

            AppCertificates.Write(folder, AppCertificates.ActiveName, pair);
            cli.Console.Step(StepStatus.Succeeded, $"Created {AppCertificates.ActiveName}.crt and .key", $"{AppCertificates.Subject(pair)}, in {folder}");

            if (parseResult.GetValue(pfxOption))
            {
                var password = KeyFolder.NewPassword();
                var pfx = Path.Combine(folder, $"{AppCertificates.ActiveName}.pfx");
                await File.WriteAllBytesAsync(pfx, AppCertificates.ExportPfx(pair, password), cancellationToken);
                KeyFolder.RestrictToOwner(pfx);
                cli.Console.Step(StepStatus.Succeeded, $"Created {AppCertificates.ActiveName}.pfx", $"password {password}");
            }

            if (forDeployment)
            {
                cli.Console.Out.MarkupLine("[grey]Copy the files next to the published server, the same set on every instance, and keep them out of the repository.[/]");
            }

            return CliApp.ExitOk;
        });

        return command;
    }

    private static Command Android(Func<CliServices> services)
    {
        var outputOption = new Option<string?>("--output", "-o") { Description = "The keystore file to create. Default: ~/.bitplatform/keys/<project>/<alias>.keystore." };
        var aliasOption = new Option<string?>("--alias") { Description = "The key's alias. Default: the project's name." };

        var command = new Command("android", "Create the keystore that signs the Android app for Google Play, and add its fingerprint to the project's assetlinks.json.")
        {
            outputOption,
            aliasOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var project = FindProject(cli.Environment.CurrentDirectory);
            var alias = parseResult.GetValue(aliasOption) ?? project?.Name ?? "release";
            var path = parseResult.GetValue(outputOption) is { } output
                ? Path.GetFullPath(output, cli.Environment.CurrentDirectory)
                : Path.Combine(KeyFolder.For(cli.Environment, project?.Name ?? alias), $"{alias}.keystore");

            if (File.Exists(path))
                return cli.UsageError("keystore-exists", $"{path} already exists. Google Play only takes updates signed with the key you started with, so bit doesn't replace it.");

            if (await KeyFolder.TrackedRepositoryAsync(cli.Runner, path, cancellationToken) is { } repository)
                return cli.UsageError("key-in-repository", $"The keystore would be in the git repository {repository}, where a commit takes it along. Pick a file outside it with -o.");

            var keystore = AndroidKeystore.Create(alias);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, keystore.Content, cancellationToken);
            KeyFolder.RestrictToOwner(path);

            cli.Console.Step(StepStatus.Succeeded, "Created the Android signing key", path);
            cli.Console.Out.MarkupLine($"  [grey]Alias[/]     {Markup.Escape(keystore.Alias)}");
            cli.Console.Out.MarkupLine($"  [grey]Password[/]  {Markup.Escape(keystore.Password)}  [grey](the keystore's and the key's)[/]");
            cli.Console.Out.MarkupLine($"  [grey]SHA-256[/]   {keystore.Sha256Fingerprint}");

            if (project is not null && ProjectContext.ReadAppId(project.Directory, project.Name) is { Length: > 0 } appId
                && WellKnownFiles.AddAndroidFingerprint(WellKnownFiles.AssetLinksPath(project.Directory, project.Name), appId, keystore.Sha256Fingerprint))
            {
                cli.Console.Step(StepStatus.Succeeded, "Added its fingerprint to assetlinks.json", appId);
            }

            cli.Console.Out.MarkupLine("[grey]Save the file and its password somewhere safe, like a password manager: Google Play only takes updates signed with this key.[/]");
            return CliApp.ExitOk;
        });

        return command;
    }

    private static int List(CliServices cli, string folder)
    {
        var certificates = Directory.Exists(folder) ? AppCertificates.List(folder) : [];

        if (certificates.Count == 0)
            return cli.UsageError("certificate-missing", $"There's no AppCertificate in {folder}.");

        foreach (var certificate in certificates)
        {
            var expiry = certificate.NotAfter == default ? "" : certificate.NotAfter < DateTimeOffset.UtcNow ? $"[red]expired {certificate.NotAfter:yyyy-MM-dd}[/]" : $"expires {certificate.NotAfter:yyyy-MM-dd}";
            cli.Console.Out.MarkupLine($"  [bold]{Markup.Escape(certificate.Name)}[/]  {(certificate.Active ? "active" : "[grey]retired[/]")}  {Markup.Escape(certificate.Subject)}  [grey]kid[/] {certificate.Thumbprint}  {expiry}");

            if (certificate.Problem is not null)
            {
                cli.Console.Warn(certificate.Problem);
            }
        }

        return certificates.Any(c => c.Problem is not null) ? CliApp.ExitCheckFailed : CliApp.ExitOk;
    }

    private static ProjectContext? FindProject(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            if (current.Exists && ProjectContext.FindProjectName(current.FullName) is { } name)
                return new ProjectContext { Name = name, Directory = current.FullName, Platforms = new HashSet<Templates.Platform>() };
        }

        return null;
    }
}
