[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace TodoTemplate.App;

public partial class App
{
    public App()
    {
        InitializeComponent();
    }

    public static string GetPreferredCulture()
    {
        var args = CultureInfoManager.GetCultureData();
        var culture = args.CurrentCulture;
        var preferredCulture = Preferences.Get(".AspNetCore.Culture", null);
        if (preferredCulture != null)
        {
            culture = preferredCulture[(preferredCulture.IndexOf("|uic=") + 5)..];
        }
        if (args.SupportedCultures.Any(sc => sc == culture) == false)
        {
            culture = args.DefaultCulture;
        }
        if (preferredCulture != $"c={culture}|uic={culture}")
        {
            Preferences.Set(".AspNetCore.Culture", $"c={culture}|uic={culture}");
        }
        return culture;
    }
}
