---
name: workbench
description: Designs and reviews a page's UI and UX in the Workbench (`src/Workbench`) before it is built into the app. The page runs on the app's own bit BlazorUI components and theme with in-memory sample data, is checked on phones, tablets and desktops in light, dark and right-to-left with every data state, and is captured as pixel-exact screenshots, including App Store, Google Play and Microsoft Store images with a colored background and a caption. The user reviews it with comments pinned on the page that reach the agent as they are written.
---

# Workbench: design the page before the app builds it

The Workbench is a web app of its own in `src/Workbench`. It runs pages on sample data, shows them on device frames
side by side, and switches what a page would otherwise wait for: the data's state, the theme, the language, the
signed-in user. It references Client.Core for the theme, the styles and the components, and nothing that needs the
API, the database or a session. A page designed here is built from the components and the theme the app ships, so
what the user approves is how the real page will look.

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
  the scenario in the query string. Pages are prerendered, as in the app.
- It serves an MCP server at `/mcp`, listed as `workbench` in the project's agent settings, and the same calls as HTTP
  endpoints under `/api/review`. An MCP client connects when its session starts: if the Workbench wasn't running then,
  reconnect the server if your client can, or use the HTTP endpoints.

## 2. Build the page

- One folder per page under `src/Workbench/Components/Pages/`: the page, its parts, and a `Data` folder with its own
  models and an in-memory service. The data looks real, varies and comes out the same on every run. The page never calls
  the app's API, its database or its pages, and adds nothing to Client.Core; it uses what the app offers that works
  offline, and models everything else itself.
- Every load goes through the scenario state the Workbench provides, so the Loading, Empty, Error, Mixed and Slow
  switches reach the page. The service awaits a short delay the way a network call would, and the page shows each state
  the way the real page will: a placeholder while loading, a message and a way forward when empty, a retry on error.
  Loads start once the page is interactive; while it prerenders, the page shows its loading state.
- What the user can change on the page lives in a bit Blex store of the page: the search text, the selection, the open
  dialog and its fields, the chosen view, the user's own edits to the sample data. The Workbench saves these stores per
  view and hands them to every screenshot, so an image shows exactly what was on the screen when it was taken, and a
  reload or a restart comes back to the same place. A value that only lives in a component's field is lost on reload and
  missing from the screenshot.
- That saved state outlives your changes. Blex drops what no longer reads, like a value of another type, and a field
  with nothing saved starts from its default. It can't tell when saved state still reads but no longer fits: a field that
  now means something else, a record in the store that gained a field, or sample data the store keeps an edited copy of,
  whose old copy would hide your new data. When a change does that, raise the store's `Version` in its `[BlexStore]`
  (start at 1), and state saved under another version is dropped. Don't write a migration: dropping it only puts the
  page back to its defaults.
- Pages follow the app's sign-in rules the way they will in the app: `[Authorize]` with the app's policies on a page
  that needs them, and `AuthorizeView` around what only some users may see or do. The Workbench signs in as one of three
  users with the app's roles: a global admin (the default), a tenant admin (a second global admin when the project has
  no multitenancy) and a demo user. Pick one in the inspector to see what each gets; signed out, a page that needs
  sign-in shows the app's not-authorized page.
- Add it to `WorkbenchPageCatalog`: its key, title, description, icon, category, the components it shows off and the
  caption its store screenshots carry.
- For a new page, offer two or three directions as design options in its catalog entry (`Variants`) and branch the
  markup on the view's variant; the first is the default. The user compares them side by side. Once they pick one,
  remove the others from the page and from the entry.
- Cases only this page has, like how many items there are, a plan or a role, a feature on or off, or very long names,
  become page switches in its entry (`Switches`). Read the chosen value with the scenario's `GetSwitch` in the page or
  its sample service. They show in the inspector, go into links and screenshots, and Compare spreads one over the views.
- Follow `AGENTS.md` section 5 as for any page of the app: bit BlazorUI components as they are, styled through their
  parameters and the theme tokens, never through their internal classes, and no color the theme already has. That is
  what makes the page follow the app's theme now and change with it later.
- Write text as `Localizer["..."]` literals while the design moves, and put it into `WorkbenchStrings.resx` once the
  user approves it.
