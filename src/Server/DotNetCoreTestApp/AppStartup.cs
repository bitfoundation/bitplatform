using AutoMapper;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Exceptions;
using Bit.Owin.Implementations;
using Bit.OwinCore;
using IdentityServer3.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

[assembly: ODataModule("Test")]

namespace DotNetCoreTestApp
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

            dependencyManager.RegisterAspNetCoreSingleSignOnClient();

            dependencyManager.RegisterDefaultWebApiAndODataConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new System.Web.Http.AuthorizeAttribute());
                });

                webApiDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "Test-Api");
                        c.ApplyDefaultApiConfig(httpConfiguration);
                    }).EnableBitSwaggerUi();
                });

                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.RegisterODataMiddleware(odataDependencyManager =>
            {
                odataDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                });

                odataDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "Test-Api");
                        c.ApplyDefaultODataConfig(httpConfiguration);
                    }).EnableBitSwaggerUi();
                });

                odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration();

            dependencyManager.RegisterRepository(typeof(TestEfRepository<>).GetTypeInfo());
            dependencyManager.RegisterRepository(typeof(CustomersRepository).GetTypeInfo());

            dependencyManager.RegisterEfCoreDbContext<TestDbContext>((sp, optionsBuilder) => optionsBuilder.UseInMemoryDatabase("TestDb"));

            dependencyManager.RegisterDtoEntityMapper();

            dependencyManager.RegisterMapperConfiguration<DefaultMapperConfiguration>();
            dependencyManager.RegisterMapperConfiguration<TestMapperConfiguration>();

            dependencyManager.RegisterSingleSignOnServer<TestUserService, TestOAuthClientsProvider>();
        }
    }

    public class TestUserService : UserService
    {
        public override Task<BitJwtToken> LocalLogin(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            if (context.UserName == context.Password)
                return Task.FromResult(new BitJwtToken { UserId = context.UserName });

            throw new DomainLogicException("LoginFailed");
        }
    }

    public class TestOAuthClientsProvider : OAuthClientsProvider
    {
        public override IEnumerable<Client> GetClients()
        {
            return new[]
            {
                GetResourceOwnerFlowClient(new BitResourceOwnerFlowClient
                {
                    ClientName = "TestResOwner",
                    ClientId = "TestResOwner",
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(7),
                    Enabled = true
                })
            };
        }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Customer> Customers { get; set; }
    }

    public class Customer : IEntity
    {
        public virtual int Id { get; set; }

        public virtual string FirstName { get; set; }

        public virtual string LastName { get; set; }
    }

    public class CustomerDto : IDto
    {
        public virtual int Id { get; set; }

        public virtual string FullName { get; set; }
    }

    public class TestMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.CreateMap<Customer, CustomerDto>()
                .ForMember(c => c.FullName, cnfg => cnfg.MapFrom(c => c.FirstName + " " + c.LastName));
        }
    }

    public class TestEfRepository<TEntity> : EfCoreRepository<TestDbContext, TEntity>, ITestRepository<TEntity>
        where TEntity : class, IEntity
    {

    }

    public interface ITestRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {

    }

    public interface ICustomersRepository : ITestRepository<Customer>
    {

    }

    public class CustomersRepository : TestEfRepository<Customer>, ICustomersRepository
    {

    }

    public class ValuesController : ApiController
    {
        public string[] Get()
        {
            return new[] { "1", "2", "3" };
        }
    }

    public class CustomersController : DtoSetController<CustomerDto, Customer, int>
    {
        public virtual IRepository<Customer> CustomersRepository { get; set; }

        public virtual ITestRepository<Customer> CustomersRepository2 { get; set; }

        public virtual ICustomersRepository CustomersRepository3 { get; set; }

        [Function]
        public int Sum(int firstNumber, int secondNumber)
        {
            if (CustomersRepository != CustomersRepository2 || CustomersRepository != CustomersRepository3)
                throw new InvalidOperationException();

            return firstNumber + secondNumber;
        }
    }
}
