using BitChangeSetManager.Api;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Security;
using Foundation.Api;
using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Core.Implementations;
using Foundation.DataAccess.Contracts;
using Foundation.DataAccess.Implementations;
using Foundation.Model.Implementations;
using IdentityServer.Api.Contracts;
using IdentityServer.Api.Implementations;
using IdentityServer.Api.Middlewares;
using IdentityServer3.Core.Services;
using Microsoft.Owin.Cors;
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
            dependencyManager.Register<IDbConnectionProvider, DefaultSqlDbConnectionProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<BitChangeSetManagerInitialData>();
            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();

            dependencyManager.RegisterOwinMiddlewareUsing(owinApp =>
            {
                owinApp.UseCors(CorsOptions.AllowAll);
            });

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            dependencyManager.RegisterSingleSignOnServer();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(BitChangeSetManagerEdmModelProvider).GetTypeInfo().Assembly);

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                    childDependencyManager.RegisterEdmModelProvider<FoundationEdmModelProvider>();
                    childDependencyManager.RegisterEdmModelProvider<BitChangeSetManagerEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterSignalRConfiguration<SignalRSqlServerScaleoutConfiguration>();
            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration<BitChangeSetManagerAppMessageHubEvents>(typeof(MessagesHub).GetTypeInfo().Assembly);

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(BitChangeSetManagerEdmModelProvider).GetTypeInfo().Assembly);

            dependencyManager.RegisterGeneric(typeof(IBitChangeSetManagerRepository<>).GetTypeInfo(), typeof(BitChangeSetManagerEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterEfDbContext<BitChangeSetManagerDbContext>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<BitChangeSetManagerDtoModelMapperConfiguration>();

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>();
            dependencyManager.Register<IViewService, DefaultViewService>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISsoPageHtmlProvider, RazorSsoHtmlPageProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISSOPageModelProvider, DefaultSSOPageModelProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.Register<IClientProvider, BitChangeSetManagerClientProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IUserService, BitChangeSetManagerUserService>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();
            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();

            dependencyManager.Register<IChangeSetRepository, ChangeSetRepository>();
            dependencyManager.Register<IUserSettingProvider, BitUserSettingProvider>();
        }
    }
}
