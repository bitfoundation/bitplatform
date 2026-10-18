---
name: test-platform
description: Test one build of bitplatform end to end on the demo host - fix what its All CI run failed on, start an All CD run at a new APP_VERSION when asked to, roll its web, Android and Windows apps out to APP_VERSION, run the Boilerplate E2E suite against the deployed demos, check that bitplatform.dev/mcp answers each release from that release's own code, check that a new bit Boilerplate project's dev container sets itself up with bit, and check that its Aspire containers are reused from one run to the next and stopped once it sits idle. It first asks which of these to do and how. Use when asked to test the platform or to start the e2e tests, or when handed an All CI and/or All CD run link.
---

# Test the platform

Take one build of the repo from its CI run to what users get, and hand back a report the maintainer can act on
without opening a log.

This runs on the **demo host** only. It serves every demo backend from IIS (`C:\inetpub\<site>`), its self-hosted
runner (`D:\Runner`) is what the CD workflows deploy through, and the Android emulator, the installed Windows apps and
the test suite all live on it. How the machine is put together, and how to rebuild it, is in
`G:\My Drive\Bit Demo Assets\Infrastructure\README.md`.

Three rules for working on it:

- **Docker Desktop stays running.** The demos' Redis is its `redis` container (restart always, port 6379), and
  without it TodoApi dies at startup with "No redis server available", which shows up as a 502 at
  todo-api.bitplatform.dev. Start Docker Desktop when it is off, never stop it, and never remove that container.
- **Memory is short.** The emulator alone takes over 3 GB. Run one heavy thing at a time - the emulator, an Aspire
  app, the dev container - and stop whatever you started once it isn't needed: the emulator if it was off, and any
  local app or `dotnet watch`. Never run `dotnet build-server shutdown`: other sessions build on this machine too.
- **Read the configuration before changing it.** Before any fix to the machine itself (IIS, permissions, folders,
  tools), query what it really is: an app pool's identity (`appcmd list apppool /text:processModel.userName`,
  elevated), the deployed `appsettings.json` under `C:\inetpub\<site>`, a tool's version. PlatformWebsite's app pool,
  for one, runs as the machine owner's account, not as ApplicationPoolIdentity, and a fix that assumed the default
  made things worse.

## Inputs

Up to two GitHub Actions links, in any order. Each may point at a run, one of its attempts (`/attempts/2`) or one of
its jobs (`/job/<id>`); the run id is the number after `/runs/`.

- An **All CI** run (`all.ci.yml`): fix what it failed on - step 1.
- An **All CD** run (`all.cd.yml`): roll its apps out before testing them - step 2.

```bash
gh run view <run-id> -R bitfoundation/bitplatform --json workflowName,headBranch,headSha,attempt,conclusion,createdAt
gh variable list -R bitfoundation/bitplatform   # APP_VERSION, and when it last changed
```

Without a CD link, step 2 only verifies: the suite tests whatever is deployed. When the user asks for a new CD run
instead, step 2 says how to start one.

Steps 5 and 6 take a commit, usually the CI run's head. Without one, they test the bit packages nuget.org serves.

## First, ask

Before doing anything, ask the user in one go what this run covers. Offer choices, fill in what the links already
answer, and repeat the plan back before starting:

1. **The branch**, or the commit, under test, unless a link names it. Read this file and run the helper from a
   checkout of that branch, pulled first: the helper takes the suite, its stages and the CD workflows from the
   checkout it runs in.
2. **The tests**: any of the CI run (step 1), the CD rollout and its versions check (step 2), the E2E suite (step 3,
   every stage or some), bitplatform.dev/mcp (step 4), the dev container (step 5) and the Aspire containers (step 6).
3. **The Bit packages**: nuget.org's, or the build of a Prerelease nuget packages run (`prerelease_packages`). The
   latter needs a successful prerelease run on the branch under test first. It publishes the branch's version to
   nuget.org only when nuget.org doesn't have it yet, so at a version that is already there it builds packages and
   nothing else: start it, and any later one a fix needs, without asking.
