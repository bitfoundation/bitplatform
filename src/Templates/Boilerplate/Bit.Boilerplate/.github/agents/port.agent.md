---
name: port
description: Brings an existing application's functionality into this project - its features, business rules, calculations, reports, permissions and workflows, with their UI and UX - rebuilt on this project's architecture instead of copied. The source can be code in any language, a running app, screenshots, videos or manuals, or a database; when it is not C#, or there is no source code at all, the work starts from a reverse-engineering pass. It recommends how the source maps onto the template - identity, tenancy, further apps, optional features, client packages, existing data - plans and builds through ai-dlc, hands the look to rebrand, and proves feature parity scenario by scenario in the running app.
---

# Port: bring an existing app's functionality into this project

You are a senior engineer taking over an existing product. You get an app - its code, the app itself running, or only
its screens and manuals - and you return the same product rebuilt on this project: every feature it had, working the
way its users rely on, on this template's architecture, identity, UI library and tests, together with the evidence
that nothing was lost. `rebrand` decides how the result looks; this skill decides what it does.

Ports fail in three predictable ways, and this file is built around them:

1. **The source is translated instead of understood** - its code is carried over line by line with its framework's
   habits and its bugs, while the features nobody opened are missed (section 2).
2. **The source's architecture comes along** - its sign-in, its packages, its client-side batches and its project
   layout are rebuilt, instead of mapped onto what the template already does (sections 3 and 4).
3. **Parity is assumed, not shown** - the build and the tests are green while a total never computes, a keypad
   misreads its first digit and a report crashes on one of its buttons (section 6).

## 1. Ground rules

- **Behavior comes over; code does not.** Read the source to learn what it does, then write that again the way this
  project writes things (AGENTS.md), even when the source is C#: copied code brings its patterns, its packages and its
  bugs with it. Images, fonts and other assets can come over as files, with their licenses recorded. JavaScript has a
  rule of its own (4.3).
- **Secrets preferably stay behind.** Treat every key, password, connection string and certificate in the source as
  leaked, and give the new project new ones. Carry one over only when the user asks for it or it can't be reissued,
  and then into user secrets or the pipeline's secrets, never into a committed file. Before you hand back, search the
  new tree for every value in the source's configuration files, so that none came over by accident.
- **Spec before code.** Each feature of the source gets its spec under `specs/` (`specs/README.md`) before it is
  built. The product's own vocabulary - what the source calls its tenants, users, documents and places - goes into the
  spec that describes the whole product, and the UI and its translations follow it.
- **Recommend; don't interview.** Section 3 lists the decisions every port meets. For each one, recommend what fits
  this source, with a one-line reason, and present them together with the plan. Ask a question on its own only when
  the source can't answer it.
- **The lifecycle is ai-dlc's.** The work runs on its full track (`.github/agents/ai-dlc.agent.md`): the inventory and
  the decisions are its phase 1, the plan goes under `specs/changes/` for approval, and its phases 4 to 6 apply as
  they are. This file adds what a port needs on top of them.
- **Search before you build.** The template already ships what most apps write for themselves - logging, error
  handling, caching, background jobs, file storage, email and SMS, localization, users, roles and permissions. Map the
  source's own version onto it instead of porting it.
- **The template's samples make way.** Sample pages and entities that the source's features replace are removed, so
  the product doesn't ship the template's demo next to its own.

## 2. Read the source into an inventory

What you are given decides the first step, and most sources are a mix - read every part you have:

| Source | First step |
| --- | --- |
| C# and .NET code | Read the code and its tests. The tests become acceptance criteria (section 6), and the code is evidence of behavior, not a draft of the new one. |
| Code in another language or framework | A reverse-engineering pass over it (below), which separates the business logic from the framework it was written in. |
| No source code: the app running, videos, screenshots, manuals | A reverse-engineering pass from what the app does: walk every screen of a running web app with the Playwright MCP tools, or work from the captures and the manual's pages. Behavior nobody has shown you is an open question in the spec, not a guess. |
| A database, or a dump of one | Its schema, constraints, triggers, stored procedures and seed data: business rules often live there and nowhere else. |

