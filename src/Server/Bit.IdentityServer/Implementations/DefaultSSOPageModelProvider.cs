using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Model;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultSSOPageModelProvider : ISSOPageModelProvider
    {
        private AppEnvironment _activeAppEnvironment;

        public virtual IContentFormatter ContentFormatter { get; set; }

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(AppEnvironmentProvider));

                _activeAppEnvironment = value.GetActiveAppEnvironment();
            }
        }

        public virtual SSOPageModel GetSSOPageModel()
        {
            return new SSOPageModel
            {
                AppTitle = _activeAppEnvironment
                                .Cultures
                                .ExtendedSingle($"Finding cultures of {_activeAppEnvironment.AppInfo.DefaultCulture} in {_activeAppEnvironment.Name} environment", c => c.Name == _activeAppEnvironment.AppInfo.DefaultCulture)
                                .Values
                                .ExtendedSingle($"Finding AppTitle in {_activeAppEnvironment.AppInfo.DefaultCulture} cultures of {_activeAppEnvironment.Name} environment", v => v.Name == "AppTitle").Title,
                AppName = _activeAppEnvironment.AppInfo.Name,
                AppVersion = _activeAppEnvironment.AppInfo.Version,
                Culture = _activeAppEnvironment.AppInfo.DefaultCulture,
                DebugMode = _activeAppEnvironment.DebugMode,
                DesiredTimeZoneValue = _activeAppEnvironment.AppInfo.DefaultTimeZone,
                Theme = _activeAppEnvironment.AppInfo.DefaultTheme,
                EnvironmentConfigsJson = ContentFormatter.Serialize(_activeAppEnvironment
                                            .Configs.Where(c => c.AccessibleInClientSide == true)
                                            .Select(c => new { value = c.Value, key = c.Key })),
                BaseHref = _activeAppEnvironment.GetHostVirtualPath()
            };
        }

        public virtual async Task<SSOPageModel> GetSSOPageModelAsync(CancellationToken cancellationToken)
        {
            return new SSOPageModel
            {
                AppTitle = _activeAppEnvironment
                                .Cultures
                                .ExtendedSingle($"Finding cultures of {_activeAppEnvironment.AppInfo.DefaultCulture} in {_activeAppEnvironment.Name} environment", c => c.Name == _activeAppEnvironment.AppInfo.DefaultCulture)
                                .Values
                                .ExtendedSingle($"Finding AppTitle in {_activeAppEnvironment.AppInfo.DefaultCulture} cultures of {_activeAppEnvironment.Name} environment", v => v.Name == "AppTitle").Title,
                AppName = _activeAppEnvironment.AppInfo.Name,
                AppVersion = _activeAppEnvironment.AppInfo.Version,
                Culture = _activeAppEnvironment.AppInfo.DefaultCulture,
                DebugMode = _activeAppEnvironment.DebugMode,
                DesiredTimeZoneValue = _activeAppEnvironment.AppInfo.DefaultTimeZone,
                Theme = _activeAppEnvironment.AppInfo.DefaultTheme,
                EnvironmentConfigsJson = ContentFormatter.Serialize(_activeAppEnvironment
                                            .Configs.Where(c => c.AccessibleInClientSide == true)
                                            .Select(c => new { value = c.Value, key = c.Key })),
                BaseHref = _activeAppEnvironment.GetHostVirtualPath()
            };
        }
    }
}
