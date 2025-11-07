# Stage 10: Configuration (appsettings.json)

## Overview

The Boilerplate project uses a sophisticated multi-layered configuration system built on top of ASP.NET Core's configuration framework. Each project in the solution has its own `appsettings.json` files, and the configuration system intelligently merges them based on a well-defined priority hierarchy.

## Configuration Priority Hierarchy

The configuration system loads settings in a specific order, where **later configurations override earlier ones**. This hierarchy is defined in [`IConfigurationBuilderExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IConfigurationBuilderExtensions.cs).

### Priority Order (Low → High)

```
1. Shared/appsettings.json                          (Lowest Priority - Base settings)
2. Shared/appsettings.{environment}.json            (If present)
3. Client/Core/appsettings.json
4. Client/Core/appsettings.{environment}.json       (If present)

Then, depending on the platform:

For Server.Web (Blazor Server & Pre-rendering) and Server.Api:
5. Server/appsettings.json
6. Server/appsettings.{environment}.json            (If present)
7. ASP.NET Core default sources (env vars, CLI args, etc.)

For Blazor WebAssembly:
5. Client.Web/appsettings.json
6. Client.Web/appsettings.{environment}.json        (If present)
7. Client.Web/wwwroot/appsettings.json              (If present)
8. Client.Web/wwwroot/appsettings.{environment}.json (If present)

For MAUI:
5. Client.Maui/appsettings.json
6. Client.Maui/appsettings.{environment}.json       (If present)

For Windows:
5. Client.Windows/appsettings.json
6. Client.Windows/appsettings.{environment}.json    (Highest Priority)
```

### How Override Works

If you add a setting in [`Shared/appsettings.json`](/src/Shared/appsettings.json), it applies to **all platforms** (server, web, mobile, desktop) unless explicitly overridden in a platform-specific `appsettings.json` file.

**Example:**
```json
// Shared/appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}

// Client.Maui/appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"  // Overrides "Warning" for MAUI only
    }
  }
}
```

In this example, MAUI apps will log at `Information` level, while all other platforms use `Warning`.

## Configuration Files by Project

### 1. Shared Project ([`/src/Shared/appsettings.json`](/src/Shared/appsettings.json))

This is the **base configuration** used by all projects (client and server). It contains:

- **Logging Configuration**: Default log levels for various loggers (Console, OpenTelemetry, Sentry, Application Insights, etc.)
- **Diagnostic Logger**: In-app logging configuration for troubleshooting

**Key Sections:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    },
    "DiagnosticLogger": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### 2. Client.Core Project ([`/src/Client/Boilerplate.Client.Core/appsettings.json`](/src/Client/Boilerplate.Client.Core/appsettings.json))

Contains settings shared across **all client platforms** (Web, MAUI, Windows):

- **ServerAddress**: The API server URL
- **Google reCAPTCHA Site Key**: For client-side CAPTCHA validation
- **Ad Unit Configuration**: For advertising integrations

**Example:**
```json
{
  "ServerAddress": "http://localhost:5030/",
  "ServerAddress_Comment": "If you're running Boilerplate.Server.Web project, then you can also use relative urls such as / for Blazor Server and WebAssembly",
  "GoogleRecaptchaSiteKey": "6LdMKr4pAAAAAKMyuEPn3IHNf04EtULXA8uTIVRw"
}
```

### 3. Server.Api Project ([`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json))

The **most comprehensive** configuration file containing:

- **Connection Strings**: Database, blob storage, SMTP, S3
- **AI Configuration**: OpenAI, Azure OpenAI, HuggingFace settings
- **Identity Settings**: JWT tokens, password requirements, token lifetimes
- **Authentication Providers**: Google, GitHub, Twitter, Apple, Facebook, Azure AD
- **Push Notifications**: VAPID keys, APNS, Firebase configuration
- **Hangfire**: Background job settings
- **Response Caching**: CDN and output caching settings
- **Cloudflare Integration**: API tokens and zone IDs
- **Supported App Versions**: Minimum versions for force update system
- **Trusted Origins**: CORS and security settings

**Key Sections:**

#### Connection Strings
```json
{
  "ConnectionStrings": {
    "mssqldb": "Data Source=(localdb)\\mssqllocaldb; Initial Catalog=BoilerplateDb;...",
    "smtp": "Endpoint=smtp://smtp.ethereal.email:587;UserName=...;Password=...",
    "s3": "Endpoint=http://localhost:9000;AccessKey=minioadmin;SecretKey=minioadmin;"
  }
}
```

#### Identity Configuration
```json
{
  "Identity": {
    "JwtIssuerSigningKeySecret": "VeryLongJWTIssuerSiginingKeySecret...",
    "Issuer": "Boilerplate",
    "Audience": "Boilerplate",
    "BearerTokenExpiration": "0.00:05:00",
    "RefreshTokenExpiration": "14.00:00:00",
    "Password": {
      "RequireDigit": "false",
      "RequiredLength": "6"
    }
  }
}
```

