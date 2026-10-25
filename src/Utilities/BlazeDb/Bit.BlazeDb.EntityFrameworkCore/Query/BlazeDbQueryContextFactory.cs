using Microsoft.EntityFrameworkCore.Query;

namespace Bit.BlazeDb.EntityFrameworkCore.Query;

/// <summary>
/// Supplies the query context EF expects. The provider's own compiler does the work, so the
/// context carries nothing provider-specific.
/// </summary>
internal sealed class BlazeDbQueryContextFactory : IQueryContextFactory
{
    private readonly QueryContextDependencies _dependencies;

    public BlazeDbQueryContextFactory(QueryContextDependencies dependencies) => _dependencies = dependencies;

    public QueryContext Create() => new BlazeDbQueryContext(_dependencies);

    private sealed class BlazeDbQueryContext : QueryContext
    {
        public BlazeDbQueryContext(QueryContextDependencies dependencies) : base(dependencies)
        {
        }
    }
}
