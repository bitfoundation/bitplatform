using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.Contracts;

namespace Bit.Tests.Data.Implementations
{
    public class TestEfRepository<TEntity> : EfCoreRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}
