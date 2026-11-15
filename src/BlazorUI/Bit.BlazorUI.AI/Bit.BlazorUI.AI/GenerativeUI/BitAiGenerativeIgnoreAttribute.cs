namespace Bit.BlazorUI;

/// <summary>
/// Excludes a component parameter, or a property of a parameter's type, from the set a model may
/// set and from the schema the model is shown.
/// </summary>
/// <remarks>
/// For parameters the application sets itself, such as a service key or a callback-like
/// identifier, which are not dangerous but which a model has no business choosing.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class BitAiGenerativeIgnoreAttribute : Attribute
{
}
