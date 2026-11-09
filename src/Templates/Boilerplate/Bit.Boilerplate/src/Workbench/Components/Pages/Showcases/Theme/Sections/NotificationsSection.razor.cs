namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Notifications category: badges, messages, people with their presence, snack bars and tags.</summary>
public partial class NotificationsSection
{
    /// <summary>Sample people, one per presence, for the BitPersona row.</summary>
    private static readonly (string NameKey, BitPersonaPresence Presence)[] people =
    [
        (nameof(WorkbenchStrings.DemoUserYas), BitPersonaPresence.Online),
        (nameof(WorkbenchStrings.DemoUserSaleh), BitPersonaPresence.Away),
        (nameof(WorkbenchStrings.DemoUserZahra), BitPersonaPresence.Dnd),
        (nameof(WorkbenchStrings.PosStaffEmma), BitPersonaPresence.Offline)
    ];

    /// <summary>Shows a success, a warning and an error snack bar.</summary>
    private void ShowSnackBars()
    {
        SnackBarService.Success(Localizer[nameof(WorkbenchStrings.ThemeSuccess)], Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Success]);
        SnackBarService.Warning(Localizer[nameof(WorkbenchStrings.ThemeWarning)], Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Warning]);
        SnackBarService.Error(Localizer[nameof(WorkbenchStrings.ThemeError)], Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Error]);
    }
}
