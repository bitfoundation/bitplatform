# Keycloak Realm Configuration

## Overview

This directory contains the Keycloak realm configuration file (`dev-realm.json`) used by the Boilerplate. Keycloak is an open-source identity and access management solution that provides enterprise-grade authentication and authorization features. Using it in your own application is optional - but with .NET Aspire enabled the container is always started, so that the Enterprise SSO sign-in option works out of the box.

## What is Keycloak?

Keycloak is a free, open-source identity server that provides:

- **Centralized User Management**: Manage users, credentials, roles, and groups from a single location
- **Single Sign-On (SSO)**: Allow users to authenticate once and access multiple applications
- **Identity Brokering**: Integrate with external identity providers (LDAP, Active Directory, etc.)
- **Fine-Grained Authorization**: Define and enforce complex access control policies
- **Standards-Based**: Supports OpenID Connect, OAuth 2.0, and SAML 2.0 protocols

## Keycloak in .NET Aspire

When you run the Boilerplate project with .NET Aspire enabled (default configuration), Keycloak is automatically started as a containerized service. This provides a complete identity server for development and testing without any manual setup.

The Keycloak container is configured by `AddKeycloak()` in
`Infrastructure/Extensions/IDistributedApplicationBuilderExtensions.cs`, which `Program.cs` calls:

```csharp
var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume();

if (builder.ExecutionContext.IsRunMode)
{
    keycloak.WithRealmImport("./Infrastructure/Realms");
}
```

**This realm is for development only.** It seeds accounts with well-known passwords and an OAuth client whose secret is
in this repository, which is why the import above is gated on run mode and never reaches `aspire publish` output.

For more information, checkout .docs/07- ASP.NET Core Identity - Authentication & Authorization.md