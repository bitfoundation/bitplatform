namespace Bit.BlazorUI;

/// <summary>
/// A helper class to ease the using of a list of the BitCascadingValue.
/// </summary>
public class BitCascadingValueList : List<BitCascadingValue>
{

#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
    public new void Add<T>(T value, string? name = null, bool isFixed = false) => base.Add(BitCascadingValue.From(value, name, isFixed));
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required

}
