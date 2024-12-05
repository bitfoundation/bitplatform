using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Besql;

public class BesqlDbContextInterceptor(IBesqlStorage storage) : ISaveChangesInterceptor
{
    public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken)
    {
        var fileName = eventData.Context!.Database.GetDbConnection().DataSource;

        await storage.Persist(fileName);

        return result;
    }
}
