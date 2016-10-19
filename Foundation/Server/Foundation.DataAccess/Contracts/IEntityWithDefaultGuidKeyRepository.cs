using Foundation.Model.Contracts;
using System;

namespace Foundation.DataAccess.Contracts
{
    public interface IEntityWithDefaultGuidKeyRepository<TEntity> : IEntityWithDefaultKeyRepository<TEntity, Guid>
        where TEntity : class, IEntityWithDefaultGuidKey
    {

    }
}
