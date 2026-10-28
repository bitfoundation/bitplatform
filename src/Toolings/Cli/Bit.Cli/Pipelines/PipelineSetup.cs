using System.Text;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Keys;
using Bit.Cli.Projects;
using Spectre.Console;

namespace Bit.Cli.Pipelines;

public sealed record PipelineOptions(IReadOnlyList<string> Environments, bool Check, bool Yes, bool RotateCertificate);

public sealed partial class PipelineSetup(CliServices cli, PipelineOptions options)
{
    public const string PublicVapidKey = "PUBLIC_VAPIDKEY";
    public const string PrivateVapidKey = "PRIVATE_VAPIDKEY";
    public const string VapidSubject = "VAPID_SUBJECT";
    public const string AppCertificate = "APP_CERTIFICATE";
    public const string RetiredAppCertificate = "APP_CERTIFICATE_RETIRED";
    public const string AndroidKeystoreFile = "ANDROID_RELEASE_KEYSTORE_FILE_BASE64";
    public const string AndroidKeystorePassword = "ANDROID_RELEASE_KEYSTORE_PASSWORD";
    public const string AndroidSigningPassword = "ANDROID_RELEASE_SIGNING_PASSWORD";

    private static readonly string[] createdByBit = [PublicVapidKey, PrivateVapidKey, AppCertificate, AndroidKeystoreFile, AndroidKeystorePassword, AndroidSigningPassword];

    private static readonly string[] optional = [RetiredAppCertificate];

    private static readonly Dictionary<string, string> hints = new(StringComparer.Ordinal)
    {
        [VapidSubject] = "a mailto: address or https URL the browsers' push services can reach you at",
        ["DATABASE_CONNECTION_STRING"] = "the connection string of this environment's database, with the rights to change its schema",
        ["AZURE_PUBLISH_PROFILE"] = "the publish profile of the App Service: the XML its Download publish profile button gives",
        ["API_AZURE_PUBLISH_PROFILE"] = "the publish profile of the API's App Service: the XML its Download publish profile button gives",
        ["OPENAI_APIKEY"] = "an OpenAI compatible API key for bit translate; without it nothing is translated",
        ["CLOUDFLARE_ZONE"] = "the zone id on your domain's Cloudflare overview page",
        ["CLOUDFLARE_TOKEN"] = "a Cloudflare API token that can purge the zone's cache",
        ["APPSTORE_API_KEY_ISSUER_ID"] = "the issuer id on App Store Connect's Users and Access > Integrations page",
        ["APPSTORE_API_KEY_ID"] = "the id of an App Store Connect API key with the App Manager role",
        ["APPSTORE_API_KEY_PRIVATE_KEY"] = "that API key's .p8 file"
    };

    private IProcessRunner Runner => cli.Runner;

