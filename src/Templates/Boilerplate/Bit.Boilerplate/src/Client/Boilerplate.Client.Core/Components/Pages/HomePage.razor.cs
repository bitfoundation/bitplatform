namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage : AppPageBase
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;
}