4. **All CI**: the linked run, a new run on the branch (every job, or which ones), or none.
5. **All CD**: the linked run, a new run (every app, or which of `server`, `wasm`, `windows`, `android` and `ios`),
   or none, which tests what is deployed now. Say which branch and `APP_VERSION` that is, from the last All CD run
   (`gh run list -R bitfoundation/bitplatform -w all.cd.yml -L 1`): it is often another branch than the one under test.
6. **`APP_VERSION`**, for a new CD run: raise it, and to which version, or keep it.
7. **Steps 5 and 6**: from the commit under test (`-Sha`) or from nuget.org.

Start, change or skip nothing beyond the answers, and ask nothing more once they are in: the user is often away
until the report. Then go in this order:

1. The prerelease run, when asked for:
   `gh workflow run prerelease.nuget.org.yml -R bitfoundation/bitplatform --ref <branch>`. Wait for it to succeed
   (`gh run watch <run-id> -R bitfoundation/bitplatform --exit-status`) before anything that takes its packages.
2. `APP_VERSION`, then All CI and All CD together, both with the chosen `prerelease_packages`. All CI's inputs name
   its jobs and all default to on; `-f <job>=false` leaves one out.
3. While they run: step 6 in the background, and step 1 as CI jobs fail.
4. Once CD is done: step 2, then steps 3 and 4.
5. Step 5 last, on its own.

Every CD job builds with `-p:Version=$APP_VERSION`, so a complete rollout shows that version everywhere: as
`<APP_VERSION>+<sha>` in the ProductVersion of the IIS assemblies and the Windows executables, as `versionName` on
Android, and in the nav panel of every Boilerplate app. The jobs read the variable when they run, so if it changed
after the CD run started, expect a mismatch and ask which version is meant.

## The helper

`.github/agents/test-platform/test-platform.ps1` (PowerShell 7) does the mechanical work. It reads what is deployed
where from the CD workflows of the Boilerplate demos (`admin-sample`, `sales-module-demo`, `todo-sample`) and of the
platform website (`platform.website`, which serves bitplatform.dev/mcp), and from the E2E suite's `DeployedApps.cs` and
`RunTests.bat`, so it follows them instead of repeating them. Nothing else the CD deploys - the library demos, the other
websites - is checked. Every command prints what it checked and exits non-zero on a failure.

| Command | What it does |
| --- | --- |
| `versions [-Sha <sha>] [-CdRun <id or url>]` | Every IIS site, Windows app and Android app of those workflows, each Boilerplate web app - the version its nav panel shows in a fresh headless Edge - and the MCP endpoint's own version, against `APP_VERSION`. With `-CdRun`, also where that CD run took its Bit packages from. |
| `android -CdRun <id or url>` | Downloads the run's APKs, boots the emulator when no device is up, installs them and checks their versions. |
| `windows [-Force]` | Opens each Windows app that is behind and taps its version, which updates it and restarts it. |
| `e2e [-Stage <stages>] [-Filter <filter>]` | Signs the global admin in once, then runs the E2E suite stage by stage like `RunTests.bat` - skipping the mac's stages when its Playwright server is down or on another version - keeping each stage's log and TRX. |
| `mcp [-McpVersion <x.y.z>] [-Tool <name> -Arguments <json>]` | One `tools/list` or `tools/call` on `https://bitplatform.dev/mcp`. |
| `devcontainer [-Sha <sha>] [-Keep]` | Creates a project with `bit new`, brings its dev container up with the devcontainers CLI and runs `bit doctor` in it, with bit and the template from that commit or from nuget.org. Then removes the container, its volumes and its image, and keeps the logs. |
| `aspire [-Sha <sha>] [-Keep]` | Creates a project with `bit new` the same way and runs its Aspire app host three times with `dotnet run`: the second run must reuse the first one's containers, they must stop once the app host has sat idle for its idle time, and the third run must bring them back on the same volumes. Then removes the project, its containers and their volumes, and keeps the logs. |

