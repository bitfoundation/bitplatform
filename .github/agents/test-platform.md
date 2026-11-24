---
name: test-platform
description: Test one build of bitplatform from its CI runs to the demo host - fix what its All CI runs failed on, start an All Apps CD run at a new APP_VERSION when asked to, and check that every demo and website it deploys came up at that version. It first asks which of these to do and how. Use when asked to test the platform, or when handed All CI and/or All Apps CD run links.
---

# Test the platform

Take one build of the repo from its CI run to what is deployed, and hand back a report the maintainer can act on
without opening a log.

bit Boilerplate, its demos (AdminPanel, Todo, Sales, the Workbench and the landing page) and bit CLI live in
[bitfoundation/bitfabric](https://github.com/bitfoundation/bitfabric) and are tested there, and the docs MCP server is
bit Fabric's, at https://api.bitfabric.co/docs-mcp. This skill covers what this repository builds and deploys: the
libraries, their demos, the BlazorEmpty template and the websites.

The deploys run on the **demo host**. It serves the library demos and the websites from IIS (`C:\inetpub\<site>`), and
its self-hosted runner (`D:\Runner`) is what the CD workflows deploy through. How the machine is put together, and how
to rebuild it, is in `G:\My Drive\Bit Demo Assets\Infrastructure\README.md`.

Three rules for working on it:

- **Memory is short.** Run one heavy thing at a time and stop whatever you started once it isn't needed. Never run
  `dotnet build-server shutdown`: other sessions build on this machine too.
- **Read the configuration before changing it.** Before any fix to the machine itself (IIS, permissions, folders,
  tools), query what it really is: an app pool's identity (`appcmd list apppool /text:processModel.userName`,
  elevated), the deployed `appsettings.json` under `C:\inetpub\<site>`, a tool's version. PlatformWebsite's app pool,
  for one, runs as the machine owner's account, not as ApplicationPoolIdentity, and a fix that assumed the default
  made things worse.
- **Every wait has a budget.** Give a local run a hard time limit, or a watchdog on its CPU and output, and a
  remote run a watcher that stops at its first failed job. Look into a failure as soon as it shows rather than when
  its run ends, and meanwhile do what doesn't depend on it. A process near 0% CPU that has printed nothing for
  minutes is hung, not slow.

## Inputs

GitHub Actions links, in any order. Each may point at a run, one of its attempts (`/attempts/2`) or one of
its jobs (`/job/<id>`); the run id is the number after `/runs/`.

- **All CI** runs: fix what they failed on - step 1. There is no single All CI: it is split by folder into All Other CI
  (`all.other.ci.yml`), All Toolings CI (`all.toolings.ci.yml`) and All Utilities CI (`all.utilities.ci.yml`), and a
  full check takes all three. The E2E suites have their own two, All Other E2E CI (`all.other.e2e.ci.yml`) and All
  Utilities E2E CI (`all.utilities.e2e.ci.yml`).
- An **All Apps CD** run (`all.apps.cd.yml`): check what it deployed - step 2.

```bash
gh run view <run-id> -R bitfoundation/bitplatform --json workflowName,headBranch,headSha,attempt,conclusion,createdAt
gh variable list -R bitfoundation/bitplatform --json name,value,updatedAt --jq '.[] | select(.name == "APP_VERSION")'
```

The other variables include the Microsoft Store's product ids: never print the whole list into a report or a chat.

Without a CD link, step 2 only verifies what is deployed. When the user asks for a new CD run instead, step 2 says how
to start one.

## First, ask

Before doing anything, ask the user in one go what this run covers. Offer choices, fill in what the links already
answer, and repeat the plan back before starting:

1. **The branch**, or the commit, under test, unless a link names it. Fetch, and check that the branch has every commit
   of the branch it merges into (develop, or bit11): one that lacks some tests older code than its merge brings, and
   its CD puts that code on the demos. Name the missing commits in the question and offer to merge them in first.
