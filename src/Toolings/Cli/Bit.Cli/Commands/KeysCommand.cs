using System.CommandLine;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Keys;
using Bit.Cli.Projects;
using Bit.Cli.Templates;
using Spectre.Console;

namespace Bit.Cli.Commands;

public sealed record AppleKeysRequest(string IssuerId, string KeyId, string PrivateKeyPath, string? BundleId, string? ProfileName, string? Output);

public static partial class KeysCommand
{
    public static Command Create(Func<CliServices> services)
    {
        return new Command("keys", "Create the keys and certificates a bit Boilerplate project signs and encrypts with.")
        {
            Vapid(services),
            Certificate(services),
            Android(services),
            Apple(services)
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
                KeyFolder.WriteSecret(pfx, AppCertificates.ExportPfx(pair, password));
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
            KeyFolder.WriteSecret(path, keystore.Content);

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
                return new ProjectContext { Name = name, Directory = current.FullName, Platforms = new HashSet<Platform>() };
        }

        return null;
    }

    private static Command Apple(Func<CliServices> services)
    {
        var issuerIdOption = new Option<string>("--issuer-id") { Required = true, Description = "The issuer id of the App Store Connect API key, shown in App Store Connect under Users and Access, Integrations." };
        var keyIdOption = new Option<string>("--key-id") { Required = true, Description = "The API key's id, e.g. 2X9R4HXF34." };
        var privateKeyOption = new Option<string>("--private-key") { Required = true, Description = "The AuthKey_<key id>.p8 file App Store Connect gave when the key was created." };
        var bundleIdOption = new Option<string?>("--bundle-id") { Description = "The app's bundle id, e.g. com.contoso.shop. Default: the ApplicationId of the project's Client.Maui, in a bit Boilerplate project's folder." };
        var profileNameOption = new Option<string?>("--profile-name") { Description = "The provisioning profile's name. A profile with this name is replaced. Default: \"<bundle id> App Store\"." };
        var outputOption = new Option<string?>("--output", "-o") { Description = "The folder to write the .p12 and .mobileprovision files to. Default: the current folder." };

        var command = new Command("apple", "Create an Apple Distribution certificate and an App Store provisioning profile with an App Store Connect API key, so the CD signs the iOS app without a Mac.")
        {
            issuerIdOption,
            keyIdOption,
            privateKeyOption,
            bundleIdOption,
            profileNameOption,
            outputOption
        };

        command.SetAction((parseResult, cancellationToken) => AppleAsync(services(), new AppleKeysRequest(
            parseResult.GetValue(issuerIdOption)?.Trim() ?? "",
            parseResult.GetValue(keyIdOption)?.Trim() ?? "",
            parseResult.GetValue(privateKeyOption) ?? "",
            parseResult.GetValue(bundleIdOption)?.Trim(),
            parseResult.GetValue(profileNameOption)?.Trim(),
            parseResult.GetValue(outputOption)), cancellationToken));

        return command;
    }

    public static async Task<int> AppleAsync(CliServices cli, AppleKeysRequest request, CancellationToken cancellationToken, HttpMessageHandler? http = null)
    {
        var directory = cli.Environment.CurrentDirectory;
        var privateKeyPath = Path.GetFullPath(request.PrivateKeyPath, directory);

        if (File.Exists(privateKeyPath) is false)
        {
            return cli.UsageError("private-key-missing", $"No file at {privateKeyPath}. Pass the AuthKey_<key id>.p8 file App Store Connect gave when the key was created.");
        }

        var projectName = Directory.Exists(directory) ? ProjectContext.FindProjectName(directory) : null;
        var bundleId = request.BundleId ?? (projectName is null ? null : ProjectContext.ReadAppId(directory, projectName));

        if (string.IsNullOrEmpty(bundleId))
        {
            return cli.UsageError("bundle-id-missing", projectName is null
                ? "Pass the app's bundle id with --bundle-id, e.g. --bundle-id com.contoso.shop, or run this in a bit Boilerplate project's folder to use its app id."
                : $"{Platforms.MauiProject(projectName)} has no <ApplicationId>. Pass the app's bundle id with --bundle-id.");
        }

        if (BundleIdRegex().IsMatch(bundleId) is false)
        {
            return cli.UsageError("bundle-id-invalid", $"Apple doesn't accept {bundleId} as a bundle id: use only Latin letters, digits, hyphens and dots, as in com.contoso.shop.");
        }

        var folder = Path.GetFullPath(request.Output ?? ".", directory);
        var certificatePath = Path.Combine(folder, $"{bundleId}.p12");
        var profilePath = Path.Combine(folder, $"{bundleId}.mobileprovision");

        if (File.Exists(certificatePath))
        {
            return cli.UsageError("certificate-exists", $"{certificatePath} already exists, and it may be the only copy of a certificate's private key. Move it away first, or pass another folder with -o.");
        }

        if (await KeyFolder.TrackedRepositoryAsync(cli.Runner, certificatePath, cancellationToken) is { } repository)
        {
            return cli.UsageError("key-in-repository", $"The .p12 holds a private key and would be in the git repository {repository}, where a commit takes it along. Pick a folder outside it with -o.");
        }

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return cli.UsageError("output-unwritable", $"Couldn't create the folder {folder}: {exp.Message}");
        }

