using Foundation.Core.Contracts;
using Foundation.Core.Models;
using IdentityServer.Api.Contracts;
using IdentityServer.Api.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Api.Implementations
{
    public class DefaultSSOPageModelProvider : ISSOPageModelProvider
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IContentFormatter _contentFormatter;

        public DefaultSSOPageModelProvider(IContentFormatter contentFormatter, IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (contentFormatter == null)
                throw new ArgumentNullException(nameof(contentFormatter));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            _contentFormatter = contentFormatter;
        }

        protected DefaultSSOPageModelProvider()
        {

        }

        public virtual SSOPageModel GetSSOPageModel()
        {
            return new SSOPageModel
            {
                AppTitle = _activeAppEnvironment
                                .Cultures
                                .Single(c => c.Name == _activeAppEnvironment.AppInfo.DefaultCulture)
                                .Values
                                .Single(v => v.Name == "AppTitle").Title,
                AppName = _activeAppEnvironment.AppInfo.Name,
                AppVersion = _activeAppEnvironment.AppInfo.Version,
                Culture = _activeAppEnvironment.AppInfo.DefaultCulture,
                DebugMode = _activeAppEnvironment.DebugMode,
                DesiredTimeZoneValue = _activeAppEnvironment.AppInfo.DefaultTimeZone,
                Theme = _activeAppEnvironment.AppInfo.DefaultTheme,
                EnvironmentConfigsJSON = _contentFormatter.Serialize(_activeAppEnvironment
                                            .Configs.Where(c => c.AccessibleInClientSide == true)
                                            .Select(c => new { value = c.Value, key = c.Key }))
            };
        }

        public virtual async Task<SSOPageModel> GetSSOPageModelAsync(CancellationToken cancellationToken)
        {
            return new SSOPageModel
            {
                AppTitle = _activeAppEnvironment
                                .Cultures
                                .Single(c => c.Name == _activeAppEnvironment.AppInfo.DefaultCulture)
                                .Values
                                .Single(v => v.Name == "AppTitle").Title,
                AppName = _activeAppEnvironment.AppInfo.Name,
                AppVersion = _activeAppEnvironment.AppInfo.Version,
                Culture = _activeAppEnvironment.AppInfo.DefaultCulture,
                DebugMode = _activeAppEnvironment.DebugMode,
                DesiredTimeZoneValue = _activeAppEnvironment.AppInfo.DefaultTimeZone,
                Theme = _activeAppEnvironment.AppInfo.DefaultTheme,
                EnvironmentConfigsJSON = _contentFormatter.Serialize(_activeAppEnvironment
                                            .Configs.Where(c => c.AccessibleInClientSide == true)
                                            .Select(c => new { value = c.Value, key = c.Key }))
            };
        }
    }
}
