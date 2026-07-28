namespace Bit.Brouter;

/// <summary>Controls scroll behavior after a successful navigation.</summary>
public enum BrouterScrollMode
{
    /// <summary>Do not change scroll position automatically.</summary>
    None = 0,

    /// <summary>Scroll the window to the top after navigation completes.</summary>
    ToTop = 1
}
