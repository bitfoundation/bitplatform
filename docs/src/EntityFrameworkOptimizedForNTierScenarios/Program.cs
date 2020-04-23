using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;
using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;

[assembly: InProcess]

namespace EntityFrameworkOptimizedForNTierScenarios
{
    #region Model

    public class Customer : IEntity
    {
        [Key]
        public virtual int Id { get; set; }

        [MaxLength(50)]
        public virtual string FirstName { get; set; }

        [MaxLength(50)]
        public virtual string LastName { get; set; }

        public virtual List<Order> Orders { get; set; } = new List<Order>();
    }

    public class Order : IEntity
    {
        [Key]
        public virtual int Id { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public virtual int CustomerId { get; set; }

        [MaxLength(50)]
        public virtual string Description { get; set; }
    }

    #endregion

    #region DbContext + Sharp Repository + TestApiController

    public class CustomersDbContextForSharpRepository : DbContext
    {
        public CustomersDbContextForSharpRepository()
            : base(new SqlConnection("Data Source=.;Initial Catalog=CustomersDb;Integrated Security=True"), contextOwnsConnection: true)
        {
            Configuration.ProxyCreationEnabled = false;
        }

        public virtual DbSet<Customer> Customers { get; set; }
    }

    public class SharpRepository<TEntity> : SharpRepository.EfRepository.EfRepository<TEntity>
        where TEntity : class
    {
        public SharpRepository(CustomersDbContextForSharpRepository dbContext)
            : base(dbContext)
        {

        }
    }

    public class CustomersForSharpRepositoryController : ApiController
    {
        public SharpRepository.Repository.IRepository<Customer> CustomersSharpRepository { get; set; }

        [Route("customers/get-customers-by-sharp-repository")]
        public List<Customer> GetCustomersBySharpRepository()
        {
            return CustomersSharpRepository.GetAll().ToList();
        }
    }

    #endregion

    #region Bit DbContext + Bit Repository + TestApiController

    [DbConfigurationType(typeof(UseDefaultModelStoreDbConfiguration))]
    public class CustomersDbContextForBitRepository : EfDbContextBase
    {
        public CustomersDbContextForBitRepository()
            : base(new SqlConnection("Data Source=.;Initial Catalog=CustomersDb;Integrated Security=True"), contextOwnsConnection: true)
        {

        }

        public CustomersDbContextForBitRepository(IDbConnectionProvider dbConnectionProvider)
            : base("Data Source=.;Initial Catalog=CustomersDb;Integrated Security=True", dbConnectionProvider)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }
    }

    public class BitRepository<TEntity> : EfRepository<TEntity>
        where TEntity : class, IEntity
    {
    }

    public class CustomersForBitRepositoryController : ApiController
    {
        public IRepository<Customer> CustomersBitRepository { get; set; }

        [Route("customers/get-customers-by-bit-repository")]
        public async Task<List<Customer>> GetCustomersByBitRepository()
        {
            return await CustomersBitRepository.GetAll().ToListAsync();
        }
    }

    public class CustomersEmptyListController : ApiController
    {
        [Route("customers/get-customers-empty")]
        public List<Customer> GetCustomersByBitRepository()
        {
            return new List<Customer> { };
        }
    }

    #endregion

    public class RepositoriesBenchmarkTest
    {
        [Benchmark]
        public void BitRepository() => Program.WebHost.Services.GetRequiredService<IHttpClientFactory>().CreateClient().GetAsync($"{Program.BaseAddress}api/customers/get-customers-by-bit-repository").GetAwaiter().GetResult().EnsureSuccessStatusCode();

        [Benchmark]
        public void SharpRepository() => Program.WebHost.Services.GetRequiredService<IHttpClientFactory>().CreateClient().GetAsync($"{Program.BaseAddress}api/customers/get-customers-by-sharp-repository").GetAwaiter().GetResult().EnsureSuccessStatusCode();

        [Benchmark]
        public void EmptyList() => Program.WebHost.Services.GetRequiredService<IHttpClientFactory>().CreateClient().GetAsync($"{Program.BaseAddress}api/customers/get-customers-empty").GetAwaiter().GetResult().EnsureSuccessStatusCode();
    }

    public class Program
    {
        public static string BaseAddress { get; set; } = "http://localhost:9000/";

        public static IWebHost WebHost { get; set; }

        public static async Task Main(string[] args)
        {
#if DEBUG
            System.Console.ForegroundColor = System.ConsoleColor.Yellow;
            System.Console.WriteLine("*****To achieve accurate results, set project configuration to release mode.*****");
            return;
#endif

            Database.SetInitializer<CustomersDbContextForBitRepository>(null);
            Database.SetInitializer<CustomersDbContextForSharpRepository>(null);

            WebHost = BuildWebHost(args);

            WebHost.Services.GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStarted.Register(() =>
                {
                    BenchmarkRunner.Run<RepositoriesBenchmarkTest>();
                });

            await WebHost.RunAsync();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            BitWebHost.CreateDefaultBuilder(args)
                .UseStartup<AppStartup>()
                .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                .UseUrls(BaseAddress)
                .Build();
    }

    public class AppStartup : AutofacAspNetCoreAppStartup, IAppModule, IAppModulesProvider
    {
        public AppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            DefaultAppModulesProvider.Current = this;

            return base.ConfigureServices(services);
        }

        public IEnumerable<IAppModule> GetAppModules()
        {
            yield return this;
        }

        public virtual void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultAspNetCoreApp();

            dependencyManager.RegisterMinimalAspNetCoreMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });
            });

            #region Register Bit Repository

            dependencyManager.RegisterEfDbContext<CustomersDbContextForBitRepository>();
            dependencyManager.RegisterRepository(typeof(BitRepository<>).GetTypeInfo());

            #endregion

            #region Register Sharp Repository

            dependencyManager.Register<CustomersDbContextForSharpRepository, CustomersDbContextForSharpRepository>();
            dependencyManager.RegisterGeneric(typeof(SharpRepository.Repository.IRepository<>).GetTypeInfo(), typeof(SharpRepository<>).GetTypeInfo());

            #endregion

            services.AddHttpClient();
        }
    }
}
