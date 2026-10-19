namespace Bit.Borm;

/// <summary>Read/write access to form data by path.</summary>
public interface IBormDataAccessor : IBormDataReader
{
    /// <summary>Writes a value at the given path, creating intermediate containers as needed.</summary>
    void SetValue(string path, object? value);

    /// <summary>
    /// The declared element type for an array path (used to materialise new items),
    /// or null if it cannot be determined.
    /// </summary>
    Type? GetElementType(string arrayPath);

    /// <summary>
    /// True when the most recent <see cref="SetValue"/> could not be applied — typically because the
    /// value did not convert to the target property's type. Accessors report rather than throw so a
    /// half-typed entry cannot take the form down; the state turns this into a validation message so
    /// the input is never discarded silently.
    /// </summary>
    bool LastWriteFailed => false;
}
