using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Bit.Cli.Keys;
using Bit.Cli.Projects;
using Spectre.Console;

namespace Bit.Cli.Pipelines;

/// <summary>
/// Push notifications and Sign in with Apple, whose secrets only files made at Google and Apple give. bit Boilerplate's CD
/// reads the server's as a secret per setting through toJSON(secrets), which no workflow names, and the Android app's
/// google-services.json from GOOGLE_SERVICES_JSON_FILE_BASE64.
/// </summary>
public sealed partial class PipelineSetup
{
    public const string GoogleServicesJson = "GOOGLE_SERVICES_JSON_FILE_BASE64";
    public const string FirebaseSettings = "SERVERAPI__ADSPUSHFIREBASE__PRIVATEKEY";
    public const string ApnsSettings = "SERVERAPI__ADSPUSHAPNS__P8PRIVATEKEY";
    public const string SignInWithAppleSettings = "SERVERAPI__AUTHENTICATION__APPLE__PRIVATEKEY";

    private const string ServerApiPrefix = "SERVERAPI__";

    /// <summary>The secret that tells whether each one is set, and the option that gives it, in the order bit asks for them.</summary>
    private static readonly Dictionary<string, Func<PipelineOptions, string?>> pushAndSignIn = new(StringComparer.Ordinal)
    {
        [FirebaseSettings] = o => o.FirebaseServiceAccount,
        [GoogleServicesJson] = o => o.GoogleServicesJson,
        [ApnsSettings] = o => o.ApnsKey,
        [SignInWithAppleSettings] = o => o.SignInWithAppleKey
    };

    /// <summary>A Firebase service account key's fields, and the AdsPushFirebase settings they go to.</summary>
    private static readonly (string Field, string Setting)[] firebaseFields = [("project_id", "PROJECTID"), ("private_key_id", "PRIVATEKEYID"), ("private_key", "PRIVATEKEY"), ("client_email", "CLIENTEMAIL"), ("client_id", "CLIENTID"), ("client_x509_cert_url", "CLIENTX509CERTURL")];

    private string? appleTeamId;

    private bool CanAsk => cli.IsInteractive && options.Yes is false;

    /// <summary>A server setting's names: a SERVERAPI__ secret goes to serverweb too when the api runs inside it.</summary>
    private static IEnumerable<string> SameSetting(string name) => name.StartsWith(ServerApiPrefix, StringComparison.Ordinal) ? [name, WebName(name)] : [name];

    private static string WebName(string name) => $"SERVERWEB__{name[ServerApiPrefix.Length..]}";

    /// <summary>
    /// For each of these the project's Server.Api has settings for, the secret that tells whether it's set, in every environment
    /// whose deployment reads a secret per setting.
    /// </summary>
    private IEnumerable<WorkflowSecret> ServerSettingsSecrets(WorkflowScan scan)
    {
        var project = ProjectContext.FindProjectName(root);
        var appSettings = project is null ? "" : Path.Combine(root, "src", "Server", $"{project}.Server.Api", "appsettings.json");
        var text = File.Exists(appSettings) ? File.ReadAllText(appSettings) : "";

        var settings = new List<string>();

        if (text.Contains("\"AdsPushFirebase\"", StringComparison.Ordinal))
            settings.Add(FirebaseSettings);

        if (text.Contains("\"AdsPushAPNS\"", StringComparison.Ordinal))
            settings.Add(ApnsSettings);

        if (AppleSettingsRegex().IsMatch(text))
            settings.Add(SignInWithAppleSettings);

        return scan.SettingsEnvironments.SelectMany(environment => settings.Select(setting => new WorkflowSecret(setting, environment, ["toJSON(secrets)"])));
    }

    private async Task FillPushAndSignInAsync(IReadOnlyList<WorkflowSecret> filling, Func<string?, string, bool> has, CancellationToken cancellationToken)
    {
        var order = pushAndSignIn.Keys.ToList();

        foreach (var secret in filling.OrderBy(s => s.Environment, StringComparer.Ordinal).ThenBy(s => order.IndexOf(s.Name)))
        {
            var environment = secret.Environment;
            // One already stored as SERVERWEB__ is replaced under that name: two names of one setting stop the deployment.
            var server = secret.Name.StartsWith(ServerApiPrefix, StringComparison.Ordinal) && has(environment, WebName(secret.Name)) && has(environment, secret.Name) is false ? "SERVERWEB" : "SERVERAPI";

            switch (secret.Name)
            {
                case FirebaseSettings:
                    await FirebaseAsync(environment, server, cancellationToken);
                    break;
                case GoogleServicesJson:
                    await GoogleServicesAsync(environment, cancellationToken);
                    break;
                case ApnsSettings:
                    await ApnsAsync(environment, server, cancellationToken);
                    break;
                case SignInWithAppleSettings:
                    await SignInWithAppleAsync(environment, server, cancellationToken);
                    break;
            }
        }
    }

