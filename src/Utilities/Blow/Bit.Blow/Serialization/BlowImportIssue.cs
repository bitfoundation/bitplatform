namespace Bit.Blow;

/// <summary>
/// One problem found reading a document: what it is, how serious, and where - a JSON path such as
/// <c>$.nodes[3].position</c> - so the user can be told precisely what is wrong with their file.
/// </summary>
/// <param name="Code">A stable code; the values are listed in <see cref="BlowImportCodes"/>.</param>
/// <param name="Severity">
/// <see cref="BlowValidationSeverity.Error"/> when the document, or an element of it, could not be read;
/// <see cref="BlowValidationSeverity.Warning"/> when it was read but is not sound (a duplicate id, an edge to a missing
/// node); <see cref="BlowValidationSeverity.Info"/> for something ignored (an unknown property).
/// </param>
/// <param name="Path">Where in the document, as a JSON path from the root (<c>$</c>).</param>
/// <param name="Message">An English description. Localize by <see cref="Code"/>.</param>
public sealed record BlowImportIssue(string Code, BlowValidationSeverity Severity, string Path, string Message);
