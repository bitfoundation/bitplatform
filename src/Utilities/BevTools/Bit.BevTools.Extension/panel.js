// The "Blazor" DevTools panel: the BevTools companion, reading the inspected page's runtimes.
import { Companion } from "./bit-bevtools-companion.js";
import { createDevToolsSource, inspectedWindowEval } from "./devtools-source.js";

const api = globalThis.chrome ?? globalThis.browser;
const source = () => createDevToolsSource(inspectedWindowEval(api));
const companion = new Companion(source(), document.body);
companion.start();

// A navigation replaces the page and its runtimes; start over on the new one.
api.devtools.network.onNavigated.addListener(() => companion.use(source()));

// Selecting a node in the Elements panel selects the component that rendered it (Firefox has no such event).
api.devtools.panels.elements?.onSelectionChanged?.addListener(() => companion.revealInspectedElement());

// DevTools closing unloads this panel: a pick it started must not keep swallowing the page's next click.
window.addEventListener("pagehide", () => companion.stop());
