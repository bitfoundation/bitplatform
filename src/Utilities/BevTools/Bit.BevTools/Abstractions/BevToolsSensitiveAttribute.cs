namespace Bit.BevTools;

/// <summary>Marks a member whose value must never be shown (rendered as «redacted»).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = true)]
public sealed class BevToolsSensitiveAttribute : Attribute
{
}
