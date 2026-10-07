# bit CLI

Create [bit platform](https://bitplatform.dev) projects that are ready to run on Windows, macOS and Linux, translate `.resx` files with an LLM, and decode stack traces minified by Bit.Minifier.

```bash
dotnet tool install --global Bit.Cli --prerelease
bit new MyApp
```

Or run it without installing anything:

```bash
dnx Bit.Cli --prerelease -- new MyApp
```

It needs a .NET 10 SDK or later. `dnx` asks once before it downloads the package.

## bit new

`bit new` creates a project from [bit Boilerplate](https://bitplatform.dev/templates) and gets it ready, so the first run works:

1. Takes the project name and options from its arguments. The [create project page](https://bitplatform.dev/templates/create-project) builds the whole command. When a newer bit is out, the run continues with it, see [Staying up to date](#staying-up-to-date).
2. Checks this machine, lists what the project needs and is missing, and installs what you tick: the .NET SDK the template's `global.json` pins, nuget.org as a package source when it's missing or disabled, Node.js, Docker with its license already accepted, WSL and the Windows Virtual Machine Platform that WSL 2 and Docker Desktop run on, Git, the Aspire CLI, the HTTPS development certificate, VS Code, the GitHub CLI when you ask for a GitHub repository, Windows long paths, and for native apps the Windows Hypervisor Platform the Android emulator uses and Windows developer mode, plus Python 3 on Linux when the offline database's native WebAssembly build needs it. Things already installed aren't shown, and neither is anything that doesn't apply to your operating system. The development certificate is trusted first, because Windows and macOS ask you to confirm it, so once that and the administrator prompt are answered you can leave it to finish. The versions come from the project: the .NET SDK from `global.json`, Node.js from `engines.node` in Client.Core's `package.json`, and the Aspire CLI from the AppHost's Aspire version, so an older Aspire CLI is updated to it. When nvm, fnm or Volta manages Node.js, Node.js is installed or updated through it, so the version it picks is the one that runs. On macOS with a native app, it also checks that Xcode is the one the project's .NET for iOS asks for.
3. Warns, only when it's sure, about hardware that makes development slow: virtualization turned off in the BIOS or UEFI while Docker Desktop or the Android emulator needs it, less than 24 GB of memory, or a project drive that is a hard disk rather than an SSD. Drives are judged only when they're internal and on real hardware; USB drives, virtual disks and virtual machines are left alone.
4. Creates the project, with a development certificate of its own, and keeps only its `.slnx`: the `.sln` the template also ships for older Visual Studio versions is removed.
5. Initializes git with `develop` and `main`, and commits. When git doesn't know your name yet, it commits as your user name and `<user name>@git.com`, set on this repository only. On Windows it also turns on git's own long path support (`core.longpaths`), which Windows long paths don't cover.
6. Checks that `dotnet` picks an SDK the project's `global.json` accepts; when it doesn't, the steps that need one are skipped with a single note instead of failing one by one. Then installs the build tools the chosen platforms need (.NET workloads), restores NuGet packages, and builds, which also generates the CSS and JS. With only the web app it builds `Web.slnf`; once any native app is picked it builds the whole `.slnx`, every app the MAUI project targets on this OS included, so it also installs the MAUI build tools and the Android SDK. When the build fails with Razor errors, it restarts the C# compiler server and builds once more: a compiler server that has gone bad reports Razor errors in files that are fine, and keeps doing so for every build until it restarts. Then it installs the Chromium that Playwright runs the UI tests with.
7. Runs `dotnet format`, and commits that on its own.
8. Adds the `Initial` EF Core migration, and commits it on its own. The app applies migrations when it starts.
9. With `--github-repo`, signs you in to GitHub in your browser when needed, creates a private repository named after the project, pushes `develop` and `main` to it, and makes `develop` its default branch.
10. Marks the folder as trusted for VS Code, Claude Code, Copilot CLI, Codex and Gemini CLI, so the project's tasks and MCP servers work without prompts. That works even when VS Code was just installed and has never run.
11. Installs the VS Code extensions the project recommends, like C# Dev Kit, Copilot and Claude Code.
12. With Aspire, starts the project once with `aspire start`, waits until its server is healthy and stops it with `aspire stop`, so the container images are already pulled when you first start it from the IDE. It comes last, so everything else is ready however long it takes. It asks `aspire doctor` first whether Aspire can use Docker, and when it can't, says why and how to fix it instead of starting. While it waits, it shows which resources are still starting or unhealthy, and it stops waiting as soon as one fails to start or Docker can't run the containers.
13. Opens the project in VS Code, or the IDE you pick. All that's left is signing in to Claude or Copilot.

A step that fails doesn't stop the rest: every step runs, and the summary lists the commands that finish whatever didn't work. The first run takes a few minutes, mostly build tools and the first build.

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
| `--signalR` | `true`, `false` | `false` |
| `--offlineDb` | `true`, `false` | `false` |
| `--sentry` | `true`, `false` | `false` |
| `--appInsights` | `true`, `false` | `false` |
| `--ads` | `true`, `false` | `false` |
| `--brouter` | `true`, `false` | `false` |
| `--sample` | `true`, `false` | `false` |
| `--advancedTests` | `true`, `false` | `false` |
| `--realProject` | `true`, `false` | `false` |
| `--apiServerUrl` | a URL | |
| `--webAppUrl` | a URL | |

And these of its own:

| Option | What it does |
|---|---|
| `-o, --output <dir>` | Create the project there. Default: `./<name>`. |
| `--platforms web,android,ios,macos,windows` | Platforms to set up and build on this machine now. Every project has all of them; the web app is always set up. Any native app means building the whole solution, which adds several GB of build tools and minutes of build, so it can wait for `bit setup`. iOS and macOS need a Mac, Windows needs Windows. |
| `--tools node,docker,...` | Tools to install when missing. Default: the ones the project needs. `none` installs nothing. |
| `--ide code\|vs\|rider\|none` | Open the project in this IDE. Default: VS Code; `none` in CI. |
| `--template-version <version>` | The bit Boilerplate version. Default: the CLI's own version. |
| `--template-package <nupkg or folder>` | Create from a local Bit.Boilerplate package, or from the template's folder in a bitplatform checkout, the way this repository's CI does. |
| `--github-repo` | Create a private GitHub repository for the project and push to it. Needs the GitHub pipeline, the default. |
| `--no-update` | Create it with this bit even when a newer one is out. |
| `-y, --yes` | Install the tools the project needs and create it without asking. Windows' permission prompt, `sudo`'s password and the GitHub sign-in still show when there's a terminal. |
| `--non-interactive` | Never ask; fail when a required value is missing. |
| `--dry-run` | Show the plan and change nothing. |
| `--no-setup` | Only create the project: nothing is installed, restored or built. Run `bit setup` in its folder later. |
| `--no-tools`, `--no-certificate`, `--no-git`, `--no-workloads`, `--no-restore`, `--no-build`, `--no-browsers`, `--no-format`, `--no-migration`, `--no-trust`, `--no-open` | Skip that step. |
| `-p, --property <name=value>` | An MSBuild property for the restore and the build, e.g. `-p:EnforceCodeStyleInBuild=true`. Repeat it for more. `bit setup` takes it too. |

## bit setup

Gets an existing project ready on this machine, e.g. after cloning it, or adds a platform later:

```bash
bit setup
bit setup --platforms android
```

It installs missing tools, build tools and packages, builds, installs Playwright's Chromium for the UI tests, starts the project once with Aspire when it has an AppHost, and installs the VS Code extensions the project recommends when VS Code is installed.

## In CI

`bit new` and `bit setup` prepare CI machines too, so a pipeline needs no steps of its own for Node.js, workloads, Playwright or the development certificate. The pipelines bit Boilerplate ships install bit once per job and run it before they publish:

```bash
dotnet tool install --global Bit.Cli --prerelease
bit setup --platforms android --no-browsers --yes
```

A CI build job lets bit build too, with its own MSBuild properties, and skips the browsers it won't test with:

```bash
bit setup --platforms android --no-browsers --yes -p:EnforceCodeStyleInBuild=true
```

A job that publishes lets bit build first too, so its CSS and JS are generated before `dotnet publish`. A web job passes `-p:Configuration=Release` and its `-p:Version`, so the publish reuses that build; a native job builds Debug, and its Release publish reuses the same JavaScript, which bit Boilerplate always minifies, with a source map.

In CI, bit installs what a build and its tests need and leaves alone what only a developer's machine needs:

- Docker, WSL, the Aspire CLI and Windows features aren't installed, and the project isn't started with Aspire.
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
  run: dotnet tool install --global Bit.Cli --prerelease

- name: Translate .resx files
  env:
    OpenAI__ApiKey: ${{ secrets.OPENAI_APIKEY }}
  run: bit translate
```

## bit decode

Reads a stack trace of an app minified by [Bit.Minifier](https://github.com/bitfoundation/bitplatform/tree/develop/src/Minifier) back into the names its source has:

```bash
bit decode bit-minifier.map trace.txt
bit decode < trace.txt
```

Without a map, the newest `obj/**/bit-minifier.map` under the current folder is used. Without a trace file, the trace is read from standard input.

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
- **Container images** the project's AppHost uses, which Docker pulls during the first Aspire start and keeps.
- **Administrator rights**: on Windows, the steps that need them (long paths, WSL, installers that need admin) run in one elevated PowerShell, so Windows asks once. It runs only the script bit wrote, checked by its hash, gives each command a time limit, and Ctrl+C stops it. On macOS and Linux, `sudo` asks for your password once.
- **A private GitHub repository**, only with `--github-repo`, on the account you sign in with. The GitHub CLI keeps that sign-in, and git uses it to push.
- **Trust entries**, only for the folder `bit new` created or the one you pass to `bit trust`: `projects` in `~/.claude.json`, `trustedFolders` in `~/.copilot/config.json`, `[projects]` in `~/.codex/config.toml`, `~/.gemini/trustedFolders.json`, and VS Code's trust store in `~/.vscode-shared/sharedStorage/state.vscdb` (only while VS Code isn't running). Each file keeps everything else in it. To undo, delete the entry, or use each tool's own trust settings.
- **bit itself**, when a newer version is out: a copy for the run in `~/.bitplatform/cli`, then `dotnet tool update --global Bit.Cli` once the run ends.
- **Its own folder**, `~/.bitplatform`: `settings.json`, the template cache, logs, and telemetry not sent yet.

## Telemetry

bit sends error reports to the bit platform team so failed runs get fixed: whether each step worked and how long it took, error codes like `NU1301`, the type and stack trace of a crash, the OS, .NET and bit versions, the bit version a run was handed over from, whether it runs in CI, through `dnx` or for a coding agent, and which hardware warnings it showed. A random ID, made on the first run, ties one machine's runs together.

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
| 3 | A check failed: `bit translate --check`, `bit doctor` |
| 130 | Canceled |

## Coming from the old tools

| Before | Now |
|---|---|
| `dnx Bit.ResxTranslator` | `dnx Bit.Cli --prerelease -- translate`, or `bit translate` |
| `dnx Bit.Minifier.Cli --decode map trace` | `dnx Bit.Cli --prerelease -- decode map trace`, or `bit decode map trace` |
