namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Configuration for tests that reach the deployed apps' backing services - the PostgreSQL database and the Azure Blob
/// Storage account. Locally the values live in this project's user secrets (<c>dotnet user-secrets set
/// "ConnectionStrings:postgresdb" "..."</c> in this project's directory); on CI, environment variables
/// (<c>ConnectionStrings__postgresdb</c>) override them.
/// </summary>
public static class TestConfiguration
{
    public static IConfigurationRoot Current { get; } = new ConfigurationBuilder()
        .AddUserSecrets(typeof(TestConfiguration).Assembly)
        .AddEnvironmentVariables()
        .Build();

    public static string GetRequiredConnectionString(string name)
    {
        return Current.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' was found in neither this project's user secrets nor the environment variables.");
    }
}
