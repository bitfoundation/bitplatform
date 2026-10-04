using System.Text;
using Bit.Butil;
using Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates04CreateProjectPage
{
    [AutoInject] private Clipboard clipboard = default!;

    private string name = "MyFirstProject";

    private readonly List<AssistantMessage> assistantMessages = [];
    private string? assistantInput;
    private string? assistantSummary;
    private string? lastAssistantReply;
    private bool isAssistantBusy;
    private string? copiedCommand;
    private Parameter<bool> realProject = new() { Value = false, Default = false };
    private readonly List<Action> realProjectUndo = [];

    private string CopyButtonText => copiedCommand == GetFinalCommand().Trim() ? "Copied" : "Copy";

    private bool android;
    private bool ios;
    private bool macOS;
    private bool windows;

    private Parameter<bool> cloudflare = new() { Value = true, Default = true };
    private Parameter<bool> sample = new() { Value = false, Default = false };
    private Parameter<bool> sentry = new() { Value = false, Default = false };
    private Parameter<bool> offlineDb = new() { Value = false, Default = false };
    private Parameter<bool> notification = new() { Value = true, Default = true };
    private Parameter<bool> appInsight = new() { Value = false, Default = false };
    private Parameter<bool> signalR = new() { Value = false, Default = false };
    private Parameter<bool> googleAds = new() { Value = false, Default = false };
    private Parameter<bool> redis = new() { Value = false, Default = false };
    private Parameter<bool> aspire = new() { Value = true, Default = true };
    private Parameter<bool> multiTenant = new() { Value = true, Default = true };
    private Parameter<bool> brouter = new() { Value = false, Default = false };

    private Parameter<string> captcha = new()
    {
        Value = "None",
        Default = "None",
        Items =
        [
            new() { Text = "None", Value = "None" },
            new() { Text = "reCaptcha", Value = "reCaptcha" },
        ]
    };

    private Parameter<string> pipeline = new()
    {
        Value = "GitHub",
        Default = "GitHub",
        Items =
        [
            new() { Text = "None", Value = "None" },
            new() { Text = "GitHub", Value = "GitHub" },
            new() { Text = "Azure", Value = "Azure" },
        ]
    };

    private Parameter<string> module = new()
    {
        Value = "None",
        Default = "None",
        Items =
        [
            new() { Text = "None", Value = "None" },
            new() { Text = "Admin", Value = "Admin" },
            new() { Text = "Sales", Value = "Sales" },
        ]
    };

    private Parameter<string> database = new()
    {
        Value = "Sqlite",
        Default = "Sqlite",
        Items =
        [
            new() { Text = "SQLite", Value = "Sqlite" },
            new() { Text = "SqlServer", Value = "SqlServer" },
            new() { Text = "PostgreSQL", Value = "PostgreSQL" },
            new() { Text = "MySQL", Value = "MySql" },
            new() { Text = "Other", Value = "Other" },
        ]
    };

    private Parameter<string> fileStorage = new()
    {
        Value = "Local",
        Default = "Local",
        Items =
        [
            new() { Text = "Local", Value = "Local" },
            new() { Text = "AzureBlobStorage", Value = "AzureBlobStorage" },
            new() { Text = "S3", Value = "S3" },
            new() { Text = "Other", Value = "Other" },
        ]
    };

    private Parameter<string> api = new()
    {
        Value = "Integrated",
        Default = "Integrated",
        Items =
        [
            new() { Text = "Integrated", Value = "Integrated" },
            new() { Text = "Standalone", Value = "Standalone" },
        ]
    };

    private Parameter<string> ide = new()
    {
        Value = "code",
        Default = "code",
        Items =
        [
            new() { Text = "VS Code", Value = "code" },
            new() { Text = "Visual Studio", Value = "vs" },
            new() { Text = "Rider", Value = "rider" },
            new() { Text = "Don't open it", Value = "none" },
        ]
    };

    private Parameter<string> theme = new()
    {
        Value = "Fluent2",
        Default = "Fluent2",
        Items =
        [
            new() { Text = "Fluent2", Value = "Fluent2" },
            new() { Text = "Fluent", Value = "Fluent" },
            new() { Text = "Cupertino", Value = "Cupertino" },
            new() { Text = "Material", Value = "Material" },
        ]
    };

    private string GetFinalCommand()
    {
        StringBuilder finalCommand = new(GetNameCommand());

        if (captcha.IsModified)
        {
            finalCommand.Append(GetCaptchaCommand());
        }

        if (pipeline.IsModified)
        {
            finalCommand.Append(GetPipelineCommand());
        }

        if (sample.IsModified)
        {
            finalCommand.Append(GetSampleCommand());
        }

        if (module.IsModified)
        {
            finalCommand.Append(GetModuleCommand());
        }

        if (cloudflare.IsModified)
        {
            finalCommand.Append(GetCloudflareCommand());
        }

        if (sentry.IsModified)
        {
            finalCommand.Append(GetSentryCommand());
        }

        if (appInsight.IsModified)
        {
            finalCommand.Append(GetAppInsightsCommand());
        }

        if (signalR.IsModified)
        {
            finalCommand.Append(GetSignalRCommand());
        }

        if (googleAds.IsModified)
        {
            finalCommand.Append(GetGoogleAdsCommand());
        }

        if (redis.IsModified)
        {
            finalCommand.Append(GetRedisCommand());
        }

        if (aspire.IsModified)
        {
            finalCommand.Append(GetAspireCommand());
        }

        if (fileStorage.IsModified)
        {
            finalCommand.Append(GetFileStorageCommand());
        }

        if (offlineDb.IsModified)
        {
            finalCommand.Append(GetOfflineDbCommand());
        }

        if (database.IsModified)
        {
            finalCommand.Append(GetDatabaseCommand());
        }

        if (notification.IsModified)
        {
            finalCommand.Append(GetNotificationCommand());
        }

        if (api.IsModified)
        {
            finalCommand.Append(GetApiCommand());
        }

        if (multiTenant.IsModified)
        {
            finalCommand.Append(GetMultiTenantCommand());
        }

        if (brouter.IsModified)
        {
            finalCommand.Append(GetBrouterCommand());
        }

        if (theme.IsModified)
        {
            finalCommand.Append(GetThemeCommand());
        }

        if (realProject.IsModified)
        {
            finalCommand.Append(GetRealProjectCommand());
        }

        if (android || ios || macOS || windows)
        {
            finalCommand.Append(GetPlatformsCommand());
        }

        if (ide.IsModified)
        {
            finalCommand.Append(GetIdeCommand());
        }

        finalCommand.Append("--yes");

        return finalCommand.ToString();
    }

    private string GetNameCommand()
    {
        return $"bit new {name} ";
    }

    private string GetIdeCommand()
    {
        return $"--ide {ide.Value} ";
    }

    private async Task SendToAssistant()
    {
        var message = assistantInput?.Trim();

        if (isAssistantBusy || string.IsNullOrEmpty(message))
            return;

        AssistantMessage userMessage = new(true, message);
        assistantMessages.Add(userMessage);
        assistantInput = null;
        isAssistantBusy = true;

        try
        {
            var request = new ProjectAssistantRequest { Message = message, Summary = assistantSummary, LastReply = lastAssistantReply, Options = CurrentOptions() };
            var response = await HttpClient.PostAsJsonAsync("api/ProjectAssistant/Chat", request, AppJsonContext.Default.ProjectAssistantRequest);

            if (await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ProjectAssistantReply) is not { } reply)
                return;

            assistantSummary = reply.Summary;
            lastAssistantReply = reply.Reply;
            assistantMessages.Add(new(false, reply.Reply));
            Apply(reply.Options);
        }
        catch
        {
            assistantMessages.RemoveAt(assistantMessages.LastIndexOf(userMessage));
            assistantInput = message;
            throw;
        }
        finally
        {
            isAssistantBusy = false;
        }
    }

    private async Task CopyCommand()
    {
        var command = GetFinalCommand().Trim();

        await clipboard.WriteText(command);

        if (command == copiedCommand)
            return;

        copiedCommand = command;
        _ = RecordCreatedProject(command);
    }

    private async Task RecordCreatedProject(string command)
    {
        try
        {
            await HttpClient.PostAsJsonAsync("api/ProjectAssistant/Created", new ProjectCreatedDto { Command = command, Summary = assistantSummary }, AppJsonContext.Default.ProjectCreatedDto);
        }
        catch (KnownException)
        {
        }
        catch (HttpRequestException)
        {
        }
    }

    private ProjectOptions CurrentOptions() => new()
    {
        Name = name,
        Database = database.Value,
        FilesStorage = fileStorage.Value,
        Api = api.Value,
        Pipeline = pipeline.Value,
        Module = module.Value,
        Captcha = captcha.Value,
        Theme = theme.Value,
        Aspire = aspire.Value,
        Multitenant = multiTenant.Value,
        Notification = notification.Value,
        Cloudflare = cloudflare.Value,
        Redis = redis.Value,
        SignalR = signalR.Value,
        OfflineDb = offlineDb.Value,
        Sentry = sentry.Value,
        AppInsights = appInsight.Value,
        Ads = googleAds.Value,
        Brouter = brouter.Value,
        Sample = sample.Value,
        Platforms = [.. new[] { (android, "android"), (ios, "ios"), (macOS, "macos"), (windows, "windows") }.Where(p => p.Item1).Select(p => p.Item2)],
        Ide = ide.Value
    };

    private void Apply(ProjectOptions options)
    {
        name = string.IsNullOrWhiteSpace(options.Name) ? name : options.Name;
        database.Value = options.Database ?? database.Value;
        fileStorage.Value = options.FilesStorage ?? fileStorage.Value;
        api.Value = options.Api ?? api.Value;
        pipeline.Value = options.Pipeline ?? pipeline.Value;
        module.Value = options.Module ?? module.Value;
        captcha.Value = options.Captcha ?? captcha.Value;
        theme.Value = options.Theme ?? theme.Value;
        aspire.Value = options.Aspire ?? aspire.Value;
        multiTenant.Value = options.Multitenant ?? multiTenant.Value;
        notification.Value = options.Notification ?? notification.Value;
        cloudflare.Value = options.Cloudflare ?? cloudflare.Value;
        redis.Value = options.Redis ?? redis.Value;
        signalR.Value = options.SignalR ?? signalR.Value;
        offlineDb.Value = options.OfflineDb ?? offlineDb.Value;
        sentry.Value = options.Sentry ?? sentry.Value;
        appInsight.Value = options.AppInsights ?? appInsight.Value;
        googleAds.Value = options.Ads ?? googleAds.Value;
        brouter.Value = options.Brouter ?? brouter.Value;
        sample.Value = options.Sample ?? sample.Value;
        ide.Value = options.Ide ?? ide.Value;

        if (options.Platforms is { } platforms)
        {
            android = platforms.Contains("android");
            ios = platforms.Contains("ios");
            macOS = platforms.Contains("macos");
            windows = platforms.Contains("windows");
        }
    }

    private void SetRealProject(bool value)
    {
        realProject.Value = value;

        if (value)
        {
            UseForRealProject(database, "PostgreSQL");
            UseForRealProject(fileStorage, "S3");
            UseForRealProject(redis, true);
            UseForRealProject(signalR, true);
            return;
        }

        foreach (var undo in realProjectUndo)
        {
            undo();
        }

        realProjectUndo.Clear();
    }

    private void UseForRealProject<T>(Parameter<T> parameter, T value)
    {
        if (parameter.IsModified)
            return;

        parameter.Value = value;
        realProjectUndo.Add(() =>
        {
            if (EqualityComparer<T>.Default.Equals(parameter.Value, value))
            {
                parameter.Value = parameter.Default;
            }
        });
    }

    private sealed record AssistantMessage(bool FromUser, string Text);

    private string GetPlatformsCommand()
    {
        List<string> platforms = ["web"];

        if (android)
        {
            platforms.Add("android");
        }

        if (ios)
        {
            platforms.Add("ios");
        }

        if (macOS)
        {
            platforms.Add("macos");
        }

        if (windows)
        {
            platforms.Add("windows");
        }

        return $"--platforms {string.Join(',', platforms)} ";
    }

    private string GetCaptchaCommand()
    {
        return $"--captcha {captcha.Value} ";
    }

    private string GetPipelineCommand()
    {
        return $"--pipeline {pipeline.Value} ";
    }

    private string GetModuleCommand()
    {
        return $"--module {module.Value} ";
    }

    private string GetCloudflareCommand()
    {
        return $"--cloudflare{(cloudflare.Value ? string.Empty : " false")} ";
    }

    private string GetSampleCommand()
    {
        return $"--sample{(sample.Value ? string.Empty : " false")} ";
    }

    private string GetSentryCommand()
    {
        return $"--sentry{(sentry.Value ? string.Empty : " false")} ";
    }

    private string GetDatabaseCommand()
    {
        return $"--database {database.Value} ";
    }

    private string GetFileStorageCommand()
    {
        return $"--filesStorage {fileStorage.Value} ";
    }

    private string GetApiCommand()
    {
        return $"--api {api.Value} ";
    }

    private string GetOfflineDbCommand()
    {
        return $"--offlineDb{(offlineDb.Value ? string.Empty : " false")} ";
    }

    private string GetNotificationCommand()
    {
        return $"--notification{(notification.Value ? string.Empty : " false")} ";
    }

    private string GetAppInsightsCommand()
    {
        return $"--appInsights{(appInsight.Value ? string.Empty : " false")} ";
    }

    private string GetSignalRCommand()
    {
        return $"--signalR{(signalR.Value ? string.Empty : " false")} ";
    }

    private string GetGoogleAdsCommand()
    {
        return $"--ads{(googleAds.Value ? string.Empty : " false")} ";
    }

    private string GetRedisCommand()
    {
        return $"--redis{(redis.Value ? string.Empty : " false")} ";
    }

    private string GetAspireCommand()
    {
        return $"--aspire{(aspire.Value ? string.Empty : " false")} ";
    }

    private string GetMultiTenantCommand()
    {
        return $"--multitenant{(multiTenant.Value ? string.Empty : " false")} ";
    }

    private string GetBrouterCommand()
    {
        return $"--brouter{(brouter.Value ? string.Empty : " false")} ";
    }

    private string GetThemeCommand()
    {
        return $"--theme {theme.Value} ";
    }

    private string GetRealProjectCommand()
    {
        return $"--realProject{(realProject.Value ? string.Empty : " false")} ";
    }

    private class Parameter<T>
    {
        public T? Value { get; set; }
        public T? Default { get; set; }
        public BitDropdownItem<string>[]? Items { get; set; }
        public bool IsModified => EqualityComparer<T>.Default.Equals(Default, Value) is false;
    }
}
