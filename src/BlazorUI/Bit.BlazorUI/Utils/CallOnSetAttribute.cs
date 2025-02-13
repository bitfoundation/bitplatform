namespace Bit.BlazorUI;

[AttributeUsage(AttributeTargets.All)]
internal class CallOnSetAttribute(string Name) : Attribute
{
    public string Name { get; set; } = Name;
}