- A package the page needs goes into the Workbench's project only, never into Client.Core: the app's size must not grow
  for a page that is only being designed.

## 3. A page the app already has

- When the user wants screenshots or design changes of a page that already exists in the app, bring a copy of it into
  `src/Workbench/Components/Pages/AppPages`, the way the Settings page is there: the same markup, styles, strings and
  validation, with the server calls replaced by sample data and a store, and the same template conditions as the
  original. Both copies start with a `[mirror]` comment that names the other.

## 4. Look at it the way the user will

- In `/workbench/<key>`, switch the device and its orientation, the theme, the language (a right-to-left one too), the
  data state, the sign-in state and the app's navigation, and compare views side by side.
- Look at images, not only at the markup: `/api/screenshots/<key>` takes the same query keys and returns a PNG at the
  device's real pixel size. Before a page is done, look at every data state in light and dark, at phone, tablet and
  desktop widths, and in a right-to-left culture. A frame that stays empty, a state without a message and text cut at
  one width are findings.
- Check the widths between the presets too: drag a view's edge, or snap it to one of the app's breakpoints in the bar
  above it.
- `/api/checks/<key>` takes the same query keys and returns an accessibility check of the view (axe-core) and the colors
  the page sets that match none of the theme's. Fix what the page's own markup causes. A finding inside a bit component
  or the app's chrome is reported to the user, not worked around in the page.
- A theme color mixed with white, black or the background counts as the theme's. A color that stands for data, like a
  category's, goes in a `--data-color` custom property (`style="--data-color:..."`, painted with `var(--data-color)`),
  which the check leaves alone.
- The **Theme and components** page (`theme`) shows the theme's tokens and every bit component the app uses, each in
  its states, with hover, focus and pressed drawn without touching the control, so they show on a phone too and no
  keyboard opens. Its Open overlay switch opens a dropdown, a date picker, a menu, a dialog, a side panel or snack bars
  for links and screenshots. When a page starts using a bit component the Theme page doesn't show, add it there in its
  states.
- The user can try another primary color, larger text, reduced motion and color blindness on
  a view. When they like a theme change, put the value into the app's theme: the Workbench changes only the view.
- Read the browser console and the Workbench's console output while you look, as the run-app skill describes.
- The page's address holds every view and its settings, so a link shows the user exactly what you saw on their own
  machine.

## 5. Store screenshots

- Pick a store size from the device list (App Store, Google Play or Microsoft Store), and turn on **Background** next to
  **App chrome**: the page sits in a device on a colored background, with a caption above it. The caption comes from the
  page's catalog entry and is translated with the Workbench's strings; a caption typed into the inspector is used as
  written, in every language. **Device frame** decides whether the device is drawn around the screen.
- The inspector shows each store's rules next to its sizes: the App Store welcomes frames and captions, Google Play
  discourages device frames and promotional words, and the Microsoft Store takes plain screenshots, with captions typed
  into Partner Center instead.
- **Screenshot** downloads the selected view, or every view as one archive.

## 6. Share a link

<!--#if (aspire == true && cloudflare == true)-->
- A link to the user's `localhost` works only on their machine. When they want to send the page to someone else (a
  teammate, a client), start the app host's `workbench` resource, then its Cloudflare quick tunnel,
  `cloudflare-tunnel-workbench`. The tunnel gets an `https://….trycloudflare.com` address: read it from the
  Aspire dashboard or the Aspire MCP `list_resources` tool. A Workbench already running outside the app host holds the
  same port, so stop that one first.
- Tell the user plainly: the link works only while their laptop is on and the app host keeps running; the address
  changes whenever the tunnel restarts; anyone who has the link can open it. Screenshots taken through the link are
  rendered on the user's machine.
- Open the link yourself before you hand it over, and stop the `workbench` resource and its tunnel when the user is
  done.
<!--#else-->
- A link to the user's `localhost` works only on their machine. When they want to send the page to someone else (a
  teammate, a client), publish the Workbench in Release to a folder outside the repository and run it from there on a
  free port, so it keeps running while they go on working in the repository.
- Put it online with a Cloudflare quick tunnel (`cloudflared` pointed at that local address; install `cloudflared` first
  if it is missing). It needs no account and prints an `https://….trycloudflare.com` address.
