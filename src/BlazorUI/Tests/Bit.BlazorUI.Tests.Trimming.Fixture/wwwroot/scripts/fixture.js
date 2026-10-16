// A script of the app's own calling into the library: the publish reads it, and keeps the script it calls
// (PageVisibility) though nothing in the app's .NET code names it.
window.fixtureDisposeVisibility = id => BitBlazorUI.PageVisibility.dispose(id);