## 1. The CI run

1. List the failed jobs of **every** attempt, not only the last one. A test that failed in one attempt and passed in
   another is flaky, and flaky counts as failing.

   ```bash
   gh api repos/bitfoundation/bitplatform/actions/runs/<run-id>/attempts/<n>/jobs --paginate \
     --jq '.jobs[] | select(.conclusion != "success" and .conclusion != "skipped") | "\(.id) \(.name)"'
   ```

2. Read each failed job's log by its id:

   ```bash
   gh api --allow-escape-sequences repos/bitfoundation/bitplatform/actions/jobs/<job-id>/logs > <job-id>.log
   grep -naE '\[31mfailed|threw exception|Test run summary|heartbeat' <job-id>.log
   ```

   Not `gh run view --job <id> --log-failed`: for a job of an earlier attempt, it prints the latest attempt's log of
   the job with the same name. A failed test step also uploads its TRX as a `*-tests-*` artifact, kept for 14 days. A
   Kestrel *"the heartbeat has been running for ..."* warning next to a failure means the test process's thread pool
   was starved - the Ubuntu runners have four cores.

3. Reproduce it here. Most template tests run straight from the template's own tree, which uses Sqlite:

   ```bash
   cd src/Templates/Boilerplate/Bit.Boilerplate/src/Tests
   dotnet build
   dotnet test --no-build --filter "FullyQualifiedName~<TestClass>"
   ```

   A test that needs what the job generated - another database, `--module Admin`, `--advancedTests` - needs the same
   project: pack the template and the CLI the way `.github/actions/setup-bit-cli` does, then replay the job's
   `bit new ...` line from `all.ci.yml` with `--template-package` in a scratch folder outside the repository, with the
   job's database in Docker and the job's connection string (`ConnectionStrings__mssqldb` and the like, in
   `all.ci.yml`).

   Run a flaky test often enough to watch it fail before the fix and not after - under load, when the failure is
   about timing.

   What has cost time here before:

   - The template tree's tests start its Aspire app host first, with every container it has (SQL Server, PostgreSQL,
     MySQL, Keycloak, both Redis, the S3 storage, MailPit), so they need Docker Desktop and are slow to start; that
     start once hung for over 15 minutes. A generated project starts only its own few containers.
   - Install a packed template with `--debug:custom-hive <folder>` on both `dotnet new install` and `dotnet new`, so
     the machine's own Bit.Boilerplate template stays as it is; `bit new` already uses a private hive in its home
     (`BIT_CLI_HOME`).
   - For a project with no containers at all: `--database Sqlite --aspire false --redis false`. With
     `--advancedTests`, keep `--signalR` and a `--module`, or the tests don't compile. `dotnet new` adds no EF
     migration, so add one in Server.Api before testing (`dotnet dnx dotnet-ef --yes -- migrations add Initial
     --output-dir Infrastructure/Data/Migrations`), or every test fails on a missing table. On Sqlite about 18 tests
     fail with "database is locked", since they share one file; CI never sees that.
   - A UI test in WebAssembly mode that fails at boot, with "Failed to find a valid digest in the 'integrity'
     attribute" in the browser console, is a stale Client.Web build, not code: delete Client.Web's `obj` and `bin`
     for its target framework and rebuild.
   - Dozens of Razor errors (RZ...) in files nobody touched come from the shared compiler server's state: build with
     `UseSharedCompilation=false`.
   - The Linux-only Chromium flakes (Brouter, some template UI tests) don't reproduce on Windows; they need CI or a
     Linux container.
   - In a fresh worktree, a library whose build runs `npm install`, such as Bswup, breaks when its target frameworks
     build in parallel: the installs race on `node_modules` (`ENOTEMPTY`), and the half-installed folder then fails
     every build with `tsc` exiting 9009. Delete `node_modules`, build one framework (`-f net10.0`), then the rest.
   - Afterwards remove the containers, volumes and leftover `dcp` processes the repro left. Never the `redis`
     container, which the demos use, nor another project's containers.

