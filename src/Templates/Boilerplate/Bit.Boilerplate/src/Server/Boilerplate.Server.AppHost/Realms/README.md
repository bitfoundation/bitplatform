# Keycloak Realm Configuration

This directory contains Keycloak realm configuration files that set up authentication and authorization for the Boilerplate application.

## Overview

The `demo-realm.json` file defines a Keycloak realm with pre-configured clients, users, roles, and security settings. This allows the Boilerplate application to demonstrate enterprise Single Sign-On (SSO) functionality using Keycloak as an OpenID Connect identity provider.

## Realm Features

### 🔐 Authentication & Security
- **Realm Name**: `demo`
- **SSL Required**: External connections only
- **Brute Force Protection**: Enabled with progressive lockout
- **Session Management**: Configurable token lifespans and session timeouts
- **Password Reset**: Enabled for user self-service

### 🌍 Internationalization Support
The realm supports multiple languages for localized user interfaces:
- **English** (en) - Default
- **Dutch** (nl)
- **Persian/Farsi** (fa)
- **Swedish** (sv)
- **Hindi** (hi)
- **Chinese** (zh)
- **Spanish** (es)
- **French** (fr)
- **Arabic** (ar)
- **German** (de)

### 👥 Pre-configured Users

| Username | Email | Password | Roles |
|----------|-------|----------|-------|
| `alice` | AliceSmith@email.com | `alice` | demo |
| `bob` | BobSmith@email.com | `bob` | demo |
| `test` | test@bitplatform.dev | `123456` | s-admin |

### Application Roles

The realm includes custom application roles for authorization:

- **`s-admin`**: Super Admin role - Full administrative access to the Boilerplate application
  - **Claims**: Automatically assigned all features via application logic
  
- **`demo`**: Demo role - Standard user access for demonstration purposes
  - **Claims**:
    - `mx-p-s`: `-1` (Unlimited privileged sessions)
    - `feat`: `3.0`, `3.1`, `4.0` (Dashboard, ManageProductCatalog, and ManageTodo features)

**Note**: The `s-admin` role has nothing to do with Keycloak admin privileges, it is specific to the Boilerplate app.
See `AppRoles.cs` and `AppFeatures.cs` for complete details about application roles and features.

### Role Claims Mapping

Role attributes are automatically mapped to JWT token claims via protocol mappers:

- **Max Privileged Sessions (`mx-p-s`)**: Controls the maximum number of concurrent privileged sessions
  - `-1` = Unlimited sessions
  - Positive number = Maximum session limit
  
- **Features (`feat`)**: Array of feature flags that grant access to specific application features
  - `3.0` = `AppFeatures.AdminPanel.Dashboard`
  - `3.1` = `AppFeatures.AdminPanel.ManageProductCatalog`
  - `4.0` = `AppFeatures.Todo.ManageTodo`
  - See `AppFeatures.cs` for all available features

These role-specific claims are included in access tokens, ID tokens, and userinfo endpoints via the `roles` scope, enabling fine-grained authorization in the Boilerplate application.

### 🔗 Client Configuration

#### Interactive Confidential Client
- **Client ID**: `interactive.confidential`
- **Client Secret**: `secret`
- **Protocol**: OpenID Connect
- **Flow Support**: Authorization Code Flow with PKCE
- **Redirect URIs**: Wildcard (`*`) for development (should be restricted in production)

#### Supported Scopes
- **`openid`**: Core OpenID Connect identity
- **`profile`**: User profile information (name, username)
- **`email`**: Email address and verification status
- **`api`**: Sample API scope
- **`offline_access`**: Refresh token support

### ⚙️ Token Configuration

| Setting | Value | Description |
|---------|-------|-------------|
| Access Token Lifespan | 5 minutes | Short-lived for security |
| SSO Session Idle Timeout | 30 minutes | Auto-logout on inactivity |
| SSO Session Max Lifespan | 10 hours | Maximum session duration |
| Offline Session Idle Timeout | 30 days | Refresh token validity |

## Integration with Boilerplate

The Keycloak realm integrates seamlessly with the Boilerplate application through:

1. **Aspire Integration**: Automatically starts Keycloak container with realm import
2. **Environment Configuration**: Connects via `KEYCLOAK_HTTP` environment variable
3. **OpenID Connect**: Uses standard OIDC flows for authentication
4. **Token Validation**: JWT tokens validated against Keycloak's public keys
5. **User Mapping**: Claims automatically mapped to application user properties

## Development Usage

When running the Boilerplate application with Aspire:

1. **Keycloak starts automatically** on port 8080
2. **Realm is imported** from this JSON file
3. **Users are immediately available** for testing
4. **Admin Console**: Access at `http://localhost:8080` (admin/P@ssw0rd)
Note: Keycloak's admin panel opens master realm by default; switch to `demo` realm to manage users.

## Security Notes

⚠️ **Important**: This configuration is designed for **development and demonstration** purposes only.

For production deployments:
- Change all default passwords
- Restrict redirect URIs to specific domains
- Use proper SSL certificates
- Configure appropriate session timeouts
- Enable additional security features for admin user (e.g., 2FA)
- Review and minimize granted permissions
- Use strong client secrets and rotate them regularly
- Enable SMTP server