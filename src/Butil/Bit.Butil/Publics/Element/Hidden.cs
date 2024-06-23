namespace Bit.Butil;

public enum Hidden
{
    /// <summary>
    /// The element is not hidden. This is the default value for the attribute.
    /// </summary>
    False,

    /// <summary>
    /// The element is hidden.
    /// </summary>
    True,

    /// <summary>
    /// The element is hidden until found, meaning that it is hidden but will be revealed if found through in page search or reached through fragment navigation.
    /// </summary>
    UntilFound
}
