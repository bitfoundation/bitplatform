# Stage 7: ASP.NET Core Identity - Authentication & Authorization

This stage explains **how identity works in the Boilerplate** - the ideas and the decisions behind them, not a
line-by-line tour. Once you have the model in your head, the code reads easily; every section links to the files
where the real logic lives.

The system is built on ASP.NET Core Identity and adds: JWT issuance the app fully controls, server-side session
tracking, a role + permission model, one-time tokens, external providers, and (optionally) multi-tenancy.

> WebAuthn, passkeys and passwordless sign-in are a separate topic - see
> [Stage 24](/.docs/24-%20WebAuthn%20and%20Passwordless%20Authentication%20(Advanced).md).

**Contents:** [Two ways in](#1-there-are-only-two-ways-in) · [Tokens & sessions](#2-tokens-and-sessions) ·
[Authorization](#3-authorization) · [Multi-tenancy](#4-multi-tenancy) · [One-time tokens](#5-one-time-tokens) ·
[External providers](#6-external-providers) · [Keycloak](#7-keycloak) · [Try it](#8-try-it-yourself)

---

## 1. There are only two ways in

Everything the UI offers reduces to one of these:

1. **Identifier + password** - identifier being a username, email or phone number.
2. **Identifier + OTP** - a 6-digit code delivered by email or SMS. Delivered as a clickable email link, this is
   what users call a "magic link".

Every other sign-in button - Google, GitHub, a passkey, a push notification code - finishes by generating an OTP
and performing an **automatic OTP sign-in** behind the scenes.

That indirection is deliberate. Because every path converges on the same final step:

- 2FA is honoured no matter how the user started signing in,
- sessions, lockout and claim issuance behave identically everywhere,
- and there is a single pipeline to reason about instead of one per provider.

> The username field exists but is commented out in the shipped sign-in/sign-up components, because most apps want
> email or phone. Re-enable it if your business needs it.

After the first step succeeds, **two-factor authentication** may still be required, depending on the user's own
settings.

---

## 2. Tokens and sessions

### The app mints its own JWTs

Authentication is stateless: the client sends `Authorization: Bearer <access token>` on every call.

- **Access token** - short-lived (5 minutes by default). It carries the user's roles and permissions, so
  authorizing a request needs no database round-trip.
- **Refresh token** - long-lived (14 days by default). It is exchanged for a fresh pair at
  `IdentityController.Refresh`, and is rotated on every use.

ASP.NET Core Identity's built-in bearer tokens are opaque, so the template replaces them:
[`AppBearerTokenOptionsConfigurator`](/src/Server/Boilerplate.Server.Api/Features/Identity/Services/AppBearerTokenOptionsConfigurator.cs)
swaps in [`AppJwtSecureDataFormat`](/src/Server/Boilerplate.Server.Api/Features/Identity/Services/AppJwtSecureDataFormat.cs),
which writes and validates ordinary JWTs. That is what lets the client read claims out of the token, and what lets
you hand the token to another service.

A few properties worth knowing:

- **Signing is asymmetric (RS256).** The private key comes from the app certificate loaded by
  [`AppCertificateService`](/src/Server/Boilerplate.Server.Api/Infrastructure/Services/AppCertificateService.cs);
  there is no signing secret in configuration. Generate your own before production - see
  [`AppCertificate.md`](/src/Server/Boilerplate.Server.Api/AppCertificate.md). The app refuses to start outside
  Development while the shipped self-signed certificate is still in use.
- **The two token classes are not interchangeable.** The refresh token is issued under its own audience, and each
  validator accepts only its own. Without that they would differ solely in lifetime, and a stolen refresh token
  would work as an API credential for two weeks.
- **Feature claims for admins are derived at read time**, not stored in the token - see
  [Authorization](#3-authorization).

### Sessions are tracked server-side

JWTs are stateless, but the app still keeps a
[`UserSession`](/src/Server/Boilerplate.Server.Api/Features/Identity/Models/UserSession.cs) row per signed-in
device, recording IP, approximate location, device and platform, app version, culture, and the SignalR connection
id used to push notifications to that device.

Its id travels in both tokens as the `s-id` claim, which is what makes **session revocation** possible: delete the
row and that device can no longer refresh. Users manage their own sessions from the Settings page.

**Privileged sessions** cap how many devices get full access at once (3 by default,
`Identity:MaxPrivilegedSessionsCount`, overridable per user with the `mx-p-s` claim; `-1` means unlimited). Older
sessions stay signed in but lose access to pages guarded by `PRIVILEGED_ACCESS`. Once a session is privileged it
stays privileged. The Settings page is deliberately *not* guarded by this policy - otherwise a user could never
reach the screen that lets them revoke a session and free a slot.

### Configuration

Everything lives under the `Identity` section of
[`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):

| Setting | Meaning |
|---|---|
| `Issuer` / `Audience` | Written into, and validated on, every token |
| `BearerTokenExpiration` | Access token lifetime (`D.HH:mm:ss`) |
| `RefreshTokenExpiration` | Refresh token lifetime |
| `*TokenLifetime` | How long each kind of one-time token stays valid (email, phone, reset password, 2FA, OTP) |
| `MaxPrivilegedSessionsCount` | Default privileged-session cap per user |
| `Password` | Standard ASP.NET Core Identity complexity rules |
| `SignIn:RequireConfirmedAccount` | Whether a confirmed email/phone is required to sign in |

Identity also brings the usual protections along, and the template does not weaken them: PBKDF2/HMAC-SHA512
password hashing, security stamps that invalidate outstanding tokens when credentials change, concurrency stamps,
and account lockout with backoff. reCAPTCHA protects sign-up when the `captcha` template option is enabled.

---

## 3. Authorization

Two mechanisms, both delivered through the token:

- **Roles** - coarse grouping (`g-admin`, `t-admin`, `demo`). A user can hold several.
- **Permissions ("features")** - fine-grained capabilities, stored as claims on a user or on a role, and inherited
  from every role the user holds.

Both are resolved once at sign-in and written into the token, which is why an authorization check costs nothing at
request time. The trade-off is that changes take effect on the next token refresh rather than instantly.

### Policies

Three built-in policies, declared in [`AuthPolicies`](/src/Shared/Infrastructure/Services/AuthPolicies.cs) and
registered in [`ISharedServiceCollectionExtensions`](/src/Shared/Infrastructure/Extensions/ISharedServiceCollectionExtensions.cs):

| Policy | Requires | Typical use |
|---|---|---|
| `PRIVILEGED_ACCESS` | This session is within the user's privileged-session cap | High-value pages: dashboard, catalog, todo |
| `ELEVATED_ACCESS` | The user re-authenticated recently (a 6-digit code, or the first minutes of a 2FA sign-in) | Dangerous actions: deleting an account, managing roles |
| `TENANT_SELECTED` | The token carries a tenant claim | Every endpoint that reads or writes tenant data |

`ELEVATED_ACCESS` is time-boxed: the claim holds the moment elevation expires, and the policy compares it against
the current time. A stale value is therefore harmless, which is why it can be carried across token refreshes.

### Feature policies

[`AppFeatures`](/src/Shared/Infrastructure/Services/AppFeatures.cs) declares the app's capabilities, grouped by
area (`Management`, `System`, `AdminPanel`, `Todo`). The app **generates one policy per feature automatically**,
so you never register them by hand - declaring a feature is enough to be able to `[Authorize]` on it.

Feature values are terse strings like `"1.1"` rather than names, purely because they ride in every JWT and the
payload should stay small.

Apply them like any other policy, and stack them freely - **all** attributes must pass:

```razor
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.Management.Users_Manage)]
```

### Claims the server owns

[`AppClaimTypes`](/src/Shared/Infrastructure/Services/AppClaimTypes.cs) lists the claim types the system computes
itself: the session id, privileged-session flag and cap, elevation deadline, the granted feature list, the
authentication method, and (under multi-tenancy) the current tenant.

Most of them are **per-session state, not data**: the session id, the privileged-session flag, the elevation
deadline and the current tenant are recomputed on every sign-in, so never insert them into the `UserClaims` /
`RoleClaims` tables and never accept them from an external provider - a stored copy would either be ignored or,
worse, believed.

The privileged-session **cap** is the exception, and it is deliberate: it is a per-role default, so it *is* stored
as a role claim (the seeded `g-admin` and `t-admin` rows carry `-1`, meaning unlimited) and read back when the token
is built. Role managers may set it through the roles page.

Admin feature claims are a special case: rather than being stored, they are re-derived while the token is read -
`g-admin` gets every feature, `t-admin` gets the tenant-scoped subset. So changing what an admin can do is a code
change, not a data migration.

---

## 4. Multi-tenancy

> Applies only when the project is generated with the **multi-tenant** option. Otherwise none of these types exist
> and every entity is single-tenant.

Multi-tenancy here is **row-level**: one database, one set of user accounts, and a tenant column on the tables that
belong to a tenant. A user can belong to several tenants and switch between them; switching re-issues the token
with a different tenant claim.

- [`Tenant`](/src/Server/Boilerplate.Server.Api/Features/Tenants/Tenant.cs) - its `Name` must be a valid sub-domain
  label, because that is how anonymous requests resolve it. `IsActive` controls whether it can be used at all.
- [`TenantUser`](/src/Server/Boilerplate.Server.Api/Features/Tenants/TenantUser.cs) - membership. `AcceptedOn` is
  null while the user has only been invited.
- A seeded default tenant (`store`) owns all sample data and acts as the fallback.

### One filter, applied everywhere

An entity opts in by implementing
[`ITenantAware`](/src/Server/Boilerplate.Server.Api/Features/Tenants/ITenantAware.cs) (a `TenantId` plus a `Tenant`
navigation). `AppDbContext.OnModelCreating` then applies the same global query filter - and the same
`OnDelete(NoAction)` relationship - to every such entity by reflection. You never write the filter yourself, and
there is no other global filter in the model to compose with.

Two limits matter more than anything else in this section:

> **A query filter constrains reads only.** It does not stamp `TenantId` on insert, and it does not apply when you
> update or delete a detached stub entity. Those paths are yours to get right.

> **Identity tables are scoped by convention, not by the filter.** `Role`, `UserRole`, `UserClaim` and
> `UserSession` carry a *nullable* `TenantId` (null = global, e.g. the `g-admin` role) and deliberately do not
> implement `ITenantAware`. Their scoping is enforced in code - by `UserClaimsService` when it builds a token, and
> by the management controllers when they list or mutate. Any new query against them must carry its own predicate.

### How a request finds its tenant

`TenantProvider` answers that question, in order:

| | Source | Applies to |
|---|---|---|
| 1 | Throws when there is no `HttpContext` | Background jobs - fails closed instead of guessing |
| 2 | The signed-in user's tenant claim | Authenticated requests. Server-issued, therefore trusted |
| 3 | A tenant whose custom `Domain` matches the whole request host | Anonymous requests on a vanity domain |
| 4 | A tenant whose `Name` matches the host's sub-domain | Anonymous requests, e.g. public product pages |
| 5 | The default tenant | Nothing else matched |

Steps 3 and 4 read a cached lookup of active tenants, invalidated whenever a tenant is created or updated - so
deactivating a tenant stops it serving anonymous traffic.

`TenantProvider` must stay a **singleton**: `AppDbContext` is pooled and resolves it from the root provider, so
making it scoped compiles but fails at the first query.

### Rules for adding tenant-scoped code

1. **Implement `ITenantAware`.** The filter comes for free. If a value must be unique per tenant, put `TenantId`
   first in the index and add a migration.
2. **Stamp `TenantId` yourself on create**, from the claim - never from the request DTO:
   ```csharp
   entityToAdd.TenantId = User.GetTenantId() ?? throw new InvalidOperationException();
   ```
   The shared DTOs deliberately expose no `TenantId` so a mapper cannot smuggle one in. Keep it that way.
3. **Load before you update or delete.** A filtered read returns `null` for another tenant's row, which becomes a
   404. Removing a hand-built stub bypasses the filter entirely. Carry the `Version` concurrency token through.
4. **Guard the endpoint with `TENANT_SELECTED`**, so the tenant always comes from the signed claim and an
   authenticated user without a tenant can never fall through to host-based resolution.
5. **Scope identity data by hand** - roles, claims, sessions. See the note above.
6. **Put the tenant id in your cache keys**, and don't mark tenant-varying responses as user-agnostic.

### Caveats worth knowing before you ship

- **`IgnoreQueryFilters()` removes isolation.** The one legitimate use in the template (looking up a product's real
  tenant to reject cross-tenant access) re-applies the check by hand immediately. Follow that shape.
- **Background jobs have no tenant.** They must ignore the filter and apply their own predicate per tenant.
- **The fallback tenant is fail-open.** Anonymous traffic on an unknown host lands there, so keep sensitive data
  out of it.
- **The host that selects a tenant is not pinned by `AllowedHosts`.** Resolution goes through the request's
  web-app URL, which honours a client-supplied origin when that origin is trusted. The list that actually governs
  it is `TrustedOrigins`. A wildcard entry - which a per-tenant client app needs - deliberately lets any caller
  select any tenant by header; enumerate origins explicitly if you don't want that.
- **A tenant's custom `Domain` outranks its sub-domain and is self-service.** Reserved-name checks stop a tenant
  claiming a host the deployment itself answers on, but nothing can tell whether a third-party domain really
  belongs to that tenant. Verify ownership out of band (DNS/ACME challenge, or admin approval) before saving one.
- **Privilege changes are eventually consistent.** Removing a user from a tenant revokes their sessions in that
  tenant, but an already-issued access token stays valid until it expires. That window is inherent to short-lived
  JWTs.

---

## 5. One-time tokens

Confirmation codes, magic links, password resets, 2FA and elevation codes all share one mechanism, and it is worth
understanding once.

The `User` entity keeps a `…RequestedOn` timestamp per token kind. When a token is generated, that timestamp is set
to now **and embedded in the token's purpose string**. Validation regenerates the purpose from the *current*
timestamp and compares.

Everything else follows from that single trick:

- Requesting a new token silently invalidates every earlier one - the old purpose string no longer matches.
- Consuming a token sets the timestamp to `null`, so it cannot be replayed.
- Expiry is just "now minus `RequestedOn` exceeds the configured lifetime".
- The same timestamp rate-limits requests, so a user cannot spam themselves with codes.

In practice: a reset requested at 10:00 stops working the moment another is requested at 10:05, and the 10:05 one
dies the instant it is used.

---

## 6. External providers

Google, Microsoft/Entra, Apple, Facebook, GitHub, Twitter (X) and Keycloak are pre-wired. Each is enabled simply by
filling in its client id and secret under the `Authentication` section of `appsettings.json` - a provider with no
credentials is not registered and its button does not appear.

The flow is ordinary OAuth/OIDC, with a Boilerplate-specific ending:

1. The user is redirected to the provider and comes back to the callback.
2. The app looks for an existing link; failing that, it looks for a local account with the same email or phone.
3. If nothing matches, a new user is created. Providers that manage their own roles (Keycloak) get no local role;
   consumer providers get the demo role.
4. A magic link is generated and the user is signed in through the normal OTP path - so 2FA still applies.

> ⚠️ **Before adding your own provider, check that it verifies email addresses.**
> Step 2 attaches an incoming identity to an *existing* account purely on the identifier the provider asserts. That
> is what makes "sign in with Google" reach the account you originally created with a password, and it is safe for
> the providers shipped here, which only ever hand out addresses their owner has proven control of.
> It is **not** safe for a provider whose users choose their own unverified address - a self-hosted Keycloak realm
> with self-registration, Entra with personal accounts, or any OIDC provider you add. There, anyone could claim
> someone else's address and take over their account. In that case, require the provider's `email_verified` claim
> before that fallback, or replace it with an explicit link-your-account flow.

---

## 7. Keycloak

Keycloak is included as a full identity server for development: with .NET Aspire enabled it starts automatically as
a container, pre-loaded from
[`dev-realm.json`](/src/Server/Boilerplate.Server.AppHost/Infrastructure/Realms/dev-realm.json) with demo accounts.

| Username | Password | Group |
|---|---|---|
| test | 123456 | `g-admin` |
| bob | bob | `demo` |
| alice | alice | `demo` |

The realm also carries a `t-admin` group, used when multi-tenancy is enabled.

**How it maps.** Keycloak **groups** become ASP.NET Core **roles**, and Keycloak **user attributes** become claims.
Keycloak's own *roles* (which are a different concept) are not mapped. Tokens are still issued by the app, not by
Keycloak - Keycloak authenticates, the app decides what the resulting token says.

**Revalidation.** A user counts as Keycloak-backed when the deployment has Keycloak configured *and* a Keycloak
refresh token is stored for them - not based on how they happened to sign in this time. Whenever a token is issued,
that refresh token is exchanged for fresh claims; if Keycloak rejects it because the account was disabled or
deleted, sign-in fails. This is *near*-real-time, not instant: while a previously fetched Keycloak access token is
still valid it is reused without contacting the server.

> **Keycloak is a trusted claims authority, by design.** Its groups become roles and its `features` attribute
> becomes app permissions directly - so whoever administers the realm can grant application-level global admin.
> That is the intended trade-off of federating identity. The exception is the per-session claims the server owns
> (session id, privileged/elevated session, tenant): those are dropped from the copy, so a realm attribute cannot
> forge an elevated session or select a tenant.

**Sharing tokens with other services.** Signing is already asymmetric, so hand another service the *public* key and
let it validate normally. The private key never leaves the API.

---

## 8. Try it yourself

Run the project and walk through these - it is faster than reading:

- **Sign up** with email or phone, confirm it, then try signing up again with the same identifier.
- **Sign in** with a password, then with a magic link. Get the password wrong repeatedly and watch lockout kick in.
- **Enable 2FA** in Settings with an authenticator app, then sign in again. Try a recovery code.
- **Sign in from several browsers**, list the sessions in Settings, revoke one, and watch that device drop out.
- **Reset a password**, then try the same link twice, and try an expired one.
- **Sign in with an external provider** and see how the account gets linked and confirmed.
- **Give a user different roles and features** and see which pages appear and which 403.
- **Sign in from more than three devices** and watch the fourth lose access to privileged pages while staying
  signed in.

---

## Video tutorial

📺 **[Comprehensive Identity System Walkthrough](https://youtu.be/-3viBEtJHLo)** (~15 minutes) - registration,
password and OTP sign-in, 2FA, session management and revocation, password reset, roles and permissions, external
providers, privileged sessions and elevated access.

### AI Wiki: answered questions

* [How does a `refresh token` function in a Boilerplate project template?](https://deepwiki.com/search/how-does-a-refresh-token-funct_6a75fa66-ab98-4367-bd1a-24b081fbf88c)
* [What would happen when I use [AuthorizedApi]](https://deepwiki.com/search/what-would-happen-when-i-use-a_c525d59d-5c55-489b-8f95-69f6df7c743d)
* [Give me high level overview of two factor auth setup and usage flows](https://deepwiki.com/search/give-me-high-level-overview-of_1883503f-2e34-41ca-821a-1246d332990f)

Ask your own question [here](https://wiki.bitplatform.dev).

---
