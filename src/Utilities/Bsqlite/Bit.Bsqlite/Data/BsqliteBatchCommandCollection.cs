using System.Data.Common;

namespace Bit.Bsqlite;

/// <summary>The commands of a <see cref="BsqliteBatch"/>.</summary>
public sealed class BsqliteBatchCommandCollection : DbBatchCommandCollection
{
    private readonly List<BsqliteBatchCommand> _commands = [];

    public override int Count => _commands.Count;

    public override bool IsReadOnly => false;

    public new BsqliteBatchCommand this[int index]
    {
        get => _commands[index];
        set => _commands[index] = value;
    }

    public override void Add(DbBatchCommand item) => _commands.Add(Cast(item));

    public override void Clear() => _commands.Clear();

    public override bool Contains(DbBatchCommand item) => _commands.Contains(Cast(item));

    public override void CopyTo(DbBatchCommand[] array, int arrayIndex) => ((ICollection<DbBatchCommand>)_commands.ToList<DbBatchCommand>()).CopyTo(array, arrayIndex);

    public override IEnumerator<DbBatchCommand> GetEnumerator() => _commands.GetEnumerator();

    public override int IndexOf(DbBatchCommand item) => _commands.IndexOf(Cast(item));

    public override void Insert(int index, DbBatchCommand item) => _commands.Insert(index, Cast(item));

    public override bool Remove(DbBatchCommand item) => _commands.Remove(Cast(item));

    public override void RemoveAt(int index) => _commands.RemoveAt(index);

    protected override DbBatchCommand GetBatchCommand(int index) => _commands[index];

    protected override void SetBatchCommand(int index, DbBatchCommand batchCommand)
        => _commands[index] = Cast(batchCommand);

    private static BsqliteBatchCommand Cast(DbBatchCommand command)
        => command as BsqliteBatchCommand
            ?? throw new InvalidCastException(
                $"Expected {nameof(BsqliteBatchCommand)} but received {command?.GetType().Name ?? "null"}.");
}
