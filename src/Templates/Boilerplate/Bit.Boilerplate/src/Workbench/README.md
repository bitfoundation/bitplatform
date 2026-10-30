# Workbench

The Workbench is where a page is designed before the app builds it. It runs pages on sample data, with the components,
the theme and the styles of this project, and shows them on phones, tablets and desktops side by side, in light and
dark, in every language the app speaks and in every state the data can be in. It also makes the screenshots the App
Store, Google Play and the Microsoft Store ask for.

It starts with pages made by the bit platform team, such as a point-of-sale dashboard and a buildings map, and fills up
with this project's own pages as they are designed.

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
  (live, loading, empty, error, mixed or slow), the signed-in state, the app's navigation, the theme and the language.
  Compare devices, themes, languages, data states or accounts side by side, download screenshots, and copy a link that
  opens the same views.
- **`/preview/<page>`**: the page alone, with its settings in the query string. This is what each view shows.
- **`/screenshots`**: the store image sets, with a device frame and a caption or plain, at the exact sizes each store
  asks for.

On a phone it shows only the page, with a floating button for the tools.

## Add a page

1. Make a folder for it under `Components/Pages/`, with the page, its parts and a `Data` folder holding its own models
   and an in-memory service. Load data through the scenario state, so the data-state switches reach the page.
2. Add the page to `Infrastructure/Services/WorkbenchPageCatalog.cs`.
3. Put its text in `Resources/WorkbenchStrings.resx`.

The page uses the project's components and theme the way any page of the app does, and calls nothing the app's server
provides. When the design is approved, the page moves into the app and gets real data.

An AI agent working on this project does all of this through its `workbench` skill, and offers to design a new page here
first when it plans one.

## How it stays in step with the app

- It references `Client.Core`, so the theme, the styles and the components are the app's own.
- The app's navigation, header, account menu and bottom bar are drawn by `Components/Chrome`, a copy that can show the
  signed-out, signed-in and multi-account states without a server. It and the app's originals start with `[mirror]`
  comments naming each other, as do the host page in `Components/App.razor` and the storage service.
- Packages the Workbench needs are referenced here only, so the app's own size does not change.

## Publishing it

The Workbench is not deployed with the app. To share it, publish it like any ASP.NET Core app to a server that has a
Chromium-based browser for the screenshots. The `Screenshots` section of `appsettings.json` caps how many captures run at
once and how many each client may ask for per minute.
