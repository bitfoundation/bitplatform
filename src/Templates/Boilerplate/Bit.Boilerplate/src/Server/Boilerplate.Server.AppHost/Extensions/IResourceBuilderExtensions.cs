namespace Aspire.Hosting.ApplicationModel;

public static class IResourceBuilderExtensions
{
    /// <summary>
    /// https://github.com/dotnet/aspire/issues/11710
    /// </summary>
    public static IResourceBuilder<PostgresServerResource> WithV18DataVolume(
        this IResourceBuilder<PostgresServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"),
            "/var/lib/postgresql/18/docker", isReadOnly);
    }
}
