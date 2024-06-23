namespace Bit.Butil;

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
