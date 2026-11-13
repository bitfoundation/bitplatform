---
name: release-notes
description: Write or update the notes of the next bit platform release in its GitHub Discussion - Key Highlights that say what users get, the dependencies added, removed or moved to a new major version, and every PR since the previous release, sorted by section and importance. Each run picks up where the last one stopped, from the progress comment on that discussion. Use when asked for release notes, to add the latest PRs to them, or to start the notes of a new release.
---

# Release notes

Keep the notes of the coming release ready to publish at any time, so the release itself is a copy and paste, and so
anyone can read them before it ships.

## Where the notes live

- One discussion per release in the Announcements category, titled `bit <major> release notes (<tag>)`. Its body is one
  line saying it is a draft that is updated as PRs land, then the release notes exactly as the GitHub release will carry
  them, from `## What's Changed` down.
- Its first comment is the progress record. Nothing else goes in that comment.
- Every run edits the body and the progress comment in place. Never open a second discussion for the same release or post
  a second progress comment.
- Look for the discussion by its tag. When there is none, this is a new release: create the discussion and its progress
  comment.
- A discussion body takes up to 65,536 characters. If the notes outgrow it, keep the highlights and Dependencies in the
  body, move the longest PR list into a comment of its own that is edited the same way, and say so in the progress
  comment.

## Reading the request

- The request often ends with a list of PR numbers. Those are open PRs to leave out. Every other open PR on develop
  counts as merged and goes in the notes. Say this reading back, in case it was meant the other way.
- The range runs from the previous stable release's tag to develop.
- The notes are written in English, whatever language the request comes in.

## Picking up where the last run stopped

Fetch develop first. The progress comment holds what the next run needs:

- the range and the tag the notes are for
- the develop commit the review reached, and the day the open PRs were read
- the open PRs counted as merged, the open PRs left out as asked, and the PRs left out by rule
- PRs folded into another one (for example, merged into a feature branch that came in as one PR)
- the last PR that changed the dependencies
- the number of PRs per section
- a short dated log, one line per run

A PR is new when it was merged after that commit, or when it is open and in none of the lists. Also look for PRs counted
as merged that were closed without merging: they come out of the notes. At the end of the run, update every line of the
comment and add one to the log.

## Keep it cheap

- Work from PR titles. Read a PR's body only for the few big ones the highlights need. Never read code to list or sort PRs.
- The PR list comes from GitHub's generate-notes API between the previous tag and develop, plus the open PRs.
- Each PR's section comes from the scope of its squash commit on develop (`feat(blazorui):` and so on), so nothing has
  to be read to sort them. `blazorui` goes to bit BlazorUI, `templates` to bit Boilerplate, `websites` to Websites, and
  everything else (each library, the CLI, infra, CI) to Tooling. A new .NET version and license changes go to Platform.
  An open PR has no squash commit yet: take its scope from its title and files.

## Structure

1. `## What's Changed`
2. `### Key Highlights`, with `####` subsections: bit platform (a new .NET version, license changes), bit BlazorUI,
   Tooling, bit Boilerplate, Websites. A new .NET version always goes in the highlights.
3. After Websites, a general note as a quote: `> **Note:** Everything in bit platform, ... is there to get you to a
   better product with fewer tokens and less time. Nothing limits what you build or what you use.` Keep it in every
   release, with examples from that release.
4. `### Dependencies`, right after the highlights, in two parts, bit packages and bit Boilerplate: what was added,
   removed or moved to a new major version since the previous release.
5. One list per section (Platform, bit BlazorUI, bit Boilerplate, Tooling, Websites) in the format GitHub generates:
   `* <PR title> by @author in <url>`. Titles stay exactly as they are on GitHub.
6. Leave out "Update project dependencies" and "Prerelease" PRs.
7. `## New Contributors` when GitHub lists any, then the Full Changelog link.

Inside each list, order by importance: new features and the most visible changes first, fixes and tests last. Keep
related PRs together. It does not have to be perfect.

## How the highlights read

These came from the maintainer's corrections, so get them right the first time:

- **Almost non-technical, and always say what the reader gets.** "No native code reads untrusted uploads" was answered
  with "so what? say it's safer". Write the benefit: safer, faster, smaller, cheaper, less work.
- **No jargon dumps.** "Docker Compose, DbGate and Redis dashboards over SSH, GitHub Free, fewer Actions minutes" became
  "give the deploy skill SSH access to a server and get your app delivered there, secure, on a VM that costs about €4 a
  month". Lead with the outcome and a concrete number.
- **Sell the strong points:** projects that keep up with the template (the template-diff skill), a professional AI-DLC,
  zero downtime deployments, security. Prefer them over routine changes like branch rules or where settings are stored.
- **Watch words that imply a catch.** "You can *still* use it" reads as "soon you can't". Drop still, for now and yet,
  unless that is the point.
- **Link the live demos and give their proof.** Workbench links to https://workbench.bitplatform.dev and says it is a
  demo: every project gets its own Workbench that runs on the developer's machine, because bit platform puts privacy
  first. The landing page links to https://landingpage.bitplatform.dev and says it scores 100 out of 100 for
  accessibility, SEO and performance. Libraries link to their own sites (`<name>.bitplatform.dev`).
- **Benefits without the details.** A library that "keeps your SQLite database in more places, runs faster, and is a
  simple move from the old package" says enough. How it does that does not belong here.
- **Order inside a subsection:** the newest and most visible first. AI components came before forms and validation.
- A breaking change gets one plain sentence on what to switch to, such as "Move from `Bit.Besql` to `Bit.Bsqlite`".

## Dependencies

Compare the package versions of the previous tag with develop plus the open PRs counted as merged, applied on top in a
throwaway merge. Never take them from release notes. For bit packages, list only what the NuGet publish workflow really
packs. For bit Boilerplate, cover the template's NuGet and npm packages. A whole framework's worth of major updates, such
as every Microsoft.AspNetCore package moving to a new .NET, is one line, not a list. Only rerun the comparison when a
new PR touches package files.

## Check before claiming

- Check a claim in the code before writing it, such as zero downtime in the template's CD workflow.
- Open every link in the highlights once and expect a 200.
- Numbers (sizes, prices, scores) come from a measurement or from the maintainer, never from a guess.

## Side work

Reading for the highlights can turn up real improvements, and the maintainer may ask for one on the spot. It gets its
own issue and PR like any other change. Its PR counts as merged: place it in the notes, and update the highlights and
Dependencies if it changes them.

## Hand back

Reply with the discussion's link and what this run changed: the PRs added or removed, the highlight and dependency
changes, and anything that needs the maintainer's decision. When the maintainer corrects the notes in a way that would
apply to the next release too, propose the lesson as a change to this file.
