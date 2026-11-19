namespace Bit.Bheadless;

/// <summary>Helpers for keyboard navigation that depends on orientation and direction.</summary>
public static class BitBhKeyboard
{
    /// <summary>The key that moves to the next item for the given orientation / direction.</summary>
    public static string NextKey(BitBhOrientation orientation, BitBhDirection dir)
        => orientation == BitBhOrientation.Vertical ? "ArrowDown" : dir == BitBhDirection.Rtl ? "ArrowLeft" : "ArrowRight";

    /// <summary>The key that moves to the previous item for the given orientation / direction.</summary>
    public static string PreviousKey(BitBhOrientation orientation, BitBhDirection dir)
        => orientation == BitBhOrientation.Vertical ? "ArrowUp" : dir == BitBhDirection.Rtl ? "ArrowRight" : "ArrowLeft";

    /// <summary>Converts a key string to the token used in <c>data-bit-bh-keys</c>.</summary>
    public static string Token(string key) => key == " " ? "Space" : key;

    /// <summary>Builds a <c>data-bit-bh-keys</c> attribute value from keys / tokens.</summary>
    public static string Keys(params string?[] keys)
        => string.Join(' ', keys.Where(k => !string.IsNullOrEmpty(k)).Select(k => Token(k!)).Distinct());

    /// <summary>The <c>data-bit-bh-keys</c> attribute name.</summary>
    public const string KeysAttribute = "data-bit-bh-keys";

    /// <summary>The data-orientation / aria-orientation value.</summary>
    public static string ToAttribute(this BitBhOrientation orientation)
        => orientation == BitBhOrientation.Vertical ? "vertical" : "horizontal";

    /// <summary>The dir attribute value.</summary>
    public static string ToAttribute(this BitBhDirection dir)
        => dir == BitBhDirection.Rtl ? "rtl" : "ltr";
}
