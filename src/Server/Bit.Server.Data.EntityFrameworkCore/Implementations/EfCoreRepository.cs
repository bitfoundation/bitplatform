using Bit.Data.Contracts;
using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfCoreRepository<TDbContext, TEntity> : EfCoreRepositoryBase<TDbContext, TEntity>
        where TEntity : class, IEntity
        where TDbContext : DbContext
    {
        public override TEntity? GetById(params object[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            return EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys(GetAll(), ids)
                .SingleOrDefault();
        }

        public override async Task<TEntity?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            return await EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys((await GetAllAsync(cancellationToken).ConfigureAwait(false)), ids)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual EfCoreDataProviderSpecificMethodsProvider EfDataProviderSpecificMethodsProvider { get; set; } = default!;
    }

    public class EfCoreRepository<TEntity> : EfCoreRepository<EfCoreDbContextBase, TEntity>, IRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}
