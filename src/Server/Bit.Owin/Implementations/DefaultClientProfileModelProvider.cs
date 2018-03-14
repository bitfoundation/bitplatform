using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Model.DomainModels;
using Bit.Owin.Contracts;
using Bit.Owin.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class DefaultClientProfileModelProvider : IClientProfileModelProvider
    {
        public virtual AppEnvironment AppEnvironment { get; set; }
        public virtual IUserSettingProvider UsersSettingsProvider { get; set; }
        public virtual IContentFormatter ContentFormatter { get; set; }

        public virtual ClientProfileModel GetClientProfileModel()
        {
            ClientProfileModel clientProfileMdoel = new ClientProfileModel
            {
                AppVersion = AppEnvironment.AppInfo.Version,
                DebugMode = AppEnvironment.DebugMode,
                AppName = AppEnvironment.AppInfo.Name
            };

            UserSetting userSetting = UsersSettingsProvider?.GetCurrentUserSetting();

            string theme = userSetting?.Theme ?? AppEnvironment.AppInfo.DefaultTheme;

            string culture = userSetting?.Culture ?? AppEnvironment.AppInfo.DefaultCulture;

            string desiredTimeZone = userSetting?.DesiredTimeZone ??
                                           AppEnvironment.AppInfo.DefaultTimeZone;

            string desiredTimeZoneValue = null;

            if (culture == null || string.Equals(culture, "Auto", StringComparison.OrdinalIgnoreCase))
                culture = "EnUs";

            if (desiredTimeZone != null &&
                !string.Equals(desiredTimeZone, "Auto", StringComparison.CurrentCulture))
                desiredTimeZoneValue = desiredTimeZone;

            string appTitle = AppEnvironment.Cultures.Any() ? AppEnvironment.Cultures
                .ExtendedSingle($"Finding culture {culture} in environment {AppEnvironment.Name}", c => c.Name == culture).Values.ExtendedSingle($"Finding AppTitle in culture {culture}", v =>
                      string.Equals(v.Name, "AppTitle", StringComparison.OrdinalIgnoreCase)).Title : string.Empty;

            clientProfileMdoel.AppTitle = appTitle;
            clientProfileMdoel.Culture = culture;
            clientProfileMdoel.DesiredTimeZoneValue = desiredTimeZoneValue;
            clientProfileMdoel.Theme = theme;

            clientProfileMdoel.EnvironmentConfigsJson = ContentFormatter.Serialize(AppEnvironment
                .Configs.Where(c => c.AccessibleInClientSide == true)
                .Select(c => new { value = c.Value, key = c.Key }));

            clientProfileMdoel.BaseHref = AppEnvironment.GetHostVirtualPath();

            return clientProfileMdoel;
        }

        public virtual async Task<ClientProfileModel> GetClientProfileModelAsync(CancellationToken cancellationToken)
        {
            ClientProfileModel clientAppProfileModel = new ClientProfileModel
            {
                AppVersion = AppEnvironment.AppInfo.Version,
                DebugMode = AppEnvironment.DebugMode,
                AppName = AppEnvironment.AppInfo.Name
            };

            UserSetting userSetting = UsersSettingsProvider == null ? null : await UsersSettingsProvider.GetCurrentUserSettingAsync(cancellationToken).ConfigureAwait(false);

            string theme = userSetting?.Theme ?? AppEnvironment.AppInfo.DefaultTheme;

            string culture = userSetting?.Culture ?? AppEnvironment.AppInfo.DefaultCulture;

            string desiredTimeZone = userSetting?.DesiredTimeZone ??
                                           AppEnvironment.AppInfo.DefaultTimeZone;

            string desiredTimeZoneValue = null;

            if (culture == null || string.Equals(culture, "Auto", StringComparison.OrdinalIgnoreCase))
                culture = "EnUs";

            if (desiredTimeZone != null &&
                !string.Equals(desiredTimeZone, "Auto", StringComparison.CurrentCulture))
                desiredTimeZoneValue = desiredTimeZone;

            string appTitle = AppEnvironment.Cultures.Any() ? AppEnvironment.Cultures
                .ExtendedSingle($"Finding culture {culture} in environment {AppEnvironment.Name}", c => c.Name == culture).Values.ExtendedSingle($"Finding AppTitle in culture {culture}", v =>
                      string.Equals(v.Name, "AppTitle", StringComparison.OrdinalIgnoreCase)).Title : string.Empty;

            clientAppProfileModel.AppTitle = appTitle;
            clientAppProfileModel.Culture = culture;
            clientAppProfileModel.DesiredTimeZoneValue = desiredTimeZoneValue;
            clientAppProfileModel.Theme = theme;

            clientAppProfileModel.EnvironmentConfigsJson = ContentFormatter.Serialize(AppEnvironment
                .Configs.Where(c => c.AccessibleInClientSide == true)
                .Select(c => new { value = c.Value, key = c.Key }));

            clientAppProfileModel.BaseHref = AppEnvironment.GetHostVirtualPath();

            return clientAppProfileModel;
        }
    }
}