---
name: upgrade
description: Brings a newer bit Boilerplate release into this project. `bit diff` creates the project with the version it came from and with the newer one, and the skill carries every upstream change that still makes sense into this codebase while keeping the team's own work, adds the database migrations the changes need, updates the version the project records, and proves the result builds, passes its tests and runs.
---

# Upgrade to a newer bit Boilerplate

This project was created from the bit Boilerplate template, and the team has changed it since. A newer template version fixes bugs, adds features and moves dependencies forward. The goal is to end up with everything the newer version brings that still applies here, and to lose none of the team's own work.

The work is yours to judge. Ask the user only what the code can't answer and what changes how the app behaves, such as a new default the team may not want or an upstream change to a feature the team removed. Don't ask about mechanics.

## 1. Know where the project stands

- `bit.json` records the bit Boilerplate version the project comes from, its name and the value of every option it was created with. The MCP server configurations (`.mcp.json`, `.vscode/mcp.json`, `.cursor/mcp.json`, `.junie/mcp/mcp.json`, `.gemini/settings.json`, `.codex/config.toml`) pin the bit platform MCP server to a version too. `bit.json` is the one to go by: a team may move those, or the bit platform packages, without upgrading the template.
- Check the recorded options against the code. The team may have turned something off after creation, switched the database provider, or removed a module, and the diff should be made with the options the project has today.
- The target is the version the user names. Without one, it's the newest stable bit Boilerplate; take a prerelease only when the user asks. The release notes of every version in between say why things changed: the bit platform MCP server's `GetBitPlatformReleases` tool returns them, and its other tools answer questions about any of those versions.
- Do the whole upgrade on a branch of its own, cut from `develop`, and never on `develop`, `main` or a feature branch: it stays one change the team reviews and merges like any other, and no other work mixes with it. Start from a clean working tree; if there are uncommitted changes, ask the user whether to commit or stash them first.
- The bit CLI must know the target version: update it, or run it through `dnx` at that version.

## 2. Get the upstream changes

- Run `bit diff` from the project folder, with the from and to versions when they aren't the defaults and with any option whose recorded value no longer matches the project. It only reads the project's `bit.json`; it creates the project with each version in a folder of its own under bit's folder in the user's home, into one git repository, so the second commit is exactly what the template changed for a project with this name and these options, and it writes that commit as a patch file there too. Its output says where.
<!--#if (framework == 'net10.0')-->
- Moving this .NET 10 project to .NET 11 is the same work: give `bit diff` the .NET 11 value of `--framework`, which `bit diff --help` lists, and it creates the project for .NET 10 and for .NET 11, so the second commit is what the move changes. With a to version too, it shows both at once.
<!--#endif-->
- Some differences come from generating a project, not from the upgrade, and stay as they are in this project: everything the template picks at random on every run, such as ports, GUIDs like the user secrets id, dates and the seeded accounts' password, plus the app certificate, the formatting `bit new` applies, and EF Core migrations, which neither version has.
- Read the whole change set before touching anything: which areas it touches, which files are new, deleted or renamed, and which changes belong together, such as a package bump and the code that needs it.

## 3. Bring each change in

Treat the version the project came from as the common ancestor of the newer version and this project:

- A file the team never changed takes the newer version as it is, apart from the generation differences above.
- A file both changed gets both: the team's change stays and upstream's is added. A three-way merge with the old version as the base does most of it; finish by hand where the two meet.
- A file upstream deleted goes, unless the team changed it; then decide from what upstream replaced it with, or ask.
- A new file comes in, unless it only serves a feature this project removed.
- A part the team rewrote or removed so thoroughly that upstream's change has nothing to attach to is left out. Say so in the report, with why.

Some changes need more than the text of the diff:

- **Packages and SDKs.** Take the newer versions in `src/Directory.Packages.props`, `global.json` and the `package.json` files, keep the team's own additions, then restore. A package the team pinned on purpose stays pinned; mention it.
- **Database.** When entities, their configurations or the seed data changed, add a new migration for every database context whose model changed, the way `AGENTS.md` describes. Never edit a migration the project already has.
- **Settings.** New `appsettings` keys and new user secrets come in with their defaults; list the ones that need a real value for production.
- **Generated code.** Regenerate what the build or a tool generates instead of merging it.
- **Naming.** Upstream text arrives with this project's name already in it; keep the team's own names, branding and copy wherever they replaced the template's.

## 4. Update the version the project records

- In `bit.json`, the version, so the next upgrade starts from here; keep the options the project really has.
- The version in the bit platform MCP server URL in every MCP configuration listed above, and in any other agent configuration that pins it.
- Search the project for the old version string and update every place that names the template or bit platform version, such as the package versions of the bit platform libraries. Leave a package the team pinned on purpose, and other packages that happen to share the number, alone.

## 5. Prove it

- No conflict markers, no leftover patch rejects or originals, and nothing of the team's own that the diff doesn't touch has changed.
- The whole solution builds without new warnings, and the code passes the project's formatting check.
- Every test passes: unit, integration and, where they can run, UI tests. A test that fails because upstream changed behavior on purpose is updated to the new behavior; any other failure is a bug in the upgrade.
- The app starts. It signs in with a seeded account, the pages the changes touched work, and with Aspire the dashboard shows every resource the project uses as healthy.

## 6. Report

Commit the upgrade on its branch, then tell the user, briefly:

- from which version to which, and the branch it is on;
- what came in, grouped by area, with the user-visible changes first;
- what was left out and why;
- the decisions that are theirs, and the migrations, settings and secrets that need their attention;
- that nothing is merged or pushed until they say so.

Delete the diff's folder when you're done, with `bit remove` and the folder `bit diff` named, so it doesn't take up space.
