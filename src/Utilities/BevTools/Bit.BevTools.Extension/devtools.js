// Adds the "Blazor" panel to the browser's DevTools. The panel talks to the inspected page only through the DevTools'
// own inspectedWindow.eval: the extension asks for no permission and injects nothing into pages.
(globalThis.browser ?? globalThis.chrome).devtools.panels.create("Blazor", "", "panel.html");
