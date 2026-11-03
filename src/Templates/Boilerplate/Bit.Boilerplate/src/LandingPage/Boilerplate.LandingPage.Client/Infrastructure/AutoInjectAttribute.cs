// [mirror] AutoInject attribute - keep in sync with:
// - src/Shared/Infrastructure/Attributes/AutoInjectAttribute.cs
namespace Microsoft.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed partial class AutoInjectAttribute : Attribute
{
}
