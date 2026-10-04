namespace Bit.BlazorUI;

public class BitShortId
{
    private static readonly string _chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly int _length = _chars.Length;
    private static readonly int _lettersLength = 26;

    /// <summary>
    /// A new short id, always starting with a letter.
    /// </summary>
    /// <remarks>
    /// These ids end up as DOM ids, which are read back by CSS selectors - a scoped rule, a querySelector, an
    /// :is() list - as well as by the aria-* attributes that point at them. An id is allowed to start with a
    /// digit in HTML, but a selector is not, so one that did could only be matched escaped. The leading
    /// character is therefore drawn from the letters alone and the rest from the full alphabet; the whole
    /// 32 bits of the source value are still spent, the leading letter carrying the low ones.
    /// </remarks>
    public static string NewId()
    {
        // read unsigned: Math.Abs(int.MinValue) is the one value of the four billion that would throw.
        var value = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray());

        return _chars[(int)(value % _lettersLength)] + ToString((int)(value / _lettersLength));
    }

    public static string ToString(int value)
    {
        var result = string.Empty;

        do
        {
            result = _chars[value % _length] + result;
            value /= _length;
        } while (value > 0);

        return result;
    }
}
