using Bit.Core;
using Bit.Core.Contracts;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using Bit.OData.Contracts;
using Bit.OData.Implementations;
using Bit.OData.ODataControllers;
using Bit.Owin.Implementations;
using Bit.OwinCore;
using Bit.OwinCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;

namespace CustomerDtoControllerSample
{
    public class AppStartup : AutofacAspNetCoreAppStartup, IAspNetCoreDependenciesManager, IDependenciesManagerProvider
    {
        public AppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            DefaultDependenciesManagerProvider.Current = this;

            return base.ConfigureServices(services);
        }

        public IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }

        public virtual void ConfigureDependencies(IServiceProvider serviceProvider, IServiceCollection services, IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultAspNetCoreApp();

            dependencyManager.RegisterMinimalAspNetCoreMiddlewares();

            dependencyManager.RegisterDefaultWebApiAndODataConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();

                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", $"Swagger-Api");
                        c.ApplyDefaultApiConfig(httpConfiguration);
                    }).EnableSwaggerUi();
                });
            });

            dependencyManager.RegisterODataMiddleware(odataDependencyManager =>
            {
                odataDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", $"Swagger-Api");
                        c.ApplyDefaultODataConfig(httpConfiguration);
                    }).EnableSwaggerUi();
                });

                odataDependencyManager.RegisterODataServiceBuilder<BitODataServiceBuilder>();
                odataDependencyManager.RegisterODataServiceBuilder<MyAppODataServiceBuilder>();
                odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();
            dependencyManager.RegisterEfDbContext<MyAppDbContext>();
            dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>();
            dependencyManager.RegisterRepository(typeof(MyAppRepository<>).GetTypeInfo());

            dependencyManager.RegisterDtoEntityMapper();
            dependencyManager.RegisterDtoEntityMapperConfiguration<DefaultDtoEntityMapperConfiguration>();
        }
    }

    public class MyAppODataServiceBuilder : IODataServiceBuilder
    {
        public IAutoODataModelBuilder AutoODataModelBuilder { get; set; }

        public string GetODataRoute()
        {
            return "MyApp";
        }

        public void BuildModel(ODataModelBuilder odataModelBuilder)
        {
            // odataModelBuilder is useful for advanced scenarios.
            AutoODataModelBuilder.AutoBuildODatModelFromAssembly(typeof(MyAppODataServiceBuilder).GetTypeInfo().Assembly, odataModelBuilder);
        }
    }

    public class Customer : IEntity
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsActive { get; set; }

        public City City { get; set; }

        [ForeignKey(nameof(City))]
        public int CityId { get; set; }

        public List<Order> Orders { get; set; } = new List<Order>();
    }

    public class City : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class Order : IEntity
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public Customer Customer { get; set; }

        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
    }

    public class CustomerDto : IDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsActive { get; set; }

        public int CityId { get; set; }

        public string CityName { get; set; }
    }

    [DbConfigurationType(typeof(UseDefaultModelStoreDbConfiguration))]
    public class MyAppDbContext : EfDbContextBase
    {
        public MyAppDbContext()
            : base(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"))
        {

        }

        public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbConnectionProvider dbConnectionProvider)
            : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbConnectionProvider)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<City> Cities { get; set; }
    }

    public class MyAppRepository<TEntity> : EfRepository<TEntity>
        where TEntity : class, IEntity
    {
    }

    public class MyAppDbContextInitializer : IAppEvents
    {
        public virtual void OnAppStartup()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<MyAppDbContext>());

            using (MyAppDbContext dbContext = new MyAppDbContext())
            {
                dbContext.Database.Initialize(force: true);

                City city1 = new City { Id = 1, Name = "1" };
                City city2 = new City { Id = 2, Name = "2" };
                dbContext.Cities.AddRange(new[] { city1, city2 });

                dbContext.Customers.AddRange(Enumerable.Range(1, 100).Select(i => new Customer
                {
                    Id = i,
                    FirstName = i.ToString(),
                    LastName = i.ToString(),
                    IsActive = i % 2 == 0, // Set IsActive based on "i" is even or odd
                    CityId = i % 2 == 0 ? city1.Id : city2.Id
                }));

                dbContext.SaveChanges();
            }
        }

        public virtual void OnAppEnd()
        {

        }
    }

    public class CustomersController : DtoController<CustomerDto>
    {
        public virtual IRepository<Customer> CustomersRepository { get; set; }

        public virtual IDtoEntityMapper<CustomerDto, Customer> Mapper { get; set; }

        [Function] // Using bit repository and bit mapper
        public virtual async Task<IQueryable<CustomerDto>> GetActiveCustomers(CancellationToken cancellationToken)
        {
            return Mapper.FromEntityQueryToDtoQuery((await CustomersRepository.GetAllAsync(cancellationToken)).Where(c => c.IsActive == true));
        }

        [Function] // Any thing you like, customer array, entity framework db context, dapper, mongo db etc.
        public virtual async Task<IQueryable<CustomerDto>> GetActiveCustomers2(CancellationToken cancellationToken)
        {
            return AppDbContext.Customers.Where(c => c.IsActive == true)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    CityId = c.CityId,
                    CityName = c.City.Name,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    IsActive = c.IsActive
                });
        }

        public virtual MyAppDbContext AppDbContext { get; set; }
    }
}
