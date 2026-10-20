#if !NET10_0_OR_GREATER
namespace Microsoft.Extensions.Logging;

/// <summary>
/// Before .NET 10 the provider alias attribute lives in Microsoft.Extensions.Logging, which this package does not
/// reference (only the abstractions come with Microsoft.AspNetCore.Components.Web). The logger factory matches the
/// attribute by its full type name, not by type identity, so this internal copy makes the
/// <c>"Logging": { "Belemetry": ... }</c> configuration section work on net8.0 and net9.0 too.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class ProviderAliasAttribute(string alias) : Attribute
{
    public string Alias { get; } = alias;
}
#endif
