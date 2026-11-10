namespace Boilerplate.Server.Api.Infrastructure.Data;

/// <summary>
/// What a deployment's one-shot <c>migrations</c> service does. It prints where the database stands for the deploy job, which
/// needs that to take the database back when the deployment fails, or when an older commit or another branch is deployed
/// (See cd-template.yml).
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

        // An older commit, or another branch on Test, is being deployed: only the image running now has the Down of the
        // applied migrations this one doesn't know, and it can take the database back only to a migration both images know.
        // EF Core skips the applied migrations an image doesn't know, so each image's MigrateAsync leaves the other's alone.
        if (applied.Except(ofThisImage).Any())
        {
            Console.WriteLine($"LastSharedMigration={applied.Intersect(ofThisImage).LastOrDefault()}");
        }

        await dbContext.Database.MigrateAsync(targetMigration);
    }
}
