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
  are the deployments, those that change the app's code. Each one is a GitHub environment with its own variables and
  secrets, and its own database, storage and keys.
- **A merge deploys only after the first deployment.** A new project's CDs run only by hand, so the merges made before
  there is anywhere to deploy to spend no Actions minutes and leave no run waiting for a runner. Section 9 turns on each
  one's `push` trigger, once that environment's first deployment has worked.
- **Built on GitHub, run on the VM.** A push to an environment's branch starts its CD workflow. Its build job runs on
  a GitHub-hosted runner: Aspire publishes the AppHost for Docker Compose, and the app's images go to GitHub Container
  Registry (GHCR), tagged with the commit. Its deploy job runs on that environment's self-hosted runner on the VM,
  which pulls those images and brings them up with Docker Compose, next to the containers the AppHost models. The VM
  holds Docker and the runner, and no SDK or source code: the job that has the source never reaches the server, and
  the one that reaches the server builds nothing.
- **Nothing reaches the VM from outside.** The runner pulls its jobs from GitHub and cloudflared dials out to
  Cloudflare, so every inbound port on the VM stays closed. Visitors reach the app on the user's domain through a
  Cloudflare Tunnel, which also gives it DNS, TLS and Cloudflare's cache.
- **The landing page is a service of its own** in the same Compose project, `landingpage`, on a hostname of its own,
  often the bare domain with the app on a subdomain such as `app.`. It needs no database, and its settings come from
  the `LANDINGPAGE_SETTINGS` secret.
- **Cloudflare is the recommendation, and it is free.** Its free plan covers the tunnel, the DNS, the certificates and
  the cache this needs, so tell the user they need no paid plan to start.
- **GitHub Free is enough too.** A private repository on it has the environments, their secrets and the branch each
  one deploys from; only protecting `develop` and `main` takes GitHub Pro or Team. Tell the user in a line that Pro or
  Team would make this more professional, with its pros and cons as they stand then, and let them choose.
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
- **Secrets.** `bit pipeline` creates the ones it can - the app certificate, the VAPID keys, the database and Redis
  passwords - and stores them straight into the environment's secrets (section 6). For the keys that only the user's
  accounts can give, open a git-ignored file for the user to paste them into, load it, then delete it. If even that is
  too much for them, take the key from the chat, store it at once and never repeat it. No secret goes into a commit or a
  log.
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
- Install curl and openssl too: the deploy job checks the public address with curl, and reads the app certificate with
  openssl.
- Never retry a failing SSH sign-in in a loop: fail2ban or the provider bans your address after a few, often for longer
  than the rest of the setup takes.
- Register a runner for each environment with the repository, using a registration token from `gh api`. Label it
  `Test` or `Production`, the label the deploy job runs on, and install it as a service. One VM can host both
  environments, each with its own runner and its own Compose project. Separate VMs mean a Test deployment can never
  touch Production.
- The deploy job keeps the environment's Compose files in `~/deployments/<project>-<environment>` of the runner's
  user. Leave that folder in place: every later deployment, and every `docker compose` command by hand, works from it.
- The Aspire dashboard, with the logs, traces and metrics it keeps across restarts, and DbGate, for the database and
  Redis, run on the VM too and listen on its loopback only: `127.0.0.1` for Production and `127.0.0.2` for Test, on
  ports `18888` and `3000`. Show the user how to open them through SSH, for Production
  `ssh -L 18888:127.0.0.1:18888 -L 3000:127.0.0.1:3000 <vm>`, then `http://localhost:18888` and
  `http://localhost:3000`; the dashboard's sign-in link is in `docker compose logs compose-dashboard`. DbGate has no
  sign-in of its own, so neither gets a route on the tunnel unless a Cloudflare Access policy stands in front of it.
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
- The landing page (`src/LandingPage`) is prerendered whatever this decides, since being found is what it is for. Its
  pages set no cookies and a short shared cache lifetime, so a Cloudflare cache rule for its hostname can serve them
  from the edge.
- Blazor Server is for development only in this template, and so is Auto, which runs on Blazor Server until
  WebAssembly has loaded: offer neither for a deployment.
- Multilingual support adds about 800 KB to what a visitor downloads. If the project isn't multilingual, make sure
  `InvariantGlobalization` is on in `src/Directory.Build.props`.
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
  `bit pipeline` creates their passwords from letters and digits only, the way Aspire does in development; keep to that
  for one you set by hand: StackExchange.Redis can't take a comma in one, and DbGate gets them inside a URL that nothing
  encodes, where a `/`, `?`, `#` or `%` breaks it (`specs/workarounds.md`).
