//+:cnd:noEmit

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();
        //#if (appInsights == true)
        AppWebView.RootComponents.Add(new()
        {
            ComponentType = typeof(BlazorApplicationInsights.ApplicationInsightsInit),
            Selector = "head::after"
        });
        //#endif
    }
}
