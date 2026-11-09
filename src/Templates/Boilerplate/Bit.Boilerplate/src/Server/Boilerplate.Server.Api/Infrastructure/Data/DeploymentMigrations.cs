namespace Boilerplate.Server.Api.Infrastructure.Data;

/// <summary>
/// What a deployment's one-shot <c>migrations</c> service does. It prints where the database stands for the deploy job, which
/// needs that to take the database back when the deployment fails or an older version is deployed (See cd-template.yml).
/// </summary>
public static class DeploymentMigrations
{
    /// <param name="dbContext">The database to migrate.</param>
    /// <param name="targetMigration">
    /// The migration to end on, running the Down of every applied one after it. Null applies all that are pending.
    /// </param>
    public static async Task Run(AppDbContext dbContext, string? targetMigration)
    {
        var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        var ofThisImage = dbContext.Database.GetMigrations().ToArray();

        Console.WriteLine($"LastAppliedMigration={applied.LastOrDefault()}");

        // An older version is being deployed: this image doesn't have the Down of the migrations it never knew.
        if (applied.Except(ofThisImage).Any())
        {
            Console.WriteLine($"LastMigrationOfThisImage={ofThisImage.LastOrDefault()}");
        }

        await dbContext.Database.MigrateAsync(targetMigration);
    }
}
