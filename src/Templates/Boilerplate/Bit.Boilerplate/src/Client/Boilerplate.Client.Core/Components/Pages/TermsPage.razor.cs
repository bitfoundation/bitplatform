namespace Boilerplate.Client.Core.Components.Pages;

public partial class TermsPage
{
    protected override string? Title => Localizer[nameof(AppStrings.TermsTitle)];
    protected override string? Subtitle => string.Empty;
}
