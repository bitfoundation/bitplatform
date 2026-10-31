# Workbench

The Workbench is where a page is designed before the app builds it. It runs pages on sample data, with the components,
the theme and the styles of this project, and shows them on phones, tablets and desktops side by side, in light and
dark, in every language the app speaks and in every state the data can be in. It also makes the screenshots the App
Store, Google Play and the Microsoft Store ask for.

It starts with pages made by the bit platform team, such as a point-of-sale dashboard, a buildings map, an AI assistant
and a shift planner, next to copies of this app's own Settings page (and its Dashboard, with the Admin module), and fills
up with this project's own pages as they are designed.

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
tunnel keep running. To keep it online, publish it like any ASP.NET Core app to a server that has a Chromium-based
browser for the screenshots. The `Screenshots` section of `appsettings.json` caps how many captures run at once and how
many each client may ask for per minute.
