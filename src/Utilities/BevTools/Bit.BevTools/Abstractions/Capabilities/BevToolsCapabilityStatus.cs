namespace Bit.BevTools;

/// <summary>Honest availability classification for every piece of information BevTools shows.</summary>
public enum BevToolsCapabilityStatus
{
    Available,
    Partial,
    RequiresInstrumentation,
    RequiresIntegration,
    RequiresRuntimeSupport,
    NotReliable,
    NotAvailable,
}
