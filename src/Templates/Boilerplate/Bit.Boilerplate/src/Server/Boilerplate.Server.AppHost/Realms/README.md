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

| Username | Email | Password | Role | Description |
|----------|-------|----------|------|-------------|
| `alice` | AliceSmith@email.com | `alice` | Standard User | Basic realm user with default permissions |
| `bob` | BobSmith@email.com | `bob` | Standard User | Basic realm user with default permissions |
| `test` | test@bitplatform.dev | `123456` | Super Admin | Administrative user with elevated privileges |

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
- **`api`**: Custom API access scope for protected resources
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

## Security Notes

⚠️ **Important**: This configuration is designed for **development and demonstration** purposes only.

For production deployments:
- Change all default passwords
- Restrict redirect URIs to specific domains
- Use proper SSL certificates
- Configure appropriate session timeouts
- Enable additional security features (2FA, account lockout policies)
- Review and minimize granted permissions
- Use strong client secrets and rotate them regularly
- Enable SMTP server