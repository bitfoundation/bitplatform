namespace Bit.Blow;

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
