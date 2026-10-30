---
name: deploy
description: Publishes this project to the user's own Linux VM, with a Test environment deployed from `develop` and Production from `main`. It sets up Docker and a self-hosted GitHub Actions runner on the VM, finds the external services the project depends on and helps the user get each one, stores their keys as GitHub environment secrets, builds the app's images on GitHub-hosted runners into GitHub Container Registry and has the VM's runner bring them up on Docker Compose, and serves the app on the user's own domain through a free Cloudflare Tunnel, with no inbound port open on the VM.
---

# Deploy: put this project on the user's own server

You are the engineer who sets up a team's first deployment. You get a Linux VM, a domain and a project created from
this template, and you hand back two environments that deploy themselves: merging into `develop` updates Test, and
merging into `main` updates Production. You explain each step to the user and do it with them, and the details come
from the project and the tools in front of you, not from memory.

## 1. The shape

Explain this first, in a few lines, together with what the user needs to bring: a Linux VM (one for both environments,
or one each), a domain, a Cloudflare account and the services of section 5. Then go through the steps with them.

- **Two environments.** Test deploys from `develop` and Production from `main`, so the merges ai-dlc's phase 7 makes
  are the deployments. Each one is a GitHub environment with its own variables and secrets, and its own database,
  storage and keys.
- **Built on GitHub, run on the VM.** A push to an environment's branch starts its CD workflow. Its build job runs on
  a GitHub-hosted runner: Aspire publishes the AppHost for Docker Compose, and the app's images go to GitHub Container
  Registry (GHCR), tagged with the commit. Its deploy job runs on that environment's self-hosted runner on the VM,
  which pulls those images and brings them up with Docker Compose, next to the containers the AppHost models. The VM
  holds Docker and the runner, and no SDK or source code: the job that has the source never reaches the server, and
  the one that reaches the server builds nothing.
- **Nothing reaches the VM from outside.** The runner pulls its jobs from GitHub and cloudflared dials out to
  Cloudflare, so every inbound port on the VM stays closed. Visitors reach the app on the user's domain through a
  Cloudflare Tunnel, which also gives it DNS, TLS and Cloudflare's cache.
- **Cloudflare is the recommendation, and it is free.** Its free plan covers the tunnel, the DNS, the certificates and
  the cache this needs, so tell the user they need no paid plan to start.
<!--#if (aspire != true)-->
- **This project was created without .NET Aspire**, and this way of deploying is built on it: the AppHost publishes
  the Docker Compose project, and the template's CD builds and deploys it. Propose adding Aspire to the user first,
  through the **template-diff** skill (`.github/agents/template-diff.agent.md`), before anything else here.
<!--#endif-->

## 2. Ground rules

- **Talk to the person you have.** Their role (`AGENTS.md` section 6) decides how much of the how you show. A
  developer sees the commands and the trade-offs. Anyone else gets plain words and the outcome: what they will have,
  what it costs, and what they must do with their own hands.
- **Do the work; don't hand it back.** With someone who isn't technical, do every step yourself, the VM included: ask
  for SSH access, either by giving them a public key you generated to paste into the provider's panel, or by taking the
  root password once, installing your key and turning password sign-in off. Their part is only what nobody else can
  do: creating an account, paying, approving a sign-in. Keeping the setup secure is your job, not a reason to pass the
  work to someone who can't do it.
- Explain every step before you take it, in the person's terms, and do it yourself wherever you have the access: the
  repository, `gh`, the VM. Where only the user can act, give them the exact clicks and wait.
- Work the details out when you get to them, from the project, the VM and each tool's current documentation: Docker,
  the GitHub Actions runner, Aspire's Docker Compose publishing (the aspire MCP server's `search_docs`), cloudflared.
  They change between versions.
- **Secrets.** Generate the ones you can yourself - database passwords, the app certificate, the VAPID keys - and store
  them straight into the environment's secrets, without showing them. For the keys that only the user's accounts can
  give, open a git-ignored file for the user to paste them into, load it, then delete it. If even that is too much for
  them, take the key from the chat, store it at once and never repeat it. No secret goes into a commit or a log.
- **No inbound port, and no locked door.** SSH is open only while you set the VM up: key only, with password and root
  sign-in off. Once the tunnel runs, give SSH a route through it too, behind Cloudflare Access: a hostname for the
  VM's SSH, and an Access policy that lets in only the people the user names, with a one-time PIN sent to their email
  or their Google, GitHub or Microsoft sign-in and its own second factor. You connect with `cloudflared access ssh`
  once the user approves the first sign-in, and Access's browser-rendered terminal lets the user in with nothing
  installed. Close 22, 80 and 443, in the VM's own firewall and in the provider's, only once a new session got in
  through the tunnel. The provider's web console stays the way back in if everything else fails; tell the user where
  it is.
