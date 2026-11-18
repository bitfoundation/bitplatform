using Bit.Blow;

namespace Bit.Bcrdt;

/// <summary>Another participant in a shared graph, with what they published about it.</summary>
/// <param name="Collaborator">Who they are, as presence knows them: name, colour, when they were last heard from.</param>
/// <param name="Cursor">Where their pointer is, in flow coordinates, or null when they have not said.</param>
/// <param name="SelectedNodes">The ids of the nodes they have selected that this replica has.</param>
/// <param name="SelectedEdges">The ids of the edges they have selected that this replica has.</param>
public sealed record BcrdtBlowParticipant(
    BcrdtCollaborator Collaborator,
    BlowXYPosition? Cursor,
    IReadOnlyList<string> SelectedNodes,
    IReadOnlyList<string> SelectedEdges);