#### AI Configuration
```json
{
  "AI": {
    "ChatOptions": {
      "Temperature": 0
    },
    "OpenAI": {
      "ChatModel": "gpt-4.1-mini",
      "ChatApiKey": null,
      "ChatEndpoint": "https://models.inference.ai.azure.com"
    }
  }
}
```

### 4. Server.Web Project ([`/src/Server/Boilerplate.Server.Web/appsettings.json`](/src/Server/Boilerplate.Server.Web/appsettings.json))

Contains Blazor-specific settings:

- **WebAppRender**: Blazor mode (Server, WebAssembly, Auto) and pre-rendering configuration
- **TrustedOrigins**: CORS configuration
- **Response Caching**: Output and CDN edge caching settings

**Example:**
```json
{
  "WebAppRender": {
    "BlazorMode": "BlazorServer",
    "BlazorMode_Comment": "BlazorServer, BlazorWebAssembly and BlazorAuto.",
    "PrerenderEnabled": false
  }
}
```

### 5. Client.Web Project ([`/src/Client/Boilerplate.Client.Web/appsettings.json`](/src/Client/Boilerplate.Client.Web/appsettings.json))

WebAssembly-specific settings:

- **VAPID Public Key**: For web push notifications (private key is server-side only)

### 6. Client.Maui Project ([`/src/Client/Boilerplate.Client.Maui/appsettings.json`](/src/Client/Boilerplate.Client.Maui/appsettings.json))

MAUI-specific settings:

- **WebAppUrl**: Used when the mobile app needs to generate web links (e.g., email confirmation)

```json
{
  "WebAppUrl": null,
  "WebAppUrl_Comment": "When the maui app sends a request to the API server, and the API server and web app are hosted on different URLs, the origin of the generated links (e.g., email confirmation links) will depend on `WebAppUrl` value."
}
```

## Environment-Specific Configuration

Each `appsettings.json` can have environment-specific overrides:

- `appsettings.Development.json` - Development environment
- `appsettings.Production.json` - Production environment
- `appsettings.Staging.json` - Staging environment (if needed)

The environment is determined by the `ASPNETCORE_ENVIRONMENT` (server) or `APP_ENVIRONMENT` (client) variables.

**Example:**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"  // More verbose in Development
    }
  }
}
```

## Configuration Matrix

Here's a visual representation of which configuration files affect each platform:

| Configuration File | Server.Api | Server.Web | Client.Web (WASM) | Client.Maui | Client.Windows |
|-------------------|------------|------------|-------------------|-------------|----------------|
| `Shared/appsettings.json` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Shared/appsettings.{env}.json` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Client.Core/appsettings.json` | ❌ | ✅ | ✅ | ✅ | ✅ |
| `Client.Core/appsettings.{env}.json` | ❌ | ✅ | ✅ | ✅ | ✅ |
| `Server.Api/appsettings.json` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `Server.Web/appsettings.json` | ❌ | ✅ | ❌ | ❌ | ❌ |
| `Client.Web/appsettings.json` | ❌ | ❌ | ✅ | ❌ | ❌ |
| `Client.Maui/appsettings.json` | ❌ | ❌ | ❌ | ✅ | ❌ |
| `Client.Windows/appsettings.json` | ❌ | ❌ | ❌ | ❌ | ✅ |

## JSON Comments with `__Comment` Convention

Since JSON doesn't natively support comments, this project uses a special convention:

**Any key ending with `__Comment` is treated as a comment and ignored by the configuration system.**

**Example:**
```json
{
  "ServerAddress": "http://localhost:5030/",
  "ServerAddress_Comment": "This is a comment explaining the ServerAddress setting",
  "Identity": {
    "BearerTokenExpiration": "0.00:05:00",
    "BearerTokenExpiration_Comment": "Format: D.HH:mm:ss"
  }
}
```

This allows you to:
- Document complex settings directly in the configuration file
- Provide usage examples and format specifications
- Explain the purpose of nullable or optional settings

## Reading Configuration in Code

### Server-Side (API/Web)

Use ASP.NET Core's built-in dependency injection:

```csharp
public class MyController : AppControllerBase
{
    [AutoInject] private IConfiguration configuration = default!;

    public IActionResult GetSetting()
    {
        var serverAddress = configuration["ServerAddress"];
        var jwtSecret = configuration["Identity:JwtIssuerSigningKeySecret"];
        return Ok(serverAddress);
    }
}
```

Or use strongly-typed options pattern:

```csharp
public class IdentitySettings
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
}

// In Startup/Program.cs
services.Configure<IdentitySettings>(configuration.GetSection("Identity"));

// In your class
public class MyService
{
    [AutoInject] private IOptions<IdentitySettings> identitySettings = default!;
    
    public void UseSettings()
    {
        var issuer = identitySettings.Value.Issuer;
    }
}
```

### Client-Side (Blazor)

```csharp
public partial class MyComponent : AppComponentBase
{
    [AutoInject] private IConfiguration configuration = default!;

