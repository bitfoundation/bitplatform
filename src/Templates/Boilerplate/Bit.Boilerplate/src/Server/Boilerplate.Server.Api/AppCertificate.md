# Application Certificate Management

This document explains how the application uses cryptographic certificates for security, including JWT authentication and Data Protection.

## Overview

The application uses asymmetric cryptography (public/private key pairs) for two critical security functions:

1. **JWT Token Signing & Validation** - Securely issue and verify access tokens
2. **Data Protection API** - Encrypt sensitive data (cookies, anti-forgery tokens, etc.) at rest

## Benefits of Public/Private Key Pairs

### JWT Token Signing

| Key Type | Purpose |
|----------|---------|
| **Public Key** | Used by **any service** to **validate** JWT tokens. This key can be freely distributed. |
| **Private Key** | Used by the **issuing server** to **sign** JWT tokens. This key must be kept secret and secure. |

**Advantages:**
- Other backend services can validate tokens without needing the private key
- Compromising the public key doesn't allow token forgery
- Follows the principle of least privilege

### Data Protection API

| Key Type | Purpose |
|----------|---------|
| **Public Key** | Used to protect data protection API keys by encrypting them. |
| **Private Key** | Used to decrypt the protected keys when the application starts. |

**Advantages:**
- Consistent encryption across multiple server instances
- Survives application restarts without invalidating protected data
- Enables load-balanced deployments with shared encryption keys

## Generating Certificates

Use OpenSSL to generate the required certificate files:

```shell
# 1. Generate the private key (3072-bit RSA)
openssl genrsa -out AppCertificate.key 3072

# 2. Generate a self-signed X.509 certificate (valid for 1 year)
openssl req -new -x509 -key AppCertificate.key -out AppCertificate.crt -days 365 -subj "/CN=AppCertificate" -sha256
```

## Why RSA 3072 + SHA-256?

The application uses **RSA 3072** paired with **SHA-256** for the following reasons:

- **The "Weakest Link" Rule:** Security is only as strong as its weakest component. RSA 3072 & SHA-256 provide **128 bits** of security strength. Using a stronger hash (like SHA-512 with 256 bits security strength) adds no real security benefit because the 3072-bit key remains the limiting factor.
- **Performance Balance:** Moving to RSA 4096 and SHA-512 would make cryptographic operations (signing and decryption) **5 to 7 times slower** without providing a meaningful security upgrade for standard production environments.
- **Industry Standard:** RSA 3072 + SHA-256 is the current "Golden Standard" recommended by NIST for secure applications until at least 2030.

## Why RSA over HMAC?
HMAC algorithms (like HMAC-SHA512) are **Symmetric**, meaning they require a shared secret. This is unsuitable for our architecture, which requires **Asymmetric** (Public/Private) keys so that external services can validate tokens without having the power to issue them.

## Why RSA over ECDSA?
While ECDSA is highly efficient for JWT signing, **ECDSA** does not support **Encryption/Decryption**. Since our Data Protection layer requires encryption, choosing ECDSA would force us to manage two separate key pairs (4 files and 4 commands). RSA provides a unified solution for both signing and encryption with a single key pair.

## Why PEM over PFX?

By default, the system uses **PEM files** (`.crt` and `.key`) instead of the bundled **PFX** format:

- **Shared Hosting Compatibility:** PFX loading often fails in restricted shared hosting environments because it tries to interact with the OS Certificate Store or write to temporary system folders. PEM loading is **memory-only**, making it "infrastructure-agnostic."
- **Simplicity:** PEM files are easier to manage in Linux-based containers and CI/CD pipelines.

**Note:** While the current implementation uses **PEM files** for maximum compatibility with shared hosting, you can easily switch to other sources. By modifying a single line in `AppCertificateService.GetAppCertificate`, you can load the certificate from:
- A password-protected **PFX** file.
- **Azure Key Vault** or **AWS Secrets Manager**.
- The local **OS Certificate Store**.

#### How to generate PFX files (Optional):
```powershell
$cert = New-SelfSignedCertificate -Subject "AppCertificate" -KeyLength 3072 -HashAlgorithm "SHA256" -NotAfter (Get-Date).AddYears(1)
Export-PfxCertificate -cert $cert.PSPath -FilePath "AppCertificate.pfx" -Password (ConvertTo-SecureString -String "USE_STRONG_P@SSW0RD_HERE" -Force -AsPlainText)
```

This architecture ensures that your security logic remains decoupled from your key storage strategy.

## OpenID Configuration Endpoint

The application exposes an OpenID Connect discovery endpoint at `/.well-known/openid-configuration`. This endpoint provides:

- **JWKS (JSON Web Key Set)** - Contains the public key for token validation
- **Issuer information** - Identifies the token issuer
- **Supported algorithms** - Lists the signing algorithms used

### Why Expose This Endpoint?

This allows **other backend (micro) services** to securely validate JWTs issued by this API without:
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