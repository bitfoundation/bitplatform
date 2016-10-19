using System;
using System.Linq;
using System.Threading.Tasks;
using Foundation.Api.Contracts;
using Foundation.Api.Models;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Foundation.Model.DomainModels;
using System.Threading;

namespace Foundation.Api.Implementations
{
    public class DefaultPageModelProvider : IDefaultPageModelProvider
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly IUserSettingProvider _usersSettingsProvider;
        private readonly IContentFormatter _contentFormatter;

        protected DefaultPageModelProvider()
        {
        }

        public DefaultPageModelProvider(IContentFormatter contentFormatter, IAppEnvironmentProvider appEnvironmentProvider, IUserSettingProvider usersSettingsProvider = null)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (contentFormatter == null)
                throw new ArgumentNullException(nameof(contentFormatter));

            _usersSettingsProvider = usersSettingsProvider;
            _appEnvironmentProvider = appEnvironmentProvider;
            _contentFormatter = contentFormatter;
        }

        public virtual DefaultPageModel GetDefaultPageModel()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            DefaultPageModel defaultPageModel = new DefaultPageModel
            {
                AppVersion = activeAppEnvironment.AppInfo.Version,
                DebugMode = activeAppEnvironment.DebugMode,
                AppName = activeAppEnvironment.AppInfo.Name
            };

            UserSetting userSetting = _usersSettingsProvider?.GetCurrentUserSetting();

            string theme = userSetting?.Theme ?? activeAppEnvironment.AppInfo.DefaultTheme;

            string culture = userSetting?.Culture ?? activeAppEnvironment.AppInfo.DefaultCulture;

            string desiredTimeZone = userSetting?.DesiredTimeZone ??
                                           activeAppEnvironment.AppInfo.DefaultTimeZone;

            string desiredTimeZoneValue = null;

            if (culture == null || string.Equals(culture, "Auto", StringComparison.OrdinalIgnoreCase))
                culture = "EnUs";

            if (desiredTimeZone != null &&
                !string.Equals(desiredTimeZone, "Auto", StringComparison.CurrentCulture))
                desiredTimeZoneValue = desiredTimeZone;

            string appTitle = activeAppEnvironment.Cultures
                .Single(c => c.Name == culture).Values.Single(v =>
                    string.Equals(v.Name, "AppTitle", StringComparison.OrdinalIgnoreCase)).Title;

            defaultPageModel.AppTitle = appTitle;
            defaultPageModel.Culture = culture;
            defaultPageModel.DesiredTimeZoneValue = desiredTimeZoneValue;
            defaultPageModel.Theme = theme;

            defaultPageModel.EnvironmentConfigsJSON = _contentFormatter.Serialize(activeAppEnvironment
                .Configs.Where(c => c.AccessibleInClientSide == true)
                .Select(c => new { value = c.Value, key = c.Key }));

            defaultPageModel.BaseHref = activeAppEnvironment.GetConfig("ClientHostVirtualPath", "/");

            return defaultPageModel;
        }

        public virtual async Task<DefaultPageModel> GetDefaultPageModelAsync(CancellationToken cancellationToken)
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            DefaultPageModel defaultPageModel = new DefaultPageModel
            {
                AppVersion = activeAppEnvironment.AppInfo.Version,
                DebugMode = activeAppEnvironment.DebugMode,
                AppName = activeAppEnvironment.AppInfo.Name
            };

            UserSetting userSetting = await _usersSettingsProvider.GetCurrentUserSettingAsync(cancellationToken);

            string theme = userSetting?.Theme ?? activeAppEnvironment.AppInfo.DefaultTheme;

            string culture = userSetting?.Culture ?? activeAppEnvironment.AppInfo.DefaultCulture;

            string desiredTimeZone = userSetting?.DesiredTimeZone ??
                                           activeAppEnvironment.AppInfo.DefaultTimeZone;

            string desiredTimeZoneValue = null;

            if (culture == null || string.Equals(culture, "Auto", StringComparison.OrdinalIgnoreCase))
                culture = "EnUs";

            if (desiredTimeZone != null &&
                !string.Equals(desiredTimeZone, "Auto", StringComparison.CurrentCulture))
                desiredTimeZoneValue = desiredTimeZone;

            string appTitle = activeAppEnvironment.Cultures
                .Single(c => c.Name == culture).Values.Single(v =>
                    string.Equals(v.Name, "AppTitle", StringComparison.OrdinalIgnoreCase)).Title;

            defaultPageModel.AppTitle = appTitle;
            defaultPageModel.Culture = culture;
            defaultPageModel.DesiredTimeZoneValue = desiredTimeZoneValue;
            defaultPageModel.Theme = theme;

            defaultPageModel.EnvironmentConfigsJSON = _contentFormatter.Serialize(activeAppEnvironment
                .Configs.Where(c => c.AccessibleInClientSide == true)
                .Select(c => new { value = c.Value, key = c.Key }));

            defaultPageModel.BaseHref = activeAppEnvironment.GetConfig("ClientHostVirtualPath", "/");

            return defaultPageModel;
        }
    }
}