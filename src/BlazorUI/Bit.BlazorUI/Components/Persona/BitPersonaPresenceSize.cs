using System.Threading;

namespace Bit.BlazorUI;

public static class BitPersonaPresenceSize
{
    public const string Size6 = "6px";
    public const string Size8 = "8px";
    public const string Size12 = "12px";
    public const string Size16 = "16px";
    public const string Size20 = "20px";
    public const string Size28 = "28px";
    public const string Size32 = "32px";

    public static int SizeToPixels(string size)
    {
        return int.Parse(size[..^2], Thread.CurrentThread.CurrentCulture);
    }
}
