namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The sign-in state a view shows: signed out, one account, or several accounts on the same device.</summary>
public enum WorkbenchAuthState
{
    SignedOut,
    SignedIn,
    MultiAccount
}
