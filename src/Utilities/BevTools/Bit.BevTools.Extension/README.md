# bit BevTools browser extension

A **Blazor** panel in the browser's DevTools (Chromium-based browsers and Firefox) that shows every BevTools runtime of
the inspected page — the Blazor Server circuit and the WebAssembly runtime side by side in a Blazor Web App — without
rendering anything into the app. It is the same frontend as the companion window (`⧉` in the in-app panel header,
or `_content/Bit.BevTools/companion.html`), hosted in DevTools.

## Build and load

```bash
dotnet build ../Bit.BevTools          # produces wwwroot/bit-bevtools-companion.js and .css
node build.mjs                        # assembles the unpacked extension in dist/
```

- **Chrome / Edge**: `chrome://extensions` → *Developer mode* → *Load unpacked* → `dist/`.
- **Firefox**: `about:debugging#/runtime/this-firefox` → *Load Temporary Add-on…* → `dist/manifest.json`.

Open DevTools on a page of a BevTools-enabled app and pick the **Blazor** panel. A runtime appears there once its
`<BevToolsPanel />` has rendered (with `Ui.ShowBadge = false` the panel itself stays invisible on the page).

## How it reaches the page, and what it may do

The extension asks for **no permission** and has **no content script**: nothing is injected into any page. The panel
talks to the inspected page only through `chrome.devtools.inspectedWindow.eval`, which DevTools allows while it is
open on that tab, and only to the page hub BevTools exposes there (`window.__BIT_BEVTOOLS__`). Every answer comes from
the runtime's own protocol (`BevToolsProtocol` in .NET), through the same inspection and redaction as the in-app
panel, so the extension never sees more than the panel would; `BitBevToolsOptions.ValueAccess` applies to it as well.
A page whose BevTools is off exposes no hub, and the panel says so.

| File | What it is |
| --- | --- |
| `manifest.json` | Manifest V3; Firefox settings under `browser_specific_settings` |
| `devtools.html` / `devtools.js` | Creates the **Blazor** panel |
| `panel.html` / `panel.js` | Hosts the companion with the DevTools source; starts over after each navigation |
| `devtools-source.js` | The companion's source over `inspectedWindow.eval`, with a page-side mailbox for asynchronous answers |
| `build.mjs` | Copies these files and the companion bundle into `dist/` |

Tests: `Tests/bit-bevtools-js/extension.test.js` (requests, answers, and that request data can never become code).
