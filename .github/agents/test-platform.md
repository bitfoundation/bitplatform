---
name: test-platform
description: Test one build of bitplatform end to end on the demo host - fix what its All CI run failed on, roll its All CD run's web, Android and Windows apps out to APP_VERSION, run the Boilerplate E2E suite against the deployed demos, and check that bitplatform.dev/mcp answers each release from that release's own code. Use when asked to test the platform or to start the e2e tests, or when handed an All CI and/or All CD run link.
---

# Test the platform

Take one build of the repo from its CI run to what users get, and hand back a report the maintainer can act on
without opening a log.

This runs on the **demo host** only. It serves every demo backend from IIS (`C:\inetpub\<site>`), its self-hosted
runner (`D:\Runner`) is what the CD workflows deploy through, and the Android emulator, the installed Windows apps and
the test suite all live on it. How the machine is put together, and how to rebuild it, is in
`G:\My Drive\Bit Demo Assets\Infrastructure\README.md`.

## Inputs

Up to two GitHub Actions links, in any order. Each may point at a run, one of its attempts (`/attempts/2`) or one of
its jobs (`/job/<id>`); the run id is the number after `/runs/`.

- An **All CI** run (`all.ci.yml`): fix what it failed on - step 1.
- An **All CD** run (`all.cd.yml`): roll its apps out before testing them - step 2.

```bash
gh run view <run-id> -R bitfoundation/bitplatform --json workflowName,headBranch,headSha,attempt,conclusion,createdAt
gh variable list -R bitfoundation/bitplatform   # APP_VERSION, and when it last changed
```

Without a CD link, step 2 only verifies: the suite tests whatever is deployed.

Every CD job builds with `-p:Version=$APP_VERSION`, so a complete rollout shows that version everywhere: as
`<APP_VERSION>+<sha>` in the ProductVersion of the IIS assemblies and the Windows executables, as `versionName` on
Android, and in the nav panel of every Boilerplate app. The jobs read the variable when they run, so if it changed
after the CD run started, expect a mismatch and ask which version is meant.

## The helper

`.github/agents/test-platform/test-platform.ps1` (PowerShell 7) does the mechanical work. It reads what is deployed
where from `.github/workflows/*.cd.yml` and from the E2E suite's `DeployedApps.cs` and `RunTests.bat`, so it follows
them instead of repeating them. Every command prints what it checked and exits non-zero on a failure.

| Command | What it does |
| --- | --- |
| `versions [-Sha <sha>]` | Every IIS site, Windows app, Android app and Boilerplate web app - the version its nav panel shows in a fresh headless Edge - and the MCP endpoint's own version, against `APP_VERSION`. |
| `android -CdRun <id or url>` | Downloads the run's APKs, boots the emulator when no device is up, installs them and checks their versions. |
| `windows [-Force]` | Opens each Windows app that is behind and taps its version, which updates it and restarts it. |
| `e2e [-Stage <stages>] [-Filter <filter>]` | Signs the global admin in once, then runs the E2E suite stage by stage like `RunTests.bat` - skipping the mac's stages when its Playwright server is down or on another version - keeping each stage's log and TRX. |
| `mcp [-McpVersion <x.y.z>] [-Tool <name> -Arguments <json>]` | One `tools/list` or `tools/call` on `https://bitplatform.dev/mcp`. |

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
   project: replay the job's `dotnet pack`, `dotnet new install` and `dotnet new bit-bp ...` lines from `all.ci.yml` in
   a scratch folder outside the repository, with the job's database in Docker and the job's connection string
   (`ConnectionStrings__mssqldb` and the like, in `all.ci.yml`).

   Run a flaky test often enough to watch it fail before the fix and not after - under load, when the failure is
   about timing.

4. Fix the cause. A timeout grows only when what it waits for is legitimately slow, and a retry that hides a race is
   not a fix. Template changes follow `src/Templates/Boilerplate/Bit.Boilerplate/AGENTS.md`, `[mirror]` comments
   included.

5. Commit on the run's branch and ask before pushing. A CI run on the pushed branch is the final proof:
   `gh workflow run all.ci.yml -R bitfoundation/bitplatform --ref <branch>`. Re-running a failed job
   (`gh run rerun <run-id> --failed`) tells flaky from broken, and proves nothing about a fix.

This machine has four cores and serves the demos too, so a local build or test run never overlaps step 3.

## 2. The CD run: every app at APP_VERSION

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
`AdminPanel.Client.Windows`, `SalesModule.Client.Windows` and `TodoSample.Client.Windows`, and
`Bit.BlazorUI.Demo.Client.Windows`. A Boilerplate app's nav-panel version button runs `ForceUpdate`: Velopack
downloads the release from the app's feed and restarts into it. The BlazorUI demo has no such button - it downloads
at startup, and Velopack applies a downloaded release on the next start, so the helper opens it twice. Every app
starts WebView2 with `--remote-debugging-port=9222`, so the helper first closes any that is running.

