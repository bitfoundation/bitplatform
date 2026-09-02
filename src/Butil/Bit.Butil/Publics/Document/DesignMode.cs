namespace Bit.Butil;

/// <summary>
/// Whether the whole document is editable.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">Document.designMode</see>
/// </summary>
public enum DesignMode
{
    /// <summary>
    /// The document's design mode is off (default).
    /// </summary>
    Off,

    /// <summary>
    /// The document is in design mode and the entire document is editable.
    /// </summary>
    On
}
