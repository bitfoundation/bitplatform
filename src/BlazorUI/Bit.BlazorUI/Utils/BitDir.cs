namespace Bit.BlazorUI;

public enum BitDir
{
    /// <summary>
    /// Ltr (left to right) is to be used for languages that are written from the left to the right (like English).
    /// </summary>
    Ltr,

    /// <summary>
    /// Rtl (right to left) is to be used for languages that are written from the right to the left (like Arabic).
    /// </summary>
    Rtl,

    /// <summary>
    /// Auto lets the user agent decide. It uses a basic algorithm as it parses the characters inside the element until it finds a character with a strong directionality, then applies that directionality to the whole element.
    /// </summary>
    Auto
}
