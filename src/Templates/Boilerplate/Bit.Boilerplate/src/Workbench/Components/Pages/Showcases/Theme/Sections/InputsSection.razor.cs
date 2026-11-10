namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Inputs category: each input component in its states, from empty and focused to invalid and disabled.</summary>
public partial class InputsSection
{
    private static readonly string[] multipleValues = ["1", "3"];

    private List<BitDropdownItem<string>> options = [];
    private List<BitChoiceGroupItem<string>> choices = [];
    private List<BitChoiceGroupItem<string>> deliveries = [];
    private List<string> tags = [];
    private DateTimeOffset[] highlightedDates = [];
    private BitFileInfo[] uploadedFiles = [];

    /// <summary>Whether the screen is a phone's, with no room for a calendar's pickers beside its days.</summary>
    private bool isPhone;

    private string SampleText => Localizer[nameof(WorkbenchStrings.ThemeSampleText)];

    private string SampleName => Localizer[nameof(WorkbenchStrings.DemoUserYas)];

    private string NameLabel => Localizer[nameof(WorkbenchStrings.ThemeName)];

    private string NamePlaceholder => Localizer[nameof(WorkbenchStrings.ThemeNamePlaceholder)];

    private string SearchText => Localizer[nameof(WorkbenchStrings.ThemeSearch)];

    private string QuantityLabel => Localizer[nameof(WorkbenchStrings.ThemeQuantity)];

    private string CodeLabel => Localizer[nameof(WorkbenchStrings.ThemeCode)];

    private string ChoiceLabel => Localizer[nameof(WorkbenchStrings.ThemeChoice)];

    private string ChoicePlaceholder => Localizer[nameof(WorkbenchStrings.ThemeChoicePlaceholder)];

    private string OptionLabel => Localizer[nameof(WorkbenchStrings.ThemeOption), 1];

    private string ToggleLabel => Localizer[nameof(WorkbenchStrings.ThemeToggle)];

    private string AmountLabel => Localizer[nameof(WorkbenchStrings.ThemeAmount)];

    private string RatingLabel => Localizer[nameof(WorkbenchStrings.ThemeInputsRateYourVisit)];

    private string TagsLabel => Localizer[nameof(WorkbenchStrings.ThemeInputsTags)];

    protected override void OnInitialized()
    {
        options = [.. Enumerable.Range(1, 4).Select(index => new BitDropdownItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];
        choices = [.. Enumerable.Range(1, 3).Select(index => new BitChoiceGroupItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];

        deliveries =
        [
            new() { Value = "bike", Text = Localizer[nameof(WorkbenchStrings.ThemeInputsBike)], IconName = BitIconName.Cycling },
            new() { Value = "van", Text = Localizer[nameof(WorkbenchStrings.ThemeInputsVan)], IconName = BitIconName.DeliveryTruck },
            new() { Value = "pickup", Text = Localizer[nameof(WorkbenchStrings.ThemeInputsPickUp)], IconName = BitIconName.Shop }
        ];

        tags = [Localizer[nameof(WorkbenchStrings.ThemeInputsVegan)], Localizer[nameof(WorkbenchStrings.ThemeInputsGlutenFree)], Localizer[nameof(WorkbenchStrings.ThemeInputsOrganic)]];

        highlightedDates = [ThemeSamples.Today.AddDays(3), ThemeSamples.Today.AddDays(10)];

        uploadedFiles =
        [
            new() { Name = "storefront.jpg", Size = 482_311, ContentType = "image/jpeg", FileId = "storefront" },
            new() { Name = "menu-autumn.pdf", Size = 1_204_880, ContentType = "application/pdf", FileId = "menu" }
        ];
    }

    private void OnPhoneChanged(bool phone) => isPhone = phone;
}
