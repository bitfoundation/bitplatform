using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Belemetry.EntityFrameworkCore;

/// <summary>What the Entity Framework Core spans carry, and which commands get one.</summary>
public sealed class BelemetryEntityFrameworkCoreOptions
{
    /// <summary>
    /// Whether the command text is recorded as <c>db.query.text</c>. EF Core parameterizes its queries, so the text
    /// rarely holds data - but raw SQL (<c>ExecuteSqlRaw</c>, <c>FromSqlRaw</c>) can, and the text of every query is
    /// a lot of bytes per span. Defaults to <see langword="false"/>.
    /// </summary>
    public bool CaptureQueryText { get; set; }

    /// <summary>
    /// Decides per command whether it gets a span. Return <see langword="false"/> to skip one - e.g. the commands of a
    /// job store's own <c>DbContext</c>. Null (the default) records every command.
    /// </summary>
    public Func<DbCommand, CommandEventData, bool>? Filter { get; set; }
}
