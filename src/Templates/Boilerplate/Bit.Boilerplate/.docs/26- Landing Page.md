# Stage 26: Landing Page

Besides the app, the project ships its **landing page**: the public site people find in search results, open from an
ad or a shared link, and read before they install the app or sign up. It is a site of its own, next to the app, in
two projects:

- [`src/LandingPage/Boilerplate.LandingPage.Server`](/src/LandingPage/Boilerplate.LandingPage.Server/) hosts it,
  prerenders every page, and serves the waitlist, the chat, the emails, the sitemap and the store links.
- [`src/LandingPage/Boilerplate.LandingPage.Client`](/src/LandingPage/Boilerplate.LandingPage.Client/) is its Blazor
  WebAssembly part: the pages, the shared parts, the styles and the texts.

It references none of the app's projects, so it builds, runs and deploys on its own, and a change to the app never
breaks it. It comes with **sample landing pages for several kinds of business**, each under its own path
(`/ecommerce`, `/ecommerce/about` and so on) and each with its own pages, look, texts and chat, plus a gallery of all
of them at `/`. Keep the sample closest to your product, or parts of several, make it your home page and delete the
rest.

## Run it

- `bit new` and `bit setup` build it and open it in the browser right after the Workbench.
- Run `dotnet watch` in `src/LandingPage/Boilerplate.LandingPage.Server`, or start the `landingpage` resource from the
  Aspire dashboard, where it waits for a start rather than starting with the app.
- It needs no database and no keys: without an AI key the chat streams a sample answer, and without an SMTP server
  the waitlist emails are written to the log (under Aspire they land in Mailpit).

## The samples

| Sample | Path | For | Launch | Pages | Chat |
| --- | --- | --- | --- | --- | --- |
| Fernhouse | `/ecommerce` | An online shop for plants and pots | Launched | Home, about | Bubble |
| Northside Market | `/retail` | A grocer with six shops and click and collect | Launched | Home, shops with map and hours, about | Concierge |
| Alder & Salt | `/hotel` | A hotel that wants direct bookings | Launched | Home, rooms, about | Concierge |
| Little Copper | `/restaurant` | Cafés whose ordering app isn't out yet | Before launch: a countdown, a pre-order on the App Store and a waitlist | Home, about | Inline |
| Pulsepath | `/fitness` | A workout app released in stages | iPhone and web, Android coming | Home with what's new, about | Inline |
| Brightlane | `/crm` | A CRM for small sales teams | Launched | Home, pricing, versus spreadsheets, about | Bubble |
| Sumwise | `/accounting` | Invoicing and bookkeeping for freelancers | Launched | Home, pricing, about | Concierge |
| Lodgeboard | `/hotel-software` | Software for independent hotels | Launched | Home, pricing, press, about | Bubble |
| Notch | `/minimal` | The smallest page that still does the job | Before launch | Home | Bubble |

Each one also has its legal pages and a not-found page of its own, and the ones that collect a waitlist have the
pages its emails link to.

## What every sample can use

The samples share their building blocks, in
[`Components/Common`](/src/LandingPage/Boilerplate.LandingPage.Client/Components/Common/), and each one picks what
fits its business:

- **Before launch, a waitlist**: the visitor leaves an email address, confirms it from an email, and can leave with
  the unsubscribe link every email carries. A hidden field and a minimum fill time keep bots out, and the server
  limits how often one visitor can try.
- **After launch, the stores**: the official App Store, Google Play and Microsoft Store badges, ordered for the
  visitor's own device; a QR code that opens the right store on the phone that scans it; one link,
  `/<sample>/download`, that sends each phone to its own store with the campaign it came from; and the App Store
  rating, read from Apple and shown once the app has one.
- **A chat** that answers visitors' questions about the product, in three looks: a bubble, a panel inside the page,
  and a concierge. It is built on Bit.BlazorUI.AI and talks to the server over a WebSocket; neither is downloaded
  until a visitor opens it.
- **Search engines and link previews**: every page has its own title, description, canonical link, language
  alternates, preview image and structured data (the organization, the app, the FAQ), and the server writes
  `sitemap.xml`, `robots.txt` and `llms.txt` from the pages' routes.
- **Two languages and both directions**: English at `/...` and Persian at `/fa-IR/...`, each with its own fonts, and a
  suggestion to switch when the browser prefers the other one.
