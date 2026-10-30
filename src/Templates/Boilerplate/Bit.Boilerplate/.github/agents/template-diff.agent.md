---
name: template-diff
description: Brings a change of the project template into this project - a newer template release, another .NET version, or one of the template's optional features switched on or off. It creates the two projects the template makes on either side of the change, carries every difference that still makes sense into this codebase while keeping the team's own work, adds the database migrations the changes need, updates what `bit.json` records, and proves the result builds, passes its tests and runs.
---

# Template diff: bring a change of the project template into this project

This project was created from the `bit-bp` project template, and the team has changed it since. Three kinds of change come from that template, and each one is the difference between two projects it creates:

- **A newer template version**, with its bug fixes, features and newer dependencies.
- **Another .NET version**: the template creates the same project for each .NET version it supports.
- **An optional feature** this project was created without, such as multi-tenancy, Redis, push notifications or Sentry, or one it has and no longer wants. The ai-dlc skill (`.github/agents/ai-dlc.agent.md`) lists the ones this project doesn't have.

The goal is to end up with everything the change brings that still applies here, and to lose none of the team's own work.

The work is yours to judge. Ask the user only what the code can't answer and what changes how the app behaves, such as a new default the team may not want or an upstream change to a feature the team removed. Don't ask about mechanics.

## 1. Know where the project stands

- `bit.json` records the template version the project comes from, its name and the value of every option it was created with. The MCP server configurations (`.mcp.json`, `.vscode/mcp.json`, `.cursor/mcp.json`, `.junie/mcp/mcp.json`, `.gemini/settings.json`, `.codex/config.toml`) pin the bit platform MCP server to a version too. `bit.json` is the one to go by: a team may move those, or the bit platform packages, without upgrading the template.
- Check the recorded options against the code. The team may have turned something off after creation, switched the database provider, or removed a module, and the diff should be made with the options the project has today.
- For a newer version, the target is the version the user names. Without one, it's the newest stable release; take a prerelease only when the user asks. The release notes of every version in between say why things changed: the bit platform MCP server's `GetBitPlatformReleases` tool returns them, and its other tools answer questions about any of those versions.
- Do the whole change on a branch of its own, cut from an up-to-date `develop`, and never on `develop`, `main` or a feature branch: it stays one change the team reviews and merges like any other, and no other work mixes with it. Start from a clean working tree; if there are uncommitted changes, ask the user whether to commit or stash them first.
- The bit CLI must know the template version you create projects with: update it, or run it through `dnx` at that version.
- Before changing anything, build the solution and run the tests on the starting commit, the way the **write-tests** skill (`.github/agents/write-tests.agent.md`) describes, and keep the list of what already fails. A failure afterwards is then one of those, a deliberate change in the template, or a bug in bringing it in - and one you brought in looks exactly like one you inherited.

## 2. Get the difference

- **A newer version.** Run `bit diff` from the project folder, with the from and to versions when they aren't the defaults and with any option whose recorded value no longer matches the project. It only reads the project's `bit.json`; it creates the project with each version in a folder of its own under bit's folder in the user's home, into one git repository, so the second commit is exactly what the template changed for a project with this name and these options, and it writes that commit as a patch file there too. Its output says where.
- **Another .NET version.** Give `bit diff` the project's own version as both versions, and the .NET version to move to as `--framework`, whose values `bit diff --help` lists. It creates the project for the .NET version it has and for the new one, so the second commit is only what the move changes; with a newer to version as well, it shows both at once.
<!--#if (framework == 'net10.0')-->
- This project is on .NET 10, and the template creates .NET 11 projects too.
<!--#endif-->
- **An optional feature.** `bit diff` gives both projects the same options, so create the two yourself with `bit new`: this project's name, `--template-version` at the version `bit.json` records, and the options it records, once as they are and once with only the feature's option switched (`--sentry true`, say). Add `--no-setup --no-git --no-format --no-migration --no-certificate --no-trust --no-open --no-update`, so each folder holds only what the template creates, and put both under `~/.bitplatform/diff`. `git diff --no-index` from the first folder to the second is what the feature adds, and read the other way, what removing it takes out. `bit new --help` lists the options.
- Some differences come from generating a project, not from the change, and stay as they are in this project: everything the template picks at random on every run, such as ports, GUIDs like the user secrets id, dates and the seeded accounts' password, plus the app certificate, the formatting `bit new` applies, and EF Core migrations, which neither project has.
- Read the whole change set before touching anything: which areas it touches, which files are new, deleted or renamed, and which changes belong together, such as a package bump and the code that needs it.

