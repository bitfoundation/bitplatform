# Workflows

Every workflow here is a CI workflow, a CD workflow, or an All workflow that groups them. A workflow's name starts with CI, CD or All, and its file is that name in lower case with dots for spaces: "CI Blex E2E" lives in `ci.blex.e2e.yml`.

## CI

Each project has a CI workflow, `ci.<project>.yml`. It runs on pull requests that touch the project, builds it, runs its unit and integration tests in Debug, and packs every package the project publishes the way the release workflows pack it. A broken package fails on the project's own pull request, long before a release.

Browser and other end-to-end suites live in a separate `ci.<project>.e2e.yml`. These never run on pull requests: they start by hand or from an All workflow. A suite that drives a demo drives it published in Release, the way its CD publishes it.

## CD

A `cd.<app>.yml` workflow deploys a demo or a website.

Two workflows publish the NuGet packages: CD Prerelease nuget packages on a `v-*-pre-*` tag, and CD Nuget packages on a push to `main`. Both pack in parallel jobs (other, utilities, toolings, and the MAUI package on Windows). Then one publish job, the only job that can sign and push, signs the bit assemblies, attests the packages and their SBOM, uploads them as the `nupkg-files` artifact and pushes them to nuget.org with trusted publishing.

## All workflows

| Workflow | Runs |
|---|---|
| All Other CI | the CI of BlazorUI, Bswup, Butil, Brouter, the templates and the websites |
| All Utilities CI | the CI of everything under `src/Utilities` |
| All Toolings CI | the CI of everything under `src/Toolings` |
| All Other E2E CI | the end-to-end suites of the products and the templates |
| All Utilities E2E CI | the end-to-end suites of the utilities |
| All Release Build | every solution in Release |
| All Apps CD | deploys every demo and website |

Apart from the exceptions below, each CI, E2E and CD workflow is called by exactly one All workflow, the one for its folder. A workflow can call at most 50 different workflows, so no single run covers everything: before a release, run the five CI and E2E ones.

All Release Build starts on pull requests titled Prerelease, Release or Version, and by hand. It builds every folder solution in Release, each in a job of its own. The Boilerplate and the websites build there too, against packages packed from the same commit, because nuget.org doesn't have the version being released yet.

## Concurrency

A newer run of a workflow on the same pull request or branch cancels the older one. A CD that only deploys a website has one group whatever the branch, since every branch deploys to the same server, so the newer deploy wins. The NuGet CDs and the CDs that upload apps to the stores queue instead: stopping them halfway would leave a release half uploaded.

## Where the Bit packages come from

Libraries, their demos and their tests reference each other as projects, so they always build the code of the same commit.

The templates, the websites and the demos made from the Boilerplate take the Bit packages from NuGet:

- from nuget.org by default;
- with `prerelease_packages`, from the newest `nupkg-files` artifact of a CD Prerelease nuget packages run on any branch. Run that workflow on a branch, then All Apps CD with `prerelease_packages`, to try a library change in those apps before it is released. That run also pushes to nuget.org any package id that is new at that version;
- in All Release Build, from the packages that same run packs.

## Adding a project

- A `ci.<project>.yml` that runs on pull requests touching the project's folder and packs its packages, and a `ci.<project>.e2e.yml` for its end-to-end suites, each with a concurrency group like its neighbours'.
- A call to each from the All workflows of its folder.
- Its folder solution in All Release Build's matrix.
- Its packages in both NuGet workflows, and its folder in CD Nuget packages' paths.
- For a demo, a `cd.<project>.demo.yml` and a call from All Apps CD.

## Exceptions

- Support tracking keeps its own name. It, CD Nuget packages, CD Prerelease nuget packages and CI CodeQL are called by no All workflow. CI CodeQL scans the whole repository on pull requests, pushes to `develop` and `main`, and once a week.
- All Release Build calls no workflows. Its jobs are matrices, one job per solution.
- All Apps CD and Support tracking have no concurrency group. The CDs that All Apps CD calls decide for themselves, and each Support tracking run handles another issue.
- The Copilot entries in Actions are GitHub's own workflows, with no file here.
- CI Templates and CI Websites skip release pull requests, because their version isn't on nuget.org yet. All Release Build builds the Boilerplate and the websites on those pull requests instead.
- The Boilerplate's tests in CI Templates E2E run on projects made with `bit new`, in Debug, not on a published build.
- The library demos take Bit.Minifier as a package, not a project.
- The release workflows pack Bit.BbScanner.Maui on Windows, the only runner that builds all of its platforms. Its CI packs the Android target only.
- BlazorEmpty builds only after `dotnet new`, so no workflow builds its folder as it is.
- The Platform and Sales websites pin their own .NET SDK.
