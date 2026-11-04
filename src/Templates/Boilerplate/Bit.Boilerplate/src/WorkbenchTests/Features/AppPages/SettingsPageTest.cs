using Boilerplate.WorkbenchTests.Features.Chrome;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>What the tests of the copy of the app's Settings page share: its sections, and text on an open one.</summary>
public abstract class SettingsPageTest : AppViewPageTest
{
    /// <summary>The button that opens and closes a section.</summary>
    protected ILocator SectionHeader(string title) => View().GetByRole(AriaRole.Heading, new() { Name = title, Exact = true }).GetByRole(AriaRole.Button);

    /// <summary>An open section's content.</summary>
    protected ILocator Section(string title) => View().GetByRole(AriaRole.Region, new() { Name = title, Exact = true });

    protected ILocator Failed(string title) => Section(title).GetByText(WorkbenchStrings.LoadFailed, new() { Exact = true });

    protected ILocator Retry(string title) => Section(title).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosRetry, Exact = true });

    /// <summary>Opens the Settings page on a section, which the Profile section needs no press for, as a new view has it open.</summary>
    protected async Task OpenSection(string title, string query = "")
    {
        await OpenWorkbench("settings", query);

        if (title != AppStrings.ProfileTitle)
        {
            await Press(SectionHeader(title));
        }

        await Expect(Section(title)).ToBeVisibleAsync();
    }

    /// <summary>Opens a preview alone on a snapshot of the Settings store, for what nothing on the page can set.</summary>
    protected async Task OpenSettingsStore(string settingsJson)
    {
        var snapshot = Server.Services.GetRequiredService<WorkbenchSnapshots>().Add($$"""{"settings":{{settingsJson}}}""");

        await OpenPreview("settings", $"?snapshot={snapshot}");
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
