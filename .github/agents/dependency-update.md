---
name: dependency-update
description: Sweep every version-pinned surface in the bitplatform repo (NuGet, npm, GitHub Actions, Azure DevOps tasks, devcontainers, dotnet-tools, global.json, container image tags), update what should move, leave what is deliberately held, and report each change with its release notes and any breaking behaviour. Use when asked to "update dependencies/packages/versions", refresh CI, or before a release.
---

# Dependency update sweep

Update every version pin in the repo in one pass, then hand back a report the maintainer can review
without opening the diff.

## Ordering

Work the surfaces in this order. Each is independent; batch the network lookups.

1. NuGet (`*.csproj`, `Directory.Packages.props`)
2. npm (`package.json`, then `npm install` to refresh `package-lock.json`)
3. GitHub Actions (`.github/workflows/`, `src/Templates/Boilerplate/Bit.Boilerplate/.github/workflows/`, `src/Butil/tests/Bit.Butil.Tests.E2E/ci/`)
4. Azure DevOps tasks (`src/Templates/Boilerplate/Bit.Boilerplate/.azure-devops/workflows/`)
5. devcontainers (`.devcontainer/`, `src/Templates/Boilerplate/Bit.Boilerplate/.devcontainer/`)
6. `.config/dotnet-tools.json` (4 files), `global.json` (3 files)
7. Container image tags in the Aspire AppHost

## The hold-back rules

Most of the value here is knowing what NOT to move. A pin in one of these categories is a decision,
not staleness — leave it, and say in the report that you left it and why.

**Roslyn and MSBuild — never bump.** An analyzer, source generator, or MSBuild task loads inside the
*consumer's* compiler, not ours. Referencing a `Microsoft.CodeAnalysis.*` newer than the oldest SDK we
support makes it fail to load there (CS8032 / AD0001). Same for `Microsoft.Build.*` in a task
assembly. These are floors for the oldest supported SDK:

| Package | Pinned | Where |
|---|---|---|
| `Microsoft.CodeAnalysis.Analyzers` | 3.3.3 | `Bit.BlazorUI.SourceGenerators`, `Bit.SourceGenerators` |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | 4.3.1 | `Bit.BlazorUI.SourceGenerators`, `Bit.SourceGenerators` |
| `Microsoft.CodeAnalysis.Workspaces.Common` | 4.3.1 | `Bit.CodeAnalyzers` |
| `Microsoft.CodeAnalysis.CSharp` | 4.8.0 | `Bit.Brouter.Generators` + its test project (must match) |
| `Microsoft.CodeAnalysis.CSharp` | 4.14.0 | `Bit.CodeAnalyzers` |
| `Microsoft.Build.Utilities.Core` | 17.7.2 | `Bit.Butil.Build` |

**TFM-conditioned floors — only the highest band moves.** A multi-targeted library pins the `.0` of
each band it supports, because the pin *is* the consumer's minimum runtime. Raising 8.0.0 to 8.0.x
raises that minimum for no gain.

```xml
<PackageReference Include="Microsoft.JSInterop" Version="8.0.0" Condition="'$(TargetFramework)' == 'net8.0'" />
```

Applies to `Bit.Besql` (JSInterop, Components.WebAssembly, EF Core Sqlite), `Bit.BlazorUI` /
`.Extras` / `.Legacy` (Components.Web, Components.WebView), and the `BlazorEmpty` template.

The rule keys on the `.0` in a *shipped library*. A TFM-conditioned pin at a concrete patch —
`Bit.BlazorUI.Tests` on Components.WebView `8.0.30` / `9.0.19` / `10.0.11` — is not a floor: nothing
consumes a test project, so every band takes the newest patch of its own band. Likewise a lone
`10.0.0` sitting among `10.0.11` siblings in `Directory.Packages.props` is staleness, not intent.

**Version ranges stay.** `[8.0.0,9.0.0)` is a compatibility contract, not a pin.

**`Bit.*` self-references track the in-development version**, currently `10.6.0-pre-04`. Never read
these off nuget.org — the published `latest` is behind the working tree by design, and unrelated
higher-numbered lines exist there.