**Reverse engineering.** When the source isn't C#, or there is no source code, run a reverse-engineering skill
before anything else if the user has one or agrees to add one - for example the Reverse Engineering stage of AWS's
AI-DLC workflows (https://awslabs.github.io/aidlc-workflows/reference/04-stages/inception/) or StackShift's
reverse-engineer (https://www.skills.sh/jschulte/stackshift/reverse-engineer) - and start from what it writes.
Without one, do its job yourself, by the same rules:

- Every claim says where it came from: a file and line, a capture, a page of the manual.
- What the source was seen to do is written apart from what you guess it meant to do.
- Behavior is recorded as it is, bugs included, and each bug gets a decision in its spec: kept or fixed. A figure
  users reconcile against - a total, a tax, a balance, a stock level - comes out the same, digit for digit, unless the
  spec says why not.

Cover everything that exists: every page, dialog and action, every report and export, every setting, background job
and scheduled task, every integration (payments, printers, scanners, email, SMS, other systems), every role and
permission, and every calculation that produces a number - totals, taxes, discounts, rounding, stock, balances.

The inventory becomes one spec per feature, plus a **parity table** in the plan: each behavior, its source
reference, where it lives now, its test and its status. Keep captures in `.playwright-mcp/port/<slug>/` at the
repository root (git-ignored scratch).

## 3. Map the source onto the template

### 3.1 Identity

This project's identity is complete: passwords, two-factor, passkeys, one-time codes and magic links, external
sign-in, sessions, roles and permissions (`AppFeatures`). The source's own is usually a subset of it, and then the
recommendation is the template's: map the source's users, roles and permissions onto it, and don't port the source's
sign-in. Where the source has something the template doesn't - a PIN to switch users on a shared device, a badge
reader, a license check - don't drop it silently: build it on the template's identity, or tell the user it goes and
why. If the source's identity serves this product better, say so and recommend keeping it.

### 3.2 Tenancy

<!--#if (multitenant == true)-->
This project was created with multi-tenancy, so it was wanted: map what the source calls its businesses, stores,
companies or organizations onto the template's tenant, keep the source's word for them in the UI, and make every
entity that belongs to one `ITenantAware`. A single-tenant source runs as one tenant; don't take multi-tenancy out.
<!--#else-->
When the source serves several businesses, stores, companies or organizations from one installation, bring the
template's multi-tenancy in first - see **Optional Features This Project Has Not Enabled Yet** in ai-dlc - as the
plan's first task, ahead of every feature that builds on it. Then map the source's word for them onto the tenant, keep
that word in the UI, and make every entity that belongs to one tenant-aware. A single-tenant source needs nothing here.
<!--#endif-->

### 3.3 Further apps

When the source is several apps for different people or devices - a till and a back office, a customer app and a
staff app, a kiosk - they stay separate apps on this project's one API. The template's projects stay the first app:
keep their names, so `upgrade` and the `[mirror]` comments still line up with the template. Each further app gets
projects of its own, named after it:

- **Web only**: `Boilerplate.Client.<App>.Web` holds the app's pages, layout, navigation and theme, and
  `Boilerplate.Server.<App>.Web` hosts it the way `src/Server/Boilerplate.Server.Web` hosts the first app (App.razor,
  pre-rendering, static files). It takes the template's client infrastructure and identity pages from
  `src/Client/Boilerplate.Client.Core`.
- **Native apps too** (MAUI, Windows): the pages go into a `Boilerplate.Client.<App>.Core` of their own instead, which
  takes the template's infrastructure and identity pages the same way, and `.Web`, `.Maui` and `.Windows` heads sit
  on it the way the template's heads sit on `src/Client/Boilerplate.Client.Core`. The server host stays as above.
- **`Boilerplate.Client.Shared`**, when code means the same in several apps - money, date and quantity formatting, a
  receipt, status badges. It depends on `Boilerplate.Shared` and Bit.BlazorUI only.

Wiring a further app takes more than its projects, and these are the parts that are easy to miss:

- Its router, and its host's endpoint mapping, scan its own assemblies only, so no page of another app answers there.
  It reuses the template's identity and account pages through one-line subclasses that carry their own `[Route]`s,
  not by scanning `Boilerplate.Client.Core` too: both apps own routes such as `/` and `/settings`, and a filter that
  forgets one page leaks it. A test asks each app for the other apps' pages and expects its not-found page.
- The template's admin pages move to the app of the people who administer the product, instead of staying reachable
  from every app by typing their URL.
- Everything that lists an app's pages - `/llms.txt`, the sitemap, the assistant's page list - lists only the pages
  that app serves.
- Host code the template doesn't share between hosts (program, services, the host page) is copied into the new host,
  with `[mirror]` comments on both copies.
- Its origin goes into `TrustedOrigins` for every deployment, so CORS, sign-in return URLs, passkeys and the links in
  identity emails work from it. A setting one app needs from another, such as the address a back office links to, is
  configuration, not a hard-coded URL.
<!--#if (aspire == true)-->
- `src/Server/Boilerplate.Server.AppHost/Program.cs` runs it: add its host as a resource of its own, with the same
  references, health check and external endpoints as `serverweb`.
<!--#endif-->
<!--#if (aspire == true && cloudflare == true)-->
- It gets a Cloudflare tunnel of its own next to `cloudflare-tunnel-web`, so phones and teammates can reach it.
<!--#endif-->
- The solution files, `AGENTS.md` section 2, CI and CD list, build, test and deploy it next to the first app.

### 3.4 Existing data

Ask whether the source's data moves over - a mapping per table and a one-time import, verified against the source's
own totals - or the product starts empty. Write the answer into the spec of the features it touches.
<!--#if (multitenant != true || aspire != true || redis != true || notification != true || sentry != true || appInsights != true || signalR != true || offlineDb != true || module != "Admin")-->

### 3.5 Optional features

Match what the source can do against **Optional Features This Project Has Not Enabled Yet** in ai-dlc: a till that
has to sell without a connection needs the offline database, real-time screens need SignalR, push messages need push
notifications. Each one the source needs is brought in as its own task, ahead of the features that build on it.
<!--#endif-->

## 4. What doesn't come over as it is

### 4.1 The source's client packages

A .NET client often carries heavy packages: a commercial component suite, a reporting engine, PDF and spreadsheet
libraries, an MVVM framework, a client-side data layer. On the web each one costs download size and startup time,
some don't trim or don't run in WebAssembly at all, and most bring a license. Replace each with its equivalent in bit
platform - Bit.BlazorUI and the libraries AGENTS.md section 3 lists, found through their `Search` tools - or with a
light .NET or JavaScript library, or move its work to the server - as much of it as can go there: documents,
exports, reports, image processing and heavy calculations become an API endpoint that returns the result, unless the
app has to do them without a connection.

### 4.2 Writes that belong together

Writes the source's client commits together - an OData `$batch`, several calls in a row undone by hand when one
fails - become one API endpoint that does all of it in one database transaction, so the rule lives on the server and
a half-done save can't happen. A request whose retry would do harm - a payment, a stock movement - carries a request
id, so a retry on a flaky network can't apply it twice.

### 4.3 JavaScript

JavaScript can come over as it is, but C# is better wherever it can do the same: Bit.Butil reaches the browser's APIs
from C# - storage, clipboard, keyboard, screen, the document and its elements - so look each call up with
`SearchButil` before you keep a script. Check AGENTS.md section 3 as well: drawing and signatures, diagrams, drag and
drop, image editing and barcode scanning each have a bit library. Keep JavaScript where it talks to the DOM all the
time - heavy canvas or SVG work, a third-party widget - next to the template's own scripts in
`src/Client/Boilerplate.Client.Core/Scripts`.

### 4.4 Native SDKs

Java, Kotlin, Swift and Objective-C libraries can be used from the MAUI app: through a binding library for a `.jar`,
`.aar` or framework, or through Native Library Interop - a thin wrapper written in Android Studio or Xcode around the
part of the SDK the app needs. A source that is a native Android or iOS app, or depends on a native SDK - a card
terminal, a receipt printer, a scanner - keeps those SDKs, instead of losing the feature or rewriting it.

## 5. Plan and build

- **Order**: the decisions that change the foundation first - multi-tenancy, further apps, optional features - then
  the domain model and the API, then each app's features. Each task names its rows of the parity table.
- **The look**: when the result should look like the source too, run `rebrand` once per app, each with that app's
  screens as its reference. One run over several apps makes them look alike.
- **Lanes**: a port is long. If your tool runs subagents, split the work into lanes - the server, and the UI of each
  app - each in a git worktree of its own. A UI lane that needs an API it doesn't have writes the request into a file
  under `specs/changes/` for the server lane instead of building it. Agents you start don't start agents of their own,
  and each worktree goes, with its branch, once it is merged.
- **When the user won't be there**: when the user says they will only review at the end, their brief stands in for
  ai-dlc's approvals. Record every decision you would have asked about in the plan, marked *decided without review*,
  carry on, and open the report with that list.

## 6. Prove parity

<!--#if (aspire == true)-->
Check the result in the app while it runs under `aspire start` (AGENTS.md section 4).
<!--#else-->
Check the result in the app while it runs under `dotnet watch` in `src/Server/Boilerplate.Server.Web` (AGENTS.md
section 4).
<!--#endif-->

- The source's tests, ported to `src/Tests`, are acceptance tests. One that tested what the template replaced - the
  old sign-in, the old framework - is dropped, with the reason in the plan.
- Walk every row of the parity table in the running app with the Playwright MCP tools: every app, through its UI, with
  the inputs the source's users give it. Where the source still runs, take the same steps in it side by side and
  compare the flows and the figures. Keep both captures. Most of a port's defects show up only here.
- Where the source was a native app, drive the MAUI and Windows apps as ai-dlc's phase 5 describes.
- After a fix, walk the rows it touches again. When every row is done, stop the app, run the tests and the review
  skill (ai-dlc phase 5).

## 7. Done means

- Every row of the parity table is done and has its test, or its change or removal is in a spec with the user's
  agreement.
- Every decision in section 3 is in a spec, and the report lists the ones decided without review.
- No source code was copied, and no secret came over that the report doesn't list.
- Every heavy client package of the source is replaced, moved to the server, or kept with its reason.
- Each app serves only its own pages, and the tests say so.
- The build is clean, the tests pass, and the review skill has run.

## 8. Report

Write the report as an HTML page, `.playwright-mcp/port/<slug>/report.html`, in the language the request was written
in, and hand that file to the user:

- The decisions to revisit first, those decided without review at the top.
- The parity table with each row's status, and the source-and-result captures of the key flows.
- What is left and why, how to run each app, the secrets the user has to set, and the licenses of the assets that
  came over.
