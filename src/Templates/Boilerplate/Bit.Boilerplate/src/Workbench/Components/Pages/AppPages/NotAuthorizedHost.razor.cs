using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Workbench.Components.Pages.AppPages;

public partial class NotAuthorizedHost
{
    public const string PageKey = "not-authorized";
    public const string CaseSwitch = "case";

    private string pageCase = default!;
    private Task<AuthenticationState> authenticationState = default!;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        pageCase = WorkbenchPageCatalog.Find(PageKey)!.Switches.Single(s => s.Key == CaseSwitch).Resolve(Scenario).Value;

        // Signed out in its own case only, and signed in as the view's user in the others.
        var user = pageCase is NotAuthorizedPage.SignedOutCase
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity(WorkbenchClaims.Of(Scenario with { Auth = WorkbenchAuthState.SignedIn }), authenticationType: "Workbench"));

        authenticationState = Task.FromResult(new AuthenticationState(user));
    }
}
