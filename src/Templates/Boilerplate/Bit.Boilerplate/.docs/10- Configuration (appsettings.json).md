# Stage 10: Configuration (appsettings.json)

Welcome to Stage 10! In this stage, you'll learn how the project manages configuration across different platforms and environments using `appsettings.json` files.

## Configuration Architecture Overview

The project uses a hierarchical configuration system where settings cascade from shared configuration files to platform-specific ones. This allows you to define common settings once and override them as needed for specific platforms or environments.

## Configuration File Locations

Each project in the solution has its own set of `appsettings.json` files:

### Shared Configuration
- [`src/Shared/appsettings.json`](/src/Shared/appsettings.json) - Base configuration shared across all platforms
- [`src/Shared/appsettings.Development.json`](/src/Shared/appsettings.Development.json) - Development environment overrides
- [`src/Shared/appsettings.Production.json`](/src/Shared/appsettings.Production.json) - Production environment overrides

### Client Core Configuration
- [`src/Client/Boilerplate.Client.Core/appsettings.json`](/src/Client/Boilerplate.Client.Core/appsettings.json) - Client-side shared configuration
- [`src/Client/Boilerplate.Client.Core/appsettings.Development.json`](/src/Client/Boilerplate.Client.Core/appsettings.Development.json) - Development overrides
- [`src/Client/Boilerplate.Client.Core/appsettings.Production.json`](/src/Client/Boilerplate.Client.Core/appsettings.Production.json) - Production overrides

### Platform-Specific Configuration
- **Blazor WebAssembly**: [`src/Client/Boilerplate.Client.Web/appsettings.json`](/src/Client/Boilerplate.Client.Web/appsettings.json)
- **.NET MAUI**: [`src/Client/Boilerplate.Client.Maui/appsettings.json`](/src/Client/Boilerplate.Client.Maui/appsettings.json)
- **Windows**: [`src/Client/Boilerplate.Client.Windows/appsettings.json`](/src/Client/Boilerplate.Client.Windows/appsettings.json)

### Server Configuration
- **API Server**: [`src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)
- **Web Server**: Server.Web uses standard ASP.NET Core configuration hierarchy
- **AppHost**: [`src/Server/Boilerplate.Server.AppHost/appsettings.json`](/src/Server/Boilerplate.Server.AppHost/appsettings.json)

## Configuration Priority Hierarchy

The configuration system loads settings in a specific order, with **later sources overriding earlier ones**. This is defined in [`IConfigurationBuilderExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IConfigurationBuilderExtensions.cs):

### Configuration Priority (Lowest to Highest)

```
Priority Order (Each level can override previous levels):
┌─────────────────────────────────────────────────────────────┐
│ 1. Shared/appsettings.json                                  │ ← Lowest Priority
├─────────────────────────────────────────────────────────────┤
│ 2. Shared/appsettings.{environment}.json                    │
├─────────────────────────────────────────────────────────────┤
│ 3. Client/Core/appsettings.json                             │
├─────────────────────────────────────────────────────────────┤
│ 4. Client/Core/appsettings.{environment}.json               │
├─────────────────────────────────────────────────────────────┤
│ 5. Platform-Specific Configuration:                         │
│    ┌─────────────────────────────────────────────────────┐  │
│    │ Server (Blazor Server/SSR + API):                   │  │
│    │   • Server/appsettings.json                         │  │
│    │   • Server/appsettings.{environment}.json           │  │
│    │   • ASP.NET Core default configuration sources      │  │
│    │     (environment variables, command line, etc.)     │  │
│    └─────────────────────────────────────────────────────┘  │
│    ┌─────────────────────────────────────────────────────┐  │
│    │ Blazor WebAssembly:                                 │  │
│    │   • Client.Web/appsettings.json                     │  │
│    │   • Client.Web/appsettings.{environment}.json       │  │
│    │   • Client.Web/wwwroot/appsettings.json             │  │
│    │   • Client.Web/wwwroot/{environment}.json           │  │
│    └─────────────────────────────────────────────────────┘  │
│    ┌─────────────────────────────────────────────────────┐  │
│    │ .NET MAUI:                                          │  │
│    │   • Client.Maui/appsettings.json                    │  │
│    │   • Client.Maui/appsettings.{environment}.json      │  │
│    └─────────────────────────────────────────────────────┘  │
│    ┌─────────────────────────────────────────────────────┐  │
│    │ Windows:                                            │  │
│    │   • Client.Windows/appsettings.json                 │  │
│    │   • Client.Windows/appsettings.{environment}.json   │  │
│    └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘ ← Highest Priority
```

### How Priority Works - Practical Example

Let's say you have a `ServerAddress` setting:

1. **Shared/appsettings.json** defines:
   ```json
   {
     "ServerAddress": "https://api.production.com/"
   }
   ```

