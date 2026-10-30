---
name: workbench
description: Designs and reviews a page's UI and UX in the Workbench (`src/Workbench`) before it is built into the app. The page runs on the app's own bit BlazorUI components and theme with in-memory sample data, is checked on phones, tablets and desktops in light, dark and right-to-left with every data state, and is captured as pixel-exact screenshots, including App Store, Google Play and Microsoft Store images.
---

# Workbench: design the page before the app builds it

The Workbench is a web app of its own in `src/Workbench`. It runs pages on sample data, shows them on device frames
side by side, and switches what a page would otherwise wait for: the data's state, the theme, the language, the
signed-in user. It references Client.Core for the theme, the styles and the components, and nothing that needs the
API, the database or a session. A page designed here is built from the components and the theme the app ships, so
the design the user approves is already the markup and the styles of the real page.

## 1. Run it

<!--#if (aspire == true)-->
- If it is already running, use it. With the app host running, start its `workbench` resource, which starts only when
  asked. Otherwise run `dotnet watch` in `src/Workbench` in the background and poll its URL with a capped retry loop.
<!--#else-->
- If it is already running, use it. Otherwise run `dotnet watch` in `src/Workbench` in the background and poll its URL
  with a capped retry loop.
<!--#endif-->
- Hot reload carries `.cs`, `.razor`, `.scss` and `.ts` edits as the **run-app** skill (`.github/agents/run-app.agent.md`)
  describes. A `.resx` edit or startup code needs a restart.
- The home page lists the pages. `/workbench/<key>` opens one with its tools; `/preview/<key>` renders it alone, with
  the scenario in the query string.

## 2. Build the page

- One folder per page under `src/Workbench/Components/Pages/`: the page, its parts, and a `Data` folder with its own
  models and an in-memory service. The data looks real, varies and comes out the same on every run. The page never calls
  the app's API, its database or its pages, and adds nothing to Client.Core; it uses what the app offers that works
  offline, and models everything else itself.
- Every load goes through the scenario state the Workbench provides, so the Loading, Empty, Error, Mixed and Slow
  switches reach the page. The service awaits a short delay the way a network call would, and the page shows each state
  the way the real page will: a placeholder while loading, a message and a way forward when empty, a retry on error.
- Add it to `WorkbenchPageCatalog`: its key, title, description, icon, category, the components it shows off and the
  caption its store screenshots carry.
- Follow `AGENTS.md` section 5 as for any page of the app: bit BlazorUI components as they are, styled through their
  parameters and the theme tokens, never through their internal classes, and no color the theme already has. That is
  what makes the page follow the app's theme now and change with it later.
- Write text as `Localizer["..."]` literals while the design moves, and put it into `WorkbenchStrings.resx` once the
  user approves it.
- A package the page needs goes into the Workbench's project only, never into Client.Core: the app's size must not grow
  for a page that is only being designed.

## 3. Look at it the way the user will

- In `/workbench/<key>`, switch the device and its orientation, the theme, the language (a right-to-left one too), the
  data state, the sign-in state and the app's navigation, and compare views side by side.
- Look at images, not only at the markup: `/api/screenshots/<key>` takes the same query keys and returns a PNG at the
  device's real pixel size. Before a page is done, look at every data state in light and dark, at phone, tablet and
  desktop widths, and in a right-to-left culture. A frame that stays empty, a state without a message and text cut at
  one width are findings.
- Read the browser console and the Workbench's console output while you look, as the run-app skill describes.
- The page's address holds every view and its settings, so a link shows the user exactly what you saw.

## 4. Settle it with the user

- Show the result per device and state, as links or images, and change it until the user approves it.
- Write what was decided into the feature's spec, as `AGENTS.md` section 6 asks.

## 5. Bring it into the app

- Once it is approved, the page moves to where its feature lives in the app, Client.Core for most. Its markup and its
  styles move as they are; the in-memory service gives way to the real data (DTOs, the controller, the client calls,
  the **scaffold-entity** skill for CRUD), and its sample models to the app's own.
- Keep the Workbench copy only when the user wants to go on designing it there. Then each copy starts with a `[mirror]`
  comment that names the other (`AGENTS.md` section 5). Otherwise remove it and its catalog entry.
- Then validate it in the running app the way phase 5 of the **ai-dlc** skill describes.

## 6. Store screenshots

- `/screenshots` makes the image sets the stores ask for: pick the store, a device frame with a caption or a plain
  screenshot, the background, the theme, the language and the pages. Each size downloads as a PNG, or all of them as one
  archive.
- Each store's rules are shown next to its sizes: the App Store welcomes frames and captions, Google Play discourages
  device frames and promotional words, and the Microsoft Store takes plain screenshots, with captions typed into Partner
  Center instead.
- A caption comes from the page's catalog entry and is translated with the Workbench's strings. A caption typed on the
  page is used as written, in every language.

## Done when

- The page renders every data state without an error, in light and dark, at phone, tablet and desktop widths and in a
  right-to-left culture, and the user has approved it.
- Its text is in `WorkbenchStrings.resx`, or in the app's resources once it has moved, and a page that stays in the
  Workbench changed nothing outside `src/Workbench`.
