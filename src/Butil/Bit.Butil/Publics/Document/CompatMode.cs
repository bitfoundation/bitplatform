namespace Bit.Butil;

/// <summary>
/// Which rendering mode the document was parsed in, decided by its doctype.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode">Document.compatMode</see>
/// </summary>
public enum CompatMode
{
    /// <summary>
    /// The document is in quirks mode.
    /// </summary>
    BackCompat,

    /// <summary>
    /// The document is in no-quirks (also known as "standards") mode or limited-quirks (also known as "almost standards") mode.
    /// </summary>
    CSS1Compat
}
