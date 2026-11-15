using System.Numerics;

namespace Bit.Bcrdt.Bink;

/// <summary>Another participant in a shared drawing, with what they published about it.</summary>
/// <param name="Collaborator">Who they are, as presence knows them: name, colour, when they were last heard from.</param>
/// <param name="Cursor">Where their pointer is, in document coordinates, or null when they have not said.</param>
/// <param name="Selection">The elements they have selected that this replica has.</param>
public sealed record BcrdtBinkParticipant(BcrdtCollaborator Collaborator, Vector2? Cursor, IReadOnlyList<Guid> Selection);
