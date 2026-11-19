using Bunit;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Shared.Features.Tenants.Dtos;
using Boilerplate.Client.Core.Components.Pages.Tenants;

namespace Boilerplate.Tests.Features.Tenants;

/// <summary>
/// The list the not-authorized page shows a user without a selected tenant (See NotAuthorizedPageTests for the page
/// itself, on the real api). Rendered here on tenants of the test's own, since what decides its layout is the membership
/// state of each one, and whether the user is a global admin.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class TenantSwitchListTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    private static readonly TenantDto alpha = new() { Id = Guid.NewGuid(), Name = "alpha", Title = "Alpha", CurrentUserMembershipStatus = TenantMembershipStatus.Member };
    private static readonly TenantDto beta = new() { Id = Guid.NewGuid(), Name = "beta", CurrentUserMembershipStatus = TenantMembershipStatus.Invited };
    private static readonly TenantDto gamma = new() { Id = Guid.NewGuid(), Name = "gamma", Title = "Gamma", CurrentUserMembershipStatus = TenantMembershipStatus.Member };

    [TestMethod]
    public async Task TenantSwitchList_Should_ListInvitationsFirst_WithAcceptOnTheirOwnRow()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        // In the api's order, by name: the invitation is in between the tenants she belongs to.
        var cut = Render(ctx, [alpha, beta, gamma], isGlobalAdmin: false, onSwitch: _ => Task.CompletedTask);

        // The headings and the buttons, in the order they are on the page.
        var order = cut.FindAll("h3, button").Select(element => element.TextContent.Trim()).ToList();
        string[] expected = [AppStrings.PendingInvitations, AppStrings.AcceptInvitation, AppStrings.SwitchTenant, "Alpha", "Gamma"];

        Assert.HasCount(expected.Length, order, string.Join(" | ", order));

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Contains(expected[i], order[i], string.Join(" | ", order));
        }

        var accept = FindButton(cut, AppStrings.AcceptInvitation);

        // Beta has no title, so it's listed by its name.
        Assert.Contains("beta", accept.ParentElement!.TextContent, "The invitation's Accept has to be on that tenant's own row.");
        Assert.DoesNotContain("Alpha", accept.ParentElement.TextContent);
    }

    /// <summary>
    /// A global admin is listed every active tenant and switches into any of them, as she does from the account menu and
    /// the Manage my tenants page (See ManageMyTenantsPage.IsPendingInvitation), so nothing is offered to her as an invitation.
    /// </summary>
    [TestMethod]
    public async Task TenantSwitchList_Should_OfferNoInvitation_ToAGlobalAdmin()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var none = new TenantDto { Id = Guid.NewGuid(), Name = "delta", CurrentUserMembershipStatus = TenantMembershipStatus.None };

        var cut = Render(ctx, [alpha, beta, none], isGlobalAdmin: true, onSwitch: _ => Task.CompletedTask);

        Assert.DoesNotContain(AppStrings.PendingInvitations, cut.Markup);
        Assert.DoesNotContain(AppStrings.AcceptInvitation, cut.Markup);

        foreach (var name in new[] { "Alpha", "beta", "delta" })
        {
            Assert.IsNotNull(FindButton(cut, name), $"{name} has to be offered as a tenant to switch into.");
        }
    }

    [TestMethod]
    public async Task TenantSwitchList_Should_ShowTheClickedTenantBusy_AndKeepTheOthersDisabled_UntilTheSwitchEnds()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var switching = new TaskCompletionSource();
        TenantDto? switchedTo = null;

        var cut = Render(ctx, [alpha, beta, gamma], isGlobalAdmin: false, onSwitch: tenant =>
        {
            switchedTo = tenant;
            return switching.Task;
        });

        var click = FindButton(cut, "Alpha").ClickAsync(new());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", FindButton(cut, "Alpha").GetAttribute("aria-busy"));
            Assert.AreEqual("true", FindButton(cut, "Gamma").GetAttribute("aria-disabled"));
            Assert.AreEqual("true", FindButton(cut, AppStrings.AcceptInvitation).GetAttribute("aria-disabled"));
        }, timeout: TimeSpan.FromSeconds(10));

        Assert.IsNotNull(switchedTo);
        Assert.AreEqual(alpha.Id, switchedTo.Id);

        switching.SetResult();
        await click;

        cut.WaitForAssertion(() =>
        {
            Assert.IsNull(FindButton(cut, "Alpha").GetAttribute("aria-busy"));
            Assert.IsNull(FindButton(cut, "Gamma").GetAttribute("aria-disabled"));
        }, timeout: TimeSpan.FromSeconds(10));
    }

    private static IRenderedComponent<CascadingAuthenticationState> Render(BunitContext ctx, IReadOnlyList<TenantDto> tenants, bool isGlobalAdmin, Func<TenantDto, Task> onSwitch)
    {
        return ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<TenantSwitchList>(list => list
                .Add(p => p.Tenants, tenants)
                .Add(p => p.IsGlobalAdmin, isGlobalAdmin)
                .Add(p => p.OnSwitch, onSwitch)));
    }

    private static IElement FindButton(IRenderedComponent<CascadingAuthenticationState> cut, string text)
    {
        return cut.FindAll("button").Single(button => button.TextContent.Contains(text, StringComparison.Ordinal));
    }
}
