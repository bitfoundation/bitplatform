// The "Blazor" DevTools panel: the BevTools companion, reading the inspected page's runtimes.
import { Companion } from "./bit-bevtools-companion.js";
import { createDevToolsSource, inspectedWindowEval } from "./devtools-source.js";

const api = globalThis.chrome ?? globalThis.browser;
const source = () => createDevToolsSource(inspectedWindowEval(api));
const companion = new Companion(source(), document.body);
companion.start();

// A navigation replaces the page and its runtimes; start over on the new one.
api.devtools.network.onNavigated.addListener(() => companion.use(source()));
