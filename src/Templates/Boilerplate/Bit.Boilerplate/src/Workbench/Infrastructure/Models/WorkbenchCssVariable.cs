namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// A CSS variable the picked component's styles use, with its value on the component's root element; IsOwn when the
/// component's styles also set it, rather than only reading it from the theme.
/// </summary>
public sealed record WorkbenchCssVariable(string Name, string Value, bool IsOwn);
