//+:cnd:noEmit
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Client.Core.Components.Pages.Products;

namespace Boilerplate.Tests.Features.Products;

/// <summary>
/// Two failures of this form that produce <b>no observable signal at all</b>, which is what makes them worth a test
/// rather than a look.
/// <list type="number">
/// <item><b>A validation error on a field with no <c>&lt;ValidationMessage&gt;</c>.</b>
/// <c>AppDataAnnotationsValidator</c> writes every failure into a <c>ValidationMessageStore</c> and renders nothing
/// itself; the page has to supply a message component per field. <c>DescriptionHTML</c> had none and the form had no
/// <c>&lt;ValidationSummary&gt;</c>, so an over-long description made <c>EditContext.Validate()</c> return false,
/// <c>OnValidSubmit</c> never fired, <c>Save</c> never ran, and - because <c>isSaving</c> is assigned inside
/// <c>Save</c> - the button did not even enter its loading state. Clicking Save did nothing whatsoever, forever.</item>
/// <item><b>A failed load falling through to the form.</b> <c>OnInitAsync</c> had no <c>catch</c>, so a transient
/// failure left <c>product</c> as the field initializer's throwaway <c>new() { Id = Guid.CreateSequentialGuid() }</c>
/// while <c>isLoading</c> went false in the <c>finally</c>. The admin got a blank but fully editable form over an Id
/// that matches no row, and every Save came back "Product could not be found".</item>
/// </list>
/// <para>
/// bUnit rather than an API test: neither defect is on the wire. The first is about which components the form renders,
/// the second about which branch the page takes after an exception - both are render-tree facts.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class AddOrEditProductPageTests
{
    // Seeded tenant-admin of the fallback tenant; holds PRIVILEGED_ACCESS, a selected tenant and ProductCatalog_Manage,
    // which is exactly what ProductController and CategoryController demand. See UserConfiguration.
    private const string TenantAdminEmail = "store-admin@bitplatform.dev";
    private const string Password = "123456";

    /// <summary>
    /// BP-518. Asserts the observable consequence - a rendered message - rather than the presence of a particular
    /// element, so it stays true if the fix is expressed as a <c>ValidationSummary</c> instead of a per-field message.
    /// </summary>
    [TestMethod]
    public async Task OverLongDescription_Should_RenderAVisibleValidationMessage()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();
        await SignIn(ctx);

        var cut = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<AddOrEditProductPage>());

        // Add mode still loads the category list, so wait for the form rather than assuming it is up.
        cut.WaitForAssertion(() => Assert.IsNotEmpty(cut.FindComponents<EditForm>()), timeout: TimeSpan.FromSeconds(30));

        var editContext = cut.FindComponent<EditForm>().Instance.EditContext!;
        var product = (ProductDto)editContext.Model;

        // Over the [MaxLength(4096)] on ProductDto.DescriptionHTML. Set through the model rather than through the
        // editor because BitRichTextEditor is JS-backed and this defect has nothing to do with the editor.
        product.Name = "a valid name";
        product.DescriptionHTML = new string('x', 5000);

        // Through the renderer's dispatcher: AppDataAnnotationsValidator calls StateHasChanged from
        // OnValidationRequested, which is exactly the re-render this test needs to observe.
        var isValid = await cut.InvokeAsync(editContext.Validate);

        Assert.IsFalse(isValid, "A 5000 character DescriptionHTML must fail the [MaxLength(4096)] on ProductDto - " +
                                "if it does not, this test is no longer exercising anything.");

        var messages = editContext.GetValidationMessages(editContext.Field(nameof(ProductDto.DescriptionHTML))).ToArray();

        Assert.IsNotEmpty(messages,
            "The validator did not record a message for DescriptionHTML, so the rest of this test cannot mean anything.");

        // WaitForAssertion, not a bare Assert: NotifyValidationStateChanged queues the message components' re-render,
        // so reading cut.Markup on the next line races it. The first version of this test did exactly that and only
        // passed because [Retry] hid it.
        cut.WaitForAssertion(() => Assert.Contains(messages[0], cut.Markup,
            "The validation failure that silently blocks Save is not rendered anywhere on the form. " +
            "AppDataAnnotationsValidator only writes to a ValidationMessageStore - every validated field needs a " +
            "<ValidationMessage For=...>, or the form needs a <ValidationSummary/>, or the user sees nothing at all " +
            $"when they press Save. Markup was:{Environment.NewLine}{cut.Markup}"), timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// BP-523. A product id that matches no row makes <c>ProductController.Get</c> throw
    /// <c>ResourceNotFoundException</c>, which is the same code path as a transient 502 - what matters is that
    /// <c>OnInitAsync</c> threw, not why.
    /// </summary>
    [TestMethod]
    public async Task AFailedLoad_Should_NotFallThroughToAnEditableForm()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();
        await SignIn(ctx);

        var cut = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<AddOrEditProductPage>(
            page => page.Add(x => x.Id, Guid.NewGuid())));

        // Positively assert the error state rather than the absence of the spinner: an "it is not still loading"
        // assertion passes vacuously while the page sits in ANY other state, which is how the first version of this
        // test passed with its own fix reverted.
        cut.WaitForAssertion(() => Assert.Contains("could not be loaded", cut.Markup), timeout: TimeSpan.FromSeconds(30));

        Assert.IsEmpty(cut.FindComponents<EditForm>(),
            "The product failed to load, yet the page rendered the edit form anyway. It is bound to a throwaway " +
            "client-side Guid that matches no row, so the admin can fill it in and every Save will fail with " +
            $"\"Product could not be found\". Markup was:{Environment.NewLine}{cut.Markup}");
    }

    /// <summary>
    /// BP-536, <b>found but not fixed</b>. Every <c>[MaxLength]</c> in the template passes
    /// <c>ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength)</c>, and that resx key holds .NET's
    /// message for a <i>misconfigured attribute</i> - "MaxLengthAttribute must have a Length value that is greater
    /// than zero..." - not for a value being too long. It has no <c>{0}</c>/<c>{1}</c> placeholders either, so neither
    /// the field name nor the limit is shown. This ships and is reachable today in all ten cultures.
    /// <para>
    /// <b>Ignored because the fix is blocked, not because the assertion is wrong.</b> The correct key is the
    /// framework's own <c>MaxLengthAttribute_ValidationError</c>, which does not exist in any of the ten .resx files,
    /// and an attribute's <c>ErrorMessage</c> cannot use the <c>IStringLocalizer</c> literal indexer that AGENTS.md
    /// mandates instead of new resx keys. <b>Unblocked by:</b> the maintainer deciding this is worth a resx addition
    /// (or a different mechanism for validation-attribute messages). Delete the [Ignore] once it lands.
    /// </para>
    /// </summary>
    [TestMethod, Ignore("BP-536 is open: fixing it needs a new AppStrings key, which AGENTS.md forbids outside an explicit translation pass.")]
    public async Task ATooLongValue_Should_NotBeReportedAsAMisconfiguredAttribute()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();
        await SignIn(ctx);

        var cut = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<AddOrEditProductPage>());

        cut.WaitForAssertion(() => Assert.IsNotEmpty(cut.FindComponents<EditForm>()), timeout: TimeSpan.FromSeconds(30));

        var editContext = cut.FindComponent<EditForm>().Instance.EditContext!;
        var product = (ProductDto)editContext.Model;

        product.Name = new string('x', 65); // over the [MaxLength(64)] on ProductDto.Name

        await cut.InvokeAsync(editContext.Validate);

        var message = editContext.GetValidationMessages(editContext.Field(nameof(ProductDto.Name))).First();

        Assert.DoesNotContain("must have a Length value", message,
            "The user typed a name that is too long and was told that a developer configured the attribute wrongly. " +
            "MaxLengthAttribute_InvalidMaxLength is .NET's message for a bad attribute argument; the message for a " +
            "value that exceeds the limit is MaxLengthAttribute_ValidationError.");
    }

    /// <summary>
    /// Signs the tenant-admin in inside the bUnit container, so every typed API client the page resolves calls the
    /// server as her.
    /// </summary>
    private async Task SignIn(BunitContext ctx)
    {
        var requiresTwoFactor = await ctx.Services.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TenantAdminEmail,
            Password = Password
        }, TestContext.CancellationToken);

        Assert.IsFalse(requiresTwoFactor, $"'{TenantAdminEmail}' is not expected to have two factor authentication enabled.");
    }

    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;
}
