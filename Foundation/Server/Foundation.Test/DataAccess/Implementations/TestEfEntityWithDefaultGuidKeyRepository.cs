using Foundation.DataAccess.Implementations;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Foundation.Model.Contracts;

namespace Foundation.Test.DataAccess.Implementations
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
