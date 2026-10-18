namespace Boilerplate.Tests.Features.TimeZone;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class TimeZoneSelectionUITests : AppPageTest
{
    [TestMethod]
    public async Task AnonymousUser_Should_PickATimeZone_AndPersistAcrossRefresh()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        await Page.GotoAsync(server.WebAppAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var timeZonePicker = await OpenTimeZonePicker();

        var (searchTerm, timeZoneId) = (await timeZonePicker.InnerTextAsync()).Contains("Tokyo", StringComparison.OrdinalIgnoreCase)
            ? ("Tehran", "Asia/Tehran")
            : ("Tokyo", "Asia/Tokyo");

        await timeZonePicker.ClickAsync();
        await Page.GetByPlaceholder(AppStrings.FindTimeZone).FillAsync(searchTerm);
        await Page.GetByRole(AriaRole.Option, new() { Name = searchTerm }).First.ClickAsync();

        await Page.WaitForFunctionAsync("zone => localStorage.getItem('time-zone') === zone", timeZoneId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        timeZonePicker = await OpenTimeZonePicker();

        await Expect(timeZonePicker).ToContainTextAsync(searchTerm);
    }

    private async Task<ILocator> OpenTimeZonePicker()
    {
        await Page.WaitForBlazorInteractive();
        await Page.Locator(".menu-chevron").ClickAsync();

        var timeZonePicker = Page.Locator(".app-menu-callout").GetByRole(AriaRole.Combobox, new() { Name = AppStrings.TimeZone, Exact = true });
        await Expect(timeZonePicker).ToBeVisibleAsync();

        return timeZonePicker;
    }
}
