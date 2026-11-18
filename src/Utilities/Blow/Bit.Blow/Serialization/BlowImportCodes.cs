namespace Bit.Blow;

/// <summary>The <see cref="BlowImportIssue.Code"/> values the serializer reports.</summary>
public static class BlowImportCodes
{
    /// <summary>The input is not valid JSON.</summary>
    public const string InvalidJson = "invalid-json";

    /// <summary>The input is not well-formed XML (GraphML).</summary>
    public const string InvalidXml = "invalid-xml";

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