<!--#endif-->
<!--#if (filesStorage == "S3")-->
- **File storage**: S3-compatible object storage. Recommend Cloudflare R2: it has no egress fees, and it lives in the
  same Cloudflare account as the tunnel. The others work the same way, and `appsettings.json` has a sample connection
  string for R2 and for Hetzner Object Storage, DigitalOcean Spaces and Contabo. RustFS, the S3 server the app runs
  in development, can be self-hosted too; on the app's own VM it needs the same backups as the database. With R2, the
  setup token of section 8 lets you create the bucket and its keys yourself.
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
  compatible provider; `appsettings.json` lists samples. Each model has a key setting of its own under `AI:OpenAI`, and
  one fills none of the others: an empty `EmbeddingApiKey` sends embeddings to `AI:HuggingFace:EmbeddingEndpoint`,
  whose default, `http://localhost:7000`, is a local server a deployment doesn't have.
<!--#endif-->
<!--#if (signalR == true)-->
- **Speech**: dictation needs a speech-to-text model, read-aloud a text-to-speech model, and the voice call a Realtime
  model. OpenAI provides all three, so one OpenAI account covers them as well as the chat, each with its own key.
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
- **Cloudflare cache purge**: the zone id, and an API token that may purge its cache, which you mint in section 8.
<!--#endif-->
- **External sign-in**: only the providers under `Authentication` the user wants to offer, each one registered with the
  environment's address.
- **App certificate**: a new `AppCertificate` for each environment, never the development one (see `AppCertificate.md`
  in the server project). Generate it yourself.
- **Client apps**: the Android, iOS, macOS and Windows signing keys, only for the apps the user ships
  (`.docs/16- CI-CD Pipeline and Environments.md`). Their CD jobs are off (`if: false`); turn on only those of the
  apps the user ships, since every run draws on the Actions minutes of the user's GitHub plan, and macOS minutes cost
  the most.

## 6. GitHub environments and secrets

- Run `bit pipeline` in the repository, with `--yes` when nobody is there to answer its questions. It reads the secrets
  the workflows use, creates the `Test` and `Production` environments, and creates and stores what it can: the app
  certificate, the VAPID keys, the database and Redis passwords and the Android signing key. `bit pipeline --check`
  lists what is still missing.
- Let each environment deploy only from its own branch (`gh api`).
- Set the variables, and the secrets only the user's accounts can give, from the git-ignored file, with `gh variable set
  --env <name> -f <file>` and `gh secret set --env <name> -f <file>`, then delete the file. The names are in the table
  of `.docs/16- CI-CD Pipeline and Environments.md`, and what the workflows read (`vars.*`, `secrets.*`) is the final
  word. A setting the CD has no secret of its own for goes into the apps' settings secrets, one `Section__Key=value`
  line each.
- The landing page's settings go into `LANDINGPAGE_SETTINGS` the same way: the `smtp` connection string its waitlist
  emails go through, and the chat's AI key. Set the `LANDING_PAGE_ADDRESS`
  variable to its public address, and the deployment checks that it answers.
- The deployment adds `SERVER_ADDRESS` and `LANDING_PAGE_ADDRESS` to the apps' `TrustedOrigins`. Their emails link only
  to a trusted address, and any other address gets a 400, so both have to be the real public addresses.
- With GitHub Pro or Team, protect `develop` and `main`, so they take changes only through a pull request whose CI
  passed.

## 7. The pipeline

<!--#if (aspire == true)-->
The template's CD already works this way (`.github/workflows/cd-template.yml`, described in
`.docs/16- CI-CD Pipeline and Environments.md`): `build_server` builds the images on a GitHub-hosted runner and pushes
them to GHCR, and `deploy_server` runs on the environment's own runner, migrates the database, brings the Compose
project up, and fails unless every container is healthy and the public address answers. What is left:

- Check that the workflows still do this, since a team may have changed them since the project was created, and that
  the Test CD is for `develop` and the Production CD for `main`, on a push only once section 9 has turned that on.
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

- **One setup token, asked for once.** Have the user create a custom API token with, on the account, Cloudflare Tunnel,
  Access: Apps and Policies, Access: Organizations, Identity Providers, and Groups, Workers R2 Storage and Account API
  Tokens, each with Edit, and on all zones of the account, Zone and DNS with Edit and Cache Purge with Purge. With it,
  do everything below yourself through Cloudflare's API, for each environment. Beyond the token, the user only changes
  the domain's nameservers at the registrar.
- The setup token never goes into a GitHub secret, a file the VM reads, a commit or a log. Every token that runs is a
  narrower one you mint with it. Once the environments are up, the user deletes it, or rolls it if more environments
  are coming.
- Add the domain to the account on the free plan if it isn't there, and give the user the nameservers to set.
- Create a named tunnel for each environment, with its configuration kept in Cloudflare: its ingress for the hostnames
  below, a proxied CNAME for each hostname to the tunnel, and its connector token for the Compose project.
<!--#if (filesStorage == "S3")-->
- For R2, create the environment's bucket, then an account API token with Object Read & Write on that bucket only. The
  token's id is the S3 Access Key ID and the SHA-256 of its value is the Secret Access Key, so `S3_CONNECTION_STRING`
  needs nothing from the user. Check the keys with a write, a read and a delete before you store them.
<!--#endif-->
<!--#if (cloudflare == true)-->
- Mint a separate token for `CLOUDFLARE_TOKEN`, with Cache Purge on the environment's zone only, rather than reusing
  the setup token.
<!--#endif-->
- Create the Access applications and policies for SSH through the tunnel (section 2), and for the Workbench if the user
  wants it.
- Name every hostname one level under the domain, such as `app-test.example.com` rather than `app.test.example.com`:
  the free plan's certificate covers one level of subdomain only.
- The Workbench isn't deployed. If the user wants it on Test, never Production, put its hostname behind Cloudflare
  Access, Cloudflare's own sign-in, and let `/alive` through for the deploy job's check. Its screenshots need a base
  image with a browser, such as Playwright's.
<!--#if (cloudflare == true)-->
- The Compose project already runs the tunnel's connector with its token (`CLOUDFLARE_TUNNEL_TOKEN`). In Cloudflare,
  point the tunnel's public hostname at `http://serverweb:8080`, and the landing page's hostname at
  `http://landingpage:8080`.
