using System.Runtime.CompilerServices;

namespace Boilerplate.Shared.Services;

public partial class CultureInfoManager
{
    /// <summary>
    /// To enable/disable multilingual support, navigate to Directory.Build.props and modify the MultilingualEnabled flag.
    /// </summary>
    public static bool MultilingualEnabled { get; } =
#if MultilingualEnabled
    true;
#else
    false;
#endif

    public static CultureInfo DefaultCulture => CreateCultureInfo("en-US");

    public static (string DisplayName, CultureInfo Culture)[] SupportedCultures =>
    [
        ("English US", CreateCultureInfo("en-US")),
        ("English UK", CreateCultureInfo("en-GB")),
        ("Dutch", CreateCultureInfo("nl-NL")),
        ("Française", CreateCultureInfo("fr-FR")),
        ("فارسی", CreateCultureInfo("fa-IR"))
        // Adding new cultures requires changing HomePage.razor's routes and MainActivity's DataPathPrefixes.
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
        var cultureInfo = SupportedCultures.FirstOrDefault(sc => sc.Culture.Name == cultureName).Culture ?? DefaultCulture;

        CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = Thread.CurrentThread.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }

    /// <summary>
    /// This is an example to demonstrate the way you can customize application culture
    /// </summary>
    private static CultureInfo CustomizeCultureInfoForFaCulture(CultureInfo cultureInfo)
    {
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

        Get_CalendarField(cultureInfo) = new PersianCalendar();

        return cultureInfo;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_calendar")]
    extern static ref Calendar Get_CalendarField(CultureInfo cultureInfo);
}