2. **The tests**: the CI runs (step 1), the CD rollout (step 2), or both.
3. **The Bit packages** of the websites: nuget.org's, or the build of a CD Prerelease nuget packages run
   (`prerelease_packages`). The latter needs a successful prerelease run on the branch under test first. It publishes
   to nuget.org only what nuget.org doesn't have yet, package by package: start it, and any later one a fix needs,
   without asking, and tell the user when the branch adds a package, since that one stays on nuget.org.
4. **All CI**: the linked runs, new runs on the branch (every job, or which ones) of the three and of the two E2E ones,
   or none.
5. **All Apps CD**: the linked run, a new run (every app, or which of `server`, `windows`, `android` and `ios`), or none,
   which checks what is deployed now. Say which branch and `APP_VERSION` that is, from the last All Apps CD run
   (`gh run list -R bitfoundation/bitplatform -w all.apps.cd.yml -L 1`): it is often another branch than the one under test.
6. **`APP_VERSION`**, for a new CD run: raise it, and to which version, or keep it.

Start, change or skip nothing beyond the answers, and ask nothing more once they are in: the user is often away
until the report. Then go in this order:

1. The prerelease run, when asked for:
   `gh workflow run cd.prerelease.nuget.packages.yml -R bitfoundation/bitplatform --ref <branch>`. Wait for it to succeed
   (`gh run watch <run-id> -R bitfoundation/bitplatform --exit-status`) before anything that takes its packages.
2. `APP_VERSION`, then the All CI runs and All Apps CD together, with the chosen `prerelease_packages`. Their inputs name
   their jobs and default to on; `-f <job>=false` leaves one out. After a prerelease run that published a new version,
   start them once nuget.org lists that version for `Bit.Minifier` and `Bit.BlazorES2019`: the library demos and their
   jobs take those two from nuget.org even with `prerelease_packages`, and fail with NU1102 until then.
3. While they run: step 1 as CI jobs fail.
4. Once CD is done: step 2.

Every CD job builds with `-p:Version=$APP_VERSION`, so a complete rollout shows that version everywhere, as
`<APP_VERSION>+<sha>` in the ProductVersion of the IIS assemblies and the Windows executables. The jobs read the
variable when they run, so if it changed after the CD run started, expect a mismatch and ask which version is meant.

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

