using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Server.Api.Data.Configurations;

// Cannot write DateTimeOffset with Offset=XX:XX:XX to PostgreSQL type 'timestamp with time zone', only offset 0 (UTC) is supported.
public partial class PostgresDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public PostgresDateTimeOffsetConverter()
        : base(
            d => d.ToUniversalTime(),
            d => d.ToUniversalTime())
    {
    }
}

public partial class NullablePostgresDateTimeOffsetConverter : ValueConverter<DateTimeOffset?, DateTimeOffset?>
{
    public NullablePostgresDateTimeOffsetConverter()
        : base(
            d => d == null ? null : d.Value.ToUniversalTime(),
            d => d == null ? null : d.Value.ToUniversalTime())
    {
    }
}
