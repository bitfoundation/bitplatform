//+:cnd:noEmit
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Maui;

public partial class MainPage
{
    public MainPage(ClientMauiSettings clientMauiSettings)
    {
        InitializeComponent();
        AppWebView.RootComponents.Add(new()
        {
            ComponentType = typeof(HeadOutlet),
            Selector = "head::after"
        });
    }
}
