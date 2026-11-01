# Stage 16: CI/CD Pipeline and Environments

The project builds, tests and deploys itself with GitHub Actions. This stage gives the big picture: the environments,
the workflows, how the server and the apps get built and deployed, and the settings they need. The workflow files in
`.github/workflows` hold the details.

---

## Environments

`AppEnvironment` ([`/src/Shared/Infrastructure/Services/AppEnvironment.cs`](/src/Shared/Infrastructure/Services/AppEnvironment.cs))
tells every part of the app - the server, the web app and the Android, iOS, Windows and macOS apps - which environment it
runs in: `Development`, `Test`, `Staging` or `Production`.

- The build decides it ([`/src/Directory.Build.props`](/src/Directory.Build.props)): `-c Debug` is `Development`,
  `-c Release` is `Production`, and `-p:Environment=Test` picks another one. It also becomes a compile-time constant, so
  C# code can check the `Development` symbol in a conditional compilation block.
- The server takes it from `ASPNETCORE_ENVIRONMENT` when it starts, which a deployment sets to the environment it deploys.
- Code asks with `AppEnvironment.IsDevelopment()`, `IsTest()`, `IsProduction()` or `AppEnvironment.Current`.

---

## The workflows

| Workflow | Runs on | Does |
| --- | --- | --- |
| `ci.yml` | every pull request, and by hand | builds the whole solution and runs every test |
| `cd-test.yml` | every push to `develop` that changes the app, and by hand | deploys the Test environment |
| `cd-production.yml` | every push to `main` that changes the app, and by hand | runs `ci.yml`, then deploys the Production environment |
| `cd-template.yml` | called by the two above | the jobs both deployments share, with `ENV_NAME` naming the environment |

Merging a pull request into `develop` updates Test, and a release into `main` updates Production. Only a change under
`src/Shared`, `src/Server` or `src/Client`, or to `src/Directory.Build.props` or `src/Directory.Packages.props`, deploys:
one to the docs, the tests or the agent files doesn't spend the plan's Actions minutes on a deployment. The runs of one
workflow queue up instead of overlapping, and one that has started is never cancelled halfway. `cd-test.yml` doesn't
wait for the tests, so protect `develop` with a required `ci.yml` check, which a private repository has with GitHub Pro
or Team.

In a public repository, every build also gets a signed attestation of the commit and the run that made it, which
`gh attestation verify` checks, and the About page links to them.

