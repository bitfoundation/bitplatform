# bit CLI

Create [bit platform](https://bitplatform.dev) projects that are ready to run on Windows, macOS and Linux, translate `.resx` files with an LLM, decode stack traces minified by Bit.Minifier, and write the profiles that compile Blazor WebAssembly apps ahead of time.

```bash
dotnet tool install --global Bit.Cli --version 11.0.0-pre-15
bit new MyApp
```

Or run it without installing anything:

```bash
dnx Bit.Cli --version 11.0.0-pre-15 -- new MyApp
```

It needs a .NET 10 SDK or later. `dnx` asks once before it downloads the package.

## bit new

`bit new` creates a project from [bit Boilerplate](https://bitplatform.dev/templates) and gets it ready, so the first run works:

1. Takes the project name and options from its arguments. The [create project page](https://bitplatform.dev/templates/create-project) builds the whole command. A name that is also the name of a type in bit Boilerplate's code, like `App` or `Program`, is refused, because its namespace would hide that type and the project wouldn't build. When a newer bit is out, the run continues with it, see [Staying up to date](#staying-up-to-date).
2. Checks this machine, lists what the project needs and is missing, and installs what you tick: the .NET SDK the template's `global.json` pins, nuget.org as a package source when it's missing or disabled, Node.js, a container runtime that is free for commercial use (Podman on Windows, Colima with the docker CLI on macOS, Docker Engine on Linux; a Docker you already have is used as it is), WSL and the Windows Virtual Machine Platform that Podman runs on, Git, the Aspire CLI, the HTTPS development certificate, VS Code, the GitHub CLI when you ask for a GitHub repository, Windows long paths, and for native apps the Windows Hypervisor Platform the Android emulator uses and Windows developer mode, plus Python 3 on Linux when the offline database's native WebAssembly build needs it. Things already installed aren't shown, and neither is anything that doesn't apply to your operating system. A project that runs without its containers, such as one with SQLite and no Redis, still gets them, but a restart they ask for is a note, not a warning. The development certificate is trusted first, because Windows and macOS ask you to confirm it, so once that and the administrator prompt are answered you can leave it to finish. On Windows that one prompt, right after the plan, also covers the build support and the Android SDK when their folders need administrator rights. The versions come from the project: the .NET SDK from `global.json`, Node.js from `engines.node` in Client.Core's `package.json`, and the Aspire CLI from the AppHost's Aspire version, so an older Aspire CLI is updated to it. When nvm, fnm or Volta manages Node.js, Node.js is installed or updated through it, so the version it picks is the one that runs. On macOS with a native app, it also checks that Xcode is the one the project's .NET for iOS asks for.
3. Warns, only when it's sure, about hardware that makes development slow: virtualization turned off in the BIOS or UEFI while the project's containers or the Android emulator need it, less than 15 GB of memory, or a project drive that is a hard disk rather than an SSD. Drives are judged only when they're internal and on real hardware; USB drives, virtual disks and virtual machines are left alone.
4. Creates the project, with a development certificate of its own, and keeps only its `.slnx`: the `.sln` the template also ships for older Visual Studio versions is removed.
5. Initializes git with `develop` and `main`, and commits. When git doesn't know your name yet, it commits as your user name and `<user name>@git.com`, set on this repository only. On Windows it also turns on git's own long path support (`core.longpaths`), which Windows long paths don't cover.
6. Checks that `dotnet` picks an SDK the project's `global.json` accepts; when it doesn't, the steps that need one are skipped with a single note instead of failing one by one. With Aspire, it records in the AppHost's user secrets whether Docker or Podman runs the containers, whichever answers, since Aspire would otherwise pick again on every run and prefer a Docker Desktop that has hung over the Podman the containers are on. Then installs the build support the chosen platforms need (.NET workloads: WebAssembly, and the native apps'), restores NuGet packages, and builds, which also generates the CSS and JS. With only the web app it builds `Web.slnf`; once any native app is picked it builds the whole `.slnx`, every app the MAUI project targets on this OS included, so it also installs their build support and the Android SDK. When the build fails with Razor errors, it restarts the C# compiler server and builds once more: a compiler server that has gone bad reports Razor errors in files that are fine, and keeps doing so for every build until it restarts. Then it installs the Chromium that Playwright runs the UI tests with.
7. Runs `dotnet format`, and commits that on its own.
8. Adds the `Initial` EF Core migration, and commits it on its own. The app applies migrations when it starts.
9. With `--github-repo`, signs you in to GitHub when needed: it shows the one-time code, opens GitHub's device page in your browser, and Esc skips the sign-in and the repository if it gets stuck. Then it creates a private repository named after the project, pushes `develop` and `main` to it, and makes `develop` its default branch.
10. Marks the folder as trusted for VS Code, Claude Code, Copilot CLI, Codex and Gemini CLI, so the project's tasks and MCP servers work without prompts. That works even when VS Code was just installed and has never run.
11. Installs the VS Code extensions the project recommends, like the C# extension and Claude Code. Copilot is built into VS Code, so it isn't one of them. bit checks in VS Code that each one is really there afterwards, tries the missing ones once more, and names any it couldn't install.
12. Starts the project: with Aspire, `aspire start`; without it, `dotnet run` of Server.Web (and of Server.Api when the API is standalone). It waits until the servers are healthy, comes last so everything else is ready however long it takes, and while it waits, shows which resources are still starting or unhealthy, and stops waiting as soon as one fails to start. It asks `aspire doctor` first whether Aspire can use Docker or Podman. Without one, a project whose servers wait for a container (SQL Server, PostgreSQL, MySQL or Redis) isn't started, with why and how to fix it; any other project starts without its containers, so only Keycloak, Mailpit and the like are missing.
13. Opens the project in VS Code, or the IDE you pick. All that's left is signing in to Claude or Copilot.
14. Opens the Aspire dashboard and then the app in your browser, and keeps the project running while the terminal you ran bit in is open. Close it, or press Ctrl+C, to stop the project. Without a terminal, such as under a coding agent or in a dev container, it only starts the project once with Aspire, so the container images are pulled, and stops it; CI starts nothing.

A step that fails doesn't stop the rest: every step runs, and the summary lists the commands that finish whatever didn't work. The first run takes a few minutes, mostly build support and the first build.

```bash
bit new Contoso.Shop
bit new Contoso.Shop --database PostgreSQL --module Admin --redis
bit new Contoso.Shop --platforms web,android
bit new Contoso.Shop --yes --no-open --no-trust
```

### Options

Every option of the bit Boilerplate template works the same way:

| Option | Values | Default |
|---|---|---|
| `--framework` | `net10.0`, `net11.0` | `net10.0` |
| `--database` | `Sqlite`, `SqlServer`, `PostgreSQL`, `MySql`, `Other` | `Sqlite` |
| `--filesStorage` | `Local`, `S3`, `AzureBlobStorage`, `Other` | `Local` |
| `--api` | `Integrated`, `Standalone` | `Integrated` |
| `--pipeline` | `GitHub`, `Azure`, `None` | `GitHub` |
| `--module` | `None`, `Admin`, `Sales` | `None` |
| `--captcha` | `None`, `reCaptcha` | `None` |
| `--theme` | `Fluent2`, `Fluent`, `Cupertino`, `Material` | `Fluent2` |
| `--aspire` | `true`, `false` | `true` |
| `--multitenant` | `true`, `false` | `true` |
| `--notification` | `true`, `false` | `true` |
| `--cloudflare` | `true`, `false` | `true` |
| `--redis` | `true`, `false` | `false` |
| `--signalR` | `true`, `false` | `true` |
| `--offlineDb` | `true`, `false` | `false` |
| `--sentry` | `true`, `false` | `false` |
| `--appInsights` | `true`, `false` | `false` |
| `--ads` | `true`, `false` | `false` |
| `--brouter` | `true`, `false` | `true` |
| `--sample` | `true`, `false` | `false` |
| `--advancedTests` | `true`, `false` | `false` |
| `--apiServerUrl` | a URL | |
| `--webAppUrl` | a URL | |

And these of its own:

| Option | What it does |
|---|---|
| `-o, --output <dir>` | Create the project there. Default: `./<name>`. |
| `--platforms web,android,ios,macos,windows` | Platforms to set up and build on this machine now. Every project has all of them; the web app is always set up. Any native app means building the whole solution, which adds several GB of build support and minutes of build, so it can wait for `bit setup`. iOS and macOS build on Windows and on a Mac, Windows builds on Windows, and Linux builds web and Android. Running the iOS app from Windows needs a Mac on the network, and the macOS app runs on macOS. |
| `--tools node,docker,...` | Tools to install when missing. Default: the ones the project needs. `none` installs nothing. |
| `--ide code\|vs\|rider\|none` | Open the project in this IDE. Default: VS Code; `none` in CI. bit installs and sets up VS Code only; Visual Studio and Rider are opened when they're already installed. |
| `--template-version <version>` | The bit Boilerplate version. Default: the CLI's own version. |
| `--template-package <nupkg or folder>` | Create from a local Bit.Boilerplate package, or from the template's folder in a bitplatform checkout, the way this repository's CI does. |
| `--github-repo` | Create a private GitHub repository for the project and push to it. Needs the GitHub pipeline, the default. |
| `--no-update` | Create it with this bit even when a newer one is out. |
| `-y, --yes` | Install the tools the project needs and create it without asking. Windows' permission prompt, `sudo`'s password and the GitHub sign-in still show when there's a terminal. |
| `--non-interactive` | Never ask; fail when a required value is missing. |
| `--dry-run` | Show the plan and change nothing. |
| `--no-setup` | Only create the project: nothing is installed, restored or built. Run `bit setup` in its folder later. |
| `--no-tools`, `--no-certificate`, `--no-git`, `--no-workloads`, `--no-restore`, `--no-build`, `--no-browsers`, `--no-format`, `--no-migration`, `--no-trust`, `--no-open`, `--no-run` | Skip that step. |
| `-p, --property <name=value>` | An MSBuild property for the restore and the build, e.g. `-p:EnforceCodeStyleInBuild=true`. Repeat it for more. `bit setup` takes it too. |

## bit setup

Gets an existing project ready on this machine, e.g. after cloning it, or adds a platform later:

```bash
bit setup
bit setup --platforms android
```

With a native platform, it first checks that the app id in Client.Maui's project file is one the native apps can have. It installs missing tools, build support and packages, builds, installs Playwright's Chromium for the UI tests, installs the VS Code extensions the project recommends when VS Code is installed, and starts the project the way `bit new` does.

## bit diff

Shows what changed in bit Boilerplate between two versions, for your project:

```bash
bit diff
bit diff 10.6.2 10.7.0
bit diff --template-package ../bitplatform/src/Templates/Boilerplate/Bit.Boilerplate
```

- It creates the project with each version, the old one first, into one git repository in your home folder, so the second commit is exactly the upgrade, and writes it as `bit-diff.patch` in that repository too. Your project isn't touched.
- In a project, it takes the name, the version and the options from the project's `bit.json`, where the template records them. Pass `from`, `to`, `--name` or any option of `bit new` to change them. `to` defaults to this bit's own version.
- An option a version doesn't have is left out for that version, and said so.
- Both sides get the project's own .NET version. `--framework` gives only the newer side another one, so `bit diff --framework net11.0` shows what moving a .NET 10 project to .NET 11 changes.
- What the template picks at random on every run, like ports, GUIDs, dates and the seeded accounts' password, differs between the two, and so does what `bit new` adds after creating a project.
- The repository goes to `~/.bitplatform/diff/<name>-<from>-<to>`, or `-o <folder>`. Delete it with `bit remove <folder> --yes` when you're done.

The project's `upgrade` skill, for the AI coding tools, runs it and applies the changes to your code.

## bit pipeline

Fills in the secrets a project's GitHub Actions workflows use:

```bash
bit pipeline
bit pipeline --check
bit pipeline --env Production
bit pipeline --rotate-certificate
```

- It reads the secrets from the workflows themselves, so a pipeline you changed is read as it is. It follows the reusable workflows they call, in the repository or in another one, with the inputs and secrets they're given, and finds the environment each job deploys to, like `environment: ${{ inputs.ENV_NAME }}` with `ENV_NAME: Production` from its caller.
- It creates the environments that don't exist yet, and stores each secret in the environment of the jobs that use it, or in the repository for a job without one. A secret the repository already has counts as set everywhere.
- It creates what it can: a VAPID key pair (`PUBLIC_VAPIDKEY`, `PRIVATE_VAPIDKEY`), an app certificate (`APP_CERTIFICATE`) and an Android signing key (`ANDROID_RELEASE_KEYSTORE_FILE_BASE64` and its two passwords), whose fingerprint it adds to the project's `assetlinks.json`. With an App Store Connect API key you give it, it also creates the iOS app's signing, as `bit keys apple` does, once for every environment that needs it, and adds the app to `apple-app-site-association.json`. It asks for the rest; `@file` reads the value from a file, and an empty answer skips it. `--yes` skips what only you can give.
- Values go to the GitHub CLI through its input, never on a command line, and bit keeps a copy of the keys it creates in `~/.bitplatform/keys`, since GitHub doesn't give a secret back. Google Play only takes updates signed with the same key, so save the keystore and its password somewhere safe too.
- `--check` changes nothing: it lists the secrets and exits with code 3 when one is missing.
- `--rotate-certificate` gives each environment a new app certificate and keeps the old one as `APP_CERTIFICATE_RETIRED`, so tokens it signed keep working.

It needs the GitHub CLI, signed in with admin rights on the repository. For another pipeline, `bit keys` creates the same keys for you to store yourself.

## In CI

`bit new` and `bit setup` prepare CI machines too, so a pipeline needs no steps of its own for Node.js, workloads, Playwright or the development certificate. The pipelines bit Boilerplate ships install bit once per job and run it before they publish:

```bash
dotnet tool install --global Bit.Cli --version 11.0.0-pre-15
bit setup --platforms android --no-browsers --yes
```

A CI build job lets bit build too, with its own MSBuild properties, and skips the browsers it won't test with:

```bash
bit setup --platforms android --no-browsers --yes -p:EnforceCodeStyleInBuild=true
```

A job that publishes lets bit build first too, so its CSS and JS are generated before `dotnet publish`. A web job passes `-p:Configuration=Release` and its `-p:Version`, so the publish reuses that build; a native job builds Debug, and its Release publish reuses the same JavaScript, which bit Boilerplate always minifies, with a source map.

In CI, bit installs what a build and its tests need and leaves alone what only a developer's machine needs:

- Docker or Podman, WSL, the Aspire CLI and Windows features aren't installed, and the project isn't started.
- The HTTPS development certificate is trusted on Linux only. There it's trusted for some clients, and the step notes that .NET's own HTTPS calls also need `~/.aspnet/dev-certs/trust` in `SSL_CERT_DIR`; any other failure is a warning.
- MAUI's `InstallAndroidDependencies` completes the runner's Android SDK, so a pipeline needs no `sdkmanager` step.
- Playwright gets every browser with its system libraries, since CI may test more than Chromium.

## bit doctor

```bash
bit doctor
bit doctor --fix
```

Checks the tools a project needs, inside a project for that project's own needs, and offers to install what's missing with `--fix`. It exits with code 3 when something needed is missing.

## bit trust

```bash
bit trust
```

Marks the git repository around the current folder as trusted for VS Code, Claude Code, Copilot CLI, Codex and Gemini CLI, e.g. after cloning a project.

## bit clean

```bash
bit clean
```

Deletes the project's build outputs and downloaded packages, so the next build starts from scratch:

- On Windows it first stops every app that runs from the project folder, like the app itself, its Aspire AppHost, or Node.js and Sass from `node_modules`. macOS and Linux don't lock open files, so there nothing is stopped.
- Runs the project's own `Clean.bat` on Windows or `Clean.sh` on macOS and Linux, which deletes `bin`, `obj`, `node_modules` and the generated CSS and JS.
- When a `bin`, `obj` or `.vs` folder is still there on Windows, it asks Windows which processes hold its files. A build process among them (`dotnet`, `MSBuild`, `VBCSCompiler`, WinUI's `XamlCompiler`, or a tool one of them started) is stopped, that one process and no other build server, and the script runs once more. Any other app that holds them, such as an IDE, keeps running and is named.

Run `bit setup` afterwards to restore and build again.

## bit rename

```bash
bit rename Contoso.Shop
```

Renames the project everywhere its name appears: files, folders, namespaces, the solution, database names and the app id.

- In a git repository it needs your changes committed or stashed first, so you can review the rename with `git diff` and undo it with `git checkout`. Outside git it asks first, or needs `--yes`.
- Stops the same apps `bit clean` stops, and deletes `bin`, `obj` and `.vs`, which hold the old name. A build process that holds one of their files is stopped and the deletion retried, like `bit clean` does.
- For a project named `MyApp`, it replaces `MyApp` where a name starts with it, as in `MyApp.Server`, `Projects.MyApp_Server_Api` and the database name `MyAppDb`, and `myapp` in the app id `com.companyname.myapp` and in names like `myapp-e2e`; for a name with dots, also the same name with underscores. `TheMyApp`, `MyApps`, `MyAppService`, a `myapp` variable and a later part of a dotted name, like the `App` of `Microsoft.AspNetCore.App`, stay as they are.
- Refuses a new name that is also the name of a type in the project's code, like `App`, and a project whose own name is one, since it can't tell the two apart. Rename that type first. It also refuses a name that would leave the native apps with an invalid app id.
- Skips `.git`, `node_modules` and binary files. Every file keeps its encoding and line endings.
- Leaves the project's own folder named as it was, since your terminal is usually in it. Rename it after closing the editors and terminals that use it.

Then review the changes with `git diff`, and run `bit setup` to restore and build.

## bit remove

```bash
bit remove
bit remove ../Contoso.Shop --yes
```

Deletes a project's folder for good. On Windows that fails while the app, a build process or Node.js still holds its files, so:

- It asks first, and says when the project has changes that aren't committed or commits that aren't pushed. Without a terminal to ask in, it needs `--yes`.
- It stops the apps that run from the folder, the same way `bit clean` does, then deletes the folder, read-only files like git's included. When a file is still in use, it stops the build process that holds it, if one does, like `bit clean` does, and tries once more. The failure names any other app that still holds a file.
- When a terminal is still in the folder, everything in it is deleted and the empty folder stays. Delete it after leaving it.
- The project's Aspire containers and volumes stay in Docker. Remove them in Docker when you no longer need their data.

## bit translate

Fills in the missing translations of `.resx` files with an OpenAI-compatible LLM, and keeps the existing ones:

```bash
bit translate
bit translate --language fa --language de
bit translate --check
bit translate --dry-run
```

It reads `Bit.ResxTranslator.json` from the current folder or the nearest one above it (or `--config <file>`):

```jsonc
{
  "DefaultLanguage": "en",
  "SupportedLanguages": [ "nl", "fa", "sv", "hi", "zh", "es", "fr", "ar", "de" ],
  "ResxPaths": [ "/src/**/*.resx" ],
  "ChatOptions": { "Temperature": "0" },
  "OpenAI": {
    "Model": "gpt-4.1-mini",
    "Endpoint": "https://api.openai.com/v1",
    "ApiKey": null
  }
}
```

- `ResxPaths` are globs relative to the config file's folder, for the base `.resx` files.
- Put the key in the `OpenAI__ApiKey` environment variable rather than in the file. `OpenAI__Model` and `OpenAI__Endpoint` work too. Without a key it translates nothing and says so, without failing a pipeline.
- Any OpenAI-compatible endpoint works: OpenAI, Azure AI Foundry (`https://YOUR_AZURE_FOUNDRY.services.ai.azure.com/openai/v1`), Google AI Studio (`https://generativelanguage.googleapis.com/v1beta/openai`), xAI (`https://api.x.ai/v1`).
- Strings go in batches of 250. A translation that drops or renumbers a `{0}` placeholder is asked for once more, and left out if it's still wrong, so the next run tries it again.
- `--check` calls no model and exits with code 3 when anything is missing, for CI.

In a pipeline:

```yaml
- name: Install the bit CLI
  run: dotnet tool install --global Bit.Cli --version 11.0.0-pre-15

- name: Translate .resx files
  env:
    OpenAI__ApiKey: ${{ secrets.OPENAI_APIKEY }}
  run: bit translate
```

## bit decode

Reads a stack trace of an app minified by [Bit.Minifier](https://github.com/bitfoundation/bitplatform/tree/develop/src/Toolings/Minifier) back into the names its source has:

```bash
bit decode bit-minifier.map trace.txt
bit decode < trace.txt
```

Without a map, the newest `obj/**/bit-minifier.map` under the current folder is used. Without a trace file, the trace is read from standard input.

The map's first line names the Bit.Minifier that wrote it, and `bit decode` runs that version from the NuGet cache, restoring it first when it isn't there, so a map is always read the way it was written.

## bit keys

Creates the keys and certificates a bit Boilerplate project signs and encrypts with:

```bash
bit keys vapid
bit keys certificate --production -o ../keys
bit keys certificate --rotate -o ../keys
bit keys android
bit keys apple --issuer-id 57246542-96fe-1a63-e053-0824d011072a --key-id 2X9R4HXF34 --private-key AuthKey_2X9R4HXF34.p8
```

`bit keys vapid` prints a new key pair for web push notifications: the public key goes in `AdsPushVapid:PublicKey` of Client.Web, the private key in `AdsPushVapid:PrivateKey` of the server.

`bit keys certificate` creates the `AppCertificate.crt` and `.key` the server signs tokens and encrypts its Data Protection keys with, RSA 3072 with SHA-256:

- In a project, without options, a development one (`OU=Development`) in Server.Api's folder. `--production` makes one for a deployed app (`OU=Production`, valid for a year), and refuses a folder where git would commit its private key.
- `--rotate` renames the current pair to `AppCertificate.<date>.crt` and `.key`, which the server keeps trusting for what it signed, and creates a new one with the same subject.
- `--pfx` also writes `AppCertificate.pfx` behind a generated password. `--list` shows the folder's certificates, which one is active, their key ids and when they expire.

`bit keys android` creates the keystore that signs the Android app for Google Play: an RSA 2048 key valid for 30 years under the project's name as its alias, with one generated password for the keystore and the key. It goes to `~/.bitplatform/keys/<project>` or `-o <file>`, never into the repository, and in a project its SHA-256 fingerprint is added to `assetlinks.json` for the app's id. Google Play only takes updates signed with the same key, so save it and its password somewhere safe.

`bit keys apple` creates the signing the CD needs to publish the iOS app, without a Mac, through the App Store Connect API:

- It needs a team API key with the Admin role, made in App Store Connect under Users and Access, Integrations: its issuer id, its key id and the `AuthKey_<key id>.p8` file App Store Connect lets you download once.
- Registers the bundle id for iOS when the team doesn't have it yet. In a project's folder the bundle id is the `ApplicationId` of its Client.Maui project; elsewhere pass `--bundle-id`.
- Creates a new private key, an Apple Distribution certificate for it, and an App Store provisioning profile for the bundle id and that certificate, named `<bundle id> App Store` or `--profile-name`. A profile with that name is replaced. When the profile can't be created, the new certificate is revoked again.
- Writes `<bundle id>.p12`, the certificate with its private key behind a generated password, and `<bundle id>.mobileprovision` to the current folder or `-o <folder>`. It never overwrites a `.p12`, which may be the only copy of a certificate's private key.
- Prints the password, only this once, the team id, and the CD secret or variable each value goes in: `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64` (the `.p12` in base64), `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD`, `APPSTORE_API_KEY_ISSUER_ID`, `APPSTORE_API_KEY_ID`, `APPSTORE_API_KEY_PRIVATE_KEY`, `IOS_CODE_SIGN_PROVISION` and `APP_ID`.

Apple allows a team only a few distribution certificates. When the team has them all, `bit keys apple` lists them: revoke one that no build signs with anymore in the [Apple Developer site](https://developer.apple.com/account/resources/certificates/list) and run it again. Apps already in the App Store keep working.

## bit aot-profile

Writes the profile that profile-guided AOT compiles a Blazor WebAssembly app with. Compiling every method ahead of time makes the app fast but its download much bigger. With a profile, only the methods the app really runs are compiled and the rest stay on the interpreter, so the download stays small and the code that runs is still compiled.

```bash
bit aot-profile src/Client/MyApp.Client.Web/MyApp.Client.Web.csproj -o app.aotprofile -p:InvariantGlobalization=true
dotnet publish src/Client/MyApp.Client.Web/MyApp.Client.Web.csproj -c Release -p:RunAOTCompilation=true -p:InvariantGlobalization=true -p:WasmAotProfilePath=app.aotprofile
```

1. Publishes the project in Release on the interpreter, with Mono's AOT profiler in it. Pass the `-p` properties the AOT publish gets: both publishes then trim alike and, with Bit.Minifier, minify to the same names, and the profile names methods by those names.
2. Starts it. A server project runs on a free local port. A standalone Blazor WebAssembly app is served from its `wwwroot` by the browser itself at `--origin`, `http://localhost:5000` by default; pass the address the app is deployed at when its API only accepts requests from there.
3. Opens it in a headless Chromium, Chrome or Edge without its service worker, waits for .NET to start, and follows the app's links to every page it reaches, up to `--max-pages` (50). Then it does the `--step`s, each a path, `click:<css selector>`, `key:<key>` or `wait:<milliseconds>`. A link that reloads the app is left out and the visit starts over without it.
4. Writes the profile: every method that ran, every `[UnmanagedCallersOnly]` method, which the compiled app calls directly and can't run without, and with `--include <assembly>` every method of that assembly, run or not.

The app needs no change for it. The browser is Playwright's Chromium when it's installed (`bit setup` installs it for a bit Boilerplate project's UI tests), then Chrome, then Edge, or `--browser <path>`. The publish goes to the temp folder and is deleted at the end.

Collect the profile in the job that publishes the app, right before the AOT publish, rather than keeping one in the repository: a profile describes one build of the app.

## Staying up to date

`bit new` creates every project from the newest stable bit Boilerplate. As it starts, it looks up the newest `Bit.Cli` on this machine's NuGet sources. When a newer one is out, it installs it next to itself in `~/.bitplatform/cli`, and the same command carries on with it in the same terminal, so nothing is asked twice. When the run ends, the installed global tool updates to that version in the background, with its log in `~/.bitplatform/logs`.

- `--template-version <version>` hands the run to the bit of that version, which knows that template best, and leaves the installed bit as it is.
- It never happens in CI, with `--template-package`, under `dnx`, which runs the version you ask it for, or in a build from source, so CI and local builds always run the code they were built from.
- `--no-update`, or `BIT_CLI_NO_UPDATE=1`, keeps the bit you have.
- `bit update` updates the installed bit on request.

## What it changes on your machine

- **Tools** you tick, with `winget` on Windows, Homebrew on macOS and the distribution's package manager on Linux. Each command is shown before it runs.
- **The .NET SDK** a project pins, next to the SDKs you already have: with `winget` on Windows (Microsoft's `dotnet-install.ps1` when winget doesn't list that build yet), Microsoft's installer package on macOS after checking it's signed by Microsoft, and Microsoft's `dotnet-install.sh` on Linux.
- **VS Code extensions** the project's `.vscode/extensions.json` recommends, only the missing ones, with `code --install-extension`.
- **Playwright's Chromium**, in Playwright's own browser folder, with the driver the project's tests were built with.
- **A container runtime**, when you tick it: on Windows, Podman for your user only, its WSL machine, and a sign-in entry under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` that starts that machine; on macOS, Colima, the docker CLI and buildx, with Colima started at sign-in by `brew services`; on Linux, Docker Engine.
- **Container images** the project's AppHost uses, which Docker or Podman pulls during the first Aspire start and keeps.
- **Administrator rights**: on Windows, the steps that need them (long paths, WSL, installers that need admin) run in one elevated PowerShell, so Windows asks once. It runs only the script bit wrote, checked by its hash, gives each command a time limit, and Ctrl+C stops it. On macOS and Linux, `sudo` asks for your password once.
- **A private GitHub repository**, only with `--github-repo`, on the account you sign in with. The GitHub CLI keeps that sign-in, and git uses it to push.
- **Trust entries**, only for the folder `bit new` created or the one you pass to `bit trust`: `projects` in `~/.claude.json`, `trustedFolders` in `~/.copilot/config.json`, `[projects]` in `~/.codex/config.toml`, `~/.gemini/trustedFolders.json`, and VS Code's trust store in `~/.vscode-shared/sharedStorage/state.vscdb` (only while VS Code isn't running). Each file keeps everything else in it. To undo, delete the entry, or use each tool's own trust settings.
- **bit itself**, when a newer version is out: a copy for the run in `~/.bitplatform/cli`, then `dotnet tool update --global Bit.Cli` once the run ends.
- **Secrets and environments** of your GitHub repository, only with `bit pipeline`, after you confirm or with `--yes`.
- **Its own folder**, `~/.bitplatform`: `settings.json`, the template cache, logs, telemetry not sent yet, the repositories `bit diff` creates, and in `keys` the keys `bit pipeline` and `bit keys android` create, which only your user can read on macOS and Linux.

## Telemetry

bit sends error reports to the bit platform team so failed runs get fixed: whether each step worked and how long it took, error codes like `NU1301`, which check refused a command, like `folder-not-empty` or `name-missing`, the type and stack trace of a crash, the OS, .NET and bit versions, the bit version a run was handed over from, whether it runs in CI, through `dnx` or for a coding agent, and which hardware warnings it showed. A random ID, made on the first run, ties one machine's runs together.

It never sends project names, paths, user or machine names, file contents, `.resx` text, prompts, API keys, environment variable values, feed URLs, git remotes, exception messages or the output of the tools it runs.

With `bit telemetry all`, or a yes to the question the first `bit new` asks, it also sends the template options, platforms, tools and IDE you pick, and translation volumes.

| To | Do |
|---|---|
| see the setting and why | `bit telemetry` |
| change it | `bit telemetry off`, `bit telemetry errors` or `bit telemetry all` |
| turn it off in CI or a container | `BIT_CLI_TELEMETRY=off`, or `DO_NOT_TRACK=1`, or `DOTNET_CLI_TELEMETRY_OPTOUT=1` |
| see exactly what would be sent | `BIT_CLI_TELEMETRY=log` prints every item to stderr and sends nothing |

The data goes to Azure Monitor (Application Insights). A build without a telemetry endpoint, like a local one, sends nothing.

## Where this build comes from

```bash
bit about
```

shows the version, the commit and the GitHub Actions run that built it. Release packages are built from this repository by GitHub Actions, which signs the bit assemblies inside them, `bit.dll` included, and attests the provenance and the SBOM of every package and of those assemblies. On Windows, `bit about` says who signed the `bit.dll` it runs once Windows has checked the signature. For a release, it also asks GitHub whether it holds a build attestation for that exact `bit.dll`, and prints the command that verifies it fully:

```bash
gh attestation verify ~/.dotnet/tools/.store/bit.cli/<version>/bit.cli/<version>/tools/net10.0/any/bit.dll --repo bitfoundation/bitplatform
```

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Done |
| 1 | A step failed |
| 2 | Wrong usage, or a value is missing |
| 3 | A check failed: `bit translate --check`, `bit doctor`, `bit pipeline --check` |
| 130 | Canceled |

## Coming from the old tools

| Before | Now |
|---|---|
| `dnx Bit.ResxTranslator` | `dnx Bit.Cli --version 11.0.0-pre-15 -- translate`, or `bit translate` |
| `dnx Bit.Minifier.Cli --decode map trace` | `dnx Bit.Cli --version 11.0.0-pre-15 -- decode map trace`, or `bit decode map trace` |
