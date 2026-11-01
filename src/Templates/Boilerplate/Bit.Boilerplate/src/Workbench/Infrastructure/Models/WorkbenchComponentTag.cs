namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// A component's tag in a .razor file: the file relative to the src folder, which of the file's tags of that component it
/// is, counted from 0, and its line.
/// </summary>
public sealed record WorkbenchComponentTag(string File, int Ordinal, int Line);
