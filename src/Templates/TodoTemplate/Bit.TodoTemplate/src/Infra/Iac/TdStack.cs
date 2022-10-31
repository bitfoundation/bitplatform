using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.KeyVault.Inputs;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using AppInsights = Pulumi.AzureNative.Insights.V20200202Preview.Component;
using AppInsightsIngestionMode = Pulumi.AzureNative.Insights.V20200202Preview.IngestionMode;
using AppInsightsWebApplicationType = Pulumi.AzureNative.Insights.V20200202Preview.ApplicationType;
using ManagedServiceIdentityType = Pulumi.AzureNative.Web.ManagedServiceIdentityType;
using SqlDatabase = Pulumi.AzureNative.Sql.Database;
using SqlDatabaseSkuArgs = Pulumi.AzureNative.Sql.Inputs.SkuArgs;
using SqlServer = Pulumi.AzureNative.Sql.Server;
using SqlServerFirewallRule = Pulumi.AzureNative.Sql.FirewallRule;
using VaultSkuArgs = Pulumi.AzureNative.KeyVault.Inputs.SkuArgs;
using VaultSkuName = Pulumi.AzureNative.KeyVault.SkuName;
using WebAppManagedServiceIdentityArgs = Pulumi.AzureNative.Web.Inputs.ManagedServiceIdentityArgs;

namespace TodoTemplate.Iac;

