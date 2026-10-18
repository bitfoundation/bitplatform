using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bsqlite.Data;

/// <summary>A parameter carried to the engine by the transport.</summary>
public sealed class BsqliteParameter : DbParameter
{
    public BsqliteParameter()
    {
    }

    public BsqliteParameter(string name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    public override DbType DbType { get; set; } = DbType.Object;

    public override ParameterDirection Direction
    {
        get => ParameterDirection.Input;
        set
        {
            if (value != ParameterDirection.Input)
            {
                throw new ArgumentException("SQLite parameters are input-only.", nameof(value));
            }
        }
    }

    public override bool IsNullable { get; set; } = true;

    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;

    public override int Size { get; set; }

    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType() => DbType = DbType.Object;
}