    private async Task FirebaseAsync(string? environment, string server, CancellationToken cancellationToken)
    {
        const string what = "Android push's server settings";

        if (FileFor(options.FirebaseServiceAccount, environment, what, "Android push: the Firebase service account key, the .json from Project settings > Service accounts") is not { } path)
            return;

        Dictionary<string, string>? fields = null;

        try
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken));
            var key = document.RootElement;

            if (key.ValueKind is JsonValueKind.Object && firebaseFields.All(f => key.TryGetProperty(f.Field, out var value) && value.ValueKind is JsonValueKind.String))
            {
                fields = firebaseFields.ToDictionary(f => f.Setting, f => key.GetProperty(f.Field).GetString()!);
            }
        }
        catch (JsonException)
        {
        }

        if (fields is null)
        {
            Invalid(what, $"{Path.GetFileName(path)} isn't a Firebase service account key, with {string.Join(", ", firebaseFields.Select(f => f.Field))}");
            return;
        }

        foreach (var (setting, value) in fields)
        {
            // The deployment refuses a line break, so the private key keeps the \n of its JSON, which the server reads.
            await SetAsync(environment, $"{server}__ADSPUSHFIREBASE__{setting}", setting is "PRIVATEKEY" ? value.ReplaceLineEndings("\\n") : value, $"from {Path.GetFileName(path)}", cancellationToken);
        }
    }

    private async Task GoogleServicesAsync(string? environment, CancellationToken cancellationToken)
    {
        if (FileFor(options.GoogleServicesJson, environment, GoogleServicesJson, "Android push: the Android app's google-services.json, from the Firebase project's settings") is not { } path)
            return;

        var text = await File.ReadAllTextAsync(path, cancellationToken);
        List<string>? packages = null;

        try
        {
            using var document = JsonDocument.Parse(text);

            if (document.RootElement.ValueKind is JsonValueKind.Object && document.RootElement.TryGetProperty("client", out var clients) && clients.ValueKind is JsonValueKind.Array)
            {
                packages = [.. clients.EnumerateArray().Select(PackageName).OfType<string>()];
            }
        }
        catch (JsonException)
        {
        }

        if (packages is null)
        {
            Invalid(GoogleServicesJson, $"{Path.GetFileName(path)} isn't a google-services.json, which lists the Firebase project's apps under \"client\"");
            return;
        }

        await SetAsync(environment, GoogleServicesJson, Convert.ToBase64String(await File.ReadAllBytesAsync(path, cancellationToken)), $"{Path.GetFileName(path)} in base64", cancellationToken);

        if (await VariableAsync(environment, "APP_ID", cancellationToken) is { } appId && packages.Contains(appId, StringComparer.Ordinal) is false)
        {
            cli.Console.StepWarning($"{Path.GetFileName(path)} has no Android app for {appId}, the APP_ID of {Scope(environment)}", "add the app to the Firebase project and download the file again, or the app gets no push notifications");
        }
    }

    private async Task ApnsAsync(string? environment, string server, CancellationToken cancellationToken)
    {
        const string what = "iOS push's server settings";

        if (FileFor(options.ApnsKey, environment, what, "iOS push: the APNs key's AuthKey_<key id>.p8") is not { } path || await AppleKeyAsync(path, environment, what, cancellationToken) is not { } key)
            return;

        await SetAsync(environment, $"{server}__ADSPUSHAPNS__P8PRIVATEKEY", key.Body, $"from {Path.GetFileName(path)}", cancellationToken);
        await SetAsync(environment, $"{server}__ADSPUSHAPNS__P8PRIVATEKEYID", key.KeyId, null, cancellationToken);
        await SetAsync(environment, $"{server}__ADSPUSHAPNS__TEAMID", key.TeamId, null, cancellationToken);

        if (await VariableAsync(environment, "APP_ID", cancellationToken) is null)
        {
            cli.Console.StepWarning($"Set the APP_ID variable of {Scope(environment)}", "the deployment sends iOS pushes to that bundle id");
        }
    }

    private async Task SignInWithAppleAsync(string? environment, string server, CancellationToken cancellationToken)
    {
        const string what = "Sign in with Apple's settings";

        if (FileFor(options.SignInWithAppleKey, environment, what, "Sign in with Apple: its key's AuthKey_<key id>.p8") is not { } path)
            return;

        var servicesId = options.AppleServicesId ?? (CanAsk ? cli.Prompter.Text("Its Services ID, such as com.contoso.shop.signin", "").Trim() : "");

        if (servicesId.Length == 0)
        {
            Invalid(what, "Sign in with Apple needs its Services ID too: pass --apple-services-id");
            return;
        }

        if (await AppleKeyAsync(path, environment, what, cancellationToken) is not { } key)
            return;

        await SetAsync(environment, $"{server}__AUTHENTICATION__APPLE__CLIENTID", servicesId, null, cancellationToken);
        await SetAsync(environment, $"{server}__AUTHENTICATION__APPLE__KEYID", key.KeyId, null, cancellationToken);
        await SetAsync(environment, $"{server}__AUTHENTICATION__APPLE__TEAMID", key.TeamId, null, cancellationToken);
        await SetAsync(environment, $"{server}__AUTHENTICATION__APPLE__PRIVATEKEY", key.Body, $"from {Path.GetFileName(path)}", cancellationToken);

        var address = await VariableAsync(environment, "SERVER_API_ADDRESS", cancellationToken) ?? await VariableAsync(environment, "SERVER_ADDRESS", cancellationToken);

        cli.Console.Out.MarkupLine(SignInWithAppleSite.For(address) is { } site
            ? $"    [grey]At {AppleSigning.IdentifiersPage}, give the Services ID {Markup.Escape(servicesId)} the domain[/] {Markup.Escape(site.Domain)} [grey]and the return URL[/] {Markup.Escape(site.ReturnUrl)}[grey].[/]"
            : $"    [grey]Set the SERVER_ADDRESS variable of {Markup.Escape(Scope(environment))}, and give the Services ID {Markup.Escape(servicesId)} its host as a domain and that address with {SignInWithAppleSite.CallbackPath} as a return URL, at {AppleSigning.IdentifiersPage}.[/]");
    }

    /// <summary>The key's body on one line, its id from the file's name and the team's id, or null when one of them is missing.</summary>
    private async Task<(string Body, string KeyId, string TeamId)?> AppleKeyAsync(string path, string? environment, string what, CancellationToken cancellationToken)
    {
        var body = AppleP8Key.Body(await File.ReadAllTextAsync(path, cancellationToken));

        if (AppleP8Key.IsValid(body) is false)
        {
            Invalid(what, $"{Path.GetFileName(path)} isn't an Apple .p8 key");
            return null;
        }

        var keyId = KeyIdRegex().Match(Path.GetFileName(path)) is { Success: true } match ? match.Groups["id"].Value
            : CanAsk ? cli.Prompter.Text("Its key id", "").Trim()
            : "";

        if (keyId.Length == 0)
        {
            Invalid(what, $"bit reads the key id from the file's name: name {Path.GetFileName(path)} AuthKey_<key id>.p8, as Apple did");
            return null;
        }

        if (await TeamIdAsync(environment, cancellationToken) is not { } teamId)
        {
            Invalid(what, $"bit doesn't know the Apple team id: pass --apple-team-id, the one under Membership details at {AppleSigning.AccountPage}");
            return null;
        }

        return (body, keyId, teamId);
    }

    /// <summary>
    /// Given, from the App Store signing bit created in this run, or else asked for.
    /// </summary>
    private async Task<string?> TeamIdAsync(string? environment, CancellationToken cancellationToken)
    {
        appleTeamId ??= options.AppleTeamId ?? appleSignings.Values.Select(s => s.TeamId).OfType<string>().FirstOrDefault();

        if (appleTeamId is null && CanAsk)
        {
            // The app's entry in apple-app-site-association.json starts with its App ID prefix, which is the team id for all
            // but old teams, so it's only offered.
            var offered = ProjectContext.FindProjectName(root) is { } project && await VariableAsync(environment, "APP_ID", cancellationToken) is { } appId
                ? WellKnownFiles.AppIdPrefixOf(WellKnownFiles.AppleAppSiteAssociationPath(root, project), appId)
                : null;

            if (cli.Prompter.Text($"Your Apple team id, under Membership details at {AppleSigning.AccountPage}", offered ?? "").Trim() is { Length: > 0 } answer)
            {
                appleTeamId = answer;
            }
        }

        return appleTeamId;
    }

    /// <summary>The option's file, or else the one the user names. Null skips it.</summary>
    private string? FileFor(string? given, string? environment, string what, string question)
    {
        var answer = (given ?? (CanAsk ? cli.Prompter.Text($"{question}, for {Scope(environment)} (empty skips it)", "") : "")).Trim().Trim('"');

        if (answer.Length == 0)
        {
            cli.Console.Step(StepStatus.Skipped, $"Skipped {what}", Scope(environment));
            return null;
        }

        var path = Path.GetFullPath(answer, cli.Environment.CurrentDirectory);

        if (File.Exists(path))
            return path;

        Invalid(what, $"{path} doesn't exist");
        return null;
    }

    private void Invalid(string what, string reason)
    {
        cli.Console.Step(StepStatus.Failed, $"Couldn't store {what}");
        cli.Console.StepHint(reason);
        failed = true;
    }

    private static string? PackageName(JsonElement client)
    {
        return client.ValueKind is JsonValueKind.Object && client.TryGetProperty("client_info", out var info) && info.ValueKind is JsonValueKind.Object
            && info.TryGetProperty("android_client_info", out var android) && android.ValueKind is JsonValueKind.Object
            && android.TryGetProperty("package_name", out var name) && name.ValueKind is JsonValueKind.String ? name.GetString() : null;
    }

    [GeneratedRegex(@"""Apple""\s*:\s*\{")]
    private static partial Regex AppleSettingsRegex();

    [GeneratedRegex(@"^AuthKey_(?<id>[A-Za-z0-9]{10})\.p8$")]
    private static partial Regex KeyIdRegex();
}
