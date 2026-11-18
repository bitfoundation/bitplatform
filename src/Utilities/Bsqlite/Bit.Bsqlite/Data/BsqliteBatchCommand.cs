using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bsqlite;

/// <summary>One command inside a <see cref="BsqliteBatch"/>.</summary>
public sealed class BsqliteBatchCommand : DbBatchCommand
{
    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override CommandType CommandType
    {
        get => CommandType.Text;
        set
        {
            if (value != CommandType.Text)
            {
                throw new ArgumentException(
                    $"SQLite has no stored procedures or table-direct commands; only {nameof(System.Data.CommandType.Text)} is supported.",
                    nameof(value));
            }
        }
    }

    private int _recordsAffected;

    public override int RecordsAffected => _recordsAffected;

    internal void SetRecordsAffected(int value) => _recordsAffected = value;

    /// <summary>The parameters, typed.</summary>
    public new BsqliteParameterCollection Parameters { get; } = [];

    protected override DbParameterCollection DbParameterCollection => Parameters;

    public override bool CanCreateParameter => true;

    public override DbParameter CreateParameter() => new BsqliteParameter();

    internal BsqliteCommandRequest ToRequest(BsqliteResultKind resultKind, int? rowLimit)
    {
        var parameters = Parameters.Count == 0 ? [] : new BsqliteParameterValue[Parameters.Count];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = Parameters[i];
            parameters[i] = new BsqliteParameterValue(
                parameter.ParameterName,
                BsqliteParameterBinding.ToTransportValue(parameter.Value));
        }

        return new BsqliteCommandRequest
        {
            CommandText = CommandText,
            Parameters = parameters,
            ResultKind = resultKind,
            RowLimit = rowLimit,
        };
    }
}
