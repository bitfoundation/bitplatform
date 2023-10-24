using System.Reflection;

namespace BlazorDual.Shared.Infra;
public class CultureInfoManager
{
    public static (string name, string code) DefaultCulture { get; } = ("English", "en-US");

    public static (string name, string code)[] SupportedCultures { get; } = new (string name, string code)[]
    {
        ("English US", "en-US"),
        ("English UK", "en-GB"),
        ("Française", "fr-FR"),
        // ("فارسی", "fa-IR"), // To add more languages, you've to provide resx files. You might also put some efforts to change your app flow direction based on CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
    };

    public static CultureInfo CreateCultureInfo(string cultureInfoId)
    {

        cultureInfo.DateTimeFormat.AMDesignator = "ق.ظ";
        cultureInfo.DateTimeFormat.PMDesignator = "ب.ظ";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
      
        return cultureInfo;
    }

    private static readonly FieldInfo _cultureDataField = typeof(TextInfo).GetField("_cultureData", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo _iReadingLayoutField = Type.GetType("System.Globalization.CultureData, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")!.GetField("_iReadingLayout", BindingFlags.NonPublic | BindingFlags.Instance)!;
}
