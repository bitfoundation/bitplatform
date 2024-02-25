﻿namespace Boilerplate.Shared.Services;
public class CultureInfoManager
{
    public static (string name, string code) DefaultCulture { get; } = ("English", "en-US");

    public static (string name, string code)[] SupportedCultures { get; } =
    [
        ("English US", "en-US"),
        ("English UK", "en-GB"),
        ("Française", "fr-FR"),
        // ("فارسی", "fa-IR"), // To add more languages, you've to provide resx files. You might also put some efforts to change your app flow direction based on CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
    ];

    public static CultureInfo CreateCultureInfo(string cultureInfoId)
    {
        var cultureInfo = OperatingSystem.IsBrowser() ? CultureInfo.CreateSpecificCulture(cultureInfoId) : new CultureInfo(cultureInfoId);

        if (cultureInfoId == "fa-IR")
        {
            CustomizeCultureInfoForFaCulture(cultureInfo);
        }

        return cultureInfo;
    }

    public void SetCurrentCulture(string? cultureInfoCookie)
    {
        var currentCulture = GetCurrentCulture(cultureInfoCookie);

        var cultureInfo = CreateCultureInfo(currentCulture);

        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }

    public string GetCurrentCulture(string? preferredCulture = null)
    {
        string culture = preferredCulture ?? CultureInfo.CurrentUICulture.Name;
        if (SupportedCultures.Any(sc => sc.code == culture) is false)
        {
            culture = DefaultCulture.code;
        }
        return culture;
    }

    /// <summary>
    /// This is an example to demonstrate the way you can customize application culture
    /// </summary>
    public static CultureInfo CustomizeCultureInfoForFaCulture(CultureInfo cultureInfo)
    {
        cultureInfo.DateTimeFormat.AMDesignator = "ق.ظ";
        cultureInfo.DateTimeFormat.PMDesignator = "ب.ظ";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";

        return cultureInfo;
    }
}
