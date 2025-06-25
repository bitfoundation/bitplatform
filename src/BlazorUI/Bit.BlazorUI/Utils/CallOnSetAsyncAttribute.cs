namespace Bit.BlazorUI;

[AttributeUsage(AttributeTargets.All)]
internal class CallOnSetAsyncAttribute(string name) : Attribute
{
    public string Name { get; set; } = name;
}