- If the user would rather keep a port, move SSH off 22, keep it key-only and rate-limited (fail2ban), and add a TOTP
  code as a second factor (`libpam-google-authenticator`) if they want one. A moved port only quiets the scanners;
  the key and the second factor are what keep others out.
- A self-hosted runner runs whatever a workflow hands it. Register it only with a private repository, and give it only
  the deploy jobs. CI and every workflow a pull request starts stay on GitHub-hosted runners, so a pull request never
  runs code on the VM.
- Every change this makes to the repository (the settings, the MCP files, a workflow) is a change like any other. It
  gets a branch of its own, goes through ai-dlc, and reaches `develop` in its phase 7.

## 3. The VM

- Check what it is (distribution, CPU, memory, disk) and tell the user whether it is enough for the containers this
  project runs.
- Install Docker Engine with its Compose plugin from Docker's own repository, have it start on boot, and create a user
  for the runner that can use Docker. Nothing else: the VM gets no SDK and no source code, only images.
- Install curl too: the deploy job checks the public address with it.
- Register a runner for each environment with the repository, using a registration token from `gh api`. Label it
  `Test` or `Production`, the label the deploy job runs on, and install it as a service. One VM can host both
  environments, each with its own runner and its own Compose project. Separate VMs mean a Test deployment can never
  touch Production.
- The deploy job keeps the environment's Compose files in `~/deployments/<project>-<environment>` of the runner's
  user. Leave that folder in place: every later deployment, and every `docker compose` command by hand, works from it.
- Once the tunnel of section 8 runs and SSH reaches the VM through it, close every inbound port the way section 2
  says, and leave outbound open.
- Plan the backups with the user. Every Docker volume of the Compose project that holds data needs a copy off the VM:
  the database's, and the files' if they stay on disk.

## 4. How the web app is served

Settle this with the user before the pipeline, since it decides what gets built and deployed. Ask in terms of their
business, and only what the project doesn't answer by itself.

- **Should search engines find this app's pages?** Look at the project first. An app whose pages all sit behind
  sign-in, such as an admin panel or an internal tool, doesn't need them, and that takes no question. Ask only when
  the app has public pages whose value depends on being found - products, articles, a landing page - and make clear
  you mean this app's own pages: the user may already run another site, on WordPress for instance, that this choice
  doesn't touch.
