using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsExternalNavigationService : IExternalNavigationService
{
    [AutoInject] private readonly NavigationManager navigationManager = default!;

    public async Task NavigateTo(string url)
    {
        // A private-use scheme (vscode://) or a loopback listener belongs to an OAuth client waiting for its callback,
        // and following it in the WebView would replace this app with that program's page. The shell resolves both;
        // external sign-in keeps to the WebView, because its callback comes back here.
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme is not ("http" or "https") || uri.IsLoopback))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }

        navigationManager.NavigateTo(url, forceLoad: true, replace: true);
    }
}
