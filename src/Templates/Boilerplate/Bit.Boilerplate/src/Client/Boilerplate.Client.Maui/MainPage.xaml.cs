//+:cnd:noEmit

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    public MainPage(ClientMauiSettings clientMauiSettings)
    {
        InitializeComponent();
        //#if (appInsights == true)
        AppWebView.RootComponents.Insert(0, new()
        {
            ComponentType = typeof(BlazorApplicationInsights.ApplicationInsightsInit),
            Selector = "head::after"
        });
        //#endif
    }
}
