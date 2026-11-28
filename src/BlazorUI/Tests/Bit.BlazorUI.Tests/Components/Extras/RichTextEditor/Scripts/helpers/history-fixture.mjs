// Test fixture for the BitRichTextEditor undo history (BitRichTextEditorHistory.ts), the compiled lazy ES module of the
// Extras package (Bit.BlazorUI.Extras/wwwroot/scripts/modules/BitRichTextEditorHistory.js). The module adds its class to
// the global BitBlazorUI namespace it finds on `window` rather than exporting it, so the globals it reads are put in
// place first, the module is imported after that, and the class is read off the namespace. The module is taken from the
// wwwroot folder beside this test project, or from the folder the BIT_RTE_MODULES environment variable names.

import path from 'node:path';
import { pathToFileURL } from 'node:url';

const MODULE_DIR = process.env.BIT_RTE_MODULES
    ? pathToFileURL(path.resolve(process.env.BIT_RTE_MODULES) + path.sep)
    : new URL('../../../../../../../Bit.BlazorUI.Extras/wwwroot/scripts/modules/', import.meta.url);

// The module sits under the Extras project's package.json, which declares no "type", so Node finds its module syntax by
// itself and says so once per file; that warning is dropped, every other one still prints.
const warningListeners = process.listeners('warning');
process.removeAllListeners('warning');
process.on('warning', (warning) => {
    if (warning && warning.code === 'MODULE_TYPELESS_PACKAGE_JSON') return;
    for (const listener of warningListeners) listener.call(process, warning);
});

if (typeof globalThis.window === 'undefined') globalThis.window = globalThis;
const BitBlazorUI = globalThis.BitBlazorUI || (globalThis.BitBlazorUI = {});
BitBlazorUI.Utils = BitBlazorUI.Utils || {};

await import(new URL('BitRichTextEditorHistory.js', MODULE_DIR).href);

if (typeof BitBlazorUI.BitRichTextEditorHistory !== 'function') {
    throw new Error(`BitBlazorUI.BitRichTextEditorHistory was not defined by the module in ${MODULE_DIR}`);
}

export const History = BitBlazorUI.BitRichTextEditorHistory;

// A state whose selection names the step it was taken at, so a test can tell which one came back.
export const state = (html, selection = html) => ({ html, selection });
