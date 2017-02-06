using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace EntityFrameworkFullAsNoTrackingCallTests
{
    class Program
    {
        static void Main(string[] args)
        {
            using (AppDbContext context = new AppDbContext())
            {
                //bool test1 = context.Set<Product>().Any();

                //bool test2 = context.Products.AnyAsync().Result;

                //Product test3 = context.Products.Find(1);

                //Product test4 = context.Products.FindAsync(1).Result;

                // System.Data.Entity.Infrastructure.DbQuery
                // System.Data.Entity.Infrastructure.DbSqlQuery

                context.Products.Any();

                context.Products.Add(new Product { });

                bool test5 = context.Products.SqlQuery("select * from Products").Any();
            }
        }
    }

    public class AppDbContext : DbContext
    {
        static AppDbContext()
        {
            //Database.SetInitializer(new DropCreateDatabaseAlways<AppDbContext>());
            Database.SetInitializer<AppDbContext>(null);
        }

        public AppDbContext()
            : base(new SqlConnection(@"Data Source=.;Initial Catalog=Test;Integrated Security=True"), contextOwnsConnection: true)
        {
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.UseDatabaseNullSemantics = false;
            Configuration.ValidateOnSaveEnabled = true;
        }

        public virtual DbSet<Product> Products { get; set; }
    }

    public class Product
    {
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }
    }
}
