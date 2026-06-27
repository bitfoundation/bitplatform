namespace Bit.Butil.E2ETests.Infrastructure;

/// <summary>Drives the deterministic <c>/e2e-observers</c> harness page.</summary>
public abstract class ButilObserversPageTest : ButilHarnessTestBase
{
    protected override string HarnessRoute => "/e2e-observers";
}