Two steps use short composite actions from
[bitfoundation/bitplatform](https://github.com/bitfoundation/bitplatform/tree/develop/.github/actions/boilerplate), at the
release tag of the project's bit platform version, so the workflows stay short and get their fixes with each upgrade:
`bit-setup` installs the bit CLI and runs `bit setup`, which installs only what the runner is missing, and
`artifact-signing` lets `vpk pack` sign the Windows app with Artifact Signing. Each is a few lines of YAML; to stop
depending on that repository, copy one into this repository and point its step at the copy.

---

<!--#if (aspire == true)-->
## How the server is deployed

The server is a Docker Compose project that .NET Aspire publishes from the app host, running on your own Linux server.

1. **Build** (`build_server`, on a GitHub-hosted runner): builds the server's images, pushes them to GitHub Container
   Registry tagged with the environment and the commit, and publishes the Docker Compose files.
2. **Deploy** (`deploy_server`, on a self-hosted runner on the server): writes the Compose project's settings from the
   environment's secrets, pulls the images, runs the database migrations on their own, brings the new version up, and
   waits until every container is healthy and the public address answers.
<!--#if (cloudflare == true)-->
3. **Purge** Cloudflare's cache, so users get the new version at once.
<!--#endif-->

The server holds only Docker and the runner, no SDK and no source code, and it keeps every inbound port closed: the
runner pulls its jobs from GitHub and the Cloudflare Tunnel dials out. The `deploy` skill
(`.github/agents/deploy.agent.md`) sets it all up: the server, the runner, the tunnel and the secrets.

Worth knowing:

- **Migrations run while the old version still serves**, so each one has to keep that version working: add a column
  in one release and drop the old one in a later release, instead of renaming it in one go. A failed migration stops
  the deployment and leaves the running version alone.
- **The switch takes a few seconds**, while the new containers start.
- **To roll back**, deploy an earlier commit again; its images are still in the registry. Migrations only go forward.
- **The data** lives in Docker volumes on the server, so back them up off the server.
- **The database password** takes effect only while its volume is still empty.
- **The Aspire dashboard and DbGate** run on the server too, reachable only through SSH; the `deploy` skill says how.
<!--#else-->
## The server

This project has no .NET Aspire app host, so the CD doesn't deploy the server: it deploys as a Docker Compose project
that Aspire publishes. Bring .NET Aspire in with the `template-diff` skill (`.github/agents/template-diff.agent.md`),
and the `deploy` skill (`.github/agents/deploy.agent.md`) sets up the rest.
<!--#endif-->

---

## The apps

The same CD builds the native apps next to the server and leaves them as artifacts of the run for a day, which keeps
the runs inside the storage GitHub Free includes; publishing them to the stores is up to you.

- **Windows**: an installer with automatic updates (Velopack). Once the `CODE_SIGNING_*` settings are there, it is
  signed with Azure Artifact Signing, so Windows shows your organization as the publisher.
- **Android**: an `.aab` signed with your keystore, for Google Play. Keep that keystore safe: without it you can't
  update the app on Google Play. `bit pipeline` creates it, and `bit keys android` writes one as a file.
- **iOS**: an `.ipa` signed for the App Store. No Mac is needed for its signing: given an App Store Connect API key,
  `bit pipeline` creates the certificate and the provisioning profile, and `bit keys apple` writes them as files.

---

## Settings and secrets

Each GitHub environment, `Test` and `Production`, has its own variables and secrets (the repository's
**Settings → Environments**), so the same workflows deploy each environment with its own values. Before it builds, the
CD writes the environment's values, such as `ServerAddress`, into the `appsettings*.json` files, so they are built into
that environment's apps.

Run `bit pipeline` in the repository. It reads the secrets the workflows use, creates the environments they deploy to,
and stores each secret in its environment. It creates what it can on its own: the app certificate, the VAPID key pair,
the database and Redis passwords and the Android signing key, and with an App Store Connect API key you give it, the iOS
app's signing. It asks for the rest, and an empty answer skips one. `bit pipeline --check` only lists what's missing.

| Name | Kind | Holds |
| --- | --- | --- |
| `SERVER_ADDRESS` | variable | The environment's public address |
| `APP_VERSION`, `APP_TITLE`, `APP_ID` | variable | The app's version, name and id |
| `OPENAI_APIKEY` | secret, optional | The key `bit translate` fills missing translations with |
<!--#if (aspire == true)-->
| `SMTP_CONNECTION_STRING` | secret | The SMTP server the app sends email through |
| `APP_CERTIFICATE` | secret | The environment's own `AppCertificate`, its `.crt` and `.key` in one PEM, never the development one |
| `APP_CERTIFICATE_RETIRED` | secret, optional | The certificate it replaced, while tokens it signed are still alive (see `AppCertificate.md`) |
| `SERVERWEB_SETTINGS` | secret, optional | Any other setting of the web app, one `Section__Key=value` line each |
<!--#if (api == "Standalone")-->
| `SERVERAPI_SETTINGS` | secret, optional | The same, for the api |
<!--#endif-->
<!--#if (database == "SqlServer" || database == "PostgreSQL" || database == "MySql")-->
| `DATABASE_PASSWORD` | secret | The database server's password |
<!--#elif (database == "Other")-->
| `DATABASE_CONNECTION_STRING` | secret | The connection string the app reads as `otherdb` |
<!--#endif-->
<!--#if (filesStorage == "S3")-->
| `S3_CONNECTION_STRING` | secret | The S3-compatible storage, such as Cloudflare R2 |
<!--#elif (filesStorage == "AzureBlobStorage")-->
| `AZURE_BLOB_STORAGE_CONNECTION_STRING` | secret | The Azure Storage account the files go to |
<!--#endif-->
<!--#if (redis == true)-->
| `REDIS_CACHE_PASSWORD`, `REDIS_PERSISTENT_PASSWORD` | secret | The passwords of the two Redis containers, letters and digits only (see [`/specs/workarounds.md`](/specs/workarounds.md)) |
<!--#endif-->
<!--#if (cloudflare == true)-->
| `CLOUDFLARE_TUNNEL_TOKEN` | secret | The connector token of the environment's Cloudflare Tunnel |
| `CLOUDFLARE_ZONE`, `CLOUDFLARE_TOKEN` | secret | The zone, and an API token that may purge its cache |
<!--#endif-->
<!--#if (notification == true)-->
| `PUBLIC_VAPIDKEY`, `PRIVATE_VAPIDKEY` | secret | The environment's web push key pair |
| `VAPID_SUBJECT` | secret | The contact the push services see, such as `mailto:you@example.com` |
<!--#endif-->
<!--#endif-->
| `ANDROID_RELEASE_*`, `APPSTORE_*`, `IOS_CODE_SIGN_PROVISION` | secret, variable | Signing the Android and iOS apps |
| `CODE_SIGNING_*`, `WINDOWS_UPDATE_FILES_URL` | secret, variable | Signing the Windows app, and where it looks for updates |

`cd-template.yml` is the final word on what each job reads.

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
