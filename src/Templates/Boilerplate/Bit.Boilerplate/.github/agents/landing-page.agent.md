---
name: landing-page
description: Changes the product's landing page, the public marketing site in `src/LandingPage` that runs apart from the app - makes one of its sample landing pages the product's own home page, switches it from a waitlist before launch to the store links after it, and edits its pages, texts in English and Persian, look, chat answers, store links, legal pages, emails, waitlist storage and analytics - then checks every change prerendered, in both languages and color schemes, on phones and desktops. Use for any change under `src/LandingPage`, for launching or announcing the app, or for questions about its landing page samples.
---

# Landing page: the product's public site

The landing page is a site of its own, next to the app: `src/LandingPage/Boilerplate.LandingPage.Server` hosts and
prerenders it, and `src/LandingPage/Boilerplate.LandingPage.Client` is its Blazor WebAssembly part. It references none
of the app's projects, has its own settings and deploys as its own service. It ships several sample landing pages, one
per kind of business, each under its own path (`/ecommerce`, `/ecommerce/about` and so on), with a gallery of them at
`/`. A project keeps the one closest to its product, makes it its home page and deletes the rest (section 4).

Paths below are relative to `src/LandingPage/Boilerplate.LandingPage.Client/` unless they start with `src/`.

## 1. Where things are

