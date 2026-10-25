using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.Attachments;

/// <summary>
/// The profile picture goes through Bmage's editor before it is uploaded (See <c>ImageEditorService</c>). In WebAssembly
/// the Bmage assemblies are lazy loaded, so opening the editor there is what proves both that they load on demand and
/// that the services the editor injects are registered.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class ImageEditorUITests : AppPageTest
{
    [TestMethod]
    [DataRow(nameof(BlazorWebAppMode.BlazorServer))]
    [DataRow(nameof(BlazorWebAppMode.BlazorWebAssembly))]
    public async Task TheImageEditor_Should_OfferCropAndRotate_AndSaveTheEditedPicture(string blazorMode)
    {
        await using var server = new AppTestServer(Context);
        await server.Build(configureTestConfiguration: configuration => configuration["WebAppRender:BlazorMode"] = blazorMode)
            .Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppAddress, PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SignInPanelUtils.FillCredentials(Page, TestData.DefaultTestEmail, TestData.DefaultTestPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(server.WebAppAddress.ToString());

        await Page.GoToInApp($"{PageUrls.Settings}/{PageUrls.SettingsSections.Profile}");

        var fileChooser = await Page.RunAndWaitForFileChooserAsync(async () =>
            await Page.GetByText(AppStrings.UploadNewImage, new() { Exact = true }).ClickAsync());

        await fileChooser.SetFilesAsync(new FilePayload
        {
            Name = "profile.png",
            MimeType = "image/png",
            Buffer = Convert.FromBase64String(ProfilePicturePngBase64)
        });

        var imageEditor = Page.Locator(".image-editor");
        var save = imageEditor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true });
        await Expect(save).ToBeEnabledAsync();

        await Expect(imageEditor.Locator(".bmg-toolbar")).ToHaveCountAsync(0);
        await Expect(imageEditor.Locator(".bmg-panel")).ToHaveCountAsync(0);
        foreach (var label in (string[])[AppStrings.Crop, AppStrings.RotateLeft, AppStrings.RotateRight, AppStrings.Undo, AppStrings.Redo])
        {
            await Expect(imageEditor.GetByRole(AriaRole.Button, new() { Name = label, Exact = true })).ToHaveCountAsync(1);
        }
        await Expect(imageEditor.GetByRole(AriaRole.Link, new() { Name = AppStrings.ImageEditorMoreFeatures })).ToHaveAttributeAsync("href", "https://bmage.bitplatform.dev/editor");

        await imageEditor.GetByRole(AriaRole.Button, new() { Name = AppStrings.RotateRight, Exact = true }).ClickAsync();
        await Expect(imageEditor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Undo, Exact = true })).ToBeEnabledAsync();

        await save.ClickAsync();

        // Watched from the page rather than the network: in Blazor Server the upload is the server's own request.
        await Expect(imageEditor).ToBeHiddenAsync();
        await Expect(Page.Locator("header .persona img").First).ToHaveAttributeAsync("src", new Regex("/api/v1/Attachment/GetAttachment/.+/UserProfileImageSmall", RegexOptions.IgnoreCase));
    }

    /// <summary>A 320 x 320 PNG: at least the 256 x 256 the small profile picture needs.</summary>
    private const string ProfilePicturePngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAUAAAAFACAIAAABC8jL9AAACtklEQVR42u3TQQkAAAgEwYtjRCMayw7+hIFJsLCpHuCpSAAGBgwMGBgMDBgYMDBgYDAwYGDAwGBgwMCAgQEDg4EBAwMGBgwMBgYMDBgYDAwYGDAwYGAwMGBgwMCAgcHAgIEBA4OBAQMDBgYMDAYGDAwYGAysAhgYMDBgYDAwYGDAwICBwcCAgQEDg4EBAwMGBgwMBgYMDBgYMDAYGDAwYGAwMGBgwMCAgcHAgIEBAwMGBgMDBgYMDAYGDAwYGDAwGBgwMGBgMDBgYMDAgIHBwICBAQMDBgYDAwYGDAwGBgwMGBgwMBgYMDBgYMDAYGDAwICBwcCAgQEDAwYGAwMGBgwMGBgMDBgYMDAYGDAwYGDAwGBgwMCAgcHAgIEBAwMGBgMDBgYMDBgYDAwYGDAwGBgwMGBgwMBgYMDAgIEBA4OBAQMDBgYDAwYGDAwYGAwMGBgwMBhYBTAwYGDAwGBgwMCAgQEDg4EBAwMGBgMDBgYMDBgYDAwYGDAwYGAwMGBgwMBgYMDAgIEBA4OBAQMDBgYMDAYGDAwYGAwMGBgwMGBgMDBgYMDAYGDAwICBAQODgQEDAwYGDAwGBgwMGBgMDBgYMDBgYDAwYGDAwICBwcCAgQEDg4EBAwMGBgwMBgYMDBgYMDAYGDAwYGAwMGBgwMCAgcHAgIEBA4OBAQMDBgYMDAYGDAwYGDAwGBgwMGBgMDBgYMDAgIHBwICBAQMDBgYDAwYGDAwGBgwMGBgwMBgYMDBgYDCwBGBgwMCAgcHAgIEBAwMGBgMDBgYMDAYGDAwYGDAwGBgwMGBgwMBgYMDAgIHBwICBAQMDBgYDAwYGDAwYGAwMGBgwMBgYMDBgYMDAYGDAwICBwcAqgIEBAwMGBgMDBgYMDBgYDAwYGDAwGBgwMGBgwMBgYMDAgIEBA4OBAQMDNwsofaDvEBH6IQAAAABJRU5ErkJggg==";
}
