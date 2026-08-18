namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>Drives the deterministic <c>/e2e</c> harness page.</summary>
public abstract class ButilPageTest : ButilHarnessTestBase
{
    protected override string HarnessRoute => "/e2e";
}
