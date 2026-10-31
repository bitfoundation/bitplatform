# Workbench

The Workbench is where a page is designed before the app builds it. It runs pages on sample data, with the components,
the theme and the styles of this project, and shows them on phones, tablets and desktops side by side, in light and
dark, in every language the app speaks and in every state the data can be in. It also makes the screenshots the App
Store, Google Play and the Microsoft Store ask for.

It starts with pages made by the bit platform team, such as a point-of-sale dashboard, a buildings map, an AI assistant
and a shift planner, next to a copy of this app's own Settings page, and fills up with this project's own pages as they
are designed.

## Run it

- `dotnet watch` in this folder, then open the address in `Properties/launchSettings.json`.
- With Aspire, start the `workbench` resource from the dashboard. It starts only when asked.
- Edits to `.cs`, `.razor`, `.scss` and `.ts` files show up while it runs. A `.resx` edit needs a restart.

Screenshots are taken by a headless Chromium-based browser on the machine that runs the Workbench: Playwright's own
Chromium when it is installed, otherwise Microsoft Edge or Google Chrome. To install Playwright's Chromium, run
`pwsh bin/Debug/net11.0/playwright.ps1 install chromium` here after a build.

## What it offers

- **Home page**: the pages, with thumbnails, and why the Workbench exists.
- **`/workbench/<page>`**: one page with its tools. Pick the device, its orientation and frame, the data state
  (live, loading, empty, error, mixed or slow), the signed-in state, the app's navigation, a colored background with a
  caption for store screenshots, the theme and the language. Compare devices, themes, languages, data states or accounts
  side by side, download screenshots, and copy a link that opens the same views.
- **Any size**: drag the edge or corner of a view, or type its size. The bar above each view shows the app's
  breakpoints, the same ones its styles and components switch at, and snaps the view to one of them.
- **This page**: a page can offer design options to compare (a table or cards, say) and switches for its own cases, like
  how many items there are or how long the names are.
- **Try the theme**: another primary color, rounder or square corners and tighter or roomier spacing, on this view only.
  Text size, reduced motion and color blindness are a switch away too.
- **Modes**: Comment pins a note on any spot of the page, Inspect names the component under the pointer with its size,
  spacing, font and the theme colors it uses, and Edit text changes a text in every language, straight into the
  translation files.
- **Review**: the comments of every view as a list with a status for the page (draft, in review, approved, built). Your
  agent answers on each comment, shows its plan as a checklist and ticks it as it goes.
- **Checks**: an accessibility check of the selected view (axe-core) and a search for colors that don't come from the
  theme.
- **Flows**: a named walk through pages, step by step, to play here or download as numbered screenshots.
- **Review sheet**: every view, its settings and the comments in one HTML or PDF file to send around.
- **`/preview/<page>`**: the page alone, with its settings in the query string. This is what each view shows.

The Workbench remembers the views you had open, and each view remembers what was done on its page (a search, an open
panel, an accent color, a moved shift), so a reload or a restart comes back to the same place. A screenshot carries that
too: it shows the view as it was when the button was pressed.

On a phone it shows only the page, with a floating button for the tools.

## Add a page

1. Make a folder for it under `Components/Pages/`, with the page, its parts and a `Data` folder holding its own models
   and an in-memory service. Load data through the scenario state, so the data-state switches reach the page.
2. Keep what the user can change on the page in a bit Blex store of its own, so it survives reloads and reaches the
   screenshots.
3. Add the page to `Infrastructure/Services/WorkbenchPageCatalog.cs`.
4. Put its text in `Resources/WorkbenchStrings.resx`.

The page uses the project's components and theme the way any page of the app does, and calls nothing the app's server
provides. When the design is approved, the page is rebuilt in the app the way the rest of the app is built and gets
real data; its Workbench code is a design, not something to copy over as it is.

An AI agent working on this project does all of this through its `workbench` skill, and offers to design a new page here
first when it plans one. It can also bring one of the app's existing pages in, for screenshots or a redesign.

## Review with your agent

The comments live in a bit Bcrdt document the Workbench keeps in `App_Data`: every browser that has the page open holds
a copy and sees changes as they happen, whether they come from you, from someone you shared a link with, or from your
agent. The agent reaches the same document through the Workbench's MCP server (`/mcp`, listed as `workbench` in the
project's agent settings) or its HTTP endpoints under `/api/review`. Once you ask it to go through the review, it keeps
listening: a new comment reaches it as you send it, it changes the page, hot reload shows the change in your view, and
it replies and ticks the comment. The MCP server and the review endpoints answer only on this machine.

## How it stays in step with the app

- It references `Client.Core`, so the theme, the styles and the components are the app's own.
- The app's navigation, header, account menu and bottom bar are drawn by `Components/Chrome`, a copy that can show the
  signed-out, signed-in and multi-account states without a server. It and the app's originals start with `[mirror]`
  comments naming each other, as do the host page in `Components/App.razor`, the storage service and the copies of the
  app's pages in `Components/Pages/AppPages`.
- Packages the Workbench needs are referenced here only, so the app's own size does not change.

## Sharing it

The Workbench is not deployed with the app. To show a page to someone else for a while, publish it to a folder, run it
from there and put it online with a Cloudflare quick tunnel: the link works while your machine, the Workbench and the
tunnel keep running. Whoever has the link can leave comments, and they reach your agent like yours do. To keep it online, publish it like any ASP.NET Core app to a server that has a Chromium-based
browser for the screenshots. The `Screenshots` section of `appsettings.json` caps how many captures run at once and how
many each client may ask for per minute. On a Workbench that stays online for anyone, turn comments off with
`Reviews:Enabled`.
