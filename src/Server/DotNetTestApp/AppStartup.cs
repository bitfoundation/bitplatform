﻿using AutoMapper;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Exceptions;
using Bit.Core.Models;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.Implementations;
using Bit.OData.ODataControllers;
using Bit.Owin.Implementations;
using DotNetTestApp;
using IdentityServer3.Core.Models;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

[assembly: ODataModule("Test")]
[assembly: AppModule(typeof(DotNetTestAppModule))]

namespace DotNetTestApp
{
    public class DotNetTestAppModule : IAppModule, IAppModulesProvider
    {
        public IEnumerable<IAppModule> GetAppModules()
        {
            yield return this;
        }

        public virtual void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            services.AddSingleton(Log.Logger);

            dependencyManager.RegisterMinimalDependencies();

            /*services.AddApplicationInsightsTelemetry("");
            dependencyManager.RegisterApplicationInsights();*/
            dependencyManager.RegisterDefaultLogger(typeof(SerilogLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultAspNetCoreApp();

            dependencyManager.RegisterMinimalAspNetCoreMiddlewares();

            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp =>
            {
                aspNetCoreApp.UseSwagger();
                aspNetCoreApp.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotNetTestApp v1"));
                aspNetCoreApp.UseSerilogRequestLogging();
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "DotNetTestApp", Version = "v1" });
            });

            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp => aspNetCoreApp.UseRouting());

            dependencyManager.RegisterAspNetCoreSingleSignOnClient();

            services.AddControllers();
            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp => aspNetCoreApp.UseEndpoints(endpoints => endpoints.MapControllers()));

            dependencyManager.Register<IODataModelBuilderProvider, AppODataModelBuilderProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);

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
            dependencyManager.RegisterRepository<CustomersRepository>();

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
                .ForMember(c => c.FullName, cnfg => cnfg.MapFrom(c => c.FirstName + " " + c.LastName))
                .ReverseMap();
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

    public class AppODataModelBuilderProvider : DefaultODataModelBuilderProvider
    {
        public override ODataModelBuilder GetODataModelBuilder(HttpConfiguration webApiConfig, string containerName, string @namespace)
        {
            var builder = ((ODataConventionModelBuilder)base.GetODataModelBuilder(webApiConfig, containerName, @namespace))
                .EnableLowerCamelCase();

            builder.ComplexType<NestedComplexType>(); // too nested!

            return builder;
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

    public class MainDto : IDto
    {
        public int Id { get; set; } = 1;

        [AutoExpand]
        public List<MainComplexType> ComplexTypes { get; set; } = new List<MainComplexType>
        {
            new MainComplexType { }
        };
    }

    [ComplexType]
    public class MainComplexType
    {
        public string Value { get; set; } = "Test";

        [AutoExpand]
        public List<NestedComplexType> ComplexTypes { get; set; } = new List<NestedComplexType>
        {
            new NestedComplexType { }
        };
    }

    [ComplexType]
    public class NestedComplexType
    {
        public string Value { get; set; } = "Test";
    }

    public class SampleController : DtoController<MainDto>
    {
        [Function, AllowAnonymous]
        public SingleResult<MainDto> GetInstance()
        {
            return SingleResult(new MainDto { });
        }
    }

    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [Microsoft.AspNetCore.Mvc.HttpGet, Microsoft.AspNetCore.Authorization.Authorize]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
