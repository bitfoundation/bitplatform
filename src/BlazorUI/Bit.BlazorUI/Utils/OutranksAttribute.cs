namespace Bit.BlazorUI;

/// <summary>
/// Marks a parameter that wins over another one the component also shows, in a way its name does not tell: a template
/// or a text drawn in place of an icon, an icon that replaces another while the component is in a state. A params
/// object does not supply it to a component that set the other one itself, since it would replace what the component
/// asked for.
/// </summary>
/// <remarks>
/// An icon taken through an XIcon, an XIconName, an XIconUrl, an XIconTemplate, the XIcons / XIconNames maps or a
/// GetXIcon selector is one setting by its names alone (<see cref="BitCascadeMap"/>), and needs none of this. The
/// attribute reaches every parameter of the setting it is put on and of the one it names, so it is put on one
/// parameter of a setting only: OnIcon outranking Icon covers OnIconName and IconName as well.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
internal sealed class OutranksAttribute(string parameter) : Attribute
{
    /// <summary>
    /// The parameter this one is shown in place of.
    /// </summary>
    public string Parameter { get; } = parameter;
}