2. **Shared/appsettings.Development.json** overrides it for all platforms in Development:
   ```json
   {
     "ServerAddress": "http://localhost:5000/"
   }
   ```

3. **Client.Maui/appsettings.Development.json** can override it specifically for MAUI in Development:
   ```json
   {
     "ServerAddress": "http://10.0.2.2:5000/"
   }
   ```

**Result**: 
- In **Production** on all platforms: Uses `https://api.production.com/`
- In **Development** on Web/Windows: Uses `http://localhost:5000/`
- In **Development** on MAUI: Uses `http://10.0.2.2:5000/` (Android emulator localhost)

## Real Configuration Examples from the Project

### Example 1: Shared Configuration for Logging

In [`src/Shared/appsettings.json`](/src/Shared/appsettings.json), you can see logging configuration that applies to all platforms:

```json
{
    "ApplicationInsights": {
        "ConnectionString": null
    },
    "Logging": {
        "LogLevel": {
            "Default": "Warning",
            "Microsoft.Hosting.Lifetime": "Information",
            "Microsoft.EntityFrameworkCore.Database.Command": "Information",
            "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None"
        },
        "ApplicationInsights": {
            "LogLevel": {
                "Default": "Warning",
                "Microsoft.Hosting.Lifetime": "Information"
            }
        },
        "Sentry": {
            "Dsn": "",
            "SendDefaultPii": true,
            "EnableScopeSync": true,
            "LogLevel": {
                "Default": "Warning",
                "Microsoft.Hosting.Lifetime": "Information"
            }
        },
        "DiagnosticLogger": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore*": "Warning",
                "Microsoft.Hosting.Lifetime": "Information"
            }
        }
    }
}
```

This configuration is automatically inherited by all platforms (Web, MAUI, Windows, Server).

### Example 2: Client Core Configuration

In [`src/Client/Boilerplate.Client.Core/appsettings.json`](/src/Client/Boilerplate.Client.Core/appsettings.json), client-specific settings are defined:

```json
{
    "ServerAddress": "http://localhost:5000/",
    "ServerAddress_Comment": "If you're running Boilerplate.Server.Web project, then you can also use relative urls such as / for Blazor Server and WebAssembly",
    "GoogleRecaptchaSiteKey": "6LdMKr4pAAAAAKMyuEPn3IHNf04EtULXA8uTIVRw",
    "AdUnitPath": "/22639388115/rewarded_web_example",
    "AdUnitPath__Comment": "The advertisement's unit path of the google ads from the Google Ad Manager panel."
}
```

These settings apply to all client platforms (Web, MAUI, Windows) but can be overridden in platform-specific files.

### Example 3: Platform-Specific Configuration (MAUI)

In [`src/Client/Boilerplate.Client.Maui/appsettings.json`](/src/Client/Boilerplate.Client.Maui/appsettings.json):

```json
{
    "WebAppUrl": null,
    "WebAppUrl_Comment": "When the maui app sends a request to the API server, and the API server and web app are hosted on different URLs, the origin of the generated links (e.g., email confirmation links) will depend on `WebAppUrl` value."
}
```

This setting only exists in the MAUI project because it's specific to mobile app scenarios.

### Example 4: Server API Configuration