<!--#if (api == "Standalone")-->
- The api gets a hostname of its own, pointed at `http://serverapi:8080`.
<!--#endif-->
<!--#else-->
- This project was created without the `cloudflare` option, so add cloudflared to the Compose project through the app
  host, with the tunnel's token as a secret, and point the tunnel's public hostname at the web app's service and the
  landing page's hostname at the `landingpage` service.
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

- Start the first deployment to Test: merge the settings change into `develop` through ai-dlc's phase 7, and run the
  Test CD by hand (`gh workflow run cd-test.yml --ref develop`), since a merge doesn't deploy yet. Watch the run. The
  deploy job already
  fails when a container isn't healthy or the public address doesn't answer; when it does, read that container's logs
  (`docker compose logs` in the deploy folder) before you change anything. Then check the environment at its public
  address: the home page, signing up with its email, and a file upload. Read whatever can't be seen from outside through that environment's
  dev-mcp.
- The migrations seed `test@bitplatform.dev`, a global admin whose password is in this repository, in every
  environment, and anyone can sign in with it until that changes. As soon as the environment's first deployment is
  up, before the checks above, change its password in the app and give the new one to the user to keep. Then open
  `<the web app's address>/settings/tfa` for them to turn two-factor sign-in on, since dev-mcp takes an account with
  it, have them put their own email and phone number on the account, where its reset links and codes go, and connect
  that environment's dev-mcp with this account.
- Then ask that environment's dev-mcp for its health (`GetHealth`), after the sign-in the troubleshoot skill
  describes. It runs every health check the server has and returns each one that isn't Healthy with its reason, which
  the deploy job's own check doesn't see, since a Degraded check still answers 200. Outside Development, every service
  whose settings are missing reports that they are not configured. Go through them with the user: configure each one
  (section 5), or, for a service they don't want, remove the code that uses it.
<!--#if (signalR == true)-->
- Check the AI chat and a voice call there too.
<!--#endif-->
- Check the landing page at its address too: a page in each language, the waitlist form and `/sitemap.xml`. Before
  launch, tell the user that its waitlist is kept in memory until it gets a home of its own, as the **landing-page**
  skill (`.github/agents/landing-page.agent.md`) describes, so every deployment empties it.
- Once Test works, uncomment the `push` block of `.github/workflows/cd-test.yml` and merge that through ai-dlc's phase
  7. Uncomment all of it, since `paths` without `branches` runs the CD on every branch. The workflow is in its own
  `paths`, so this merge is the first deployment that starts by itself: watch it, and fix whatever it shows before going
  on. Then tell the user that from now on every merge into `develop` that changes the app deploys.
- Production follows: a release PR from `develop` into `main`, merged on the user's go-ahead, then the Production CD by
  hand on the same go-ahead (`gh workflow run cd-production.yml --ref main`), since that merge doesn't deploy yet. Check
  it the way you checked Test.
- Once Production works, uncomment the `push` block of `.github/workflows/cd-production.yml` the same way, on a branch
  merged into `develop`. It reaches `main` with the next release, and since GitHub reads a push's workflow from the
  pushed commit, that release's merge is the first deployment to Production that starts by itself. Offer the user that
  release now, so this is proven before they rely on it, merge it only on their go-ahead, and watch the run. Tell them
  that from then on merging a release into `main` deploys it.
- Hand back a short list: what runs where, the public addresses, how to open the dashboards, where each secret lives,
  what is backed up, and how to roll back (deploy the images of the last good commit again, or revert the merge).
- After the first deployment to Production, unless the database already has a backup, end with a warning the user
  can't miss: nothing in this project backs the database up, so a lost disk or a bad migration can lose every record.
  Ask them to arrange a backup kept off the machine the database runs on, and to restore it once to see it works,
  before real users arrive.
