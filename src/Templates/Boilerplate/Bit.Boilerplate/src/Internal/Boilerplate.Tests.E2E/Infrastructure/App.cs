namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The demo apps as platform-agnostic identities. Which platforms actually carry an app - and how to reach it there -
/// is the <see cref="IAppOpener"/>s' knowledge; a test written against <see cref="AppsTestBase"/> just names the app
/// and runs wherever it exists.
/// </summary>
public enum App
{
    AdminPanel,
    AdminPanelWasmStandalone,
    Todo,
    TodoAot,
    TodoSmall,
    TodoOffline,
    Sales,
}