    private string root = "";
    private string repository = "";
    private IReadOnlyList<string> workflowTexts = [];
    private AndroidKeystore? keystore;
    private readonly List<string> changedFiles = [];
    private bool failed;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var top = await Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["rev-parse", "--show-toplevel"], WorkingDirectory = cli.Environment.CurrentDirectory, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken);

        if (top.Succeeded is false)
            return cli.UsageError("not-a-repository", "Run bit pipeline inside your project's git repository.");

        root = Path.GetFullPath(top.Output.Trim());

        if (Directory.Exists(Path.Combine(root, ".github", "workflows")) is false)
            return cli.UsageError("no-github-workflows", "This repository has no GitHub Actions workflows. bit pipeline fills the secrets of GitHub Actions; for another pipeline, create the keys with bit keys.");

        if (await SignInAsync(cancellationToken) is false)
            return CliApp.ExitFailed;

        var view = await Gh(["repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner"], cancellationToken);
        repository = view.Output.Trim();

        if (view.Succeeded is false || repository.Length == 0 || repository.Contains(' ', StringComparison.Ordinal))
        {
            cli.Console.Fail("This repository isn't on GitHub, or git's remote doesn't point at it. Push it to GitHub first, e.g. with gh repo create.");
            return CliApp.ExitFailed;
        }

        var scan = await new GitHubWorkflows(root, FetchAsync).ScanAsync(cancellationToken);
        workflowTexts = scan.Texts;

        foreach (var note in scan.Notes)
        {
            cli.Console.Warn(note);
        }

        foreach (var unknown in options.Environments.Where(e => scan.Environments.Contains(e, StringComparer.Ordinal) is false))
        {
            cli.Console.Warn($"No workflow deploys to the environment {unknown}. The workflows deploy to {(scan.Environments.Count == 0 ? "no environment" : string.Join(", ", scan.Environments))}.");
        }

        var wanted = scan.Secrets.Where(s => s.Environment is null || options.Environments.Count == 0 || options.Environments.Contains(s.Environment, StringComparer.Ordinal)).ToList();

        if (wanted.Count == 0)
        {
            cli.Console.Out.MarkupLine("The workflows use no secrets.");
            return CliApp.ExitOk;
        }

        if (await ListSecretsAsync(null, cancellationToken) is not { } repositorySecrets)
            return CliApp.ExitFailed;

        var existing = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var newEnvironments = new List<string>();

        foreach (var environment in wanted.Select(s => s.Environment).OfType<string>().Distinct(StringComparer.Ordinal))
        {
            if ((await Gh(["api", $"repos/{repository}/environments/{Uri.EscapeDataString(environment)}", "--silent"], cancellationToken)).Succeeded)
            {
                if (await ListSecretsAsync(environment, cancellationToken) is not { } secrets)
                    return CliApp.ExitFailed;

                existing[environment] = secrets;
            }
            else
            {
                existing[environment] = [];
                newEnvironments.Add(environment);
            }
        }

        bool IsSet(WorkflowSecret secret) => repositorySecrets.Contains(secret.Name) || (secret.Environment is { } environment && existing[environment].Contains(secret.Name));

        var missing = wanted.Where(s => IsSet(s) is false).ToList();
        Show(wanted, IsSet);

        if (options.Check)
            return missing.Any(s => optional.Contains(s.Name) is false) ? CliApp.ExitCheckFailed : CliApp.ExitOk;

        var rotating = options.RotateCertificate ? wanted.Where(s => s.Name is AppCertificate && IsSet(s)).ToList() : [];

        if (missing.All(s => optional.Contains(s.Name)) && rotating.Count == 0)
        {
            cli.Console.Out.MarkupLine($"Every secret the workflows use is set in {Markup.Escape(repository)}.");
            return CliApp.ExitOk;
        }

        if (options.Yes is false && cli.IsInteractive && cli.Prompter.Confirm($"Store the missing secrets in {repository}?", true) is false)
            return CliApp.ExitOk;

        foreach (var environment in newEnvironments)
        {
            var created = await Gh(["api", "-X", "PUT", $"repos/{repository}/environments/{Uri.EscapeDataString(environment)}", "--silent"], cancellationToken);
            cli.Console.Step(created.Succeeded ? StepStatus.Succeeded : StepStatus.Failed, created.Succeeded ? $"Created the {environment} environment" : $"Couldn't create the {environment} environment", created.Succeeded ? null : LastLine(created));

            if (created.Succeeded is false)
                return CliApp.ExitFailed;
        }

        foreach (var scope in wanted.GroupBy(s => s.Environment))
        {
            var used = scope.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var absent = scope.Where(s => IsSet(s) is false).Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (absent.Count > 0)
            {
                await FillAsync(scope.Key, used, absent, cancellationToken);
            }

            if (rotating.Any(s => s.Environment == scope.Key))
            {
                await RotateAsync(scope.Key, used, cancellationToken);
            }
        }

        if (keystore is not null)
        {
            cli.Console.Out.MarkupLine($"[grey]The Android signing key is kept in[/] {Markup.Escape(Path.Combine(KeyFolder.For(cli.Environment, repository), $"{keystore.Alias}.keystore"))} [grey]with the password[/] {Markup.Escape(keystore.Password)}[grey]. Save both somewhere safe, like a password manager: Google Play only takes updates signed with this key.[/]");
        }

        if (changedFiles.Count > 0)
        {
            cli.Console.Out.MarkupLine($"[grey]Changed in the repository, to commit:[/] {Markup.Escape(string.Join(", ", changedFiles.Distinct(StringComparer.Ordinal).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))))}");
        }

        return failed ? CliApp.ExitFailed : CliApp.ExitOk;
    }

    private async Task FillAsync(string? environment, HashSet<string> used, HashSet<string> absent, CancellationToken cancellationToken)
    {
        if (absent.Contains(PublicVapidKey) || absent.Contains(PrivateVapidKey))
        {
            var keys = VapidKeys.Create();

            foreach (var (name, value) in new[] { (PublicVapidKey, keys.PublicKey), (PrivateVapidKey, keys.PrivateKey) }.Where(k => used.Contains(k.Item1)))
            {
                await SetAsync(environment, name, value, absent.Contains(name) ? "created by bit" : "replaced, to match the new pair", cancellationToken);
            }

            if (used.Contains(PrivateVapidKey) is false)
            {
                cli.Console.Out.MarkupLine($"    [grey]The workflows don't use {PrivateVapidKey}, so put this private key where the server reads AdsPushVapid:PrivateKey:[/] {keys.PrivateKey}");
            }
        }

        if (absent.Contains(AppCertificate))
        {
            var pair = AppCertificates.CreateProduction();
            AppCertificates.Write(BackupFolder(environment), AppCertificates.ActiveName, pair);
            await SetAsync(environment, AppCertificate, pair.Pem, "created by bit", cancellationToken);
        }

        if (absent.Contains(AndroidKeystoreFile))
        {
            keystore ??= CreateKeystore();

            await SetAsync(environment, AndroidKeystoreFile, Convert.ToBase64String(keystore.Content), "created by bit", cancellationToken);

            foreach (var name in new[] { AndroidKeystorePassword, AndroidSigningPassword }.Where(used.Contains))
            {
                await SetAsync(environment, name, keystore.Password, absent.Contains(name) ? "created by bit" : "replaced, to match the new key", cancellationToken);
            }

            await AddFingerprintAsync(environment, keystore, cancellationToken);
            absent.Remove(AndroidKeystorePassword);
            absent.Remove(AndroidSigningPassword);
        }

        foreach (var name in absent.Order(StringComparer.Ordinal))
        {
            if ((createdByBit.Contains(name) && name is not AndroidKeystorePassword and not AndroidSigningPassword) || optional.Contains(name))
                continue;

            if (await AskAsync(environment, name, cancellationToken) is { Length: > 0 } value)
            {
                await SetAsync(environment, name, value, null, cancellationToken);
            }
            else
            {
                cli.Console.Step(StepStatus.Skipped, $"Skipped {name}", Scope(environment));
            }
        }
    }

    private async Task RotateAsync(string? environment, HashSet<string> used, CancellationToken cancellationToken)
    {
        var folder = BackupFolder(environment);
        var current = AppCertificates.Read(folder, AppCertificates.ActiveName);

        if (current is null && cli.IsInteractive && options.Yes is false)
        {
            var path = cli.Prompter.Text($"bit has no copy of {environment ?? "the repository"}'s {AppCertificate}. Its PEM file, so tokens it signed keep working (empty: everyone signs in again)", "");
            current = path.Length > 0 && File.Exists(path) ? AppCertificates.Parse(await File.ReadAllTextAsync(path, cancellationToken)) : null;
        }

        var next = AppCertificates.CreateProduction();

        if (current is not null && used.Contains(RetiredAppCertificate))
        {
            await SetAsync(environment, RetiredAppCertificate, current.Pem, "the certificate it replaces", cancellationToken);
        }
        else
        {
            cli.Console.StepWarning(current is null
                ? $"Tokens signed by {Scope(environment)}'s old app certificate stop working, so everyone signs in again"
                : $"The workflows don't use {RetiredAppCertificate}, so tokens signed by the old app certificate stop working and everyone signs in again");
        }

        if (AppCertificates.Read(folder, AppCertificates.ActiveName) is not null)
        {
            AppCertificates.Retire(folder, DateTimeOffset.UtcNow);
        }

        AppCertificates.Write(folder, AppCertificates.ActiveName, next);
        await SetAsync(environment, AppCertificate, next.Pem, "a new one", cancellationToken);
    }

    private AndroidKeystore CreateKeystore()
    {
        var alias = workflowTexts.Select(t => KeyAliasRegex().Match(t)).FirstOrDefault(m => m.Success)?.Groups["alias"].Value
            ?? ProjectContext.FindProjectName(root)
            ?? "release";

        var created = AndroidKeystore.Create(alias);
        var path = Path.Combine(KeyFolder.For(cli.Environment, repository), $"{alias}.keystore");

        if (File.Exists(path))
        {
            File.Move(path, Path.Combine(Path.GetDirectoryName(path)!, $"{alias}.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.keystore"));
        }

        File.WriteAllBytes(path, created.Content);
        KeyFolder.RestrictToOwner(path);
        return created;
    }

    private async Task AddFingerprintAsync(string? environment, AndroidKeystore created, CancellationToken cancellationToken)
    {
        if (ProjectContext.FindProjectName(root) is not { } name)
            return;

        var packageName = await VariableAsync(environment, "APP_ID", cancellationToken) ?? ProjectContext.ReadAppId(root, name);
        var assetLinks = WellKnownFiles.AssetLinksPath(root, name);

        if (packageName is { Length: > 0 } && WellKnownFiles.AddAndroidFingerprint(assetLinks, packageName, created.Sha256Fingerprint))
        {
            changedFiles.Add(assetLinks);
        }
    }

    private async Task<string?> AskAsync(string? environment, string name, CancellationToken cancellationToken)
    {
        if (cli.IsInteractive is false || options.Yes)
            return null;

        var hint = hints.GetValueOrDefault(name);

        if (name is VapidSubject)
        {
            var email = (await Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["config", "user.email"], WorkingDirectory = root, Timeout = TimeSpan.FromSeconds(30) }, cancellationToken)).Output.Trim();
            var subject = cli.Prompter.Text($"{name} for {Scope(environment)}: {hint}", email.Contains('@', StringComparison.Ordinal) ? $"mailto:{email}" : "");
            return subject.Length == 0 || subject.StartsWith("mailto:", StringComparison.Ordinal) || subject.StartsWith("https://", StringComparison.Ordinal) ? subject : $"mailto:{subject}";
        }

        var answer = cli.Prompter.Secret($"{name} for {Scope(environment)}{(hint is null ? "" : $": {hint}")} (@file reads a file, empty skips it)");

        if (answer.StartsWith('@') && answer.Length > 1)
        {
            var path = Path.GetFullPath(answer[1..].Trim().Trim('"'), cli.Environment.CurrentDirectory);

            if (File.Exists(path) is false)
            {
                cli.Console.Warn($"{path} doesn't exist, so {name} is skipped.");
                return null;
            }

            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        return answer;
    }

    private async Task SetAsync(string? environment, string name, string value, string? detail, CancellationToken cancellationToken)
    {
        string[] arguments = environment is null
            ? ["secret", "set", name, "--repo", repository]
            : ["secret", "set", name, "--repo", repository, "--env", environment];

        var result = await Runner.RunAsync(new ProcessSpec { FileName = "gh", Arguments = arguments, WorkingDirectory = root, StandardInput = value, Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);

        cli.Console.Step(result.Succeeded ? StepStatus.Succeeded : StepStatus.Failed,
            result.Succeeded ? $"Stored {name}" : $"Couldn't store {name}",
            result.Succeeded ? string.Join(", ", new[] { Scope(environment), detail }.OfType<string>()) : LastLine(result));

        failed |= result.Succeeded is false;
    }

    private void Show(IReadOnlyList<WorkflowSecret> wanted, Func<WorkflowSecret, bool> isSet)
    {
        var width = wanted.Max(s => s.Name.Length) + 2;

        foreach (var scope in wanted.GroupBy(s => s.Environment))
        {
            cli.Console.Out.MarkupLine($"[bold]{Markup.Escape(Scope(scope.Key))}[/]");

            foreach (var secret in scope)
            {
                var state = isSet(secret) ? "[green]set[/]"
                    : optional.Contains(secret.Name) ? "[grey]not set, optional[/]"
                    : createdByBit.Contains(secret.Name) ? "[yellow]missing[/] [grey]bit creates it[/]"
                    : "[yellow]missing[/]";

                cli.Console.Out.MarkupLine($"  {Markup.Escape(secret.Name.PadRight(width))}{state}");
            }
        }
    }

    private async Task<bool> SignInAsync(CancellationToken cancellationToken)
    {
        if (Runner.FindExecutable("gh") is null)
        {
            cli.Console.Fail("bit pipeline stores the secrets with the GitHub CLI, which isn't installed. Install it from https://cli.github.com, or with bit doctor --fix in a project created with --github-repo.");
            return false;
        }

        if ((await Gh(["auth", "status"], cancellationToken)).Succeeded)
            return true;

        if (cli.HasTerminal)
        {
            await Runner.RunAsync(new ProcessSpec { FileName = "gh", Arguments = ["auth", "login", "--web", "--git-protocol", "https", "--hostname", "github.com"], WorkingDirectory = root, Interactive = true, Timeout = TimeSpan.FromMinutes(15) }, cancellationToken);

            if ((await Gh(["auth", "status"], cancellationToken)).Succeeded)
                return true;
        }

        cli.Console.Fail("You aren't signed in to GitHub. Run gh auth login --web, then bit pipeline again.");
        return false;
    }

    private async Task<HashSet<string>?> ListSecretsAsync(string? environment, CancellationToken cancellationToken)
    {
        string[] arguments = environment is null
            ? ["secret", "list", "--repo", repository, "--json", "name", "--jq", ".[].name"]
            : ["secret", "list", "--repo", repository, "--env", environment, "--json", "name", "--jq", ".[].name"];

        var result = await Gh(arguments, cancellationToken);

        if (result.Succeeded)
            return result.OutputLines.Select(l => l.Trim()).Where(l => l.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);

        cli.Console.Fail($"Couldn't read the secrets of {Scope(environment)}: {LastLine(result)}. Storing secrets needs admin rights on {repository}.");
        return null;
    }

    private async Task<string?> VariableAsync(string? environment, string name, CancellationToken cancellationToken)
    {
        if (environment is not null && await Gh(["variable", "get", name, "--repo", repository, "--env", environment], cancellationToken) is { Succeeded: true } scoped && scoped.Output.Trim().Length > 0)
            return scoped.Output.Trim();

        return await Gh(["variable", "get", name, "--repo", repository], cancellationToken) is { Succeeded: true } shared && shared.Output.Trim().Length > 0 ? shared.Output.Trim() : null;
    }

    private async Task<string?> FetchAsync(string uses, CancellationToken cancellationToken)
    {
        var at = uses.LastIndexOf('@');
        var parts = uses[..at].Split('/', 3);

        if (parts.Length < 3)
            return null;

        var result = await Gh(["api", $"repos/{parts[0]}/{parts[1]}/contents/{parts[2]}?ref={Uri.EscapeDataString(uses[(at + 1)..])}", "--jq", ".content"], cancellationToken);

        try
        {
            return result.Succeeded ? Encoding.UTF8.GetString(Convert.FromBase64String(string.Concat(result.Output.Where(c => char.IsWhiteSpace(c) is false)))) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string BackupFolder(string? environment) => KeyFolder.For(cli.Environment, $"{repository}/{environment ?? "repository"}");

    private static string Scope(string? environment) => environment ?? "the repository";

    private static string LastLine(ProcessResult result) => result.OutputLines.LastOrDefault()?.Trim() ?? $"exit code {result.ExitCode}";

    private Task<ProcessResult> Gh(string[] arguments, CancellationToken cancellationToken)
    {
        return Runner.RunAsync(new ProcessSpec { FileName = "gh", Arguments = arguments, WorkingDirectory = root, Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);
    }

    [GeneratedRegex(@"AndroidSigningKeyAlias=[""']?(?<alias>[A-Za-z0-9._-]+)")]
    private static partial Regex KeyAliasRegex();
}
