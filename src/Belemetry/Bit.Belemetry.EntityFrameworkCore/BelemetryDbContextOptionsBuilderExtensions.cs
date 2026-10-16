using Bit.Belemetry.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore;

/// <summary>Registration of the Belemetry Entity Framework Core instrumentation.</summary>
public static class BelemetryDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Records a span for every command the context executes, on the <c>Bit.Belemetry.EntityFrameworkCore</c>
    /// source, which Belemetry collects by default.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;(options => options
    ///     .UseSqlServer(connectionString)
    ///     .UseBitBelemetry());
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseBitBelemetry(this DbContextOptionsBuilder optionsBuilder, Action<BelemetryEntityFrameworkCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        var options = new BelemetryEntityFrameworkCoreOptions();
        configure?.Invoke(options);
        return optionsBuilder.AddInterceptors(new BelemetryDbCommandInterceptor(options));
    }

    /// <inheritdoc cref="UseBitBelemetry(DbContextOptionsBuilder, Action{BelemetryEntityFrameworkCoreOptions}?)"/>
    public static DbContextOptionsBuilder<TContext> UseBitBelemetry<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, Action<BelemetryEntityFrameworkCoreOptions>? configure = null)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)UseBitBelemetry((DbContextOptionsBuilder)optionsBuilder, configure);
    }
}