public class TdStack : Stack
{
    public TdStack()
    {
        string stackName = Pulumi.Deployment.Instance.StackName;

        Config pulumiConfig = new();

        var sqlDatabaseDbAdminId = pulumiConfig.Require("sql-server-td-db-admin-id");
        var sqlDatabaseDbAdminPassword = pulumiConfig.RequireSecret("sql-server-td-db-admin-password");

        var defaultEmailFrom = pulumiConfig.Require("default-email-from");
        var emailServerHost = pulumiConfig.Require("email-server-host");
        var emailServerPort = pulumiConfig.Require("email-server-port");
        var emailServerUserName = pulumiConfig.Require("email-server-userName");
        var emailServerPassword = pulumiConfig.RequireSecret("email-server-password");

        var identityCertificatePassword = pulumiConfig.RequireSecret("identity-certificate-password");

        ResourceGroup resourceGroup = new($"td-{stackName}", new ResourceGroupArgs
        {
            ResourceGroupName = $"td-{stackName}"
        }, options: new() { ImportId = $"/subscriptions/{GetClientConfig.InvokeAsync().GetAwaiter().GetResult().SubscriptionId}/resourceGroups/td-prod" });

        Workspace appInsightsWorkspace = new($"insights-wkspc-td-{stackName}", new()
        {
            WorkspaceName = $"insights-wkspc-td-{stackName}",
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            RetentionInDays = 30
        });

        AppInsights appInsights = new($"app-insights-td-{stackName}", new()
        {
            ResourceName = $"app-insights-td-{stackName}",
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ApplicationType = AppInsightsWebApplicationType.Web,
            Kind = "web",
            IngestionMode = AppInsightsIngestionMode.LogAnalytics,
            DisableIpMasking = true,
            WorkspaceResourceId = Output.Tuple(resourceGroup.Name, appInsightsWorkspace.Name).Apply(t =>
            {
                (string resourceGroupName, string workspaceName) = t;
                return GetWorkspace.InvokeAsync(new GetWorkspaceArgs { ResourceGroupName = resourceGroupName, WorkspaceName = workspaceName });
            }).Apply(workspace => workspace.Id)
        });

        SqlServer sqlServer = new($"sql-server-td-{stackName}", new()
        {
            ServerName = $"sql-server-td-{stackName}",
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            AdministratorLogin = sqlDatabaseDbAdminId,
            AdministratorLoginPassword = sqlDatabaseDbAdminPassword
        });

        SqlDatabase sqlDatabase = new($"sql-database-td-{stackName}", new()
        {
            DatabaseName = $"sql-database-td-{stackName}",
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerName = sqlServer.Name,
            Sku = new SqlDatabaseSkuArgs
            {
                Tier = "Basic",
                Name = "Basic",
                Capacity = 5
            }
        });

        AppServicePlan appServicePlan = new($"app-plan-td-{stackName}", new()
        {
            Name = $"app-plan-td-{stackName}",
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Kind = "Linux",
            Reserved = true,
            Sku = new SkuDescriptionArgs
            {
                Tier = "Basic",
                Name = "B1",
                Size = "B1",
                Capacity = 1,
                Family = "B"
            }
        });

        string vaultName = $"vault-td-{stackName}";
        string sqlDatabaseConnectionStringSecretName = $"sql-connection-secret";
        string emailServerPasswordSecretName = "email-server-password-secret";
        string identityCertificatePasswordSecretName = "identity-certificate-password-secret";

        WebApp webApp = new($"app-service-td-{stackName}", new()
        {
            Name = $"app-service-td-{stackName}",
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerFarmId = appServicePlan.Id,
            ClientAffinityEnabled = false,
            HttpsOnly = true,
            Identity = new WebAppManagedServiceIdentityArgs() { Type = ManagedServiceIdentityType.SystemAssigned },
            SiteConfig = new SiteConfigArgs
            {
                Use32BitWorkerProcess = false,
                AlwaysOn = true,
                Http20Enabled = true,
                WebSocketsEnabled = true,
                NetFrameworkVersion = "v7.0",
                FtpsState = FtpsState.Disabled,
                LinuxFxVersion = "DOTNETCORE|7.0",
                AppCommandLine = "dotnet TodoTemplate.Server.Api.dll",
                AppSettings = new()
                {
                    new NameValuePairArgs { Name = "ApplicationInsights__InstrumentationKey", Value = appInsights.InstrumentationKey },
                    new NameValuePairArgs { Name = "APPINSIGHTS_INSTRUMENTATIONKEY", Value = appInsights.InstrumentationKey },
                    new NameValuePairArgs { Name = "ASPNETCORE_ENVIRONMENT", Value = stackName == "test" ? "Test" : "Production" },
                    new NameValuePairArgs { Name = "APPLICATIONINSIGHTS_CONNECTION_STRING", Value = appInsights.ConnectionString },
                    new NameValuePairArgs { Name = "APPINSIGHTS_PROFILERFEATURE_VERSION", Value = "disabled" },
                    new NameValuePairArgs { Name = "APPINSIGHTS_SNAPSHOTFEATURE_VERSION", Value = "disabled" },
                    new NameValuePairArgs { Name = "ApplicationInsightsAgent_EXTENSION_VERSION", Value = "~3" },
                    new NameValuePairArgs { Name = "XDT_MicrosoftApplicationInsights_BaseExtensions", Value = "~1" },
                    new NameValuePairArgs { Name = "InstrumentationEngine_EXTENSION_VERSION", Value = "~1" },
                    new NameValuePairArgs { Name = "SnapshotDebugger_EXTENSION_VERSION", Value = "disabled"},
                    new NameValuePairArgs { Name = "XDT_MicrosoftApplicationInsights_Mode", Value = "recommended" },
                    new NameValuePairArgs { Name = "XDT_MicrosoftApplicationInsights_PreemptSdk", Value = "disabled" },
                    new NameValuePairArgs { Name = "AppSettings__EmailSettings__DefaulFromEmail", Value = defaultEmailFrom },
                    new NameValuePairArgs { Name = "AppSettings__EmailSettings__Host", Value = emailServerHost },
                    new NameValuePairArgs { Name = "AppSettings__EmailSettings__Port", Value = emailServerPort },
                    new NameValuePairArgs { Name = "AppSettings__EmailSettings__UserName", Value = emailServerUserName },
                    new NameValuePairArgs
                    {
                        Name = "AppSettings__EmailSettings__Password",
                        Value = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={emailServerPasswordSecretName})"
                    },
                    new NameValuePairArgs
                    {
                        Name = "AppSettings__JwtSettings__IdentityCertificatePassword",
                        Value = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={identityCertificatePasswordSecretName})"
                    },
                },
                ConnectionStrings = new()
                {
                    new ConnStringInfoArgs
                    {
                        Name = "SqlServerConnectionString",
                        Type = ConnectionStringType.SQLAzure,
                        ConnectionString = $"@Microsoft.KeyVault(VaultName={vaultName};SecretName={sqlDatabaseConnectionStringSecretName})"
                    }
                }
            }
        });

        Output.Tuple(resourceGroup.Name, webApp.Name).Apply(t =>
        {
            (string resourceGroupName, string webAppName) = t;

            Output.Create(GetWebApp.InvokeAsync(new() { Name = webAppName, ResourceGroupName = resourceGroupName }))
                .Apply(webAppToGetIPAddresses =>
                {
                    var ipAddresses = webAppToGetIPAddresses.PossibleOutboundIpAddresses;

                    foreach (var ipAddress in ipAddresses.Split(','))
                    {
                        new SqlServerFirewallRule($"fw-{stackName}-{ipAddress}", new()
                        {
                            Name = $"fw-{stackName}-{ipAddress}",
                            FirewallRuleName = $"fw-{stackName}-{ipAddress}",
                            EndIpAddress = ipAddress,
                            ResourceGroupName = resourceGroup.Name,
                            ServerName = sqlServer.Name,
                            StartIpAddress = ipAddress
                        });
                    }

                    return string.Empty;
                });

            return string.Empty;
        });

        Vault vault = new Vault($"vault-td-{stackName}", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            VaultName = vaultName,
            Properties = new VaultPropertiesArgs()
            {
                Sku = new VaultSkuArgs { Name = VaultSkuName.Standard, Family = SkuFamily.A },
                TenantId = Output.Create(GetClientConfig.InvokeAsync()).Apply(clientConfig => clientConfig.TenantId),
                EnabledForDeployment = true,
                EnabledForDiskEncryption = true,
                EnabledForTemplateDeployment = true,
                EnableSoftDelete = false,
                AccessPolicies = new List<AccessPolicyEntryArgs>
                {
                    new AccessPolicyEntryArgs
                    {
                        TenantId = Output.Create(GetClientConfig.InvokeAsync()).Apply(clientConfig => clientConfig.TenantId),
                        ObjectId = Output.Tuple(resourceGroup.Name, webApp.Name).Apply(t =>
                        {
                            (string resourceGroupName, string webAppName) = t;
                            return GetWebApp.InvokeAsync(new GetWebAppArgs{ ResourceGroupName = resourceGroupName, Name = webAppName });
                        }).Apply(app => app.Identity!.PrincipalId),
                        Permissions = new PermissionsArgs
                        {
                            Secrets =
                            {
                                "get"
                            }
                        }
                    }
                }
            }
        });

        Secret identityCertificatePasswordSecret = new(identityCertificatePasswordSecretName, new()
        {
            ResourceGroupName = resourceGroup.Name,
            VaultName = vault.Name,
            SecretName = identityCertificatePasswordSecretName,
            Properties = new SecretPropertiesArgs
            {
                Value = identityCertificatePassword
            }
        });

        Secret emailServerPasswordSecret = new(emailServerPasswordSecretName, new()
        {
            ResourceGroupName = resourceGroup.Name,
            VaultName = vault.Name,
            SecretName = emailServerPasswordSecretName,
            Properties = new SecretPropertiesArgs
            {
                Value = emailServerPassword
            }
        });

        Secret sqlDatabaseConnectionStringSecret = new(sqlDatabaseConnectionStringSecretName, new()
        {
            ResourceGroupName = resourceGroup.Name,
            VaultName = vault.Name,
            SecretName = sqlDatabaseConnectionStringSecretName,
            Properties = new SecretPropertiesArgs
            {
                Value = Output.Tuple(sqlServer.Name, sqlDatabase.Name, sqlDatabaseDbAdminPassword).Apply(t =>
                {
                    (string _sqlServer, string _sqlDatabase, string _sqlDatabasePassword) = t;
                    return $"Data Source=tcp:{_sqlServer}.database.windows.net;Initial Catalog={_sqlDatabase};User ID={sqlDatabaseDbAdminId};Password={_sqlDatabasePassword};Application Name=Todo;Encrypt=True;";
                })
            }
        });
    }
}
