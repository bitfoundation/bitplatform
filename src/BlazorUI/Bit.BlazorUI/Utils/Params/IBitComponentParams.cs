namespace Bit.BlazorUI;

/// <summary>
/// Defines the contract for parameters used by a bit BlazorUI component, which a <see cref="BitParams"/> provides to it.
/// </summary>
public interface IBitComponentParams
{
    /// <summary>
    /// The cascading name the component reads this params object by, which is its ParamName constant.
    /// </summary>
    string Name { get; }
}