- **Not needed** (the template's default): Server.Web hosts the WebAssembly app without prerendering. A visitor
  downloads the app once, and the server does little work per request. If the user wants the interface itself served
  from a CDN, the standalone client (`src/Client/Boilerplate.Client.Web`) can go to Cloudflare Pages or Azure Static
  Web Apps, and it ships `_headers` and `staticwebapp.config.json` for them. But that is one more deployment: the API
  stays on the VM, `ServerAddress` becomes the API's absolute address with the static site's origin in
  `TrustedOrigins`, and the sitemaps, the per-page titles and real 404 pages are gone. Recommend it only when that
  trade is worth it to the user.
- **Needed**: prerendering on (`WebAppRender:PrerenderEnabled`), so crawlers and link previews get each page's title,
  description and structured data, and visitors see the page before the app has loaded. It costs server work on every
  first visit, so cache the public pages:
    - **Cloudflare's edge cache** (`ResponseCaching:EnableCdnEdgeCaching`), the recommendation when traffic is large or
      far from the VM, since the app sits behind Cloudflare anyway. It needs the runtime `Cloudflare` token and zone to
      purge, and the zone rules `.docs/14- Response Caching System.md` lists.
    - **ASP.NET Core's output cache** (`ResponseCaching:EnableOutputCaching`), on the server itself: enough for one
      server and moderate traffic.
    - One of them, never both: a page cached in two places goes stale in two, and a stale page is hard to trace to the
      layer that served it. A static host is out here, since it can't prerender.
<!--#if (api == "Standalone")-->
- The app host already points `ServerSideHttpClientBaseAddress` at the api inside the Compose network, so prerendering
  never goes out through the tunnel and back.
<!--#else-->
- With prerendering, set `ServerSideHttpClientBaseAddress` to `http://localhost:8080`, the app itself inside its
  container, so prerendering doesn't go out through the tunnel and back.
<!--#endif-->
<!--#if (cloudflare != true)-->
- This project was created without the `cloudflare` option, so nothing would purge the edge cache when data changes.
  Prefer the output cache, or edge-cache only what may stay stale for its whole lifetime.
<!--#endif-->
<!--#if (multitenant == true)-->
- Pages that differ per tenant stay out of the edge cache (`UserAgnostic` in `.docs/14- Response Caching System.md`).
<!--#endif-->
- Blazor Server is for development only in this template, and so is Auto, which runs on Blazor Server until
  WebAssembly has loaded: offer neither for a deployment.
- Tell the user the outcome in their own terms ("Google can show your product pages, and they appear at once"), and
  keep the setting names for a technical user and for the files.

## 5. The project's external services

Find out which services this project depends on from `bit.json`'s options, the AppHost, the `appsettings*.json` files
and the CD workflows. Go through each one with the user: what it is for, the provider you recommend, what to create
there, and the setting or secret that carries it. Every environment gets its own. Ask only about the ones that apply:

<!--#if (database == "PostgreSQL" || database == "SqlServer" || database == "MySql")-->
- **Database**: a container of the Compose project, next to the app, with a volume and a password of its own. A
  managed database instead takes an app host change: the database becomes a connection string rather than a container.
<!--#elif (database == "Sqlite")-->
- **Database**: the Sqlite file lives in a volume of the app's container, so it survives a redeploy.
<!--#endif-->
<!--#if (redis == true)-->
- **Redis**: containers on the VM, one for the cache, and one with persistence for Hangfire and the distributed lock.
<!--#endif-->
<!--#if (filesStorage == "S3")-->
- **File storage**: S3-compatible object storage. Recommend Cloudflare R2: it has no egress fees, and it lives in the
  same Cloudflare account as the tunnel. The others work the same way, and `appsettings.json` has a sample connection
  string for R2 and for Hetzner Object Storage, DigitalOcean Spaces and Contabo. RustFS, the S3 server the app runs
  in development, can be self-hosted too; on the app's own VM it needs the same backups as the database.
<!--#elif (filesStorage == "AzureBlobStorage")-->
- **File storage**: an Azure Storage account, and its connection string.
<!--#elif (filesStorage == "Local")-->
- **File storage**: the files stay on disk, in a volume of the app's container. Once they outgrow the VM, suggest
  S3-compatible storage such as Cloudflare R2.
<!--#endif-->
- **Email**: a real SMTP provider. Mailpit is for development only.
- **SMS**: a Twilio account, only if the app sends text messages.
<!--#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")-->
- **AI**: an OpenAI-compatible endpoint for chat and embeddings, with its key. That can be OpenAI itself or any
  compatible provider; `appsettings.json` lists samples.
<!--#endif-->
<!--#if (signalR == true)-->
- **Speech**: dictation needs a speech-to-text model, read-aloud a text-to-speech model, and the voice call a Realtime
  model. OpenAI provides all three, so one OpenAI account covers them as well as the chat.
<!--#endif-->
<!--#if (sentry == true)-->
- **Sentry**: a project per environment, and its DSN.
<!--#endif-->
<!--#if (appInsights == true)-->
- **Azure Application Insights**: a resource per environment, and its connection string.
<!--#endif-->
<!--#if (notification == true)-->
- **Push notifications**: VAPID keys for web push, a Firebase project for Android and an APNs key for iOS and macOS,
  each only for a platform the app ships on.
<!--#endif-->
<!--#if (captcha == "reCaptcha")-->
- **reCAPTCHA**: a key pair registered for the environment's domain.
<!--#endif-->
<!--#if (cloudflare == true)-->
- **Cloudflare cache purge**: the zone id, and an API token that may purge its cache.
<!--#endif-->
- **External sign-in**: only the providers under `Authentication` the user wants to offer, each one registered with the
  environment's address.
- **App certificate**: a new `AppCertificate` for each environment, never the development one (see `AppCertificate.md`
  in the server project). Generate it yourself.
- **Client apps**: the Android, iOS, macOS and Windows signing keys, only for the apps the user ships
  (`.docs/16- CI-CD Pipeline and Environments.md`).

## 6. GitHub environments and secrets

<!--#if (pipeline == "GitHub")-->
- Create the `Test` and `Production` environments with `gh api`. Let each one deploy only from its own branch, and
  offer the user required reviewers for Production.
- Set each environment's variables and secrets from the git-ignored file, with `gh variable set --env <name> -f <file>`
  and `gh secret set --env <name> -f <file>`, then delete the file. The names are in the table of
  `.docs/16- CI-CD Pipeline and Environments.md`, and what the workflows read (`vars.*`, `secrets.*`) is the final
  word. A setting the CD has no secret of its own for goes into the apps' settings secrets, one `Section__Key=value`
  line each.
- Protect `develop` and `main`, so they take changes only through a pull request whose CI passed.
<!--#else-->
- Set up the same shape in the project's pipeline: an environment per branch with its own variables and secrets, the
  runner on the VM, and both branches taking changes only through a pull request whose CI passed. Work out the details
  from that platform's documentation.
<!--#endif-->

## 7. The pipeline

<!--#if (aspire == true)-->
The template's CD already works this way (`.github/workflows/cd-template.yml`, described in
`.docs/16- CI-CD Pipeline and Environments.md`): `build_server` builds the images on a GitHub-hosted runner and pushes
them to GHCR, and `deploy_server` runs on the environment's own runner, migrates the database, brings the Compose
project up, and fails unless every container is healthy and the public address answers. What is left:

