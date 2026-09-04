namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The demo apps as platform-agnostic identities; which platform carries an app, and how to reach it there, is the
/// <see cref="IAppOpener"/>s' knowledge.
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