- **Light and dark**, following the visitor's system until they choose.
- Legal pages (privacy, terms, cookies, imprint and accessibility), a not-found page of each sample's own, an
  announcement bar, a countdown, an FAQ, share buttons and a consent banner. It installs and works as a PWA as well.

## The tour

The gallery offers a tour of the samples and what comes built into them: working pieces of the apps, the launch states,
the store links and the App Store listing, the chat as a bubble and as a side tab, the waitlist, the inner pages,
prices in the visitor's currency, the two languages and themes, the privacy choices, the legal and not-found pages, and
what search engines get. Along the way it opens the chat and checks two guests in at Lodgeboard's front desk, and the
steps over a working preview let the visitor use it. It runs on bit Bonboarding, which is downloaded only when a visitor starts it,
keeps no progress in the browser and wears the gallery's look on every sample. The steps are in
[`LandingTourSteps.cs`](/src/LandingPage/Boilerplate.LandingPage.Client/Components/Tour/LandingTourSteps.cs), each with
the page it opens and the element it points at, and their texts in `TourStrings.resx` and `TourStrings.fa.resx`. A
step whose sample is gone is left out, and one whose element isn't there shows in the middle of the screen.

## How it's built

- **Prerendered on every visit.** The server renders each page to HTML, so a crawler, a link preview and a visitor on
  a slow phone all see it at once, then WebAssembly takes over. bit Bswup runs it as a PWA in `AlwaysPrerender` mode:
  each visit still gets the server's fresh page, and the app's files come from the device's cache. A new version
  reloads the page as soon as it's ready.
- **Small to download.** Trimmed and minified with Bit.Minifier, the chat and the tour lazy-loaded, only the page's
  language's data downloaded, so changing the language reloads the page, fonts self-hosted as small subsets, static files precompressed with Brotli, and responses
  compressed with Brotli and Gzip (and Zstandard on .NET 11).
- **Cacheable.** The pages set no cookies, the antiforgery check included, so a CDN can cache them; static files are
  cached for a week when their URL carries a version, and pages for ten minutes.
- **No scripts for crawlers and benchmarks.** Search engine crawlers, Lighthouse and PageSpeed Insights get the
  prerendered page without its JavaScript tags, so a benchmark isn't charged for code a visitor's device already keeps
  and a crawler reads the page as it is. That response is marked `private`, so a CDN never hands it to a visitor. The
  other direction takes a rule on the CDN: a page a visitor left in its cache still has the scripts, so these callers
  have to bypass it.
- **Light on the server.** The waitlist and the chat have rate limits of their own, the rating is looked up only for
  the samples' own apps and cached for hours, and the chat checks where its connection comes from, keeps
  conversations short and answers only from the facts it is given about the product.
- **Its own conventions.** Text goes into resx keys, in each sample's own file (`<Sample>Strings.resx` and
  `<Sample>Strings.fa.resx`) or in `LandingStrings.resx` for what the samples share; components inherit
  `LandingComponentBase`. The app's own conventions that name its types and files don't apply here.
- **Analytics only with consent.** bit Belemetry sends page views, interactions and performance to Azure Monitor only
  after the visitor agrees, and without cookies. Errors are sent either way, as a legitimate interest.

## Make it yours

Your AI agent knows the landing page through its `landing-page` skill
([`.github/agents/landing-page.agent.md`](/.github/agents/landing-page.agent.md)): ask it to make a sample your home
page, to change its texts, look or pages, or to launch it. In short:

- **Make a sample your home page**: give it an empty `Path` in its `<Sample>Site.cs`, give its home page the
  gallery's routes, and delete the gallery, its tour and the other samples.
- **Brand it**: the sample's palette, fonts and shapes are in `Styles/sites/_<sample>.scss`, and the **rebrand** skill
  re-skins it together with the app.
- **Launch**: set each platform in `<Sample>Site.cs` from `ComingSoon` or `PreOrder` to `Live` with its store link.
  The page then shows the store badges instead of the waitlist.
- **Keep the waitlist somewhere**: it lives in memory until you implement `IWaitlistStore` in
  [`WaitlistStore.cs`](/src/LandingPage/Boilerplate.LandingPage.Server/Features/Waitlist/WaitlistStore.cs) over your
  API, your database or your mailing list service, so every restart empties it.