        var key = new AppleApiKey(request.IssuerId, request.KeyId, await File.ReadAllTextAsync(privateKeyPath, cancellationToken));
        var profileName = string.IsNullOrEmpty(request.ProfileName) ? AppleSigning.DefaultProfileName(bundleId) : request.ProfileName;
        AppleSigningResult result;

        try
        {
            result = await cli.Console.RunWithStatusAsync($"Creating the App Store signing of {bundleId}", _ => new AppleSigning(http).CreateAppStoreSigningAsync(key, bundleId, profileName, cancellationToken));
        }
        catch (AppleSigningException exp)
        {
            cli.Console.Fail(exp.Message, AppleSigning.Attempt(key, bundleId, profileName));
            return CliApp.ExitFailed;
        }

        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        using (var stream = new FileStream(certificatePath, options))
        {
            stream.Write(result.Pkcs12);
        }

        await File.WriteAllBytesAsync(profilePath, result.Profile, CancellationToken.None);

        var console = cli.Console;
        console.Out.MarkupLine($"[green]{console.OkSymbol}[/] Created an Apple Distribution certificate, valid until {result.CertificateExpiry:yyyy-MM-dd}, and the App Store profile [bold]{Markup.Escape(result.ProfileName)}[/] for {Markup.Escape(bundleId)}.");
        console.Out.MarkupLine($"  [grey]Certificate[/]  {Markup.Escape(certificatePath)}");
        console.Out.MarkupLine($"  [grey]Password[/]     {Markup.Escape(result.Pkcs12Password)}");
        console.Out.MarkupLine($"  [grey]Profile[/]      {Markup.Escape(profilePath)}");
        console.Out.MarkupLine($"  [grey]Team id[/]      {Markup.Escape(result.TeamId ?? "unknown")}");
        console.Out.WriteLine();
        console.Dim("The iOS job of the project's CD reads them from these secrets and variables of its GitHub environment:");

        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadLeft(2).PadRight(2)).AddColumn();

        void Row(string name, string value) => grid.AddRow(Markup.Escape(name), Markup.Escape(value));

        Row("APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64", $"secret: the file {Path.GetFileName(certificatePath)} in base64");
        Row("APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD", "secret: the password above");
        Row("APPSTORE_API_KEY_ISSUER_ID", $"secret: {request.IssuerId}");
        Row("APPSTORE_API_KEY_ID", $"secret: {request.KeyId}");
        Row("APPSTORE_API_KEY_PRIVATE_KEY", $"secret: the text of {Path.GetFileName(privateKeyPath)}");
        Row("IOS_CODE_SIGN_PROVISION", $"variable: {result.ProfileName}");
        Row("APP_ID", $"variable: {bundleId}");
        console.Out.Write(grid);

        var base64Command = cli.Environment.Os switch
        {
            HostOs.Windows => $"[Convert]::ToBase64String([IO.File]::ReadAllBytes('{certificatePath.Replace("'", "''", StringComparison.Ordinal)}'))",
            HostOs.MacOS => $"base64 -i {ProcessSpec.ShellQuote(certificatePath, HostOs.MacOS)}",
            _ => $"base64 -w0 {ProcessSpec.ShellQuote(certificatePath, HostOs.Linux)}"
        };

        console.Dim(cli.Environment.Os is HostOs.Windows ? "Get the file in base64 with this PowerShell command:" : "Get the file in base64 with:");
        console.UnwrappedLine($"  {base64Command}");
        console.Dim("The password is shown only this once. The .p12 and .p8 files hold private keys: keep them out of the repository.");
        return CliApp.ExitOk;
    }

    [GeneratedRegex(@"^[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*$")]
    private static partial Regex BundleIdRegex();
}
