using AutoMapper;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Exceptions;
using Bit.Owin.Implementations;
using Bit.OwinCore;
using Microsoft.AspNet.OData;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Swashbuckle.Application;
using Swashbuckle.Examples;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

[assembly: ODataModule("MyApp")]

namespace CustomerDtoControllerSample
{
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
                    }).EnableBitSwaggerUi();
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
                    }).EnableBitSwaggerUi();
                });

                odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();
            dependencyManager.RegisterEfDbContext<MyAppDbContext>();
            dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>();
            dependencyManager.RegisterRepository(typeof(MyAppRepository<>).GetTypeInfo());

            dependencyManager.RegisterDtoEntityMapper();
            dependencyManager.RegisterMapperConfiguration<DefaultMapperConfiguration>();
            dependencyManager.RegisterMapperConfiguration<MyAppDtoEntityMapperConfiguration>();
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
            : base(DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"))
        {

        }

        public MyAppDbContext(AppEnvironment appEnvironment, IDbConnectionProvider dbConnectionProvider)
            : base(appEnvironment.GetConfig<string>("AppConnectionString"), dbConnectionProvider)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<City> Cities { get; set; }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<Category> Categories { get; set; }
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
            return AppDbContext.Customers.AsNoTracking().Where(c => c.IsActive == true)
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

        public class SendEmailToCustomerParams
        {
            public int customerId { get; set; }

            public string message { get; set; }
        }

        public class SendEmailToCustomerParamsExample : IExamplesProvider
        {
            public object GetExamples()
            {
                return new SendEmailToCustomerParams
                {
                    customerId = 1,
                    message = "test"
                };
            }
        }

        [SwaggerRequestExample(typeof(SendEmailToCustomerParams), typeof(SendEmailToCustomerParamsExample), jsonConverter: typeof(StringEnumConverter))]
        [Action]
        public async Task SendEmailToCustomer(SendEmailToCustomerParams args)
        {
            // ...
        }

        [Function]
        public virtual async Task<SingleResult<CustomerDto>> GetCustomerById(int customerId, CancellationToken cancellationToken)
        {
            return SingleResult(Mapper.FromEntityQueryToDtoQuery(await CustomersRepository.GetAllAsync(cancellationToken))
                .Where(c => c.Id == customerId));
        }

        [Function]
        public virtual async Task<SingleResult<CustomerDto>> GetCustomerById2(int customerId, CancellationToken cancellationToken)
        {
            return SingleResult(Mapper.FromEntityQueryToDtoQuery((await CustomersRepository.GetAllAsync(cancellationToken)))
                .Where(c => c.Id == customerId));
        }
    }

    #region Model

    public class Category : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public List<Product> Products { get; set; }
    }

    public class Product : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }
    }

    #endregion

    #region Dto

    public class CategoryDto : IDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<ProductDto> Products { get; set; }

        public bool IsActive { get; set; }

        public bool HasProduct { get; set; }
    }

    public class ProductDto : IDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public CategoryDto Category { get; set; }
    }

    #endregion

    #region DtoSetControllers

    public class CategoriesController : DtoSetController<CategoryDto, Category, int /* Key type */>
    {
        public async override Task<SingleResult<CategoryDto>> Create(CategoryDto dto, CancellationToken cancellationToken)
        {
            // custom logic ...

            return await base.Create(dto, cancellationToken);
        }

        public async override Task<SingleResult<CategoryDto>> Update(int key, CategoryDto dto, CancellationToken cancellationToken)
        {
            // custom logic ...

            return await base.Update(key, dto, cancellationToken);
        }

        public async override Task<SingleResult<CategoryDto>> PartialUpdate(int key, Delta<CategoryDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            // custom logic ...

            return await base.PartialUpdate(key, modifiedDtoDelta, cancellationToken);
        }

        public async override Task Delete(int key, CancellationToken cancellationToken)
        {
            throw new BadRequestException(); // We may disable delete for this Dto.
        }

        public async override Task<SingleResult<CategoryDto>> Get(int key, CancellationToken cancellationToken)
        {
            return await base.Get(key, cancellationToken);
        }

        public async override Task<IQueryable<CategoryDto>> GetAll(CancellationToken cancellationToken)
        {
            return (await base.GetAll(cancellationToken)).Where(c => c.IsActive == true); // Return active categories only.
        }
    }

    public class ProductsController : DtoSetController<ProductDto, Product, int>
    {

    }

    #endregion

    public class MyAppDtoEntityMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.CreateMap<Category, CategoryDto>()
                .ForMember(category => category.HasProduct, config => config.MapFrom(category => category.Products.Any()));
        }
    }
}