4. Fix the cause. A timeout grows only when what it waits for is legitimately slow, and a retry that hides a race is
   not a fix. Template changes follow `src/Templates/Boilerplate/Bit.Boilerplate/AGENTS.md`, `[mirror]` comments
   included. Files under `src/Templates/Boilerplate/Bit.Boilerplate` are UTF-8 without a BOM - its own
   `.editorconfig` says so - and CI's `dotnet format` fails on a BOM; check new files with
   `dotnet format whitespace <slnx> --verify-no-changes --include <folder>`.

5. Commit on the run's branch. Push to a branch of your own without asking, for the user to review with the report;
   ask before pushing to anyone else's. A CI run on the pushed branch is the final proof:
   `gh workflow run all.ci.yml -R bitfoundation/bitplatform --ref <branch>`. Re-running a failed job
   (`gh run rerun <run-id> --failed`) tells flaky from broken, and proves nothing about a fix.

This machine has four cores and serves the demos too, so a local build or test run never overlaps step 3.

## 2. The CD run: every app at APP_VERSION

**Starting one.** All CD deploys to the live demos and the stores, so start it only when the user asks. It builds the
branch it is started on.

```bash
gh variable set APP_VERSION -R bitfoundation/bitplatform --body <new version>
gh workflow run all.cd.yml -R bitfoundation/bitplatform --ref <branch> -f prerelease_packages=<true|false>
```

Raise `APP_VERSION` first, to the version the user names or the next one: Google Play refuses a bundle whose version
code it already has, App Store Connect a build number it already has, and the Windows apps only update to a higher
version. Leave it alone until every job has started. The `server`, `wasm`, `windows`, `android` and `ios` inputs are
all on by default, and `-f <input>=false` leaves that part out. A run of the web apps alone can stay at the current
version; a run at a new version that leaves an app out leaves that app behind, and `versions` says so.

**Web.** The deploy jobs mirror each bundle into `C:\inetpub\<site>` and restart its app pool, and the WebAssembly
standalone apps go to Azure Static Web Apps. There is nothing to do but check.

**Android.** `android -CdRun <run>`. Every Android job uploads an artifact called `android-bundle`, and a re-run job
adds another, so the helper tells them apart by package and takes the newest of each. The device is the one AVD,
`pixel_7_-_api_36_0`. `adb` is not on `PATH`: platform-tools is under `C:\Program Files (x86)\Android\android-sdk`,
where the helper finds it. It is a Google Play image, so after a cold boot it updates Android System WebView on its
own; note the version (`adb shell dumpsys webviewupdate`) in the report, since a jump explains Android failures that no
code change does. An APK signed with another key than the installed app, or older than it, goes in only
after uninstalling the app, which drops its data.

**Windows.** `windows`. The apps are Velopack installs in `%LocalAppData%\<AppId>`: the Boilerplate's
`AdminPanel.Client.Windows`, `SalesModule.Client.Windows` and `TodoSample.Client.Windows`. The nav-panel version
button runs `ForceUpdate`: Velopack downloads the release from the app's feed and restarts into it. Every app starts
WebView2 with `--remote-debugging-port=9222`, so the helper first closes any that is running.

Then `versions -Sha <CD head sha> -CdRun <run>`. Anything still behind is a failed rollout: find the CD job that did not deliver -
its log, the feed's `releases.win.json`, the Play upload - before testing anything.

**Bit packages.** All CD has a `prerelease_packages` option. With it, the Boilerplate demos and the platform website
are built with the Bit packages of the latest Prerelease nuget packages run (`prerelease.nuget.org.yml`) instead of
nuget.org's, and each job that took them leaves a "Bit packages" notice naming that run's branch and commit. `versions`
reads those notices when given the CD run. Say where the packages came from next to the versions table: a library
change is on the demos only when it names the branch that has it.

