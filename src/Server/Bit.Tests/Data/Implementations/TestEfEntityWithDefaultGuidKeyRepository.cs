using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.Contracts;

namespace Bit.Tests.Data.Implementations
{
    public class TestEfEntityWithDefaultGuidKeyRepository<TEntity> : EfEntityWithDefaultGuidKeyRepository<TEntity>
        where TEntity : class, IEntityWithDefaultGuidKey
    {
        public TestEfEntityWithDefaultGuidKeyRepository(TestDbContext context)
            : base(context)
        {

        }

        protected TestEfEntityWithDefaultGuidKeyRepository()
            : base()
        {

        }
    }
}
