using Foundation.DataAccess.Implementations;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Foundation.Model.Contracts;

namespace Foundation.Test.DataAccess.Implementations
{
    public class TestEfRepository<TEntity> : EfRepository<TEntity>
        where TEntity : class, IEntity
    {
        public TestEfRepository(TestDbContext context)
            : base(context)
        {

        }

        protected TestEfRepository()
            : base()
        {

        }
    }
}
