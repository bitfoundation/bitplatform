namespace AdminPanel.Client.Core.Components.Layout;

public partial class NavBar
{
    [CascadingParameter(Name = Parameters.CurrentUrl)] private string? currentUrl { get; set; }

    private bool IsActive(string url)
    {
        return currentUrl == url;
    }
}
