namespace Bit.BlazorUI;

/// <summary>
/// The outline of the coin of a BitPersona.
/// </summary>
public enum BitPersonaShape
{
    /// <summary>
    /// A circle, which is the shape a picture of a person is shown in.
    /// </summary>
    Circular,

    /// <summary>
    /// A square with the rounded corners of a control, which is the shape most design systems reserve
    /// for entities - teams, rooms, service accounts - rather than people.
    /// </summary>
    Rounded,

    /// <summary>
    /// A square with sharp corners, for a tile or a logo that has to fill the coin edge to edge.
    /// </summary>
    Square
}