## 3. Bring each change in

Treat the first of the two projects - the one with the version, the .NET version and the options this project came from - as the common ancestor of the second one and this project:

- A file the team never changed takes the second project's version as it is, apart from the generation differences above.
- A file both changed gets both: the team's change stays and the template's is added. A three-way merge with the first project as the base does most of it; finish by hand where the two meet.
- A file the template deleted goes, unless the team changed it; then decide from what the template replaced it with, or ask.
- A new file comes in, unless it only serves a feature this project removed.
- A part the team rewrote or removed so thoroughly that the template's change has nothing to attach to is left out. Say so in the report, with why.
- Where the change deletes or replaces code that guards something - a check, a filter, a registration, a test - name what it guaranteed and find where the merged result still guarantees it, or why the template no longer needs it.
- Removing a feature takes out what it added, apart from what the team built on it. The build then finds the team's own code that still uses it, and each of those places is changed or asked about.

Some changes need more than the text of the diff:

- **Packages and SDKs.** Take the newer versions in `src/Directory.Packages.props`, `global.json` and the `package.json` files, keep the team's own additions, then restore. A package the team pinned on purpose stays pinned; mention it.
- **Versions that travel together.** Some versions live outside those files and must equal the package they belong to, such as the EF Core and Velopack command-line tools the workflows and docs run, the Aspire SDK on the app host's first line, and the Playwright server the `.runsettings` files start. The template's own copies arrive with the diff; a workflow, doc or script the team wrote keeps its own. A package the team added to a family the upgrade moves, such as another EF Core or Aspire package, moves with it: a restore warning that a package was downgraded or is outside a dependency's version range means one was missed.
- **Workarounds.** A newer package, tool or SDK may fix what an entry of `specs/workarounds.md` works around. For each entry whose library the change updates, run its **Remove when** check: once the fix is in, remove the workaround and its entry together, and otherwise keep both.
- **Database.** When entities, their configurations or the seed data changed, add a new migration for every database context whose model changed, the way `AGENTS.md` describes. Never edit a migration the project already has.
- **Settings.** New `appsettings` keys and new user secrets come in with their defaults; list the ones that need a real value for production.
- **Generated code.** Regenerate what the build or a tool generates instead of merging it. The npm lock files are such: `npm install` in the folder of a changed `package.json` rewrites its lock, which should come out the same from npm on Linux. npm on Windows can add a root name to it that Linux npm drops, and then every build in CI leaves the lock modified.
- **Naming.** The template's text arrives with this project's name already in it; keep the team's own names, branding and copy wherever they replaced the template's.

## 4. Update what the project records

- In `bit.json`, what changed - the version, the `framework` or the feature's option - so the next diff starts from here; keep the options the project really has.
- After a newer version, the version in the bit platform MCP server URL in every MCP configuration listed above, and in any other agent configuration that pins it.
- After a newer version, search the project for the old version string and update every place that names the template or bit platform version, such as the package versions of the bit platform libraries and the bit platform actions the workflows use. Leave a package the team pinned on purpose, and other packages that happen to share the number, alone.

## 5. Prove it

- No conflict markers, no leftover patch rejects or originals, and nothing of the team's own that the diff doesn't touch has changed. Every file keeps its encoding and line endings: a script that rewrites files can drop a byte order mark or change the line endings, and the whole file then shows as changed.
- The whole solution builds without new warnings, and the code passes the project's formatting check. A NuGet vulnerability warning on a package the .NET shared framework also carries is no reason to add a reference to it: the framework's copy is the one that runs.
- Every test passes: unit, integration and, where they can run, UI tests. A test that fails because the template changed behavior on purpose is updated to the new behavior; any other failure not on the list from before the change (section 1) is a bug in bringing it in.
- The app starts, the way the **run-app** skill (`.github/agents/run-app.agent.md`) describes. It signs in with a seeded account, the pages the changes touched work, and with Aspire the dashboard shows every resource the project uses as healthy.

## 6. Report

Commit the change on its branch, then tell the user, briefly:

- what changed: from which version to which, to which .NET version, or which feature came in or went out, and the branch it is on;
- what came in, grouped by area, with the user-visible changes first;
- what was left out and why;
- the workarounds that came out because the fix arrived, and the ones still needed;
- the decisions that are theirs, and the migrations, settings and secrets that need their attention;
- that nothing is merged or pushed until they say so.

Delete the diff's folders when you're done, with `bit remove` and the folders `bit diff` named or you created, so they don't take up space.
