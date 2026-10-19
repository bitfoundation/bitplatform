namespace Bit.Blex;

/// <summary>
/// Marks a partial <c>void</c> method whose name starts with <c>On</c> as an action implementation.
/// The generator emits a public wrapper (with the <c>On</c> prefix stripped) that batches the
/// mutation into a single, named, time-travel-recorded action.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class BlexActionAttribute : Attribute
{
    /// <summary>
    /// Optional explicit action name. When it is a valid C# identifier it is used as the public
    /// wrapper method name and the display label. When it contains characters that aren't valid in
    /// an identifier (such as spaces) it is treated purely as the display label, and the wrapper
    /// method name is derived from the implementation method by stripping its <c>On</c> prefix.
    /// </summary>
    public string? Name { get; set; }
}
