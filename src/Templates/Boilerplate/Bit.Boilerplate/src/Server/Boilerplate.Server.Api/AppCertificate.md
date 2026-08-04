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

> **⚠ `AppCertificate.key` is a production secret. Do not commit the one you generate.**
> The file ships with the template as a *development* default and is therefore tracked in git, so the commands above
> overwrite a tracked file and `git add -A` will take your real key with it. Anyone who can read the repository can
> then mint a valid token for any user - including the global administrator - offline, and decrypt the Data Protection
> key ring; there is no key id, no revocation list and no server-side state that would stop them. Deliver the key out
> of band instead (a CI/CD secret, Azure Key Vault, AWS Secrets Manager, the OS certificate store - see *Why PEM over
> PFX* below), and keep it out of the repository, e.g. `git rm --cached AppCertificate.key` plus a `.gitignore` entry.

## Rotating the Certificate

The app trusts **more than one** certificate at a time, so a rotation does not sign anybody out:

- `AppCertificate.crt` / `.key` is the **active** certificate - the only one that writes anything. New tokens are
  signed with it and the Data Protection key ring is encrypted to it.
- any `AppCertificate.{name}.crt` / `.key` pair sitting next to it is a **retired** certificate, which only ever
  reads: it validates tokens and decrypts existing key ring entries, and never signs or encrypts anything new.

Each certificate's thumbprint is its `kid`, in the JWT header and in the published JWKS alike, so a validator - this
app, a sibling service, or anything reading `/.well-known/jwks` - picks the right key on its own.

### The procedure

```shell
# 1. Retire the current certificate by renaming both files (any name you like).
mv AppCertificate.crt AppCertificate.old.crt
mv AppCertificate.key AppCertificate.old.key

# 2. Generate the new pair under the active name (see "Generating Certificates" above).
openssl genrsa -out AppCertificate.key 3072
openssl req -new -x509 -key AppCertificate.key -out AppCertificate.crt -days 365 -subj "/CN=AppCertificate" -sha256
```

Deploy. From that moment new tokens are signed with the new key, tokens already in the wild keep validating against
the retired one, and the existing Data Protection key ring is still readable.

### When the retired pair can be deleted

Two independent clocks have to run out, not one:

1. **Tokens** - every access and refresh token signed by it must have expired, i.e. at least
   `Identity:RefreshTokenExpiration` (14 days by default) after the rotation.
2. **The Data Protection key ring** - every persisted key encrypted to it must be gone. This is the longer and less
   obvious one: the ring keeps old keys around to decrypt payloads that are still in circulation, so deleting the
   certificate early makes those rows permanently unreadable. Wait until no key protected by the retired certificate
   remains in the `DataProtectionKeys` table, or re-encrypt the ring to the active certificate first.

Until both have elapsed the old private key can still mint tokens this app accepts - so if you are rotating *because
the old key leaked*, skip the overlap entirely, delete the retired pair immediately and accept the sign-out and the
key-ring reset.

> Both files are copied to the output directory by the `AppCertificate.*` glob in the csproj, so a retired pair needs
> no project change. Every instance behind a load balancer must carry the same set.

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

**Note:** While the current implementation uses **PEM files** for maximum compatibility with shared hosting, you can easily switch to other sources. By modifying a single line in `AppCertificateService.LoadCertificate`, you can load the certificate from:
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

- **`jwks_uri`** - Points at `/.well-known/jwks`, which carries the public key for token validation
- **`issuer`** - Identifies the token issuer

The discovery document itself is deliberately minimal - those two fields and nothing else. The JWKS carries one key
per trusted certificate, each with its own `kid` (the certificate's thumbprint), `use: "sig"` and `alg: "RS256"`.
Pin the algorithm on the consuming side anyway (`ValidAlgorithms`, as in the snippet below) rather than trusting the
one the document advertises.

### Why Expose This Endpoint?

This allows **other backend (micro) services / resource servers** to securely validate JWTs issued by this API without:
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
        // The address of the host that serves the API
        options.Authority = "http://localhost:5030";
        options.RequireHttpsMetadata = builder.Environment.IsDevelopment() is false;
        options.TokenValidationParameters = new()
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],

            RequireExpirationTime = true,

            ValidateAudience = true,
            ValidAudience = "Boilerplate",

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
