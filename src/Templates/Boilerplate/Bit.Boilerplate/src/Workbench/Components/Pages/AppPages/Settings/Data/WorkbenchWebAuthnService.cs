using System.Text.Json;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The stand-in for the app's <see cref="IWebAuthnService"/>: the view's device has a passkey ceremony that always succeeds
/// at once, without the browser's own prompt, and remembers whether the view's account has a passkey on it (See
/// <see cref="SettingsSampleAccount.HasPasskey"/>), so turning passwordless sign-in on and off, and confirming it's you with a
/// passkey, work in a view and a screenshot.
/// </summary>
public sealed class WorkbenchWebAuthnService(SettingsSampleAccount account) : IWebAuthnService
{
    public ValueTask<bool> IsWebAuthnAvailable() => ValueTask.FromResult(true);

    public ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options) => ValueTask.FromResult(JsonSerializer.SerializeToElement(new { id = "workbench", type = "public-key" }));

    public ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options) => ValueTask.FromResult(JsonSerializer.SerializeToElement(new { id = "workbench", type = "public-key" }));

    public ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null) => ValueTask.FromResult(account.HasPasskey && (userId is null || userId == account.User.Id));

    public ValueTask<Guid[]> GetWebAuthnConfiguredUserIds() => ValueTask.FromResult<Guid[]>(account.HasPasskey ? [account.User.Id] : []);

    public ValueTask SetWebAuthnConfiguredUserId(Guid userId)
    {
        account.ChangePasskey(true);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId = null)
    {
        account.ChangePasskey(false);
        return ValueTask.CompletedTask;
    }
}
