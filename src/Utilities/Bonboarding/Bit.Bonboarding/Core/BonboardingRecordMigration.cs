using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding;

/// <summary>
/// Reads a stored record the way the current version of its tour sees it: the record itself when the versions agree,
/// the tour's <see cref="BonboardingTourDefinition.MigrateRecord"/> result when they do not, and nothing otherwise
/// (which is what a version bump has always meant).
/// </summary>
internal static class BonboardingRecordMigration
{
    public static BonboardingRecord? Apply(BonboardingTourDefinition tour, BonboardingRecord? record, ILogger logger)
    {
        if (record is null || record.Version == tour.Version) return record;
        if (tour.MigrateRecord is not { } migrate) return null;

        try
        {
            return migrate(record) is { } migrated ? migrated with { TourId = tour.Id, Version = tour.Version } : null;
        }
        catch (Exception ex)
        {
            // Application code, run while a tour starts: a broken migration must not stop the tour, and discarding
            // the record is exactly what would have happened without one.
            logger.LogWarning(ex, "BonboardingService could not migrate the record of tour {TourId} from version {From} to {To}; it is discarded.",
                tour.Id, record.Version, tour.Version);
            return null;
        }
    }
}
