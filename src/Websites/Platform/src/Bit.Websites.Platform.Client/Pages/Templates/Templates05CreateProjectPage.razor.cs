using System.Text;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates05CreateProjectPage
{
    private double verticalSpacing = 1;
    private double horizontalSpacing = 1;
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
        ],
        Details = new()
        {
            ["reCaptcha"] = new DetailItem()
            {
                Text = "To integrate Google reCAPTCHA into the Sign Up page, include --captcha reCaptcha in the dotnet new command."
            }
        }
    };

    private Parameter pipeline = new()
    {
        Selected = "GitHub",
        Default = "GitHub",
        Items = [
            new() { Text = "None", Value = "None" },
            new() { Text = "GitHub", Value = "GitHub" },
            new() { Text = "Azure", Value = "Azure" },
        ],
        Details = new()
        {
            ["GitHub"] = new DetailItem()
            {
                Command = "--pipeline GitHub",
                Reference = "https://github.com/features/actions",
                Text = "Github Actions"
            },
            ["Azure"] = new DetailItem()
            {
                Command = "--pipeline Azure",
                Reference = "https://azure.microsoft.com/en-us/products/devops/pipelines",
                Text = "Azure Devops"
            }
        }
    };

    private Parameter sample = new()
    {
        Selected = "None",
        Default = "None",
        Items = [
            new() { Text = "None", Value = "None" },
            new() { Text = "Admin", Value = "Admin" },
            new() { Text = "Todo", Value = "Todo" },
        ],
        Details = new()
        {
            ["Admin"] = new DetailItem()
            {
                Reference = "https://adminpanel.bitplatform.dev/"
            },
            ["Todo"] = new DetailItem()
            {
                Reference = "https://todo.bitplatform.dev/"
            }
        }
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
            ["Sqlite"] = new DetailItem()
            {
                Reference = "https://www.sqlite.org/download.html"
            },
            ["SqlServer"] = new DetailItem()
            {
                Reference = "https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb",
                Text = "LocalDB is insalled by default along with Visual Studio."
            },
            ["PostgreSQL"] = new DetailItem()
            {
                Command = "winget install --id=PostgreSQL.PostgreSQL.14  -e",
                Reference = "https://www.postgresql.org/download/"
            },
            ["MySQL"] = new DetailItem()
            {
                Reference = "https://mariadb.org/download",
                Text = "Maria db is supported as well."
            },
            ["Cosmos"] = new DetailItem()
            {
                Command = "winget install -e --id Microsoft.Azure.CosmosEmulator",
                Reference = "https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=windows%2Ccsharp&pivots=api-nosql"
            }
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
            ["AzureBlobStorage"] = new DetailItem()
            {
                Reference = "https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=npm%2Cblob-storage#install-azurite"
            }
        }
    };

    private Parameter api = new()
    {
        Selected = "Integrated",
        Default = "Integrated",
        Items = [
            new() { Text = "Integrated", Value = "Integrated" },
            new() { Text = "Standalone", Value = "Standalone" },
        ],
        Details = new()
        {
            ["Integrated"] = new DetailItem()
            {
                Text = @"If this parameter is set to Integrated, the Server.Web project will encompass all features of the Api project, 
hence provides options for various modes such as Blazor Auto, Blazor Server, pre-rendering, and static SSR.",
                Image = "api-integrated.webp"
            },
            ["Standalone"] = new DetailItem()
            {
                Text = "Conversely, if the parameter is set to Standalone, you will need to separately run and publish both the Server.Api and Server.Web projects.",
                Image = "api-standalone.webp"
            }
        }
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
        return $"--windows {windows.SelectedBoolean.ToString().ToLowerInvariant()} ";
    }

    private string GetAppCenterCommand()
    {
        return $"--appCenter {appCenter.SelectedBoolean.ToString().ToLowerInvariant()} ";
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
        return $"--offlineDb {offlineDb.SelectedBoolean.ToString().ToLowerInvariant()} ";
    }

    private string GetAppInsightsCommand()
    {
        return $"--appinsights {appInsight.SelectedBoolean.ToString().ToLowerInvariant()} ";
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
        public BitDropdownItem<string>[]? Items { get; set; }
        public Dictionary<string, DetailItem> Details { get; set; }
    }

    public class DetailItem
    {
        public string? Command { get; set; }
        public string? Reference { get; set; }
        public string? Text { get; set; }
        public string? Image { get; set; }
    }
}
