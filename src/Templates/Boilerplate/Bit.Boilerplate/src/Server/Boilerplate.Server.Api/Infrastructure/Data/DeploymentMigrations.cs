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
    /// The migrations that going back to <paramref name="migration"/> leaves applied. EF Core runs the Down of the applied
    /// migrations after the one it goes back to and of none before it, in the order of their ids: the order the migrations
    /// were added in, which isn't always the order they reached main in.
    /// </summary>
    /// <example>
    /// Two branches off main each add a migration, 20261102_AddInvoices and then 20261103_AddProductTags, and the second
    /// one reaches main first. Production has 20261101_Initial and 20261103_AddProductTags applied.
    /// <list type="bullet">
    /// <item>
    /// The first branch reaches main, and its deployment applies 20261102_AddInvoices and prints
    /// <c>ThisImageKeeps=20261102_AddInvoices</c>. If it fails before its apps start, it goes back to
    /// 20261103_AddProductTags, which undoes nothing, and the pipeline logs:
    /// "Migrations of this version that come before 20261103_AddProductTags stay applied, since going back to it undoes
    /// only those after it: 20261102_AddInvoices. The running version works on them, but undo them by hand if they get in
    /// the way."
    /// </item>
    /// <item>
    /// It succeeds, and Production is then rolled back to the commit before it. That commit's image doesn't have
    /// 20261102_AddInvoices, the last migration both have is 20261103_AddProductTags, and the deployment prints
    /// <c>RunningImageKeeps=20261102_AddInvoices</c>. The running image goes back to 20261103_AddProductTags, which undoes
    /// nothing, and the pipeline logs:
    /// "Migrations this version doesn't have that come before 20261103_AddProductTags stay applied, since going back to it
    /// undoes only those after it: 20261102_AddInvoices. This version works on them, but the next deployment finds them
    /// applied."
    /// </item>
    /// </list>
    /// On Test, the same happens when two branches each add a migration and then merge a newer main that has one after both.
    /// </example>
    private static string Before(IEnumerable<string> migrations, string? migration)
    {
        return migration is null
            ? ""
            : string.Join(", ", migrations.Where(m => string.Compare(m, migration, StringComparison.OrdinalIgnoreCase) < 0));
    }
}