Every Prerelease nuget packages run, on any branch, uploads all the packages it built as its `nupkg-files` artifact,
and each job of a CD or CI run with `prerelease_packages` takes the newest unexpired one when that job starts. So a
prerelease run that finishes in the middle of a CD run hands the jobs that start after it another build, maybe of
another branch: compare the jobs' notices. The prerelease run also pushes its packages to nuget.org, skipping every
version nuget.org already has, so a second run at the same version changes nothing there and its build lives only in
its artifact. All CI's `prerelease_packages` option works the same way for its template and website jobs.

## 3. The E2E suite

`src/Templates/Boilerplate/Bit.Boilerplate/src/Internal/Boilerplate.Tests.E2E` drives the deployed demos with
Playwright and MSTest; no local server is involved. `RunTests.bat` defines its stages: the web apps on chromium and on
webkit through the Playwright server on the mac (`PLAYWRIGHT_SERVER_ENDPOINT`), on Firefox locally, then the Android
apps (CDP through `adb forward`, port 9223), the Windows apps (CDP on 9222) and the APIs with their database.

Run `e2e` in the background. It builds once, gives each stage its own time limit (`-TimeoutMinutes`, 120 by
default), and keeps each stage's console log and TRX as `TestResults\<stage>-<time>.log` and `.trx` - the suite
itself writes every stage to the same `TestResults.trx`, and a re-run must not replace the full run's evidence. Look in every few minutes; a test process that sits near 0% CPU
for long is hung, not slow.

What it needs, all of it already on this machine:

- **The mac's Playwright server** at the address in `RunTests.bat`, running the **same version** as
  `Microsoft.Playwright.MSTest.v4` in `src/Templates/Boilerplate/Bit.Boilerplate/src/Directory.Packages.props`: any
  other version fails every test of both mac stages at connect. `.runsettings` has the command that starts it. The
  helper checks both before those stages and skips them when the server is down or on another version - ask the
  user to (re)start it rather than swapping in a local webkit.
- **Firefox** for Playwright, which the helper installs when it is missing.
- **User secrets** (`UserSecretsId` 154C08DF-...): the tenant and global admin accounts, the global admin's
  authenticator key and the demos' PostgreSQL connection string. Never print them. They must match the deployments:
  every test that needs the global admin signs in again after a failed attempt, so a wrong password locks the live
  account out within a minute. The helper therefore signs it in once before any stage (`GlobalAdminTwoFactorCodeTests`)
  and starts nothing when that fails; the account's password is the user's to fix.
- **The apps to itself.** The Android and Windows stages clear the apps' data, and the Windows stage moves the
  machine's own app data aside and puts it back at the end. The Android stage also clears the emulator's Chrome and
  texts the emulator the SMS the deployment sent. Don't run the helper's `android` or `windows` while those stages run.
- **No system sheets on the emulator.** Google Password Manager's "Save password?" sheet after a sign-in and Gboard's
  stylus tour each swallow a Back press, and the Android two-factor back navigation test then fails with the soft
  keyboard "still open" as if the app were wrong. `android` and the Android stage turn the device's autofill service
  and stylus handwriting off, so a new or wiped AVD gets them too. The Android stage's WebView videos are blank:
  `adb exec-out screencap -p` shows what is really on the screen.
