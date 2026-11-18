namespace Bit.BevTools;

/// <summary>Where code is written: a file of the build, a line and a column, from the assembly's portable PDB.</summary>
/// <param name="Path">The file as the build saw it: an absolute path on the machine that built the assembly.</param>
/// <param name="Line">One-based line.</param>
/// <param name="Column">One-based column.</param>
/// <param name="Url">The configured editor link for it (<see cref="BevToolsUiOptions.EditorUrlTemplate"/>), when one is configured.</param>
public sealed record BevToolsSourceLocation(string Path, int Line, int Column, string? Url)
{
    /// <summary>
    /// The assembly was edited with hot reload since it was built: its symbols are the build's, so this line may have
    /// moved (the file is still the right one).
    /// </summary>
    public bool MayHaveMoved { get; init; }

    /// <summary>The file name, whichever platform built it: WebAssembly reads Windows paths from a PDB built on Windows.</summary>
    public string FileName => Path[(Path.LastIndexOfAny(['/', '\\']) + 1)..];

    public override string ToString() => Path + ":" + Line;
}
