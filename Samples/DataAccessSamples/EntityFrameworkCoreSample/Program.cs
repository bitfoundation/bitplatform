using Bit.Core;
using Bit.Core.Contracts;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.Contracts;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace EntityFrameworkCoreSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9000/";

            using (WebApp.Start<AppStartup>(url: baseAddress))
            {
                HttpClient client = new HttpClient();

                HttpResponseMessage response = client.GetAsync($"{baseAddress}api/customers/get-customers").GetAwaiter().GetResult();

                Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                Console.WriteLine("Press any key to exit...");

                Console.ReadKey();
            }
        }
    }

    public class Customer : IEntity
    {
        public virtual int Id { get; set; }

        public virtual string FirstName { get; set; }

        public virtual string LastName { get; set; }

        public virtual List<Order> Orders { get; set; } = new List<Order>();
    }

    public class Order : IEntity
    {
        public virtual int Id { get; set; }

        public virtual string Description { get; set; }

        public virtual int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }
    }

    public class MyAppDbContext : EfCoreDbContextBase
    {
        public MyAppDbContext()
            : base(new DbContextOptionsBuilder().UseSqlServer(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString")).Options)
        {

        }

        public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbContextObjectsProvider dbContextCreationOptionsProvider)
              : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbContextCreationOptionsProvider)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Order> Orders { get; set; }
    }

    public class MyAppRepository<TEntity> : EfCoreRepository<TEntity>
        where TEntity : class, IEntity
    {
        public MyAppRepository(MyAppDbContext dbContext)
            : base(dbContext)
        {

        }
    }

    public class CustomersController : ApiController
    {
        public virtual IRepository<Customer> CustomersRepository { get; set; }

        [Route("customers/get-customers")]
        public virtual async Task<List<Customer>> GetCustomers(CancellationToken cancellationToken)
        {
            return await (await CustomersRepository.GetAllAsync(cancellationToken)).ToListAsync(cancellationToken);
        }

        public virtual async Task AddNewCustomer(CancellationToken cancellationToken, Customer customer)
        {
            await CustomersRepository.AddAsync(customer, cancellationToken);
        }
    }

    public interface IOrdersRepository : IRepository<Order>
    {
        Task<long> GetOrdersCount(CancellationToken cancellationToken);
    }

    public class OrdersRepository : MyAppRepository<Order>, IOrdersRepository
    {
        public OrdersRepository(MyAppDbContext dbContext)
            : base(dbContext)
        {

        }

        public virtual async Task<long> GetOrdersCount(CancellationToken cancellationToken)
        {
            return await (await GetAllAsync(cancellationToken)).LongCountAsync(cancellationToken);
        }
    }

    public class OrdersController : ApiController
    {
        public virtual IOrdersRepository OrdersRepository { get; set; }

        [Route("orders/get-orders-count")]
        public virtual async Task<long> GetOrdersCount(CancellationToken cancellationToken)
        {
            return await OrdersRepository.GetOrdersCount(cancellationToken);
        }
    }

    public class MyAppDbContextInitializer : IAppEvents
    {
        public virtual void OnAppStartup()
        {
            using (MyAppDbContext dbContext = new MyAppDbContext())
            {
                dbContext.Database.EnsureCreated();
            }
        }

        public virtual void OnAppEnd()
        {

        }
    }

    public class AppStartup : OwinAppStartup, IOwinDependenciesManager, IDependenciesManagerProvider
    {
        public override void Configuration(IAppBuilder owinApp)
        {
            DefaultDependenciesManagerProvider.Current = this;

            base.Configuration(owinApp);
        }

        public IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }

        public void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterMinimalOwinMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();
            dependencyManager.RegisterEfCoreDbContext<MyAppDbContext, SqlDbContextObjectsProvider>();
            dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>();
            dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(MyAppRepository<>).GetTypeInfo());
            dependencyManager.Register<IOrdersRepository, OrdersRepository>();
        }
    }
}
