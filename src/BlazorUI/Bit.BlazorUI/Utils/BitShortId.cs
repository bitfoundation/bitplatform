namespace Bit.BlazorUI;

internal class BitShortId
{
    private static readonly string _chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly int length = _chars.Length;

    public static string NewId() => ToString(Math.Abs(BitConverter.ToInt32(Guid.NewGuid().ToByteArray())));

    public static string ToString(int value)
    {
        var result = string.Empty;

        do
        {
            result = _chars[value % length] + result;
            value /= length;
        } while (value > 0);

        return result;
    }
}
