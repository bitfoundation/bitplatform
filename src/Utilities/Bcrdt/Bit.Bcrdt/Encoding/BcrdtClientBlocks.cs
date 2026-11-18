namespace Bit.Bcrdt;

/// <summary>Blocks read from an update for one client, plus how far integration has consumed them.</summary>
internal sealed class BcrdtClientBlocks(List<BcrdtAbstractStruct> blocks)
{
    public readonly List<BcrdtAbstractStruct> Blocks = blocks;
    public int Position;

    public bool Exhausted => Position >= Blocks.Count;
}
