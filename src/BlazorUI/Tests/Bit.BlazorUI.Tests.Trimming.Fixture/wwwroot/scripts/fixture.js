// A script of the app's own calling into the library through the global, as a script of an app does: the publish reads
// it, and keeps the script it calls (PageVisibility) though nothing in the app's .NET code names it.
window.fixtureDisposeVisibility = id => window.BitBlazorUI.PageVisibility.dispose(id);
