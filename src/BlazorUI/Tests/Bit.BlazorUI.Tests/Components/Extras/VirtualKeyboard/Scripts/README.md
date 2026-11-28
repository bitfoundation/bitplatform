# BitVirtualKeyboard script tests

`node --test` suites of the BitVirtualKeyboard text engine (`BitVirtualKeyboardEngine.ts`), run by
`BitVirtualKeyboardScriptTests` against the module the Extras build compiles it into
(`Bit.BlazorUI.Extras/wwwroot/scripts/modules/BitVirtualKeyboardEngine.js`), so build `Bit.BlazorUI.Extras` first.

- `helpers/engine-fixture.mjs` puts the browser globals the module reads in place, imports it and exports the class.
- Run them by hand from this folder with `node --test` (Node 22.7+ or 20.19+, no package.json needed); set
  `BIT_VKB_MODULES` to another modules folder to run them against it.
- What needs a DOM (attaching to an input, focus, pointer gestures) is tested in the E2E suite instead.