3. Reproduce it here, with the test project and filter of the failed step:

   ```bash
   dotnet build <test project>
   dotnet test <test project> --no-build --filter "FullyQualifiedName~<TestClass>"
   ```

   Run a flaky test often enough to watch it fail before the fix and not after - under load, when the failure is
   about timing.

   What has cost time here before:

   - A UI test in WebAssembly mode that fails at boot, with "Failed to find a valid digest in the 'integrity'
     attribute" in the browser console, is a stale client build, not code: delete the client's `obj` and `bin`
     for its target framework and rebuild.
   - Dozens of Razor errors (RZ...) in files nobody touched come from the shared compiler server's state: build with
     `UseSharedCompilation=false`.
   - The Linux-only Chromium flakes (Brouter, for one) don't reproduce on Windows; they need CI or a Linux container.
   - In a fresh worktree, a library whose build runs `npm install`, such as Bswup, breaks when its target frameworks
     build in parallel: the installs race on `node_modules` (`ENOTEMPTY`), and the half-installed folder then fails
     every build with `tsc` exiting 9009. Delete `node_modules`, build one framework (`-f net10.0`), then the rest.
   - A job whose runner "received a shutdown signal" in the middle of a step ran the runner out of memory, and its log
     says nothing more. To see what grew, have the step print `free -m` and the top processes by RSS every 30 seconds
     from a background loop, and take a `dotnet-gcdump` of a `dotnet` process past a few GB, then kill it so the
     failed-step upload still runs. `dotnet-gcdump` stops at ten million objects, so a heap that looks small next to
     the process is a cut-off count, not native memory. One lucky pass of the job alone on a runner proves nothing;
     compare a step's duration with an earlier run's instead.
   - A CD Prerelease nuget packages run signs the libraries with the release key, which no other build uses. A library
     that grants its internals to another one under the repository's key alone builds everywhere else and fails
     there with CS0281; it has to name both keys.
   - A trimmed publish that crashes ILLink with a stack overflow on a new .NET points at one type the trimmer keeps
     coming back to. Find it by trimming a console app that roots one suspect assembly, then one namespace, then one
     type of it, with a root descriptor.
   - A test that fails at random in CI on timing often fails here too when it runs on its own, since a single test
     pays the cold start. Run it alone a few times before calling it CI-only. One that keeps passing alone can still
     fail next to a slow test that takes the CPU, so run the two together as well.
   - A Linux-only Chromium wait that times out needs evidence before a fix: have the test log every request the page
     starts and finishes, run the job a few times on a branch of your own, and read what was still in flight. A
     network idle that never comes with nothing in flight is Chromium's event, not the app.
   - A library's E2E failure on another branch than develop: run that library's E2E workflow on develop too
     (`gh workflow run ci.<library>.e2e.yml --ref develop`) before calling it the branch's own. New tests of a
     library sometimes reach develop without ever passing in CI.
   - A failure only one branch has can come from its runner image rather than its code: compare the jobs' `runs-on`.
     A newer Ubuntu ships a newer ICU, whose culture data changes formatting, such as the calendar a culture uses
     by default and whether a date shows its era.
   - A WebAssembly demo or sample started from its build output answers its `_framework` files with empty 200s
     outside Development, since its static web assets come from the build's development manifest. A probe of
     your own starts it with `ASPNETCORE_ENVIRONMENT=Development`, as the suites do.
   - A job cancelled with no runner and no steps never ran: `gh api repos/bitfoundation/bitplatform/check-runs/<job-id>/annotations`
     says "The job was not acquired by Runner of type hosted", usually for macOS when GitHub has no capacity. Re-run it
     once its run ends.
   - A Prerelease run that stops at `dotnet pack` with CP0002 met a public API that a merge changed without its
     `CompatibilitySuppressions.xml`. `dotnet build` the project with `-p:GeneratePackageOnBuild=false` first, as the
     workflow does, or the pack fails with NU5026, then pack with `-p:ApiCompatGenerateSuppressionFile=true` and put
     back the BOM it drops. Only packing validates: All Apps CD and a demo's Release build still go through.
   - A template conditional that stays in a project made from BlazorEmpty at random, and only in CI, comes from the
     template engine's one conditional switch for all `.cs` files: a file that ends with it off leaves it off for the
     next one, unless that one starts with `//+:cnd:noEmit`. A template in a folder is read in the file system's order,
     which on a Linux runner changes from run to run, while Windows reads by name and never shows it. To reproduce
     it, plant a file that leaves it off right before the one that failed.
   - A WebAssembly-only failure that reads `Could not load type of field ... Could not load file or assembly` names a
     lazily loaded assembly that was touched before it loaded: in the interpreter, a method that reads a field, casts
     or calls `new` with such a type works out its layout as the method first runs, and the failure stays for the
     rest of the visit. A click right after the page loads, before the download is done, reproduces it in a sample
     that lazy loads the library; keep such a type out of fields, casts and method bodies that can run before the load.

4. Fix the cause. A timeout grows only when what it waits for is legitimately slow, and a retry that hides a race is
   not a fix. Check new files with `dotnet format whitespace <slnx> --verify-no-changes --include <folder>`.

5. Before fixing a failure, fetch develop and read its newest commits, open issues and open PRs: other sessions work
   on this machine and on develop's failures too, so the fix may be merged or under way already.

   Put the run's fixes in one issue and one PR, cut from develop when they apply there and from bit11 otherwise, not
   on the branch under test: only a fix to that branch's own changes goes on it. Push to a branch of your own without
   asking, for the user to review with the report; ask before pushing to anyone else's. The All CI runs on the pushed
   branch are the final proof: `gh workflow run <file> -R bitfoundation/bitplatform --ref <branch>` for `all.other.ci.yml`,
   `all.toolings.ci.yml` and `all.utilities.ci.yml`, since no one run covers them all, plus the E2E workflow of a
   failure in an end-to-end suite (its `ci.<project>.e2e.yml`, such as `ci.templates.e2e.yml` for BlazorEmpty), which
   none of them runs. Re-running a failed job (`gh run rerun <run-id> --failed`) tells flaky from broken, and proves
   nothing about a fix. It has to wait for the whole run to end: a new run with every other job off answers sooner.
   GitHub won't re-run a run whose branch is gone, as after its PR was merged: start the new run on develop or on the
   fix branch instead. A push to a branch with an open PR also starts that PR's CI workflows; cancel the ones the All
   CI runs cover anyway, so the runners go to it.

   A fix in a bit library - any `Bit.*` project, its tests, demos and samples included - gets an issue and a PR of its
   own instead, so its owners review it. Fixes to the websites and to this procedure go in the run's one PR.