**iOS** goes to TestFlight and cannot be checked from here; say so.

Then `versions -Sha <CD head sha>`. Anything still behind is a failed rollout: find the CD job that did not deliver -
its log, the feed's `releases.win.json`, the Play upload - before testing anything.

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
  machine's own app data aside and puts it back at the end. Don't run the helper's `android` or `windows` while
  those stages run.

Triage every failure:

1. Re-run just that test the same way - `e2e -Stage <stage> -Filter "FullyQualifiedName~<Class>.<Method>"` - to
   tell flaky from broken.
2. Read the server's side. The demo backends run on this machine and log to the Windows **Application** event log,
   source `.NET Runtime`. How much gets there is each site's own `Logging` section in its server-owned
   `appsettings.json` under `C:\inetpub\<site>`: the Boilerplate backends log from Information up, the platform
   website only warnings and errors. The category names the app (`AdminPanel.*`, `TodoSample.*`, `SalesModule.*`,
   `Bit.Websites.Platform.*`, `Bit.BlazorUI.Demo.*`), and a framework category (`Microsoft.*`) belongs to whichever
   app logs from the same `ProcessId`. The Windows apps log there too (`*.Client.Windows.*`), and so does the suite's
   own test process - its HttpClient warnings time every call it made to a deployment.

   ```powershell
   Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = '.NET Runtime'; StartTime = (Get-Date).AddMinutes(-30) } |
       Sort-Object TimeCreated |
       ForEach-Object { '{0:HH:mm:ss} pid={1} {2} {3}' -f $_.TimeCreated, $_.ProcessId, $_.LevelDisplayName, ($_.Message -replace '\s+', ' ') }
   ```

3. Name it: a test bug, a product bug, the infrastructure (the mac, the emulator, an outside service's outage), or
   flaky. Fix the first two at their cause, as in step 1.

**A fix is proven only by the next CD.** The suite tests the deployed build, so a fix to the template or to a demo
reaches it only through another All CD run on the fixed commit. Verify what can be verified locally, commit, and say
plainly which failures stay until that CD.

## 4. bitplatform.dev/mcp

The platform website serves one set of tools per release tag `v-X.Y.Z` at or above `Mcp:MinimumVersion`, each from
that tag's own worktree, source index and documentation servers. Prerelease tags are never served, and a `?v=` it
does not serve silently gets the newest one it does.

1. **What it should serve.** The plain `v-X.Y.Z` tags of
   `git ls-remote --tags https://github.com/bitfoundation/bitplatform.git 'v-*'`. Check that the newest
   `feat(release): v-X.Y.Z` commit on `develop` has its tag: an untagged release is answered by the previous one,
   without a word.
2. **What it does serve.** `mcp -McpVersion X.Y.Z` for each. The library tools (`Search*` and `Get*` of BlazorUI,
   Brouter, Butil, Bswup and Bmotion) and the source tools (`FindBitPlatformSymbols`, `GetBitPlatformSymbolSource`,
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
   same way (`src/Butil/Bit.Butil/Publics`, ...).
4. **The answers are good.** Put one real task to each library's `Search` tool, worded the way a developer would
   describe it - BlazorUI "a dropdown the user can also type into", Butil "copy text to the clipboard", Bmotion "fade
   a list item in", Brouter "a route parameter that must be a number", Bswup "show the download progress of the app's
   update" - follow up with the `Get` tool it points to, and ask `SearchBitPlatformCode` about the Boilerplate. An
   answer must be on topic, specific and true to the version asked for; one that answers wrongly is a failure too.

Like an E2E failure, an MCP problem is fixed in `src/Websites/Platform` or in the library's own MCP server, and goes
away only after the next CD.

## 5. Report

Lead with the verdict, then:

1. **Deployment**: the `versions` table, and whatever had to be rolled out.
2. **CI**: per failure, the test, its cause, the fix and its commit, and the local evidence - how many runs, before
   and after.
3. **E2E**: per stage, the passed, failed and skipped counts; per failure, its kind, its evidence (the assertion, the
   event log lines) and what happens next.
4. **MCP**: the served versions, their tool counts, the version check, and how good the answers were.
5. **Waiting for a CD**: every fix that only the next CD can prove.

Anything that looks like a defect in a bit platform library or in the `bit-bp` template itself: say so, and offer to
report it - an issue on bitfoundation/bitplatform, or `SendBitPlatformFeedback`.
