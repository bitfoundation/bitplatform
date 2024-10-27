//+:cnd:noEmit

using Boilerplate.Client.Core;

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    public MainPage(ClientAppSettings clientAppSettings)
    {
        InitializeComponent();
        //#if (appInsights == true)
        if (string.IsNullOrEmpty(clientAppSettings.ApplicationInsights?.ConnectionString) is false)
        {
            AppWebView.RootComponents.Add(new()
            {
                ComponentType = typeof(BlazorApplicationInsights.ApplicationInsightsInit),
                Selector = "head::after"
            });
        }
        //#endif
    }
}
