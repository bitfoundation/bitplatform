namespace Bit.Blex;

/// <summary>
/// Marks a backing field as a piece of reactive state. The generator emits a public property
/// (PascalCased, with the leading underscore removed) whose setter flows through the dispatch
/// pipeline so changes are tracked, notified and recorded for time-travel.
/// </summary>
/// <remarks>
/// Change detection uses <see cref="EqualityComparer{T}.Default"/>. For reference types such as
/// <c>List&lt;T&gt;</c> that do not override equality, this is reference equality, so mutating a
/// collection in place will not raise a notification. Assign a new instance (or use an immutable
/// collection) when updating collection or object state.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class BlexStateAttribute : Attribute
{
}
