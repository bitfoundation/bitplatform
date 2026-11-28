---
name: dependency-update
description: Sweep every version-pinned surface in the bitplatform repo (NuGet, npm, GitHub Actions, devcontainers, dotnet tools, global.json), update what should move, leave what is deliberately held, and report each change with its release notes and any breaking behaviour. Use when asked to "update dependencies/packages/versions", refresh CI, or before a release.
---

# Dependency update sweep

Update every version pin in the repo in one pass, then hand back a report the maintainer can review
without opening the diff.

## First, check the checkout is up to date

The sweep reads its pins from the working tree, so a checkout that is behind `develop` sweeps stale
pins: it proposes bumps that already landed upstream and misses packages added since the last pull.
Before reading a single pin, fetch and count:

```bash
up=$(git remote -v | awk '$2 ~ /bitfoundation\/bitplatform(\.git)?$/ && $3 == "(fetch)" { print $1; exit }')
git fetch "$up" "+refs/heads/develop:refs/remotes/$up/develop"
git rev-list --count HEAD.."$up"/develop   # commits the checkout is missing
```

`$up` is the remote that points at `bitfoundation/bitplatform`: `upstream` when `origin` is your
fork, `origin` in a plain clone. If the count is not 0:

- On `develop` with a clean tree, fast-forward it: `git merge --ff-only "$up"/develop`.
- Anywhere else (another branch, local changes, commits of its own), leave the checkout alone and run
  the whole sweep, scan included, in a worktree on the latest `develop`
  (`git worktree add --detach ../bitplatform-sweep "$up"/develop`). Once the issue exists, name the
  branch there with `git switch -c <n>` instead of adding the worktree in Ship it step 2.

Open the report with the commit the sweep read.

## Ordering

Work the surfaces in this order. Each is independent; batch the network lookups.

1. NuGet (`*.csproj`, `*.props`)
2. npm (`package.json`, then `npm install` to refresh `package-lock.json`)
3. GitHub Actions (`.github/workflows/`, `.github/actions/`)
4. devcontainers (`.devcontainer/`)
5. Tool versions in workflows and docs (`dnx <package>@<version>`, `vpk`), `global.json` (4 files)
6. Downloads checked against a hash: the Jsign jar the NuGet publish workflows download with `curl`, whose SHA-256 moves
   with it, from the release

