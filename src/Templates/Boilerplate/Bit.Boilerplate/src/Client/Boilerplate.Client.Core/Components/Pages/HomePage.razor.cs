namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage : BasePage
{
    [CascadingParameter(Name = "IsUserAuthenticated")]
    private bool isUserAuthenticated { get; set; }

    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;
}