This machine has four cores and serves the demos too, so keep local builds and test runs small while a CD deploys.

## 2. The CD run: every app at APP_VERSION

**Starting one.** All Apps CD deploys to the live demos and the stores, so start it only when the user asks. It builds the
branch it is started on.

```bash
gh variable set APP_VERSION -R bitfoundation/bitplatform --body <new version>
gh workflow run all.apps.cd.yml -R bitfoundation/bitplatform --ref <branch> -f prerelease_packages=<true|false>
```

Raise `APP_VERSION` first, to the version the user names or the next one: Google Play refuses a bundle whose version
code it already has, App Store Connect a build number it already has, and the Windows apps only update to a higher
version. Leave it alone until every job has started. The `server`, `windows`, `android` and `ios` inputs are all on by
default, and `-f <input>=false` leaves that part out.

**Web.** The deploy jobs mirror each bundle into `C:\inetpub\<site>`, restart its app pool and check its health URL, so
a deploy job that passed delivered its site. Check the ProductVersion of each site's main assembly under
`C:\inetpub\<site>` against `<APP_VERSION>+<sha>`, and open each demo once.

**A deploy job that finds no app pool of its name** deploys a demo whose IIS site was never made on this machine,
usually a library demo added since the host was set up. Creating it changes the machine, so ask first; then add it
the way the other library demos are set up, with its site's folder at the `site-path` its CD deploys to, and add it
to the README's exports. Re-running the deploy job, or that demo's own CD, delivers it.

A job that fails to download an artifact with "Egress is over the account limit" ran into GitHub's transfer quota for
the account's runners, not a broken build: re-run the failed jobs later.

**Bit packages.** All Apps CD has a `prerelease_packages` option. With it, the platform and sales websites are built with
the Bit packages of the latest CD Prerelease nuget packages run (`cd.prerelease.nuget.packages.yml`) instead of
nuget.org's, and each job that took them leaves a "Bit packages" notice naming that run's branch and commit. Say where
the packages came from in the report: a library change is on the websites only when it names the branch that has it.

Every CD Prerelease nuget packages run, on any branch, uploads all the packages it built as its `nupkg-files` artifact,
and each job of a CD or CI run with `prerelease_packages` takes the newest unexpired one when that job starts. So a
prerelease run that finishes in the middle of a CD run hands the jobs that start after it another build, maybe of
another branch: compare the jobs' notices. bitfoundation/bitfabric's workflows read the same artifact. The prerelease
run also pushes its packages to nuget.org, skipping every version nuget.org already has, so a second run at the same
version changes nothing there and its build lives only in its artifact. The `prerelease_packages` option of All Other CI
and All Other E2E CI works the same way for their website and BlazorEmpty jobs. The push also stops at the first package
nuget.org refuses, and every package after it stays off nuget.org: compare the artifact's packages with nuget.org's
before calling a package published.

## 3. Report

Lead with the verdict, then:

1. **Deployment**: per deploy job, the site and the version it runs, where the Bit packages came from, and whatever
   had to be rolled out again.
2. **CI**: per failure, the test, its cause, the fix and its commit, and the local evidence - how many runs, before
   and after.
3. **Waiting for a CD**: every fix that only the next CD can prove, and for a library fix whether it can be tried
   through `prerelease_packages` before its release.

Anything that looks like a defect in a bit platform library: say so, and offer to report it as an issue on
bitfoundation/bitplatform. A defect in bit Boilerplate or bit CLI goes to bitfoundation/bitfabric.