- **The keyboard has to resize the page.** An Android click that times out because an element just above the target
  intercepts it means the soft keyboard panned the page instead of resizing it, and the click lands that far off.
  Over CDP (`adb forward` to the app's `webview_devtools_remote_<pid>` socket), `innerHeight` must shrink when a field
  takes the focus; a `visualViewport` that moves down instead is the app's bug, not the test's.

Triage every failure:

1. Re-run just that test the same way - `e2e -Stage <stage> -Filter "FullyQualifiedName~<Class>.<Method>"` - to
   tell flaky from broken. A stage's console log counts every failed attempt of a test that retries, its TRX only
   the last one: a test the TRX shows passed failed once and passed on its retry. Compare a failure only with earlier
   runs against a deployment from the same branch: the demos run whichever branch the last CD built, and the
   branches differ in their .NET.
2. Read the server's side. The demo backends run on this machine and log to the Windows **Application** event log,
   source `.NET Runtime`. How much gets there is each site's own `Logging` section in its server-owned
   `appsettings.json` under `C:\inetpub\<site>`: the Boilerplate backends log from Information up, the platform
   website only warnings and errors. The category names the app (`AdminPanel.*`, `TodoSample.*`, `SalesModule.*`,
   `Bit.Websites.Platform.*`), and a framework category (`Microsoft.*`) belongs to whichever
   app logs from the same `ProcessId`. The Windows apps log there too (`*.Client.Windows.*`), and so does the suite's
   own test process - its HttpClient warnings time every call it made to a deployment.

   ```powershell
   Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = '.NET Runtime'; StartTime = (Get-Date).AddMinutes(-30) } |
       Sort-Object TimeCreated |
       ForEach-Object { '{0:HH:mm:ss} pid={1} {2} {3}' -f $_.TimeCreated, $_.ProcessId, $_.LevelDisplayName, ($_.Message -replace '\s+', ' ') }
   ```

3. Name it: a test bug, a product bug, the infrastructure (the mac, the emulator, an outside service's outage), or
   flaky. Fix the first two at their cause, as in step 1.

A download size that grew is broken down by file before it is called growth. The published `.br` of each file sits
next to it in `C:\inetpub\<site>\wwwroot`, and that is what has to arrive: a response larger than it, or chunked with
no `Content-Length`, was compressed again on the way. Compare the libraries' assets and the trimmed WebAssembly files
with a publish of the commit the expected sizes were measured on. The service worker's own scripts, its asset list
included, report no size to Playwright, so the total leaves them out.

Two failures on .NET 11 are known gaps of third-party libraries; report them as known and don't triage them: the
template's MySQL provider doesn't work there (All CI keeps its MySQL job off), and CommunityToolkit.Datasync doesn't
sync, so the offline Todo test that waits for an item added offline to reach the server fails every time. Check them
again once those libraries support .NET 11.

**A fix is proven only by the next CD.** The suite tests the deployed build, so a fix to the template or to a demo
reaches it only through another All CD run on the fixed commit. Verify what can be verified locally, commit, and say
plainly which failures stay until that CD.

**Trying an Android fix before its CD.** Build the template tree's `Client.Maui` against the live AdminPanel: point
Client.Core's `ServerAddress` at the AdminPanel API and the Maui project's `WebAppUrl` at the AdminPanel web app, as
temporary edits. Build with `-p:Version=<APP_VERSION>`, or the live API answers with "Time for an update" and blocks
the flow. Keep the template's own application id, so the official AdminPanel app stays installed, and point the
suite's AdminPanel Android app id in `DeployedApps.cs` at it for the local run only. The AVD has little free space,
so build one ABI (a temporary `<RuntimeIdentifiers>android-x64</RuntimeIdentifiers>` in the csproj; a global
`-p:RuntimeIdentifier` breaks the build) and, if it still doesn't fit, lower the low-storage limit for the run with
`adb shell settings put global sys_storage_threshold_max_bytes 104857600`. `adb` is under
`C:\Program Files (x86)\Android\android-sdk\platform-tools`; in Git Bash, `MSYS_NO_PATHCONV=1` keeps device paths
intact, and host paths then need forward slashes. Afterwards revert every edit, uninstall the test app, delete that
storage setting, run `am clear-debug-app`, remove `/data/local/tmp/chrome-command-line`, `pm clear com.android.chrome`,
and `adb emu kill` if the emulator was off before.

A fix in a bit library does not have to wait for its release: the demos take the libraries as packages, and
`prerelease_packages` swaps nuget.org's for the fix's. Ask the user to run Prerelease nuget packages on the fix's
branch and then All CD with that option, with no other prerelease run finishing in between. Both branches have to be
on the same version in `src/Bit.Build.props`; otherwise the action stops before it changes anything and names both
versions.

## 4. bitplatform.dev/mcp

The platform website serves one set of tools per release tag `v-X.Y.Z` at or above `Mcp:MinimumVersion`, each from
that tag's own worktree, source index and documentation servers. Prerelease tags are never served, and a `?v=` it
does not serve silently gets the newest one it does.

1. **What it should serve.** The plain `v-X.Y.Z` tags of
   `git ls-remote --tags https://github.com/bitfoundation/bitplatform.git 'v-*'`. Check that the newest
   `feat(release): v-X.Y.Z` commit on `develop` has its tag: an untagged release is answered by the previous one,
   without a word.
2. **What it does serve.** `mcp -McpVersion X.Y.Z` for each. The library tools (`Search*`, `Get*` and `Inspect*` of BlazorUI,
   Brouter, Butil, Bswup, Bmotion and - from the first release that ships them - Bink, BbScanner, Bonboarding, Blex and Bmage) and the source tools (`FindBitPlatformSymbols`, `GetBitPlatformSymbolSource`,
   `SearchBitPlatformCode`) must all be there. Only the five shared ones (`AskGitHubRepository`,
   `SendBitPlatformFeedback`, `microsoft_docs_*`) means no release is served at all, and the README's
   *PlatformWebsite's MCP* section says why that happens. The server version it reports is the website's own build,
   which must be `APP_VERSION`.
3. **Each version answers from its own code.** Find an API difference between two served tags, then ask both:

   ```bash
   git diff v-A v-B -- src/BlazorUI/Bit.BlazorUI/Components | grep -E '^[-+].*\[Parameter\]'
   ```

   `BitAccordion.Busy` came in 10.6.1, so
   `mcp -McpVersion 10.6.1 -Tool GetBitBlazorUIComponent -Arguments '{"name":"BitAccordion"}'` lists `Busy` and the
   same call on 10.6.0 does not. Pick a new difference for each new pair of releases; the other libraries work the
   same way (`src/Butil/Bit.Butil/Publics`, ...). The `source:` link of the same answer points at that release's
   code too - its tag (`blob/v-10.6.1/...`), never `develop`.
4. **The answers are good.** Put one real task to each library's `Search` tool, worded the way a developer would
   describe it - BlazorUI "a dropdown the user can also type into", Butil "copy text to the clipboard", Bmotion "fade
   a list item in", Brouter "a route parameter that must be a number", Bswup "show the download progress of the app's
   update", Bink "a signature pad that exports a PNG", BbScanner "scan a QR code from an uploaded photo", Bonboarding "a step that waits for a panel that loads later", Blex "a counter whose value survives a page reload", Bmage "shrink an uploaded photo under 200 KB without its location" - follow up with the `Get` tool it points to, and ask `SearchBitPlatformCode` about the Boilerplate. An
   answer must be on topic, specific and true to the version asked for; one that answers wrongly is a failure too.

Like an E2E failure, an MCP problem is fixed in `src/Websites/Platform` or in the library's own MCP server, and goes
away only after the next CD.

A website fix that goes through its AI chat client can be tried here without a key. The site registers that client
only when `AppSettings:OpenAI:ChatApiKey` is set and sends it to `AppSettings:OpenAI:ChatEndpoint`, so give it any key
and point the endpoint at a small local stand-in that answers `POST /v1/chat/completions` with a `chat.completion`
JSON (its message content a JSON string where the code asks for structured output; a 400 for a marker phrase to try
the failure path). Run the server project with those two settings as environment variables, in Development, and
drive the page with Playwright. In Development the pages use server interactivity, so their calls never show in the
browser's network events: set the `Microsoft.AspNetCore.Hosting.Diagnostics` log level to Information and count the
request lines in the server's log. Stop the site and the stand-in afterwards; a running site locks the build output.

## 5. bit Boilerplate's dev container

A new project's dev container is the .NET SDK image with Docker inside it. When it's created, it installs bit and runs
`bit setup`, which does the rest the way it does on any machine: Node.js, the Aspire CLI, the build tools, the
certificate, the packages, the build and Chromium for the UI tests. `devcontainer` checks that on Docker Desktop's Linux
engine, as VS Code and Codespaces would run it: `bit new` makes a project here without setting it up, the
devcontainers CLI brings its container up, and `bit doctor` runs inside it.

- With `-Sha`, bit and the template come from that commit, as in CI: bit is packed into a feed the container mounts,
  and the project's `nuget.config` maps `Bit.Cli` to that feed alone, so a newer prerelease on nuget.org can't stand
  in for it. Without `-Sha`, it tests what nuget.org serves, which is what users get.
- It's heavy: a few GB in Docker Desktop's VM and about ten minutes, most of them for the container's image and the
  restore. Run it on its own, after the E2E suite, never next to it.
- Done means every `bit setup` step passed and `bit doctor` exits with 0, with VS Code not needed inside a container.
  Its memory warning describes Docker Desktop's VM, not this machine, and is expected.
- The logs of `bit new`, of the container's start and of `bit doctor` stay in the folder the helper names. `-Keep`
  leaves the container up, to look inside it with the devcontainers CLI; bit's own logs are in its home folder there.

A failure in bit or in the template is fixed like a CI failure, and the next `devcontainer -Sha` on the fix proves it;
no CD is involved.

## 6. bit Boilerplate's Aspire containers

A new project's Aspire app host leaves its containers (the database, Redis, Keycloak, MailPit and the like) running
when it stops, so the next run reuses them instead of starting them from scratch, and stops them once no run has been
alive for its idle time. `aspire` checks that the way a developer meets it:

- It creates a project with `bit new`, with its database and Redis in containers, and runs the app host with
  `dotnet run` three times, each time until the web app answers, then ends it the way closing its terminal does.
- The second run reuses every container of the first: the same container, neither re-created nor restarted, and up
  sooner. A re-created container is a failure: Aspire re-creates a persistent container when anything its lifecycle
  key covers changed between the runs, such as its image, environment or ports.
- After the second run the containers keep running for the idle time, then stop. Stopped, not removed: the third run
  brings them back on the same volumes.
- `-Sha` works as for `devcontainer`; the Bit packages come from nuget.org at the version the template asks for.
- It takes about half an hour, most of it the idle wait, and a few GB of Docker Desktop's memory while the app runs.
  Start it in the background early and go on with the other steps, but never next to `devcontainer`.

Two things are expected and are not failures. `dotnet test` and a `dotnet run` or `aspire start` re-create each
other's containers, since the test host picks random ports and the ports are part of the lifecycle key. And a run
that was killed, as closing its terminal does, leaves DCP's own `tunnel-proxy-...` container behind, a few MB, which
the next Aspire run removes and the idle stop doesn't touch.

A failure is fixed in the template like a CI failure, and the next `aspire -Sha` on the fix proves it.

## 7. Report

Lead with the verdict, then:

1. **Deployment**: the `versions` table, where the Bit packages came from, and whatever had to be rolled out.
2. **CI**: per failure, the test, its cause, the fix and its commit, and the local evidence - how many runs, before
   and after.
3. **E2E**: per stage, the passed, failed and skipped counts; per failure, its kind, its evidence (the assertion, the
   event log lines) and what happens next.
4. **MCP**: the served versions, their tool counts, the version check, and how good the answers were.
5. **Dev container**: the commit or the nuget.org version tested, each `bit setup` step with its time, `bit doctor`'s
   exit code, and per failure its cause and fix.
6. **Aspire containers**: the commit or the nuget.org version tested, how soon each run was up, whether the second run
   reused every container, how long after the last run they stopped, and per failure its cause and fix.
7. **Waiting for a CD**: every fix that only the next CD can prove, and for a library fix whether it can be tried
   through `prerelease_packages` before its release.

Anything that looks like a defect in a bit platform library or in the `bit-bp` template itself: say so, and offer to
report it - an issue on bitfoundation/bitplatform, or `SendBitPlatformFeedback`.
