using System;
using Bit.Model.Contracts;

namespace Bit.Data.Contracts
{
    public interface IEntityWithDefaultGuidKeyRepository<TEntity> : IEntityWithDefaultKeyRepository<TEntity, Guid>
        where TEntity : class, IEntityWithDefaultGuidKey
    {

    }
}
