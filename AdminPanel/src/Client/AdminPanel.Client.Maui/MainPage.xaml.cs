
namespace AdminPanel.Client.Maui;

public partial class MainPage
{
    public MainPage(ClientMauiSettings clientMauiSettings)
    {
        InitializeComponent();
        if (string.IsNullOrEmpty(clientMauiSettings.ApplicationInsights?.ConnectionString) is false)
        {
            AppWebView.RootComponents.Add(new()
            {
                ComponentType = typeof(BlazorApplicationInsights.ApplicationInsightsInit),
                Selector = "head::after"
            });
        }
    }
}