In [`src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json), you'll find comprehensive server settings:

```json
{
    "ConnectionStrings": {
        "mssqldb": "Data Source=(localdb)\\mssqllocaldb; Initial Catalog=BoilerplateDb;...",
        "s3": "Endpoint=http://localhost:9000;AccessKey=minioadmin;SecretKey=minioadmin;",
        "smtp": "Endpoint=smtp://smtp.ethereal.email:587;UserName=..."
    },
    "AI": {
        "ChatOptions": {
            "Temperature": 0
        },
        "OpenAI": {
            "ChatModel": "gpt-4.1-mini",
            "ChatApiKey": null,
            "ChatEndpoint": "https://models.inference.ai.azure.com"
        }
    },
    "Identity": {
        "JwtIssuerSigningKeySecret": "VeryLongJWTIssuerSiginingKeySecret...",
        "Issuer": "Boilerplate",
        "Audience": "Boilerplate",
        "BearerTokenExpiration": "0.00:05:00",
        "RefreshTokenExpiration": "14.00:00:00"
    },
    "Email": {
        "DefaultFromEmail": "DoNotReply@bitplatform.dev"
    },
    "GoogleRecaptchaSecretKey": "6LdMKr4pAAAAANvngWNam_nlHzEDJ2t6SfV6L_DS"
}
```

## The `__Comment` Pattern

Since JSON doesn't support comments natively, the project uses a special pattern: properties ending with `__Comment` or `_Comment`.

### How It Works

When you want to add explanatory comments to configuration files, append `__Comment` or `_Comment` to the setting name:

```json
{
    "ServerAddress": "http://localhost:5000/",
    "ServerAddress_Comment": "If you're running Boilerplate.Server.Web project, then you can also use relative urls such as / for Blazor Server and WebAssembly",
    
    "BearerTokenExpiration": "0.00:05:00",
    "BearerTokenExpiration_Comment": "Format: D.HH:mm:ss",
    
    "MaxPrivilegedSessionsCount": 3,
    "MaxPrivilegedSessionsCount_Comment": "Is the maximum number of concurrent privileged sessions a user can have.",
    
    "ConnectionStrings": {
        "Aspire__Comment": "Running Boilerplate.Server.AppHost `overrides` the following connection strings at runtime.",
        "mssqldb": "Data Source=(localdb)\\mssqllocaldb; Initial Catalog=BoilerplateDb;...",
        "smtp": "Endpoint=smtp://smtp.ethereal.email:587;...",
        "smtp_Comment": "You can also use https://ethereal.email/create for testing purposes."
    }
}
```

### Real Examples from the Project

Here are actual `__Comment` usages found in the codebase:

**From `src/Server/Boilerplate.Server.Api/appsettings.json`:**
```json
{
    "ConnectionStrings": {
        "Aspire__Comment": "Running Boilerplate.Server.AppHost `overrides` the following connection strings at runtime."
    },
    "Email__Comment": "You can also use https://ethereal.email/create for testing purposes.",
    "Identity": {
        "BearerTokenExpiration_Comment": "BearerTokenExpiration used as JWT's expiration claim. Format: D.HH:mm:ss"
    },
    "UserInformationCache": {
        "UseIsolatedStorage__Comment": "Useful for testing or in production when managing multiple codebases with a single database."
    },
    "ForceUpdate": {
        "SupportedAppVersions__Comment": "Enabling `AutoReload` ensure the latest app version is always applied in Web & Windows apps."
    }
}
```

**From `src/Server/Boilerplate.Server.AppHost/appsettings.Development.json`:**
```json
{
    "Parameters": {
        "sqlserver__Comment": "The username is `sa` by default",
        "s3__Comment": "The username is `minioadmin` by default"
    }
}
```

**From `src/Client/Boilerplate.Client.Core/appsettings.json`:**
```json
{
    "AdUnitPath__Comment": "The advertisement's unit path of the google ads from the Google Ad Manager panel."
}
```

**Why This Pattern?**
- JSON doesn't allow traditional `//` or `/* */` comments
- Comment properties are ignored by the configuration system (they're never bound to settings classes)
- Provides in-file documentation for developers
- Works with JSON schema validation and tooling
- Easy to understand the purpose and format of configuration values

## Practical Configuration Scenarios

### Scenario 1: Adding a New Shared Setting

If you want to add a setting that applies to all platforms:

1. Add it to [`src/Shared/appsettings.json`](/src/Shared/appsettings.json):
   ```json
   {
     "MyNewFeature": {
       "EnabledByDefault": true,
       "EnabledByDefault_Comment": "Controls whether the new feature is enabled for all platforms"
     }
   }
   ```

2. This setting will automatically be available in:
   - All client platforms (Web, MAUI, Windows)
   - Server projects (API, Web)

3. Override it for specific environments or platforms if needed

### Scenario 2: Platform-Specific Override

If you need different values for different platforms:

1. Define the base value in [`src/Shared/appsettings.json`](/src/Shared/appsettings.json)
2. Override in platform-specific files:
   - For MAUI: [`src/Client/Boilerplate.Client.Maui/appsettings.json`](/src/Client/Boilerplate.Client.Maui/appsettings.json)
   - For Web: [`src/Client/Boilerplate.Client.Web/appsettings.json`](/src/Client/Boilerplate.Client.Web/appsettings.json)
   - For Windows: [`src/Client/Boilerplate.Client.Windows/appsettings.json`](/src/Client/Boilerplate.Client.Windows/appsettings.json)

### Scenario 3: Environment-Specific Settings

For different values in Development vs Production:

1. Define production values in `appsettings.json`
2. Override in `appsettings.Development.json` for local development
3. Use `appsettings.Production.json` if production needs explicit overrides

Example from [`src/Shared/appsettings.Development.json`](/src/Shared/appsettings.Development.json):
```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Trace"
        }
    }
}
```

This increases logging verbosity in Development without affecting Production.

## Configuration Implementation Details

The configuration loading logic is implemented in [`IConfigurationBuilderExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IConfigurationBuilderExtensions.cs):

```csharp
public static partial class IConfigurationBuilderExtensions
{
    /// <summary>
    /// Configuration priority (Lowest to highest) =>
    /// Shared/appsettings.json
    /// Shared/appsettings.{environment}.json (If present)
    /// Client/Core/appsettings.json
    /// Client/Core/appsettings.{environment}.json (If present)
    /// ...
    /// </summary>
    public static IConfigurationBuilder AddClientConfigurations(
        this IConfigurationBuilder builder, 
        string clientEntryAssemblyName)
    {
        // Load Shared configuration
        var sharedAssembly = Assembly.Load("Boilerplate.Shared");
        configBuilder.AddJsonStream(sharedAssembly.GetManifestResourceStream(
            "Boilerplate.Shared.appsettings.json")!);
        
        // Load environment-specific Shared configuration
        var envSharedAppSettings = sharedAssembly.GetManifestResourceStream(
            $"Boilerplate.Shared.appsettings.{AppEnvironment.Current}.json");
        if (envSharedAppSettings != null)
        {
            configBuilder.AddJsonStream(envSharedAppSettings);
        }
        
        // Load Client.Core configuration
        var clientCoreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(asm => asm.GetName()?.Name is "Boilerplate.Client.Core");
        configBuilder.AddJsonStream(clientCoreAssembly.GetManifestResourceStream(
            "Boilerplate.Client.Core.appsettings.json")!);
        
        // ... Platform-specific configuration loading continues ...
    }
}
```

## Accessing Configuration in Code

### In Controllers and Services

Configuration is automatically injected via `IConfiguration`:

```csharp
public class MyController : AppControllerBase
{
    [AutoInject] private IConfiguration configuration = default!;
    
    public IActionResult GetSettings()
    {
        var serverAddress = configuration["ServerAddress"];
        var timeout = configuration.GetValue<int>("ApiTimeout");
        
        return Ok(new { serverAddress, timeout });
    }
}
```

### Using Strongly-Typed Settings Classes

The project uses settings classes (e.g., `ServerApiSettings`, `ClientCoreSettings`) that are automatically bound to configuration sections:

```csharp
public partial class ServerApiSettings : ServerSharedSettings
{
    public IdentitySettings Identity { get; set; } = default!;
    public EmailSettings Email { get; set; } = default!;
    public SmsSettings Sms { get; set; } = default!;
    // ... more settings ...
}
```

These are registered in service configuration:

```csharp
services.AddOptions<ServerApiSettings>()
    .Bind(configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Then injected where needed:

```csharp
public class EmailService
{
    [AutoInject] private IOptionsSnapshot<ServerApiSettings> settings = default!;
    
    public async Task SendEmailAsync()
    {
        var fromEmail = settings.Value.Email.DefaultFromEmail;
        // Use configuration...
    }
}
```

## Best Practices

### ✅ DO:
- **Put shared settings in `Shared/appsettings.json`** - Anything common to all platforms belongs here
- **Use environment-specific files** - Keep development settings separate from production
- **Add comments with `_Comment` suffix** - Document what settings do and their expected format
- **Use strongly-typed settings classes** - Create classes that bind to configuration sections
- **Test configuration priority** - Verify settings override correctly in different environments

### ❌ DON'T:
- **Don't duplicate settings** - If a setting applies to all platforms, don't repeat it in each platform's file
- **Don't hardcode sensitive data** - Use user secrets, environment variables, or Azure Key Vault for production secrets
- **Don't forget to add new files to projects** - Ensure new `appsettings.{environment}.json` files have `Copy to Output Directory` set correctly
- **Don't store secrets in source control** - Never commit API keys, passwords, or connection strings to Git

## Configuration and Environments

The project supports three environments:
- **Development** - Local development (uses `appsettings.Development.json`)
- **Staging** - Pre-production testing environment
- **Production** - Live production environment (uses `appsettings.Production.json`)

Environment is determined by `AppEnvironment.Current` which is set during build time based on the `APP_ENVIRONMENT` MSBuild property.

See [`Directory.Build.props`](/src/Directory.Build.props) for environment configuration and [`AppEnvironment.cs`](/src/Shared/Enums/AppEnvironment.cs) for the environment enum.

## Summary

The configuration system provides:
- ✅ **Hierarchical configuration** with clear priority rules
- ✅ **Shared base settings** that apply to all platforms
- ✅ **Platform-specific overrides** for unique requirements
- ✅ **Environment-specific configuration** for Development/Staging/Production
- ✅ **In-file documentation** using the `_Comment` pattern
- ✅ **Type-safe configuration** through strongly-typed settings classes
- ✅ **Flexible deployment** supporting various hosting scenarios

This architecture ensures that:
1. You can define a setting **once** and use it everywhere
2. You can **override** settings for specific platforms or environments when needed
3. Configuration is **well-documented** and easy to understand
4. Changes are **scoped** appropriately (shared vs. platform-specific)

---