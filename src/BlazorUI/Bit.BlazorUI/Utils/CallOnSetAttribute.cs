namespace Bit.BlazorUI;

[AttributeUsage(AttributeTargets.All)]
internal class CallOnSetAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
}