    protected override async Task OnInitAsync()
    {
        var serverAddress = configuration["ServerAddress"];
        var recaptchaKey = configuration["GoogleRecaptchaSiteKey"];
    }
}
```

## Best Practices

### 1. Use the Right Configuration File

- **Shared settings** (used by all platforms) → `Shared/appsettings.json`
- **Client-only settings** (all client platforms) → `Client.Core/appsettings.json`
- **Server-only settings** → `Server.Api/appsettings.json` or `Server.Web/appsettings.json`
- **Platform-specific overrides** → Platform-specific `appsettings.json`

### 2. Secrets Management

**⚠️ NEVER commit sensitive data to source control!**

For sensitive configuration (API keys, passwords, connection strings):

- **Development**: Use User Secrets or environment variables
  ```bash
  dotnet user-secrets set "Identity:JwtIssuerSigningKeySecret" "YourSecretKey"
  ```
  
- **Production**: Use environment variables, Azure Key Vault, AWS Secrets Manager, or similar services

### 3. Environment Variables Override Configuration

ASP.NET Core configuration allows environment variables to override `appsettings.json` values using double underscores

### 4. Document Your Settings

Always add `__Comment` keys to explain:
- The purpose of the setting
- Valid values or formats
- Where to obtain values (e.g., API keys)

```json
{
  "GoogleRecaptchaSecretKey": "6LdMKr4pAAAAANvngWNam...",
  "GoogleRecaptchaSecretKey_Comment": "Create one at https://console.cloud.google.com/security/recaptcha/create for Web Application Type"
}
```

### 5. Use JSON Schema

All `appsettings.json` files include a schema reference for IntelliSense:

```json
{
  "$schema": "https://json.schemastore.org/appsettings.json"
}
```

This provides autocomplete and validation in VS Code and Visual Studio.

## Common Configuration Scenarios

### Scenario 1: Changing the API Server Address

To change the API address for all client platforms:

**[`/src/Client/Boilerplate.Client.Core/appsettings.json`](/src/Client/Boilerplate.Client.Core/appsettings.json)**
```json
{
  "ServerAddress": "https://api.yourcompany.com/"
}
```

### Scenario 2: Different Log Levels per Environment

**[`/src/Shared/appsettings.json`](/src/Shared/appsettings.json)** (Base)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

**[`/src/Shared/appsettings.Development.json`](/src/Shared/appsettings.Development.json)** (Override)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Scenario 3: Platform-Specific Settings

Different server addresses for MAUI vs Web:

**[`/src/Client/Boilerplate.Client.Core/appsettings.json`](/src/Client/Boilerplate.Client.Core/appsettings.json)** (Default)
```json
{
  "ServerAddress": "https://api.yourcompany.com/"
}
```

**[`/src/Client/Boilerplate.Client.Maui/appsettings.json`](/src/Client/Boilerplate.Client.Maui/appsettings.json)** (MAUI Override)
```json
{
  "ServerAddress": "https://mobile-api.yourcompany.com/"
}
```

### Scenario 4: .NET Aspire Override

When running with .NET Aspire (via `Boilerplate.Server.AppHost`), Aspire automatically overrides certain connection strings at runtime. The `appsettings.json` files contain the default values used when NOT running through Aspire.

From [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):
```json
{
  "ConnectionStrings": {
    "Aspire__Comment": "Running Boilerplate.Server.AppHost `overrides` the following connection strings at runtime.",
    "mssqldb": "Data Source=(localdb)\\mssqllocaldb; ..."
  }
}
```

## Troubleshooting

### Configuration Not Taking Effect

1. **Check the priority order** - A higher-priority file might be overriding your setting
2. **Verify the environment** - Ensure `ASPNETCORE_ENVIRONMENT` or `APP_ENVIRONMENT` is set correctly
3. **Check for typos** - Configuration keys are case-sensitive
4. **Restart the application** - Configuration is loaded at startup

### Finding Which File Contains a Setting

Use your IDE's "Find in Files" feature to search across all `appsettings*.json` files:

**VS Code:**
- Press `Ctrl+Shift+F` (Windows/Linux) or `Cmd+Shift+F` (Mac)
- Search for the setting name
- Filter by `appsettings*.json`

### Debugging Configuration Values

Add this code to see the final resolved configuration:

```csharp
var allSettings = configuration.AsEnumerable();
foreach (var setting in allSettings)
{
    logger.LogInformation($"{setting.Key} = {setting.Value}");
}
```

## Summary

The Boilerplate project's configuration system provides:

✅ **Hierarchical Configuration** - Base settings with platform-specific overrides  
✅ **Environment-Aware** - Different settings for Development, Staging, Production  
✅ **Centralized & DRY** - Shared settings avoid duplication  
✅ **Flexible** - Easy to override at any level  
✅ **Documented** - Built-in comment convention  
✅ **Secure** - Support for secrets management  

Understanding this configuration hierarchy is crucial for effectively customizing and deploying your Boilerplate application across different platforms and environments.