- **A sample** is one folder, `Components/Pages/<Sample>/`: its `<Sample>Site.cs` (name, path, accent color, the
  platforms with their store links and launch state, the web app's address, the company the legal pages name), its
  layout, pages, not-found page, logo, and its texts in `<Sample>Strings.resx` with `<Sample>Strings.fa.resx`, whose
  `Card*` texts fill its gallery tile. Its look is `Styles/sites/_<sample>.scss`, imported from `Styles/landing.scss`,
  and its tile's colors are copied into `$gallery-brands` in `Styles/_gallery.scss`. Its link preview is `wwwroot/images/og/<sample>.png` (1200 by 630), which `LandingHead` uses unless a page passes its
  own, and it has a web app manifest of its own at `/<sample>/manifest.webmanifest`.
- **The samples' list** is `Infrastructure/LandingSites.cs`. The gallery, the smart download links and the not-found
  handling read it, and the sitemap finds the pages from their routes.
- **Shared parts** are in `Components/Common/`: the head tags (`LandingHead`: title, description, canonical and
  language links, social previews, structured data), navigation, footer, store badges, the download QR code, the
  App Store rating, the waitlist form, FAQ, countdown, announcement bar, share buttons, the consent banner and the
  legal pages. Their texts are in `Resources/LandingStrings.resx` with its `.fa.resx`.
- **The chat** is `Components/Chat/`, in three looks (bubble, inline, concierge) that the samples pick from. It loads
  only when a visitor opens it. What it may say about each sample is in
  `src/LandingPage/Boilerplate.LandingPage.Server/Features/Chat/LandingChatPrompts.cs`.
- **The tour** of the samples is `Components/Tour/`, started from the gallery's button and rendered next to the router
  in `Components/Routes.razor`, so it survives the move from one sample's layout to another's. `LandingTourSteps.cs`
  lists its steps, each with the page it opens and the CSS class it points at, and `TourStrings.resx` with its
  `.fa.resx` holds their texts. A step that acts on the page (opening the chat, pressing
  a demo's buttons) does it through the small helpers in `wwwroot/scripts/landing.js`. Bit.Bonboarding runs it and, like the chat, is downloaded only when a visitor starts it;
  nothing outside `LandingTourHost` and `LandingTourSteps` may use Bonboarding's types.
- **The server** (`src/LandingPage/Boilerplate.LandingPage.Server`): `Features/Waitlist` (join, confirm,
  unsubscribe, and where the list is kept), `Features/Email` (the Razor email templates), `Features/Seo` (sitemap,
  robots.txt, llms.txt, found from the pages' routes), `Features/Download` (the link that sends each phone to its own
  store, and its QR code), `Features/StoreListing` (the App Store rating) and `appsettings.json`.
- **Languages**: English at `/...` and Persian at `/fa-IR/...`, set up in `Infrastructure/LandingCultures.cs`. The
  project's `InvariantGlobalization` switch (`src/Directory.Build.props`) leaves it English only, like the app.

## 2. Ground rules

- **In the Workbench, without the app shell.** When the user wants one of its pages in the Workbench, it comes over as
  the **workbench** skill's section 3 says (`.github/agents/workbench.agent.md`), with no app shell around it.
- **Its own conventions** where `AGENTS.md` section 5 names the app's types: components inherit
  `LandingComponentBase` and pages `LandingPageBase`, and text goes into resx keys, read through `nameof`: a
  sample's own text into its `<Sample>Strings`, shared text into `LandingStrings`, the English and the Persian in the
  same change. Bit.BlazorUI components first, code-behind files and scoped SCSS still apply. Keep the `[mirror]`
  comments in step.
- **Both languages, both directions, both color schemes, phone to desktop**, for every change.
- **What a crawler or a link preview should see is in the prerendered page**: no text that appears only after the
  app loads, every page with its own title, description and preview image through `LandingHead`, and one `h1`.
- **Keep the download small.** The chat, the tour and their assets load on first use. Before adding a package, find out what it
  adds to the visitor's download and prefer what the project already has. Images are sized and in a modern format,
  fonts are self-hosted subsets with `font-display: swap`, and work that isn't needed for the first view starts after
  the page is interactive.
- **No switches in settings and no content editor**: a change to the page is a change to the code, published again.
- **Every form or button that does something calls the server** and shows its waiting, success and failure states.
- **Each sample has a look of its own.** The sample that is the product's home page wears the product's brand: the
  **rebrand** skill (`.github/agents/rebrand.agent.md`) covers it.
- **The samples' companies, people, ratings and quotes are fictional.** Before launch, replace every one of them on
  the sample that stays with the product's real ones from the user, and never invent a review, a rating, a customer,
  an award or a legal statement.

## 3. Run it

- Run `dotnet watch` in `src/LandingPage/Boilerplate.LandingPage.Server`, or start the `landingpage` resource from
  the Aspire dashboard, where it waits for a start. `bit setup` starts it right after the Workbench.
- Without an AI key the chat streams a sample answer, and without an SMTP server the emails are written to the log;
  under Aspire they land in Mailpit.
- Check pages the way the **run-app** skill (`.github/agents/run-app.agent.md`) uses a browser: each changed page in
  English and Persian, light and dark, at phone and desktop widths, and its prerendered HTML (the page's source
  before scripts run) for its text, title, description, canonical link and structured data.

## 4. Make the product's home page from the samples

1. Agree with the user on the sample closest to the product, or pick one by the product's kind of business and say
   why. When the user wants parts of several samples, keep the one whose layout comes closest and bring the others'
   pages, sections or features into it, each with its texts in both languages, its styles and its chat facts.
2. In that sample's `<Sample>Site.cs`, set `Path` to an empty string, and give its home page the two routes the
   Gallery page has (`/` and `/{culture:nonfile}`) instead of the ones built from `Path`. Its other pages then answer
   at `/about`, `/fa-IR/about` and so on.
3. Delete `Components/Pages/Gallery/`, every other sample's folder, styles, images and chat facts, and their entries
   in `LandingSites.cs` and the project file. A deleted sample's copy in the Workbench goes with it, with its catalog
   entry, its import in `src/Workbench/Styles/workbench.scss` and its strings. `Components/Layout/SiteLayout.razor` and
   the gallery's texts in `LandingStrings` go with the gallery when nothing else uses them, and so does its tour: `Components/Tour/`,
   `<LandingTour />` in `Components/Routes.razor`, `LandingTourLauncher` and its registration, and the
   `Bit.Bonboarding` and `TourStrings` lines in the client's project file.
4. Replace the sample's name, logo, texts, images (its link preview included), company details, store links and chat
   facts with the product's, in both languages, and give it the product's brand, or the look of a site the user points
   to, through the palette, fonts and shapes in its `Styles/sites/_<sample>.scss`.
5. Build, then check the home page, a few inner pages, a missing page, `/sitemap.xml`, `/robots.txt` and the download
   link, in both languages.

## 5. Launch and the waitlist

- **Before launch**, platforms are `ComingSoon` or `PreOrder` in `<Sample>Site.cs`, and the page collects a waitlist:
  the visitor confirms by email, and every email has an unsubscribe link.
- **The waitlist is kept in memory** until the project gives it a home: implement `IWaitlistStore` in
  `src/LandingPage/Boilerplate.LandingPage.Server/Features/Waitlist/WaitlistStore.cs` over the app's API, a database
  or the user's mailing list service, and register it. Point this out before launch, since every restart empties the
  list.
- **To launch**, set each platform that is out to `Live` with its store link, and the App Store app id for the rating.
  The page then shows the store badges, the QR code and the smart download link instead of the waitlist. Send the
  launch announcement (`WaitlistService.SendLaunchAnnouncementAsync`) to the confirmed entries from wherever the list
  is kept, once.
- Emails go through the `smtp` connection string, the same format as the app's server, and a deployment gives it the
  app's own, `SMTP_CONNECTION_STRING`.

## 6. Settings and services

- `Site:AllowIndexing` is on only in `appsettings.Production.json`, so Development and Test stay out of search
  results. The search engines' verification codes go into `Site:GoogleSiteVerification` and
  `Site:BingSiteVerification`.
- `AI:OpenAI` holds the chat's model and key, the same setting as the app's server. The chat answers only from the
  facts in `LandingChatPrompts.cs`, so keep those true and in step with the page.
- Analytics and monitoring go through bit Belemetry to Azure Monitor, with the connection string in the client's
  `appsettings.json`, and only after the visitor agrees in the consent banner. Errors are the exception: they are sent
  from the first moment, whatever the visitor chooses, and the privacy page says so.
- The deployment reads each of the server's settings from a secret of its own, such as
  `LANDINGPAGE__AI__OPENAI__CHATAPIKEY`, or a variable for what isn't secret; the **deploy** skill
  (`.github/agents/deploy.agent.md`) covers its domain and its secrets. It can't reach the browser: the client's
  `appsettings.json` is built into its assembly, so the Azure Monitor connection string the browser sends with goes
  there.

## Done means

- The change shows in both languages, both directions and both color schemes, at phone and desktop widths, with no
  console errors, and its text is in the prerendered page.
- Every new text has its English and Persian keys, and no literal text is left in markup.
- Every new page has its title, description and preview, and shows in `/sitemap.xml`.
- What the change does with the server - a form, the chat, the waitlist - works with its waiting and failure states.
<!--#if (advancedTests == true)-->
- The build is clean, and `dotnet test` passes in `src/LandingPage/Boilerplate.LandingPage.Tests`: it checks every page
  in the sitemap for its head tags, every sample's 404, the waitlist, the chat and the rating lookups.
<!--#else-->
- The build is clean.
<!--#endif-->
