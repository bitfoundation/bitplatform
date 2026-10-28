# Application Certificate Management

This document explains how the application uses cryptographic certificates for security, including JWT authentication and Data Protection.

## Overview

The application uses asymmetric cryptography (public/private key pairs) for these security functions:

1. **JWT Token Signing & Validation** - Securely issue and verify access tokens
2. **Data Protection API** - Encrypt sensitive data (cookies, anti-forgery tokens, etc.) at rest
3. **One-time codes** - The 6-digit sign-in, reset, step-up and 2FA codes are keyed on a secret derived from the active
   certificate's private key (See `AppUserManager`), so the security stamp inside a token is not enough to compute
   them. Rotating the active certificate invalidates the codes sent in the last few minutes, and a leaked private key
   brings that ability back to whoever also reads a token.

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

`bit new` gives every project a development certificate of its own. For a deployed app, create one with the bit CLI:

```shell
bit keys certificate --production -o <a folder outside the repository>
```

It writes `AppCertificate.crt` and `AppCertificate.key`: RSA 3072 with SHA-256, `OU=Production`, valid for a year. It refuses a
folder where git would commit the key.
<!--#if (pipeline == "GitHub")-->

With the GitHub pipeline, `bit pipeline` creates one for each environment instead and keeps it in that environment's
`APP_CERTIFICATE` secret, and the deploy job writes it next to the app before the app or its migrations start.
<!--#endif-->

> **⚠ `AppCertificate.key` is a production secret. Do not commit the one you generate.**
> The file ships with the template as a *development* default and is therefore tracked in git, so a production key
> written over it goes along with the next `git add -A`. Anyone who can read the repository can
> then mint a valid token for any user - including the global administrator - offline, and decrypt the Data Protection
> key ring; there is no key id, no revocation list and no server-side state that would stop them. Deliver the key out
> of band instead (a CI/CD secret, Azure Key Vault, AWS Secrets Manager, the OS certificate store - see *Why PEM over
> PFX* below), and keep it out of the repository, e.g. `git rm --cached AppCertificate.key` plus a `.gitignore` entry.

The certificate the template ships has the subject `CN=AppCertificate, OU=Development`, and so does the one `bit new`
generates for each project on the developer's machine. Both are development certificates: outside the Development
environment the app refuses to start while any certificate with `OU=Development` is loaded. Production needs one
generated as above, with `OU=Production`, and delivered out of band.

`bit keys certificate --list -o <folder>` shows the certificates in a folder: which one is active, each one's key id
and when it expires.

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
bit keys certificate --rotate -o <the folder with your certificates>
```

It renames the active pair to `AppCertificate.<date>.crt` / `.key`, which retires it, and creates a new active pair with
the same subject.
<!--#if (pipeline == "GitHub")-->

With the GitHub pipeline, `bit pipeline --rotate-certificate` does the same in each environment: the new certificate
goes into `APP_CERTIFICATE` and the old one into `APP_CERTIFICATE_RETIRED`, which the deploy job writes as
`AppCertificate.retired.crt` / `.key`. GitHub doesn't give a secret back, so bit keeps a copy of every certificate it
creates under `~/.bitplatform/keys`; on another machine it asks for the old certificate's PEM file.
<!--#endif-->

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
<!--#if (pipeline == "GitHub")-->

With the GitHub pipeline, deleting the retired pair means deleting the environment's `APP_CERTIFICATE_RETIRED` secret,
plus `AppCertificate.retired.crt` and `.key` on the server, since a deploy leaves the files it doesn't bring in place.
<!--#endif-->

> Both files are copied to the output directory by the `AppCertificate.*` glob in the csproj, so a retired pair needs
> no project change. Every instance behind a load balancer must carry the same set.

## Why RSA 3072 + SHA-256?

The application uses **RSA 3072** paired with **SHA-256** for the following reasons:

- **The "Weakest Link" Rule:** Security is only as strong as its weakest component. RSA 3072 & SHA-256 provide **128 bits** of security strength. Using a stronger hash (like SHA-512 with 256 bits security strength) adds no real security benefit because the 3072-bit key remains the limiting factor.
- **Performance Balance:** Moving to RSA 4096 and SHA-512 would make cryptographic operations (signing and decryption) **5 to 7 times slower** without providing a meaningful security upgrade for standard production environments.
- **Matches a published security strength:** NIST SP 800-57 Part 1 Rev. 5 puts a 3072-bit RSA key and SHA-256 in the same **128-bit security-strength** category, and lists that category as acceptable for the foreseeable future - unlike the 112-bit one (RSA 2048), which it has already scheduled for retirement. NIST does not endorse a single "standard" algorithm pair for applications, so treat this as *"the smallest key that clears the 128-bit bar"*, not as a mandate.

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
```shell
bit keys certificate --production --pfx -o <a folder outside the repository>
```

It also writes `AppCertificate.pfx`, protected by a password it creates and shows once.

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

            // No ValidIssuer: a token names the origin it was minted at (https://api.myproject.com, or wherever the
            // api was reached), which is also what the discovery document publishes - so JwtBearer takes it from
            // there. Pin ValidIssuers only when the api is reachable at more than one origin.
            ValidateIssuer = true
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
