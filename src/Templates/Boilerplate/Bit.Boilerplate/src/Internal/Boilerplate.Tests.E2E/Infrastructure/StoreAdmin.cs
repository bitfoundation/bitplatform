namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The seeded t-admin of the default store tenant - the tenant whose catalogue the Sales demo shows. See
/// UserConfiguration. The deployment has two factor authentication turned on for it, so signing in takes a mailed code.
/// </summary>
public static class StoreAdmin
{
    public const string Email = "store-admin@bitplatform.dev";
    public const string Password = "123456";
}
