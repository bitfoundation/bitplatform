namespace Bit.Butil;

/// <summary>How a virtual keyboard should capitalize what the user types into the element.</summary>
public enum Autocapitalize
{
    /// <summary>The attribute is absent, so the element inherits the behaviour of its form or document.</summary>
    NotSet,

    /// <summary>No automatic capitalization.</summary>
    None,

    /// <summary>The historical spelling of <see cref="None"/>, still accepted.</summary>
    Off,

    /// <summary>The historical spelling of <see cref="Sentences"/>, still accepted.</summary>
    On,

    /// <summary>Capitalize the first letter of each sentence. The default for most inputs.</summary>
    Sentences,

    /// <summary>Capitalize the first letter of every word.</summary>
    Words,

    /// <summary>Capitalize every letter.</summary>
    Characters
}
