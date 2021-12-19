using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Model.Contracts;
using Bit.Owin;
using Bit.Owin.Implementations;
using EntityFrameworkCoreSample;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

[assembly: AppModule(typeof(EntityFrameworkCoreSampleAppModule))]

namespace EntityFrameworkCoreSample
{
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
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Order> Orders { get; set; }
    }

    public class MyAppRepository<TEntity> : EfCoreRepository<TEntity>
        where TEntity : class, IEntity
    {
    }

    public class CustomersController : ApiController
    {
        public virtual IRepository<Customer> CustomersRepository { get; set; }

        [Route("customers/get-customers")]
        public virtual async Task<List<Customer>> GetCustomers(CancellationToken cancellationToken)
        {
            return await (await CustomersRepository.GetAllAsync(cancellationToken)).ToListAsync(cancellationToken);
        }

        public virtual async Task AddNewCustomer(Customer customer, CancellationToken cancellationToken)
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
        public virtual IDependencyManager DependencyManager { get; set; }

        public virtual void OnAppStartup()
        {
            DependencyManager.TransactionAction("CreateDatabase", async resolver =>
            {
                MyAppDbContext dbContext = resolver.Resolve<MyAppDbContext>();
                dbContext.Database.EnsureCreated();
            }).GetAwaiter().GetResult();
        }

        public virtual void OnAppEnd()
        {

        }
    }

    public class EntityFrameworkCoreSampleAppModule : IAppModule
    {
        public virtual void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultAspNetCoreApp();

            dependencyManager.RegisterMinimalAspNetCoreMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();
            dependencyManager.RegisterEfCoreDbContext<MyAppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(serviceProvider.GetRequiredService<IDbConnectionProvider>().GetDbConnection(serviceProvider.GetRequiredService<AppEnvironment>().GetConfig<string>("AppConnectionString"), rollbackOnScopeStatusFailure: true));
            });
            dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>();
            dependencyManager.RegisterRepository(typeof(MyAppRepository<>).GetTypeInfo());
            dependencyManager.RegisterRepository(typeof(OrdersRepository).GetTypeInfo());
        }
    }
}
