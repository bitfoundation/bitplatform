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

Running under `dotnet watch` is the Workbench's dev mode, which is how its agent skill starts it. Only dev mode can change
things: Inspect and Edit text write into the code, the review takes comments and flows, and the agent can connect. Any
other copy, like a published demo or the plain `dotnet run` bit CLI starts, has no hot reload to show a change and no agent
to answer a comment, so those stay read-only there. The rest works: the pages and their states, what a visitor changes
on a page, which stays in their browser, screenshots and checks.

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
- **Try the theme**: another primary color on this view only.
  Text size, reduced motion and color blindness are a switch away too.
- **Modes**: Comment pins a note on any spot of the page, Inspect names the component under the pointer with its size,
  spacing, font and the theme colors it uses, and Edit text changes a text in every language, straight into the
  translation files. A click in Inspect mode opens the component's parameters, the ones it inherits too, its styles
  and classes and the CSS variables it uses: a change is written into its tag in the code, and hot reload shows it.
- **Review**: the comments of every view as a list with a status for the page (draft, in review, approved, built). Your
  agent answers on each comment, shows its plan as a checklist and ticks it as it goes.
- **Checks**: an accessibility check of the selected view (axe-core) and a search for colors that don't come from the
  theme.
- **Flows**: the screens of a task side by side on a board at `/flows`, each a page in the state that matters, like a
  list and the dialog its button opens, with arrows for how you get from one to the next. Present one screen by screen
  in a meeting. Your agent draws them, or you add the selected view to a flow from the Flows tab.
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

The comments, page statuses and flows live in a bit Bcrdt document the Workbench keeps in `App_Data/Reviews` in its
project folder. Git ignores that folder, so the review outlives rebuilds and stays on your machine; the review sheet is
the way to send it around. Every browser that has the page open holds a copy and sees changes as they happen, whether
they come from you or from your agent. The agent reaches the same document through the Workbench's MCP server (`/mcp`,
listed as `workbench` in the project's agent settings) or its HTTP endpoints under `/api/review`. Once you ask it to go
through the review, it keeps listening: a new comment reaches it as you send it, it changes the page, hot reload shows
the change in your view, and it replies and ticks the comment. Each reply plays a short sound, and with "Notify me"
turned on in the Review tab it also shows a notification, on a phone too.

Only this machine can talk to the agent, and only in dev mode. The MCP server and the review endpoints answer only those
requests, and anywhere else, through a shared link or a public address, the review is read-only: comments, replies and
statuses can be read, the agent shows as offline, and nothing can be added, so nobody reaches your agent without being at
your computer.

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
tunnel keep running. Whoever has the link sees the review but can't add to it. To keep it online, publish it like any
ASP.NET Core app to a server that has a Chromium-based browser for the screenshots. The `Screenshots` section of
`appsettings.json` caps how many captures run at once and how many each client may ask for per minute.
<!--#if (advancedTests == true)-->

## Tests

`src/WorkbenchTests` tests the Workbench itself: its stores and sample data, the review's HTTP and MCP endpoints, its
components with bUnit and its pages in a browser with Playwright. Run `dotnet test` in that folder.
<!--#endif-->
