using System.Text.RegularExpressions;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Shared.Infrastructure.Exceptions;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>
/// Hosts the app's settings page on the view's sample account (See SettingsSampleAccount), opened on the section the
/// Section switch picks. It does what the app's MainLayout does for the page: loads the signed-in user, hands it down, and
/// follows the page's PROFILE_UPDATED. A link of the page to one of its own sections, such as the privacy section's to the
/// account settings, opens that section here. When the app signs the device out, as it does after the account is deleted,
/// the view's sign-in follows, and the not-authorized page shows.
/// </summary>
public partial class SettingsHost
{
    public const string SectionSwitch = "section";

    [AutoInject] private IUserController userController = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler = default!;
    [AutoInject] private WorkbenchScenarioSignOut signOut = default!;
    [AutoInject] private WorkbenchAppLinks appLinks = default!;

    private bool isReady;
    private UserDto? currentUser;
    private Action? unsubscribe;
    private IDisposable? linkHandler;

    /// <summary>The section the page opens on, as the app's address names it (PageUrls.SettingsSections); Profile by default.</summary>
    private string Section => Scenario.GetSwitch(SectionSwitch) ?? PageUrls.SettingsSections.Profile;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (IsPrerendering)
            return;

        // The account the stand-ins answer from is saved with the view, so it's restored before the page reads it.
        await WhenStateRestored();

        signOut.SignedOut += OnSignedOut;
        linkHandler = appLinks.Add(FollowSettingsLink);
        unsubscribe = pubSubService.Subscribe(ClientAppMessages.PROFILE_UPDATED, async payload =>
        {
            if (payload is not UserDto updated || updated.Id != currentUser?.Id)
                return;

            currentUser = updated;
            await InvokeAsync(StateHasChanged);
        });

        isReady = true;

        // The page shows now, on its own placeholders while the user loads, as Blazor renders on its own only at the first
        // await that doesn't finish at once, and a load in the Loading state never finishes.
        StateHasChanged();

        try
        {
            currentUser = await userController.GetCurrentUser(CurrentCancellationToken);
        }
        catch (KnownException exception)
        {
            // As MainLayout's: the error is shown, and the page goes on without the user.
            exceptionHandler.Handle(exception);
        }
    }

    /// <summary>A link to the settings page or one of its sections opens that section here, as the app's router would.</summary>
    private Func<Task>? FollowSettingsLink(Uri target)
    {
        var path = CultureSegmentRegex().Replace(target.AbsolutePath, string.Empty).TrimEnd('/');

        if (path.StartsWith(PageUrls.Settings, StringComparison.OrdinalIgnoreCase) is false)
            return null;

        var section = path[PageUrls.Settings.Length..].Trim('/');

        if (section.Contains('/'))
            return null;

        return async () =>
        {
            await ReportScenario(Scenario.WithSwitch(SectionSwitch, section.Length is 0 ? null : section.ToLowerInvariant()));
            StateHasChanged();
        };
    }

    private Task OnSignedOut(WorkbenchScenario signedOut) => InvokeAsync(() => ReportScenario(signedOut));

    public override async ValueTask DisposeAsync()
    {
        signOut.SignedOut -= OnSignedOut;
        linkHandler?.Dispose();
        unsubscribe?.Invoke();

        await base.DisposeAsync();
    }

    /// <summary>The culture segment the app's routes may start with, such as /fa-IR/settings.</summary>
    [GeneratedRegex("^/[a-z]{2}-[A-Z]{2}(?=/|$)")]
    private static partial Regex CultureSegmentRegex();
}
