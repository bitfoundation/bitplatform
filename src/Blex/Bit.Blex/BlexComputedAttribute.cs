namespace Bit.Blex;

/// <summary>
/// Marks a parameterless method (named <c>ComputeXxx</c> or <c>GetXxx</c>) as derived/computed state.
/// The generator emits a memoized public property <c>Xxx</c> that recomputes lazily after any state change.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class BlexComputedAttribute : Attribute
{
}
