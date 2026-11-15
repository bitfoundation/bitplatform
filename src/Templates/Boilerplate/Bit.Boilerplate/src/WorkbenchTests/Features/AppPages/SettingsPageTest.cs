//+:cnd:noEmit
using Boilerplate.WorkbenchTests.Features.Chrome;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>What the tests of the app's Settings page in the Workbench share: its sections, and text on an open one.</summary>
public abstract class SettingsPageTest : AppViewPageTest
{
    /// <summary>The button that opens and closes a section.</summary>
    protected ILocator SectionHeader(string title) => View().GetByRole(AriaRole.Heading, new() { Name = title, Exact = true }).GetByRole(AriaRole.Button);

    /// <summary>An open section's content.</summary>
    protected ILocator Section(string title) => View().GetByRole(AriaRole.Region, new() { Name = title, Exact = true });

    /// <summary>The section of the app's address (PageUrls.SettingsSections) each title stands for, which the page's Section switch takes.</summary>
    protected static string SectionKeyOf(string title) => title switch
    {
        _ when title == AppStrings.AccountTitle => PageUrls.SettingsSections.Account,
        _ when title == AppStrings.TfaTitle => PageUrls.SettingsSections.Tfa,
        _ when title == AppStrings.SessionsTitle => PageUrls.SettingsSections.Sessions,
        _ when title == AppStrings.PrivacyTitle => PageUrls.SettingsSections.Privacy,
        //#if (ads == true)
        _ when title == AppStrings.UpgradeTitle => PageUrls.SettingsSections.UpgradeAccount,
        //#endif
        _ => PageUrls.SettingsSections.Profile
    };

    /// <summary>
    /// Opens the Settings page on a section through its Section switch, as a reviewer does, so a reload opens it again;
    /// Profile, the page's own first section, needs no switch.
    /// </summary>
    protected async Task OpenSection(string title, string query = "")
    {
        if (title != AppStrings.ProfileTitle)
        {
            query = $"{(query.Length is 0 ? "?" : $"{query}&")}{WorkbenchScenario.SwitchQueryPrefix}section={SectionKeyOf(title)}";
        }

        await OpenWorkbench("settings", query);

        await Expect(Section(title)).ToBeVisibleAsync();
    }

    /// <summary>Opens a preview alone on a snapshot of the view's sample account, for what nothing on the page can set.</summary>
    protected async Task OpenSettingsStore(string settingsJson, string query = "")
    {
        var snapshot = Server.Services.GetRequiredService<WorkbenchSnapshots>().Add($$"""{"settings":{{settingsJson}}}""");

        await OpenPreview("settings", $"?snapshot={snapshot}{query}");
    }

    /// <summary>A field's validation message: the format with the field's name in it.</summary>
    protected static string Message(string format, params string[] arguments) => string.Format(CultureInfo.InvariantCulture, format, arguments);

    /// <summary>Enters a code into a BitOtpInput digit by digit, each in its own box, as a finger wouldn't outrun the server the way a fast keyboard does.</summary>
    protected async Task TypeCode(ILocator group, string code = "123456")
    {
        foreach (var (digit, index) in code.Select((digit, index) => (digit, index)))
        {
            await group.GetByRole(AriaRole.Textbox).Nth(index).FillAsync(digit.ToString());
        }
    }
}