**TypeScript is held at 5.9.3 in the libraries.** 7.x is the Go rewrite
(`microsoft/typescript-go`), not an incremental release. `Boilerplate.Client.Core` is the canary and
is already on 7.0.2 — if a bump is wanted, move one library project, build it, and only then the
rest. Do not move all eleven at once.

**Vitest is held at `^3`** in `src/Bmotion/Tests/bit-bmotion-js` and `src/Bswup/Tests/bit-bswup-js`.

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

A `Condition="..."` attribute sits between `PackageVersion` and `Include` on many
`Directory.Packages.props` lines — a naive `Include="X" Version="Y"` regex silently drops ~60
packages, including everything Aspire, FusionCache, and Agents.AI.

Then per package (`https://api.nuget.org/v3-flatcontainer/<lowercased-id>/index.json` returns
versions ascending):

- latest stable = last entry with no `-`
- target = latest stable, **except** a package whose newest release is a prerelease with no stable
  above it, which takes the prerelease. That is the whole `OpenTelemetry.*` 1.18.0-beta.1 family,
  `Microsoft.Agents.AI.Hosting`, `Microsoft.SemanticKernel.Connectors.HuggingFace`,
  `Aspire.Hosting.Keycloak` and `Aspire.Hosting.Maui`.

Run the lookups with `xargs -P 12`; ~210 packages otherwise takes minutes.

**Check for intra-repo drift too.** The same package at different versions across projects is a bug
even when neither is behind latest — grep for duplicate ids with differing versions and reconcile,
unless the split is a TFM floor.

### npm

Only four distinct dev dependencies exist: `typescript`, `sass`, `esbuild`, `vitest`. Read latest
with `npm view <pkg> version` and `npm view <pkg> dist-tags --json`. After editing a `package.json`,
run `npm install` in that directory so `package-lock.json` matches — a lock file left behind is the
most common miss.

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

### Azure DevOps

Tasks are major-versioned (`UseDotNet@2`, `FileTransform@2`). Confirm against Microsoft's task
reference; there is no API. All eight in use were current as of the last sweep.

### devcontainers

Features pin a **major only** (`ghcr.io/devcontainers/features/node:2`). Read the current major from
`repos/devcontainers/features/contents/src/<feature>/devcontainer-feature.json` → `.version`. Before
bumping a major, check the feature's own option schema still accepts the options we pass, and skim
`repos/devcontainers/features/commits?path=src/<feature>/devcontainer-feature.json` for what the
major changed.

Feature *options* take a major too, never `latest` — `"helm": "3"`, not `"helm": "latest"`. The
install scripts resolve a partial version through `find_version_from_git_tags`.

The `image` follows the newest .NET SDK: `curl -s
https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json` → `.latest-sdk`. Both
devcontainers must agree.

### dotnet-tools and global.json

`vpk` must equal the `Velopack` PackageReference version in the same project — Velopack requires the
CLI and the library to match, and this has drifted before. `dotnet-ef` should equal the EF Core
package version.

`src/global.json` is `rollForward: disable` and tracks the newest SDK. The two template
`global.json` files are `10.0.100` + `latestFeature` deliberately — they must accept any 10.0.x on a
consumer's machine. Do not "update" them.

### Container images

In `Boilerplate.Server.AppHost/Infrastructure/Extensions/IDistributedApplicationBuilderExtensions.cs`.
Pin a major (`pgvector/pgvector:pg18`, `mssql/server:2025-latest`), never `latest`. Where the image
publishes no major-only tag — `redis/redis-stack`, whose tags are all `<version>-v<build>` — pin the
full newest tag and note it.

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

**5. Not verified** — anything only a real build or CI run can prove. Deploy-path action bumps
(`azure/webapps-deploy`), devcontainer feature majors, and analyzer bumps are in this category.

## After editing

- `npm install` in every directory whose `package.json` changed.
- `dotnet build` the affected solutions; a Roslyn or MSBuild pin change needs a real build.
- Never rewrite files through Python or `sed` — BOM and CRLF get clobbered. Use targeted edits.
