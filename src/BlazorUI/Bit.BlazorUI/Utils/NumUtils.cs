namespace Bit.BlazorUI;

public class NumUtils
{
    public static string ToInvariantString(double value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
