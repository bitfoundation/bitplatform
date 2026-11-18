
namespace Bit.Bink;

/// <summary>Who caused a document change. History records local changes only; collaboration forwards local and undo/redo changes.</summary>
public enum BinkChangeOrigin : byte
{
    /// <summary>A tool or API call in this session.</summary>
    Local = 0,
    /// <summary>An undo of a previous local change.</summary>
    Undo = 1,
    /// <summary>A redo of a previously undone change.</summary>
    Redo = 2,
    /// <summary>A change received from another collaborator.</summary>
    Remote = 3,
    /// <summary>The document was loaded or reset.</summary>
    Load = 4,
}