- Tell the user plainly: the link works only while their laptop is on and both the Workbench and the tunnel keep
  running; every new tunnel gets a new address; anyone who has the link can open it. Screenshots taken through the link
  are rendered on the user's machine.
- Open the link yourself before you hand it over, and stop the tunnel and the published copy when the user is done.
<!--#endif-->

## 7. Settle it with the user

- Show the result per device and state, as links or images, and change it until the user approves it.
- The user pins comments on the page in Comment mode. The comments reach you as they are written: call `get_review`, then `wait_for_review` with the cursor it returns, again and again, until the
  user says the review is over. Tell the user you are listening.
- For each comment, mark it working (`set_comment_status`), show your plan as a checklist (`set_comment_tasks`) and tick
  it as you go, change the page, which hot reload brings to the reviewer's view, then reply in the reviewer's language
  (`reply_to_comment`) and mark it done. A comment names its element (component, text, CSS path) and its view;
  `get_comment_screenshot` shows the view as it was when the comment was left.
- Without the MCP server, the same calls are HTTP on the Workbench's own address: `GET /api/review`,
  `GET /api/review/wait?cursor=<cursor>&agent=<your name>`, which answers within a minute, `POST /api/review/<id>/reply`
  and `POST /api/review/<id>/status` with a JSON body (`text` or `status`, and `agent`), `PUT /api/review/<id>/tasks`
  (`tasks`, each with `text` and `done`), `GET /api/review/<id>/screenshot`, `POST /api/review/pages/<key>/status` and
  `POST /api/review/flows`. They answer only on this machine, and opened from anywhere else the review is read-only, so
  only someone at this computer can reach you.
- Keep each page's review status current (`set_page_status`): in review when you hand it to the user, approved when
  they approve it, built once it is in the app.
- Text the user changes in Edit text mode is saved into `WorkbenchStrings` or `AppStrings` in every language; read the
  diff. A text written in the page itself reaches you as a comment asking to move it into the resources.
- To explain a task that crosses screens, draw a flow (`save_flow`): each screen is a page in a state, reached by the
  taps you list (like the dialog that "Add building" opens), and each link is an arrow that starts at the element its
  tap names. The user opens it at `/flows/<id>` to understand the app or to present it in a meeting. Draw it again with
  the same id when the pages change.
- The review sheet (Screenshot, then Review sheet) puts every open view, its settings and the comments into one HTML or
  PDF file. Offer it when the user wants something to send around or to attach to the pull request.
- Write what was decided into the feature's spec, as `AGENTS.md` section 6 asks.

## 8. Bring it into the app

- **Important:** the approval covers how the page looks and behaves, its UI, styles and design, and its text with every
  translation, not its code. Workbench pages are written for the Workbench: they don't inherit `AppPageBase` or
  `AppComponentBase`, they keep their state in bit Blex stores the app may not have, and their data comes from sample
  services instead of the server. Never copy the files into the app as they are. Their text is the exception: move the
  page's entries from `WorkbenchStrings` into the app's resources as they are, translations included.
- Read the Workbench page again and build it into the app the way the rest of the app is built, following `AGENTS.md`:
  its base classes, services and conventions, its own resources, and real data from the server (DTOs, the controller,
  the client calls, the **scaffold-entity** skill for CRUD). Keep the markup and the styles the user approved.
- Keep the Workbench copy only when the user wants to go on designing it there. Then each copy starts with a `[mirror]`
  comment that names the other (`AGENTS.md` section 5). Otherwise remove it and its catalog entry.
- Then validate it in the running app the way phase 5 of the **ai-dlc** skill describes.

## Done when

- The page renders every data state without an error, in light and dark, at phone, tablet and desktop widths and in a
  right-to-left culture, and the user has approved it.
- Its own markup causes no serious or critical accessibility finding, and every review comment is done or answered.
- A screenshot shows what the view showed when it was taken, including what the user typed or chose on the page.
- Its text is in `WorkbenchStrings.resx`, or in the app's resources once it has been rebuilt there, and a page that stays
  in the Workbench changed nothing outside `src/Workbench`.
<!--#if (advancedTests == true)-->
- `dotnet test` in `src/WorkbenchTests` passes: it checks every page's catalog entry and runs the checks on each page.
<!--#endif-->
