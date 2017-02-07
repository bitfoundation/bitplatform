using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace EntityFrameworkFullAsNoTrackingCallTests
{
    class Program
    {
        static void Main(string[] args)
        {
            using (AppDbContext context = new AppDbContext())
            {
                bool problem1 = context.Set<Product>().Any();

                bool problem2 = context.Products.AnyAsync().Result;

                List<Product> problem3 = context.Products.ToList();

                // Any call of methods of following classes
                // System.Data.Entity.QueryableExtensions
                // System.Linq.Queryable
                // System.Linq.Enumerable

                // On Any instance of
                // System.Data.Entity.Infrastructure.DbQuery

                bool ok1 = context.Set<Product>().AsNoTracking().Any();

                bool ok2 = context.Products.AsNoTracking().AnyAsync().Result;

                List<Product> ok3 = context.Products.AsNoTracking().ToList();

                bool ok4 = new int[] { }.Any();
            }
        }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext()
            : base("AppDbConnectionString")
        {

        }

        public virtual DbSet<Product> Products { get; set; }
    }

    public class Product
    {
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }
    }
}