bit Boilerplate and bit CLI live in [bitfoundation/bitfabric](https://github.com/bitfoundation/bitfabric), and their
dependencies are swept there.

## The hold-back rules

Most of the value here is knowing what NOT to move. A pin in one of these categories is a decision,
not staleness — leave it, and say in the report that you left it and why.

**Roslyn and MSBuild — never bump.** An analyzer, source generator, or MSBuild task loads inside the
*consumer's* compiler, not ours. Referencing a `Microsoft.CodeAnalysis.*` newer than the oldest SDK we
support makes it fail to load there (CS8032 / AD0001). Same for `Microsoft.Build.*` and
`System.Reflection.Metadata` in a task assembly, which MSBuild supplies at run time. These are floors
for the oldest supported SDK:

| Package | Pinned | Where |
|---|---|---|
| `Microsoft.CodeAnalysis.Analyzers` | 3.3.3 | `Bit.BlazorUI.SourceGenerators`, `Bit.SourceGenerators` |
| `Microsoft.CodeAnalysis.Analyzers` | 3.3.4 | `Bit.BlazorUI.Analyzers` (the lowest `Microsoft.CodeAnalysis.CSharp` 4.8.0 accepts) |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | 4.3.1 | `Bit.BlazorUI.SourceGenerators`, `Bit.SourceGenerators` |
| `Microsoft.CodeAnalysis.Workspaces.Common` | 4.3.1 | `Bit.CodeAnalyzers` |
| `Microsoft.CodeAnalysis.CSharp` | 4.8.0 | `Bit.Brouter.Generators`, `Bit.Blex.Generators`, `Bit.BlazeDb.Generators` + their test projects, and `Bit.Blex.Demo.Server` and `Bit.BlazeDb.Demo.Server` (run their generators for their MCP servers) (must match) |
| `Microsoft.CodeAnalysis.CSharp` | 4.8.0 | `Bit.BlazorUI.Analyzers` (the breaking-change analyzer the Bit.BlazorUI package ships into an app's build) |
| `Microsoft.CodeAnalysis.CSharp` | 4.14.0 | `Bit.CodeAnalyzers` |
| `Microsoft.Build.Utilities.Core` | 17.7.2 | `Bit.Butil.Build`, `Bit.BlazorUI.Build` |
| `System.Reflection.Metadata` | 1.6.0 | `Bit.BlazorUI.Build` |

**TFM-conditioned floors — only the highest band moves.** A multi-targeted library pins the `.0` of
each band it supports, because the pin *is* the consumer's minimum runtime. Raising 10.0.0 to 10.0.x
raises that minimum for no gain.

```xml
<PackageReference Include="Microsoft.JSInterop" Version="10.0.0" Condition="'$(TargetFramework)' == 'net10.0'" />
```

Applies to `Bit.BlazorUI` / `.Assets` / `.Extras` / `.Legacy` / `.AI`, `Bit.Bswup` and `Bit.Butil`
(Components.Web), and the `BlazorEmpty` template.

The rule keys on the `.0` in a *shipped library*. A TFM-conditioned pin at a concrete patch —
`Bit.BlazorUI.Tests` on Components.WebView `10.0.x` — is not a floor: nothing
consumes a test project, so every band takes the newest patch of its own band. Likewise a lone
`10.0.0` sitting among `10.0.11` siblings is staleness, not intent.

**Version ranges stay.** `[10.0.0,11.0.0)` is a compatibility contract, not a pin. Its lower bound is the band's
floor, so once a new major ships, a range that starts at its preview or release candidate moves to its `.0`: a dotted
prerelease bound also makes nuget.org list the package as SemVer 2.0.0, which older clients' searches leave out. An
EF Core range is the exception, see the EF Core rule below.

**`Bit.*` self-references track the in-development version**, currently `11.0.0-pre-25`. Never read
these off nuget.org — the published `latest` is behind the working tree by design, and unrelated
higher-numbered lines exist there.

**TypeScript is held at 5.9.3 in the libraries.** 7.x is the Go rewrite
(`microsoft/typescript-go`), not an incremental release. If a bump is wanted, move one library project,
build it, and only then the rest. Do not move them all at once.

**A JavaScript action that vendors `node_modules` must have no symlinks in its tree.** The runner can't stage
them, so every job using it dies before the action runs (`Could not find file '.../node_modules/.bin/...'`), on
Linux and macOS at least. It is invisible in the release notes, so check every such action's new commit:

```bash
gh api "repos/<owner>/<repo>/git/trees/<sha>?recursive=1" --jq '[.tree[]|select(.mode=="120000")]|length'
```

`devops-actions/variable-substitution` v2.0.0 and v3.0.0 had them, and v3.1.0 fixed it
([devops-actions/variable-substitution#370](https://github.com/devops-actions/variable-substitution/issues/370)).

**MAUI's resizetizer breaks on SVG filters.** MAUI 10.0.101 to 10.0.110 and 11.0.0-rc.2 to 11.0.0 bundle an `Svg.Skia` that
calls a SkiaSharp overload missing from the SkiaSharp next to it, so any SVG it resizes that has a `<filter>` fails
the build with `MAUIR0001` / `MissingMethodException: SKImageFilter.CreateMatrixConvolution`
([dotnet/maui#38319](https://github.com/dotnet/maui/issues/38319)). 10.0.120 and 11.0.10 fixed it, and the app icon
of the BlazorUI demo has a drop shadow (its `<filter>`), so MAUI never goes below those.
Before a major ships, MAUI can't stay behind: a MAUI built for an
older preview doesn't compile against the newer preview's Android workload (CS1705 on `Java.Interop`). So MAUI
follows the MAUI manifest of the SDK's newest workload set (`Microsoft.NET.Workloads.<band>` on NuGet), which
can stay on the previous release candidate when a new one ships, together with Android and iOS; then MAUI stays
too. A service release of a shipped major doesn't wait for a set that names it when its `Microsoft.Maui.Core`
references the same `Java.Interop` and `Microsoft.iOS` versions as the one before it. The SDK waits for MAUI the same way: on the day a major ships, MAUI and the first workload set of the new band
can come hours or days after the SDK, and until that workload set is on NuGet a GA SDK's `dotnet workload restore`
installs the stale preview workloads bundled in the SDK. The set itself can appear before the packs it names, so
check that each manifest and pack it points at downloads too. A feature band's patch SDK waits for its own workload
set the same way: SDK 10.0.404 without one left `wasm-tools` unusable (NETSDK1147). So the SDK, the major's `.0` packages and the `global.json` files move together once it is
out.

**A project that targets only .NET 10 keeps every package built with .NET on its .NET 10 line**:
`Microsoft.Extensions.*` (the AI ones too), `Microsoft.AspNetCore.*`, `System.*` and `System.CommandLine`, whose
2.0.x ships with .NET 10 and 3.x with .NET 11. A .NET 11 project takes their new major.

**The `Xamarin.AndroidX.Camera.*` packages of `Bit.BbScanner.Maui` stay on the newest 1.5.x.** 1.6.x's
`Camera.View` pulls `Xamarin.AndroidX.Fragment` 1.9.0, which absorbed the ktx classes, while the rest of the graph
still brings `Fragment.Ktx` 1.8.9.x. R8 then fails an app's Android build with
`Type androidx.fragment.app.FragmentKt is defined multiple times`.

**A shipped library's EF Core floor follows bit Boilerplate's EF Core.** bit Boilerplate, in
[bitfoundation/bitfabric](https://github.com/bitfoundation/bitfabric), keeps its EF Core on the version its PostgreSQL
provider names, so a library's EF Core floor in that band (Bsqlite's Sqlite, BlazeDb's EF Core provider) can't go
above it: the generated project's restore fails with NU1605.

**Linux runners are on `ubuntu-26.04`** in the GitHub workflows. It
defaults to JDK 25 (`(default)` in the image readme's Java table), which .NET for Android accepts from .NET 11
RC2 on (`LatestSupportedJavaVersion` 25.0.99 in `Xamarin.Android.Common.props.in`). .NET for Android 10 accepts up
to 21, so a job that builds a .NET 10 Android app stays on `ubuntu-24.04`.

When a held pin's rationale no longer holds — the oldest supported SDK moved, TS 7 was adopted
repo-wide — say so in the report rather than acting on it.

## Finding what is behind

### NuGet

Extract every pin, attribute order varies, so do not assume `Include` precedes `Version`:

```bash
grep -rhoE '<Package(Reference|Version)[^>]*/?>' --include=*.csproj --include=*.props src/ \
  | grep 'Version="' \
  | sed -E 's/.*Include="([^"]+)".*/\1|&/' | sed -E 's/\|.*Version="([^"]+)".*/|\1/' \
  | sort -u
```

A `Condition="..."` attribute can sit between `PackageReference` and `Include` (the TFM-conditioned floors) — a
naive `Include="X" Version="Y"` regex silently drops those packages.

NuGet audit warnings (NU1902/NU1903) on a transitive package whose DLL the shared framework also
carries, such as `System.Security.Cryptography.Pkcs` in `Microsoft.AspNetCore.App`, are noise: the
framework's copy wins and the package's file never reaches the output. Check `bin` before adding a
direct reference to silence one.

Then per package (`https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json` returns
versions ascending):

- latest stable = last entry with no `-`
- target = latest stable, **except** a package whose newest release is a prerelease with no stable
  above it, which takes the prerelease, such as the `OpenTelemetry.*` packages on beta and rc.

Run the lookups with `xargs -P 12`; one at a time takes minutes.

**Check for intra-repo drift too.** The same package at different versions across projects is a bug
even when neither is behind latest — grep for duplicate ids with differing versions and reconcile,
unless the split is a TFM floor.

### npm

The direct dependencies are `typescript`, `sass`, `esbuild`, `vitest`, `happy-dom`, `axe-core`, `@tailwindcss/cli`
and `@web-alchemy/fonttools`; list them from every `package.json` rather than trusting this line.

A `vitest` bump needs `npm test` in every `Tests/bit-*-js` folder, on the Node version their CI jobs set up: newer
Node versions give happy-dom tests a `localStorage` of their own and fail them. Several import the built
`wwwroot` of their library, so build it first. Read latest
with `npm view <pkg> version` and `npm view <pkg> dist-tags --json`. After editing a `package.json`,
run `npm install` in that directory so `package-lock.json` matches — a lock file left behind is the
most common miss.

The lock files also carry transitive packages that no direct bump moves, and that is where the
repository's Dependabot alerts sit. Run `npm audit fix` (never `--force`) in every directory with a
lock file, and report what only a held major would fix.

A lock file has to come out of `npm install` on Linux unchanged. npm on Windows can write a `"name"`
into the lock's root package from the folder name when `package.json` has none, and Linux npm drops
it again, so every Linux build ends with a modified `package-lock.json`. Run `npm install` once in a
Linux container to check, or keep the root package as it was.

### GitHub Actions

Every step is SHA-pinned with a `# vX.Y.Z` comment. Resolve the tag to a commit and update both:

```bash
gh api "repos/<owner>/<repo>/releases?per_page=100" --jq '.[]|select(.prerelease==false)|.tag_name' \
  | sed 's/^v//' | sort -V | tail -1
gh api repos/<owner>/<repo>/git/ref/tags/v<tag> --jq '.object.sha,.object.type'
```

**Never use `releases/latest` here.** It returns whatever the repo flagged as latest, which is not
the newest release when an action maintains parallel major lines. `Azure/webapps-deploy` ships v2
and v3 side by side and flags `v2.2.19` — a sweep that trusts the flag silently keeps a deploy step
one major behind. Enumerate and sort instead, as above.

If `.object.type` is `tag` it is annotated — dereference with
`gh api repos/<owner>/<repo>/git/tags/<sha> --jq .object.sha` to get the commit. Pinning the tag
object instead of the commit does not work.

Where the pin's comment is a moving major (`# v2`), rewrite it to the concrete release the SHA
belongs to (`# v2.0.0`), so the comment records a version rather than a branch.

Runner images (`runs-on`) are part of this: no `ubuntu-latest`, pin the concrete image.

**Xcode follows the iOS workload.** The iOS jobs pin a runner image and `setup-xcode`'s `xcode-version`, and both
must give the Xcode that .NET for iOS asks for: the `RecommendedXcodeVersion` in `Versions.plist` of the
`Microsoft.iOS.Sdk.net<tfm>_<x.y>` pack whose version the newest workload set of the SDK's band names
(`Microsoft.NET.Workloads.<band>` on NuGet). A workload set can raise it without any SDK bump, and then every iOS
build fails with "This version of .NET for iOS requires Xcode ...". When the `macOS-*` image doesn't have that Xcode,
use GitHub's image that does (`xcode-27` for Xcode 27).

### devcontainers

Features pin a **major only** (`ghcr.io/devcontainers/features/node:2`). Read the current major from
`repos/devcontainers/features/contents/src/<feature>/devcontainer-feature.json` → `.version`. Before
bumping a major, check the feature's own option schema still accepts the options we pass, and skim
`repos/devcontainers/features/commits?path=src/<feature>/devcontainer-feature.json` for what the
major changed.

Feature *options* take a major too, never `latest` — `"helm": "3"`, not `"helm": "latest"`. The
install scripts resolve a partial version through `find_version_from_git_tags`.

The `image` follows the newest SDK of the major `src/global.json` uses: `curl -s
https://builds.dotnet.microsoft.com/dotnet/release-metadata/<major>.0/releases.json` → `.latest-sdk`. MCR publishes an
SDK's image hours after the SDK itself, so ask `https://mcr.microsoft.com/v2/dotnet/sdk/manifests/<tag>` before moving
to it.

### dnx tool versions and global.json

There are no `.config/dotnet-tools.json` manifests any more: a tool is run with `dnx <package>@<version>` or
installed with `dotnet tool install <package> --version <version>`, so the pins live in the workflow and doc lines
that call them (`grep -rnE "dnx .*@|tool install"`). `vpk` must equal the `Velopack` PackageReference version in the
same project — Velopack requires the CLI and the library to match, and this has drifted before.

`src/global.json` is `rollForward: disable` and tracks the newest SDK. The BlazorEmpty template's `global.json` pins
`11.0.100` + `latestFeature`, so it accepts any 11.0.x on a consumer's machine. While the next major is a preview or
release candidate, it pins the same SDK as `src/global.json`, also `rollForward: disable`, and goes back to
`<major>.0.100` + `latestFeature` once it ships. The Platform and Sales websites pin their own .NET SDK.

## The report

Deliver this, not a diff dump.

**1. Changed** — one table, grouped by surface:

| Package | From | To | Notes |
|---|---|---|---|

**2. Breaking or important** — for every changed entry, fetch the release notes between the two
versions and read them:

- GitHub-hosted: `gh api repos/<owner>/<repo>/releases --jq '.[]|select(...)|.body'`
- NuGet-only: the package's `releaseNotes` from
  `https://api.nuget.org/v3/registration5-gz-semver2/<id>/index.json`, else the project URL

Call out, with a one-line consequence and a link: any major bump; anything the notes label breaking,
removed, or deprecated; behaviour changes in a deploy or build path; security fixes. A minor or patch
whose notes are pure dependency bumps needs one line, not a summary. Say plainly when a change is
routine.

**3. Held back** — what was left and which hold-back rule covers it. Flag any pin whose rationale has
expired.

**4. Missed / drifted** — pins that are behind for no discoverable reason, and the same package at
different versions across projects. These are the ones worth a maintainer's attention.

**5. Not verified** — anything only a real build or CI run can prove. Deploy-path action bumps,
devcontainer feature majors, and analyzer bumps are in this category.

## After editing

- `npm install` in every directory whose `package.json` changed.
- `dotnet build` the affected solutions; a Roslyn or MSBuild pin change needs a real build.
- Never rewrite files through Python or `sed` — BOM and CRLF get clobbered. Use targeted edits.

## Ship it

Finish every sweep with an issue, a PR and a full CI run. `upstream` is `bitfoundation/bitplatform`,
`origin` is the maintainer's fork.

1. Issue on upstream, label `dependencies`, title `Project dependencies are outdated`. The body
   lists what is behind, in a sentence or two per surface.
2. Branch named after the issue number, cut from `upstream/develop`, not from whatever branch is
   checked out. When the working tree sits on another branch, apply the diff in a worktree
   (`git worktree add ../bitplatform-<n> -b <n> upstream/develop`, then `git diff | git apply --3way`)
   so that branch and any build running on it stay untouched.
3. One commit, `feat(deps): update project dependencies #<n>`. Push to `origin`.
4. PR from `<fork-owner>:<n>` into `upstream/develop`, title `Update project dependencies (#<n>)`.
   The body starts with `closes #<n>` and carries the report: Changed, Breaking or important,
   Held back, Missed / drifted, Not verified.
5. Run All Other CI (`all.other.ci.yml`), All Toolings CI (`all.toolings.ci.yml`), All Utilities CI
   (`all.utilities.ci.yml`) and CI Templates E2E (`ci.templates.e2e.yml`, projects made from BlazorEmpty)
   on the fork against the branch and give the four URLs with the PR link. All CI is split by folder now, so no one run
   covers every project:

   ```bash
   for w in all.other.ci.yml all.toolings.ci.yml all.utilities.ci.yml ci.templates.e2e.yml; do
     gh workflow run $w --repo <fork-owner>/bitplatform --ref <n>
   done
   for w in all.other.ci.yml all.toolings.ci.yml all.utilities.ci.yml ci.templates.e2e.yml; do
     gh run list --repo <fork-owner>/bitplatform --workflow $w --branch <n> --limit 1 --json url --jq '.[0].url'
   done
   ```

   A red job goes into the PR as a comment, with the failing step and its first error line.
6. CodeRabbit reviews the PR a few minutes after it opens, so come back to the PR once its review is in. It skips
   later pushes when incremental reviews are off ("Review skipped"), so after the last push comment
   `@coderabbitai review` and come back for that review too. Check each of its comments against the code: fix the ones that hold in the same branch,
   and answer every other thread with the reason it stays. Its pre-merge checks can't see the lock files it skips,
   so an "inconclusive" that only asks for them needs no answer.
7. Clean up at once, without asking: `git restore` the pushed files in the original checkout (after
   `git diff <pushed-commit> -- <files>` comes back empty), delete build artifacts the sweep created,
   and `git worktree remove` the worktree. `git status` must match what it was before the sweep.
