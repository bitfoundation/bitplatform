using BitChangeSetManager.Api;
using BitChangeSetManager.DataAccess;
using Foundation.Api;
using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Core.Implementations;
using Foundation.DataAccess.Contracts;
using Foundation.DataAccess.Implementations;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Foundation.Model.Implementations;
using Owin;
using System.Collections.Generic;
using System.Reflection;

namespace BitChangeSetManager
{
    public class AppStartup : OwinAppStartup, IDependenciesManager, IDependenciesManagerProvider
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
            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);

            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>();
            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>();
            dependencyManager.Register<IRequestInformationProvider, DefaultRequestInformationProvider>();
            dependencyManager.Register<ILogger, DefaultLogger>();
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>();
            dependencyManager.Register<ILogStore, ConsoleLogStore>();
            dependencyManager.Register<IDbConnectionProvider, DefaultSqlDbConnectionProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(BitChangeSetManagerEdmModelProvider).GetTypeInfo().Assembly);

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                    childDependencyManager.RegisterEdmModelProvider<FoundationEdmModelProvider>();
                    childDependencyManager.RegisterEdmModelProvider<BitChangeSetManagerEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(BitChangeSetManagerEdmModelProvider).GetTypeInfo().Assembly);

            dependencyManager.RegisterGeneric(typeof(IBitChangeSetManagerRepository<>).GetTypeInfo(), typeof(BitChangeSetManagerEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterEfCoreDbContext<BitChangeSetManagerDbContext, InMemoryDbContextObjectsProvider>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<BitChangeSetManagerDtoModelMapperConfiguration>();

            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();
        }
    }
}
