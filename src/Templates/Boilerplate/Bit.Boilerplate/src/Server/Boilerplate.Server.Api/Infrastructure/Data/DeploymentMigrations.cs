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
        var unknown = applied.Except(ofThisImage).ToArray();

        // Where the deploy job takes the database back to when this deployment fails before its apps start.
        var undoTo = applied.LastOrDefault();
        Console.WriteLine($"LastAppliedMigration={undoTo}");

        // An older commit, or another branch on Test, is being deployed: only the image running now has the Down of the
        // applied migrations this one doesn't know, and it can take the database back only to a migration both images know.
        // EF Core skips the applied migrations an image doesn't know, so each image's MigrateAsync leaves the other's alone.
        if (unknown.Any())
        {
            undoTo = applied.Intersect(ofThisImage).LastOrDefault();
            Console.WriteLine($"LastSharedMigration={undoTo}");
            Console.WriteLine($"RunningImageKeeps={Before(unknown, undoTo)}");
        }

        Console.WriteLine($"ThisImageKeeps={Before(ofThisImage.Except(applied), undoTo)}");

        await dbContext.Database.MigrateAsync(targetMigration);
    }

    /// <summary>
    /// The migrations that going back to <paramref name="migration"/> leaves applied: EF Core runs the Down of the applied
    /// migrations after the one it goes back to and of none before it. One image's migrations come before a migration both
    /// know when, for example, two branches each added one and then merged a newer main.
    /// </summary>
    private static string Before(IEnumerable<string> migrations, string? migration)
    {
        return migration is null
            ? ""
            : string.Join(' ', migrations.Where(m => string.Compare(m, migration, StringComparison.OrdinalIgnoreCase) < 0));
    }
}
