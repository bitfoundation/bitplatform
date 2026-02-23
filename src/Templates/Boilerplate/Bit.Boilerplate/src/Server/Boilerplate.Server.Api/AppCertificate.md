# Application Certificate Management

This document explains how the application uses cryptographic certificates for security, including JWT authentication and Data Protection.

## Overview

The application uses asymmetric cryptography (public/private key pairs) for two critical security functions:

1. **JWT Token Signing & Validation** - Securely issue and verify access tokens
2. **Data Protection API** - Encrypt sensitive data at rest

## Benefits of Public/Private Key Pairs

### JWT Token Signing

| Key Type | Purpose |
|----------|---------|
| **Private Key** | Used by the **issuing server** to **sign** JWT tokens. This key must be kept secret and secure. |
| **Public Key** | Used by **any service** to **validate** JWT tokens. This key can be freely distributed. |

**Advantages:**
- Other backend services can validate tokens without needing the private key
- Compromising the public key doesn't allow token forgery
- Follows the principle of least privilege

### Data Protection API

| Key Type | Purpose |
|----------|---------|
| **Private Key** | Used to **decrypt** protected data (cookies, anti-forgery tokens, etc.) |
| **Public Key** | Used to **encrypt** data for protection |

**Advantages:**
- Consistent encryption across multiple server instances
- Survives application restarts without invalidating protected data
- Enables load-balanced deployments with shared encryption keys

## Generating Certificates

Use OpenSSL to generate the required certificate files:

```shell
# 1. Generate the private key (2048-bit RSA)
openssl genrsa -out AppCertificate.Private.pem 2048

# 2. Extract the public key from the private key
openssl rsa -in AppCertificate.Private.pem -pubout -out AppCertificate.Public.pem

# 3. Generate a self-signed X.509 certificate (valid for 1 year)
openssl req -new -x509 -key AppCertificate.Private.pem -out AppCertificate.Cert.pem -days 365 -subj "/CN=AppCertificate"
```

## OpenID Configuration Endpoint

The application exposes an OpenID Connect discovery endpoint at `/.well-known/openid-configuration`. This endpoint provides:

- **JWKS (JSON Web Key Set)** - Contains the public key for token validation
- **Issuer information** - Identifies the token issuer
- **Supported algorithms** - Lists the signing algorithms used

### Why Expose This Endpoint?

This allows **other backend services** to securely validate JWTs issued by this API without:
- Sharing the private key
- Hardcoding the public key
- Manual key distribution

The public key is automatically fetched and cached by consuming services.

## Integrating Other Backend Services

Other .NET services can validate tokens issued by this API using the following configuration:

```cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:5030";
        options.RequireHttpsMetadata = builder.Environment.IsDevelopment() is false;
        options.TokenValidationParameters = new()
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            ValidateIssuerSigningKey = true,

            RequireExpirationTime = true,

            ValidateAudience = true,
            ValidAudience = "Boilerplate",

            ValidateIssuer = true,
            ValidIssuer = "Boilerplate"
        };

        // OR

        options.TokenValidationParameters = new()
        {
            ValidateAudience = false,

            ValidateIssuer = true,
            ValidIssuer = "Boilerplate"
        };
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

### How It Works

1. The consuming service calls `/.well-known/openid-configuration` on startup
2. It retrieves the JWKS endpoint URL from the configuration
3. It fetches the public key(s) from the JWKS endpoint
4. Incoming JWTs are validated using the fetched public key
5. Keys are cached and periodically refreshed

This pattern enables a **zero-trust architecture** where services can independently verify token authenticity without sharing secrets.