- **Replace its preview image**: the picture a shared link shows is `images/og/<sample>.png`, 1200 by 630, under
  the client's `wwwroot`. Each sample also has its own web app manifest, so a visitor who installs it gets that
  sample, under its own name, starting at its own page.
- **Teach the chat**: it answers from each sample's facts in
  [`LandingChatPrompts.cs`](/src/LandingPage/Boilerplate.LandingPage.Server/Features/Chat/LandingChatPrompts.cs).
- **Replace the fictional parts**: the samples' companies, people, ratings and quotes are made up. Before you
  launch, the sample you keep needs your real company details, legal texts, reviews and links.

<!--#if (advancedTests == true)-->
## Tests

`src/LandingPage/Boilerplate.LandingPage.Tests` runs the landing page in its own process and checks it the way a
crawler and a visitor reach it: every page in the sitemap is prerendered with its title, description, canonical link,
language alternates, one heading and valid structured data; a missing page answers 404 inside its sample's layout;
the waitlist confirms, unsubscribes, keeps bots out and limits retries; the chat streams its answers and refuses other
sites; and the App Store is asked only about the samples' own apps. It needs no database, browser or keys. Run
`dotnet test` in that folder; the project's CI runs it too.
<!--#endif-->

## Settings

| Setting | What it does |
| --- | --- |
| `TrustedOrigins` | The addresses the site is served from, such as `https://landing.example.com`, as in the app. The waitlist emails, the sitemap and the download QR code link to the address a request came in on only when it's listed here, since any client can write the `Host` header. Any other address gets a 400. Localhost needs no entry, and the template's CD adds `LANDING_PAGE_ADDRESS` on its own. |
| `Site:AllowIndexing` | Whether search engines may index the site. On only in `appsettings.Production.json`. |
| `Site:GoogleSiteVerification`, `Site:BingSiteVerification` | The search consoles' verification codes. |
| `Email:DefaultFromEmail`, `Email:DefaultFromName` | The sender of the waitlist emails. |
| `ConnectionStrings:smtp` | The SMTP server, in the same format as the app's server. Without it, emails are logged and `/health` answers `Degraded`. |
| `AI:ChatOptions`, `AI:OpenAI:ChatModel`, `AI:OpenAI:ChatApiKey`, `AI:OpenAI:ChatEndpoint` | The chat's model and what it is sent, the same settings and user secrets as the app's server. |
| `WebAppRender:BlazorMode` | WebAssembly, always prerendered, since being found is what the site is for. Blazor Server and Auto are for development only. |
| `Belemetry:AzureMonitorConnectionString` | In the client's `appsettings.json`: where errors go, and analytics after consent. The server sends its logs, traces and metrics there too. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Where the server also sends them over OTLP, such as the Aspire dashboard. |
| `Belemetry:OtlpEndpoint` | Where the browser sends them over OTLP/HTTP, when the server's own endpoint is one a browser can't reach. A deployment sets it to the landing page's own address, whose `/v1/` paths the tunnel, or Caddy without Cloudflare, sends to the dashboard. |

The server's settings are in
[`appsettings.json`](/src/LandingPage/Boilerplate.LandingPage.Server/appsettings.json), and a deployment reads them
from the `LANDINGPAGE_SETTINGS` secret, one `Section__Key=value` line each
([Stage 16](/.docs/16-%20CI-CD%20Pipeline%20and%20Environments.md)).

## Languages

English and Persian ship, and the project's `InvariantGlobalization` switch (`src/Directory.Build.props`) leaves only
English. To add a language, add it to
[`LandingCultures.cs`](/src/LandingPage/Boilerplate.LandingPage.Client/Infrastructure/LandingCultures.cs), add
`src/LandingPage` to `ResxPaths` in `Bit.ResxTranslator.json`, and run `bit translate`
([Stage 5](/.docs/05-%20Localization%20and%20Multi-language%20Support.md)).

## Deploy it

It is a service of its own in the Docker Compose project, `landingpage`, built and deployed by the same CD as the app.
Give it a hostname of its own, often the bare domain with the app on a subdomain such as `app.`, pointed at
`http://landingpage:8080` through the Cloudflare Tunnel (or Caddy, without Cloudflare), and set the `LANDING_PAGE_ADDRESS` variable so each
deployment checks that it answers. The **deploy** skill walks through it.

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
