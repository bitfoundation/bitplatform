namespace Bit.Bsqlite;

/// <summary>How <see cref="IBsqliteDbContextFactory{TContext}"/> prepares the schema the first time a session opens.</summary>
public enum BsqliteSchemaStrategy
{
    /// <summary>Do nothing; the application manages the schema itself.</summary>
    None,

    /// <summary>Call <c>Database.MigrateAsync()</c> once per session, applying pending migrations.</summary>
    Migrate,

    /// <summary>Call <c>Database.EnsureCreatedAsync()</c> once per session.</summary>
    EnsureCreated,
}
