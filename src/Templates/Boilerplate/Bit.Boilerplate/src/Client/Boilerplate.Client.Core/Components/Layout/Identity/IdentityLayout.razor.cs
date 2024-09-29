namespace Boilerplate.Client.Core.Components.Layout.Identity;

public partial class IdentityLayout
{
    [CascadingParameter(Name = Parameters.CurrentUrl)] private string? CurrentUrl { get; set; }

    private bool IsHomePage => CurrentUrl == Urls.HomePage;
}