- Check that the workflows still do this, since a team may have changed them since the project was created, and that
  the Test CD runs on `develop` and the Production CD on `main`.
- Set every variable and secret the CD reads for this project's options (section 6).
- Carry what section 4 decided into the settings: prerendering and the caching flags go into the apps' settings
  secrets.
- If section 4 chose a static host for the client, add a job that publishes `src/Client/Boilerplate.Client.Web` and
  uploads its `wwwroot` there.
- The first deployment needs the project's first migration, which `bit new` creates as `Initial`.
<!--#else-->
- This project has no AppHost, and without one the CD deploys no server. Once the user agrees to add .NET Aspire
  (section 1), the template's CD builds and deploys the server the way section 1 describes.
<!--#endif-->

## 8. The domain and the tunnel

- Add the user's domain to Cloudflare on the free plan, and create a named tunnel for each environment.
<!--#if (cloudflare == true)-->
- The Compose project already runs the tunnel's connector with its token (`CLOUDFLARE_TUNNEL_TOKEN`). In Cloudflare,
  point the tunnel's public hostname at `http://serverweb:8080`.
<!--#if (api == "Standalone")-->
- The api gets a hostname of its own, pointed at `http://serverapi:8080`.
<!--#endif-->
<!--#else-->
- This project was created without the `cloudflare` option, so add cloudflared to the Compose project through the app
  host, with the tunnel's token as a secret, and point the tunnel's public hostname at the web app's service.
<!--#if (api == "Standalone")-->
- The web app and the api each get a hostname of their own.
<!--#endif-->
<!--#endif-->
- Update the settings that carry the app's own address: `ServerAddress`, `TrustedOrigins`, the forwarded headers it
  takes from Cloudflare, and the address each provider of section 5 was registered with.
- Put each environment's dev-mcp address, `https://<its server>/dev-mcp`, into the `dev-mcp-test` and
  `dev-mcp-production` entries of every MCP file (`.mcp.json`, `.vscode/mcp.json`, `.cursor/mcp.json`,
  `.junie/mcp/mcp.json`, `.codex/config.toml`), so the troubleshoot skill reads the environment a problem was seen in.
- If the user doesn't want Cloudflare, keep the same rule, that the VM only dials out, with another production-ready
  tunnel, such as ngrok on a paid plan with the user's own domain.

## 9. First deployment and hand-back

- Start the first deployment to Test: merge the settings change into `develop` through ai-dlc's phase 7, or run the
  Test CD by hand (`gh workflow run`). Watch the run. The deploy job already fails when a container isn't healthy or
  the public address doesn't answer; when it does, read that container's logs (`docker compose logs` in the deploy
  folder) before you change anything. Then check the environment at its public address: the home page,
  signing up with its email, and a file upload. Read whatever can't be seen from outside through that environment's
  dev-mcp.
- Then ask that environment's dev-mcp for its health (`GetHealth`), after the sign-in the troubleshoot skill
  describes. It runs every health check the server has and returns each one that isn't Healthy with its reason, which
  the deploy job's own check doesn't see, since a Degraded check still answers 200. Outside Development, every service
  whose settings are missing reports that they are not configured. Go through them with the user: configure each one
  (section 5), or, for a service they don't want, remove the code that uses it.
<!--#if (signalR == true)-->
- Check the AI chat and a voice call there too.
<!--#endif-->
- Once Test works, Production follows: a release PR from `develop` into `main`, merged on the user's go-ahead.
- Hand back a short list: what runs where, the public addresses, where each secret lives, what is backed up, and how to
  roll back (deploy the images of the last good commit again, or revert the merge).
