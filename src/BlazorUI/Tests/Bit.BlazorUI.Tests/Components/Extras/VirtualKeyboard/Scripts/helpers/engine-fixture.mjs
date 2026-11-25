// Test fixture for the BitVirtualKeyboard text engine (BitVirtualKeyboardEngine.ts), the compiled lazy ES module of the
// Extras package (Bit.BlazorUI.Extras/wwwroot/scripts/modules/BitVirtualKeyboardEngine.js). The module adds its class to
// the global BitBlazorUI namespace it finds on `window` rather than exporting it, so the globals it reads are put in
// place first, the module is imported after that, and the class is read off the namespace. The module is taken from the
// wwwroot folder beside this test project, or from the folder the BIT_VKB_MODULES environment variable names.

import path from 'node:path';
import { pathToFileURL } from 'node:url';

const MODULE_DIR = process.env.BIT_VKB_MODULES
    ? pathToFileURL(path.resolve(process.env.BIT_VKB_MODULES) + path.sep)
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

await import(new URL('BitVirtualKeyboardEngine.js', MODULE_DIR).href);

if (typeof BitBlazorUI.BitVirtualKeyboardEngine !== 'function') {
    throw new Error(`BitBlazorUI.BitVirtualKeyboardEngine was not defined by the module in ${MODULE_DIR}`);
}

export const Engine = BitBlazorUI.BitVirtualKeyboardEngine;