using System.Text;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates05CreateProjectPage
{
    private string name = "MyFirstProject";

    private Parameter windows = new()
    {
        SelectedBoolean = true,
        DefaultBoolean = true,
    };

    private Parameter appCenter = new()
    {
        SelectedBoolean = false,
        DefaultBoolean = false,
    };

    private Parameter offlineDb = new()
    {
        SelectedBoolean = false,
        DefaultBoolean = false,
    };

    private Parameter appInsight = new()
    {
        SelectedBoolean = false,
        DefaultBoolean = false,
    };

    private Parameter captcha = new()
    {
        Selected = "reCaptcha",
        Default = "reCaptcha",
        Items = [
            new() { Text = "None", Value = "None" },
            new() { Text = "reCaptcha", Value = "reCaptcha" },
        ]
    };

    private Parameter pipeline = new()
    {
        Selected = "GitHub",
        Default = "GitHub",
        Items = [
            new() { Text = "None", Value = "None" },
            new() { Text = "GitHub", Value = "GitHub" },
            new() { Text = "Azure", Value = "Azure" },
        ]
    };

    private Parameter sample = new()
    {
        Selected = "None",
        Default = "None",
        Items = [
            new() { Text = "None", Value = "None" },
            new() { Text = "Admin", Value = "Admin" },
            new() { Text = "Todo", Value = "Todo" },
        ]
    };

    private Parameter database = new()
    {
        Selected = "Sqlite",
        Default = "Sqlite",
        Items = [
            new() { Text = "Sqlite", Value = "Sqlite" },
            new() { Text = "SqlServer", Value = "SqlServer" },
            new() { Text = "PostgreSQL", Value = "PostgreSQL" },
            new() { Text = "MySQL", Value = "MySQL" },
            new() { Text = "Cosmos", Value = "Cosmos" },
            new() { Text = "Other", Value = "Other" },
        ],
        Details = new()
        {
            ["Sqlite"] = (null, "https://www.sqlite.org/download.html", null),
            ["SqlServer"] = (null, "https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb", "LocalDB is insalled by default along with Visual Studio."),
            ["PostgreSQL"] = ("winget install --id=PostgreSQL.PostgreSQL.14  -e", "https://www.postgresql.org/download/", null),
            ["MySQL"] = (null, "https://mariadb.org/download", "Maria db is supported as well."),
            ["Cosmos"] = ("winget install -e --id Microsoft.Azure.CosmosEmulator", "https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=windows%2Ccsharp&pivots=api-nosql", null),
        }
    };

    private Parameter fileStorage = new()
    {
        Selected = "Local",
        Default = "Local",
        Items = [
            new() { Text = "Local", Value = "Local" },
            new() { Text = "AzureBlobStorage", Value = "AzureBlobStorage" },
            new() { Text = "Other", Value = "Other" },
        ],
        Details = new()
        {
            ["AzureBlobStorage"] = (null, "https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=npm%2Cblob-storage#install-azurite", null),
        }
    };

    private Parameter api = new()
    {
        Selected = "Integrated",
        Default = "Integrated",
        Items = [
            new() { Text = "Integrated", Value = "Integrated" },
            new() { Text = "Standalone", Value = "Standalone" },
        ]
    };

    private string GetFinalCommand()
    {
        StringBuilder finalCommand = new($"dotnet new bit-bp {GetNameCommand()}");

        if (IncludeCommand(captcha))
        {
            finalCommand.Append(GetCaptchaCommand());
        }

        if (IncludeCommand(pipeline))
        {
            finalCommand.Append(GetPipelineCommand());
        }

        if (IncludeCommand(sample))
        {
            finalCommand.Append(GetSampleCommand());
        }

        if (IncludeCommand(windows))
        {
            finalCommand.Append(GetWindowsCommand());
        }

        if (IncludeCommand(appCenter))
        {
            finalCommand.Append(GetAppCenterCommand());
        }

        if (IncludeCommand(database))
        {
            finalCommand.Append(GetDatabaseCommand());
        }

        if (IncludeCommand(fileStorage))
        {
            finalCommand.Append(GetFileStorageCommand());
        }

        if (IncludeCommand(api))
        {
            finalCommand.Append(GetApiCommand());
        }

        if (IncludeCommand(offlineDb))
        {
            finalCommand.Append(GetOfflineDbCommand());
        }

        if (IncludeCommand(appInsight))
        {
            finalCommand.Append(GetAppInsightsCommand());
        }

        return finalCommand.ToString();
    }

    private string GetNameCommand()
    {
        return $"--name {name} ";
    }

    private string GetCaptchaCommand()
    {
        return $"--captcha {captcha.Selected} ";
    }

    private string GetPipelineCommand()
    {
        return $"--pipeline {pipeline.Selected} ";
    }

    private string GetSampleCommand()
    {
        return $"--sample {sample.Selected} ";
    }

    private string GetWindowsCommand()
    {
        return $"--windows {(windows.SelectedBoolean ? "true" : "false")} ";
    }

    private string GetAppCenterCommand()
    {
        return $"--appCenter {appCenter.SelectedBoolean} ";
    }

    private string GetDatabaseCommand()
    {
        return $"--database {database.Selected} ";
    }

    private string GetFileStorageCommand()
    {
        return $"--fluentStorage {fileStorage.Selected} ";
    }

    private string GetApiCommand()
    {
        return $"--api {api.Selected} ";
    }

    private string GetOfflineDbCommand()
    {
        return $"--offlineDb {offlineDb.SelectedBoolean} ";
    }

    private string GetAppInsightsCommand()
    {
        return $"--appinsights {appInsight.SelectedBoolean} ";
    }

    private bool IncludeCommand(Parameter parameter)
    {
        if (parameter.Selected is not null)
            return !string.Equals(parameter.Selected, parameter.Default, StringComparison.InvariantCultureIgnoreCase);

        return parameter.SelectedBoolean != parameter.DefaultBoolean;
    }

    private class Parameter
    {
        public string? Selected { get; set; }
        public string? Default { get; set; }
        public bool SelectedBoolean { get; set; }
        public bool DefaultBoolean { get; set; }
        public bool IsChanged => Selected != Default || SelectedBoolean != DefaultBoolean;
        public BitDropdownItem<string>[]? Items { get; set; }
        public Dictionary<string, (string? installCommand, string reference, string? text)> Details { get; set; }
    }
}
