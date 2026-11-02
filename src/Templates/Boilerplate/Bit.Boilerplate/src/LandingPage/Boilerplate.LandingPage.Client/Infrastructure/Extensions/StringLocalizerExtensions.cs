namespace Microsoft.Extensions.Localization;

public static class StringLocalizerExtensions
{
    public static string In(this IStringLocalizer localizer, CultureInfo culture, string name, params object[] arguments)
    {
        var current = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = culture;
            return localizer[name, arguments];
        }
        finally
        {
            CultureInfo.CurrentUICulture = current;
        }
    }
}
