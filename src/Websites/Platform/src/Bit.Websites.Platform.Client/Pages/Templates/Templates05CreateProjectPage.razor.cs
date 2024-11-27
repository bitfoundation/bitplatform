using System.Text;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates05CreateProjectPage
{
    private string name = "MyFirstProject";

    private Parameter<bool> windows = new() { Value = true, Default = true };
    private Parameter<bool> sentry = new() { Value = false, Default = false };
    private Parameter<bool> offlineDb = new() { Value = false, Default = false };
    private Parameter<bool> notification = new() { Value = false, Default = false };
    private Parameter<bool> appInsight = new() { Value = false, Default = false };
    private Parameter<bool> signalR = new() { Value = false, Default = false };

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

    private Parameter<string> dotnetVersion = new()
    {
        Value = "net9.0",
        Default = "net9.0",
        Items = 
        [
            new() { Text = ".NET 8", Value = "net8.0" },
            new() { Text = ".NET 9", Value = "net9.0" },
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

    private Parameter<string> sample = new()
    {
        Value = "None",
        Default = "None",
        Items = 
        [
            new() { Text = "None", Value = "None" },
            new() { Text = "Admin", Value = "Admin" },
            new() { Text = "Todo", Value = "Todo" },
        ]
    };

    private Parameter<string> database = new()
    {
        Value = "Sqlite",
        Default = "Sqlite",
        Items = 
        [
            new() { Text = "Sqlite", Value = "Sqlite" },
            new() { Text = "SqlServer", Value = "SqlServer" },
            new() { Text = "PostgreSQL", Value = "PostgreSQL" },
            new() { Text = "MySQL", Value = "MySQL" },
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

    private string GetFinalCommand()
    {
        StringBuilder finalCommand = new($"dotnet new bit-bp {GetNameCommand()}");

        if (captcha.IsModified)
        {
            finalCommand.Append(GetCaptchaCommand());
        }

        if (dotnetVersion.IsModified)
        {
            finalCommand.Append(GetDotNetVersionCommand());
        }

        if (pipeline.IsModified)
        {
            finalCommand.Append(GetPipelineCommand());
        }

        if (sample.IsModified)
        {
            finalCommand.Append(GetSampleCommand());
        }

        if (windows.IsModified)
        {
            finalCommand.Append(GetWindowsCommand());
        }

        if (sentry.IsModified)
        {
            finalCommand.Append(GetSentryCommand());
        }

        if (database.IsModified)
        {
            finalCommand.Append(GetDatabaseCommand());
        }

        if (fileStorage.IsModified)
        {
            finalCommand.Append(GetFileStorageCommand());
        }

        if (api.IsModified)
        {
            finalCommand.Append(GetApiCommand());
        }

        if (offlineDb.IsModified)
        {
            finalCommand.Append(GetOfflineDbCommand());
        }

        if (notification.IsModified)
        {
            finalCommand.Append(GetNotificationCommand());
        }

        if (appInsight.IsModified)
        {
            finalCommand.Append(GetAppInsightsCommand());
        }

        if (signalR.IsModified)
        {
            finalCommand.Append(GetSignalRCommand());
        }

        return finalCommand.ToString();
    }

    private string GetNameCommand()
    {
        return $"--name {name} ";
    }

    private string GetCaptchaCommand()
    {
        return $"--captcha {captcha.Value} ";
    }

    private string GetDotNetVersionCommand()
    {
        return $"--framework {dotnetVersion.Value} ";
    }

    private string GetPipelineCommand()
    {
        return $"--pipeline {pipeline.Value} ";
    }

    private string GetSampleCommand()
    {
        return $"--sample {sample.Value} ";
    }

    private string GetWindowsCommand()
    {
        return $"--windows {windows.Value.ToString().ToLowerInvariant()} ";
    }

    private string GetSentryCommand()
    {
        return $"--sentry {sentry.Value.ToString().ToLowerInvariant()} ";
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
        return $"--offlineDb {offlineDb.Value.ToString().ToLowerInvariant()} ";
    }

    private string GetNotificationCommand()
    {
        return $"--notification {notification.Value.ToString().ToLowerInvariant()} ";
    }

    private string GetAppInsightsCommand()
    {
        return $"--appInsights {appInsight.Value.ToString().ToLowerInvariant()} ";
    }

    private string GetSignalRCommand()
    {
        return $"--signalR {signalR.Value.ToString().ToLowerInvariant()} ";
    }

    private class Parameter<T>
    {
        public T? Value { get; set; }
        public T? Default { get; set; }
        public BitDropdownItem<string>[]? Items { get; set; }
        public bool IsModified => EqualityComparer<T>.Default.Equals(Default, Value) is false;
    }
}
