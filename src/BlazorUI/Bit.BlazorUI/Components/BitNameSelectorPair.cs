namespace Bit.BlazorUI;

public class BitNameSelectorPair<TItem, TProp>(string name)
{
    /// <summary>
    /// Custom class property name.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Custom class property selector.
    /// </summary>
    public Func<TItem, TProp?>? Selector { get; set; }
}
