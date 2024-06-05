using System.Reflection;

namespace Boilerplate.Shared.Services;

public class CultureInfoManager
{
    public static CultureInfo DefaultUICulture { get; } = CreateCultureInfo("en-US");

    public static CultureInfo[] SupportedUICultures { get; } =
    [
        CreateCultureInfo("en-US"),
        CreateCultureInfo("en-GB"),
        CreateCultureInfo("fr-FR"),
        CreateCultureInfo("fa-IR")
    ];

    public static CultureInfo CreateCultureInfo(string name)
    {
        var cultureInfo = OperatingSystem.IsBrowser() ? CultureInfo.CreateSpecificCulture(name) : new CultureInfo(name);

        if (name == "fa-IR")
        {
            CustomizeCultureInfoForFaCulture(cultureInfo);
        }

        return cultureInfo;
    }

    public void SetCurrentCulture(string cultureName)
    {
        var uiCultureInfo = SupportedUICultures.FirstOrDefault(sc => sc.Name == cultureName) ?? DefaultUICulture; // for string values from resx files, detect RTL or LTR etc. 
        var cultureInfo = CreateCultureInfo(cultureName); // for ToString call on numbers etc.

        CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = Thread.CurrentThread.CurrentCulture = cultureInfo;

        CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentUICulture = uiCultureInfo;
    }

    /// <summary>
    /// This is an example to demonstrate the way you can customize application culture
    /// </summary>
    public static CultureInfo CustomizeCultureInfoForFaCulture(CultureInfo cultureInfo)
    {
        cultureInfo.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cultureInfo, new PersianCalendar());

        cultureInfo.DateTimeFormat.AMDesignator = "ق.ظ";
        cultureInfo.DateTimeFormat.PMDesignator = "ب.ظ";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        cultureInfo.DateTimeFormat.AbbreviatedDayNames =
        [
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        ];
        cultureInfo.DateTimeFormat.ShortestDayNames =
        [
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        ];

        return cultureInfo;
    }
}
