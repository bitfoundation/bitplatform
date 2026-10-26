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

/// <summary>What reading a document produced.</summary>
public sealed class BlowImportResult
{
    internal BlowImportResult(BlowDocument? document, IReadOnlyList<BlowImportIssue> issues, int sourceVersion)
    {
        Document = document;
        Issues = issues;
        SourceVersion = sourceVersion;
    }

    /// <summary>
    /// The document, or null when it could not be read at all (not JSON, over a limit, from a newer version). With
    /// errors among <see cref="Issues"/> it is the part that could be read: elements that were broken are left out.
    /// </summary>
    public BlowDocument? Document { get; }

    /// <summary>Everything found, in document order.</summary>
    public IReadOnlyList<BlowImportIssue> Issues { get; }

    /// <summary>The application document version the file was saved with, before any migration.</summary>
    public int SourceVersion { get; }

    /// <summary>True when the document was read completely: there is a document and no error.</summary>
    public bool Succeeded => Document is not null && !Issues.Any(i => i.Severity == BlowValidationSeverity.Error);
}

/// <summary>The <see cref="BlowImportIssue.Code"/> values the serializer reports.</summary>
public static class BlowImportCodes
{
    /// <summary>The input is not valid JSON.</summary>
    public const string InvalidJson = "invalid-json";

    /// <summary>The input is larger than <see cref="BlowImportLimits.MaxBytes"/>.</summary>
    public const string TooLarge = "too-large";

    /// <summary>The JSON nests deeper than <see cref="BlowImportLimits.MaxDepth"/>.</summary>
    public const string TooDeep = "too-deep";

    /// <summary>There are more nodes, edges or ports than the limits allow.</summary>
    public const string TooMany = "too-many";

    /// <summary>A string is longer than <see cref="BlowImportLimits.MaxStringLength"/>.</summary>
    public const string StringTooLong = "string-too-long";

    /// <summary>The JSON is not a Blow document.</summary>
    public const string NotADocument = "not-a-document";

    /// <summary>The document was written in a Blow schema this version of the library does not know.</summary>
    public const string UnsupportedSchema = "unsupported-schema";

    /// <summary>The document was saved by a newer version of the application than <see cref="BlowSerializerOptions.CurrentVersion"/>.</summary>
    public const string NewerVersion = "newer-version";

    /// <summary>No migration upgrades the document from its version.</summary>
    public const string MissingMigration = "missing-migration";

    /// <summary>A migration threw or returned nothing.</summary>
    public const string MigrationFailed = "migration-failed";

    /// <summary>A required property is missing: an element without an id, an edge without a source or target.</summary>
    public const string MissingProperty = "missing-property";

    /// <summary>A property has a value of the wrong kind or out of range.</summary>
    public const string InvalidValue = "invalid-value";

    /// <summary>A property the schema does not have was ignored.</summary>
    public const string UnknownProperty = "unknown-property";

    /// <summary>Node or edge data could not be read into the type mapped for it, and was kept as JSON.</summary>
    public const string DataNotRead = "data-not-read";
}
