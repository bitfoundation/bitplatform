namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Lists category: a basic list filled, loading and empty, a carousel, a swiper and an order's timeline.</summary>
public partial class ListsSection
{
    /// <summary>The team the list shows: the resource key of each one's name, and their job in English for the localizer.</summary>
    private static readonly (string NameKey, string RoleKey)[] people =
    [
        (nameof(WorkbenchStrings.DemoUserYas), nameof(WorkbenchStrings.ThemeNotificationsOwner)),
        (nameof(WorkbenchStrings.DemoUserSaleh), nameof(WorkbenchStrings.ShiftsRoleBaker)),
        (nameof(WorkbenchStrings.DemoUserZahra), nameof(WorkbenchStrings.ShiftsRoleBarista)),
        (nameof(WorkbenchStrings.PosStaffEmma), nameof(WorkbenchStrings.ThemeExtrasDelivery))
    ];

    private static readonly (string NameKey, string RoleKey)[] noPeople = [];

    private (string Icon, string Title, string Text)[] slides = [];
    private List<BitTimelineItem> steps = [];

    protected override void OnInitialized()
    {
        slides =
        [
            (BitIconName.Breakfast, Localizer[nameof(WorkbenchStrings.ThemeListsBreakfastBox)], Localizer[nameof(WorkbenchStrings.ThemeListsCroissantJuiceAndCoffee)]),
            (BitIconName.Coffee, Localizer[nameof(WorkbenchStrings.ThemeListsOatFlatWhite)], Localizer[nameof(WorkbenchStrings.ThemeListsTwoForOneBeforeTen)]),
            (BitIconName.Cake, Localizer[nameof(WorkbenchStrings.ThemeListsCarrotCake)], Localizer[nameof(WorkbenchStrings.ThemeListsBakedEveryMorning)]),
            (BitIconName.Shop, Localizer[nameof(WorkbenchStrings.ThemeListsClickAndCollect)], Localizer[nameof(WorkbenchStrings.ThemeListsReadyInFifteenMinutes)])
        ];

        steps =
        [
            new() { PrimaryText = Localizer[nameof(WorkbenchStrings.PosOrdered)], SecondaryText = "09:12", IconName = BitIconName.ShoppingCart, Color = BitColor.Success },
            new() { PrimaryText = Localizer[nameof(WorkbenchStrings.ThemeListsBaked)], SecondaryText = "09:40", IconName = BitIconName.Cake, Color = BitColor.Success },
            new() { PrimaryText = Localizer[nameof(WorkbenchStrings.ThemeListsOnItsWay)], SecondaryText = "10:05", IconName = BitIconName.DeliveryTruck, Color = BitColor.Primary },
            new() { PrimaryText = Localizer[nameof(WorkbenchStrings.ThemeListsDelivered)], SecondaryText = Localizer[nameof(WorkbenchStrings.ThemeListsAbout1030)], IconName = BitIconName.Home, IsDisabled = true }
        ];
    }
}
