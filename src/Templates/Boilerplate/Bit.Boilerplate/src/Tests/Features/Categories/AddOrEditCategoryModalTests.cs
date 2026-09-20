using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Shared.Features.Categories;
using Boilerplate.Client.Core.Components.Pages.Categories;

namespace Boilerplate.Tests.Features.Categories;

/// <summary>
/// AddOrEditCategoryModal's NavigationLock refuses a navigation while the form is modified, and otherwise closes the
/// modal. "Modified" is the EditContext's, so a field that commits only on change left an edit that was typed but not
/// yet blurred looking unmodified: the browser's back button closed the modal and dropped the edit. The deployed
/// counterpart is Boilerplate.Tests.E2E's UnsavedCategoryTestsBase.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class AddOrEditCategoryModalTests
{
    [TestMethod]
    public async Task ATypedButUnsavedName_Should_RefuseLeaving_AndKeepTheModalOpen()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var cut = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<AddOrEditCategoryModal>());
        var modal = cut.FindComponent<AddOrEditCategoryModal>();

        await cut.InvokeAsync(() => modal.Instance.ShowModal(new CategoryDto { Id = Guid.NewGuid(), Name = "Tesla", Color = "#FFCD56" }));

        var nameInput = cut.WaitForElement($"input[placeholder='{AppStrings.EnterCategoryName}']", TimeSpan.FromSeconds(10));

        // Input only - no change event - which is all a user who types and then presses back has sent.
        await nameInput.InputAsync(new() { Value = "Tesla (edited)" });

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var before = navigationManager.Uri;

        await cut.InvokeAsync(() => navigationManager.NavigateTo("/"));

        Assert.AreEqual(before, navigationManager.Uri, "The navigation went through while the category had unsaved changes.");

        cut.WaitForAssertion(() => Assert.Contains(AppStrings.EditCategory, cut.Markup,
            "The modal closed on the refused navigation, dropping the typed name."), timeout: TimeSpan.FromSeconds(10));

        Assert.AreEqual("Tesla (edited)", cut.Find($"input[placeholder='{AppStrings.EnterCategoryName}']").GetAttribute("value"));
    }

    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;
}
