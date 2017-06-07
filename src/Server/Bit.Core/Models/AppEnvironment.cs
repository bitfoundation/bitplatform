using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Core.Models
{
    [Serializable]
    public class AppEnvironment
    {
        public virtual string Name { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual bool DebugMode { get; set; }

        public virtual EnvironmentAppInfo AppInfo { get; set; } = new EnvironmentAppInfo { };

        public virtual EnvironmentSecurity Security { get; set; } = new EnvironmentSecurity { };

        public virtual EnvironmentCulture[] Cultures { get; set; } = new EnvironmentCulture[] { };

        public virtual EnvironmentTheme[] Themes { get; set; } = new EnvironmentTheme[] { };

        public virtual List<EnvironmentConfig> Configs { get; set; } = new List<EnvironmentConfig>();

        public virtual T GetConfig<T>(string configKey)
        {
            if (configKey == null)
                throw new ArgumentNullException(nameof(configKey));

            if (Configs == null)
                throw new InvalidOperationException("App environment is not valid");

            EnvironmentConfig config = Configs.SingleOrDefault(c =>
                string.Equals(c.Key, configKey, StringComparison.OrdinalIgnoreCase));

            if (config == null)
                throw new InvalidOperationException($"No config found named {configKey} in app environment named {Name}");

            return (T)config.Value;
        }

        public virtual T GetConfig<T>(string configKey, T defaultValueOnNotFound)
        {
            if (configKey == null)
                throw new ArgumentNullException(nameof(configKey));

            if (Configs == null)
                throw new InvalidOperationException("App environment is not valid");

            EnvironmentConfig config = Configs.SingleOrDefault(c =>
                string.Equals(c.Key, configKey, StringComparison.OrdinalIgnoreCase));

            if (config == null)
                return defaultValueOnNotFound;
            else
                return (T)config.Value;
        }
    }

    [Serializable]
    public class EnvironmentAppInfo
    {
        public virtual string Version { get; set; }

        public virtual string Name { get; set; }

        public virtual string DefaultTimeZone { get; set; }

        public virtual string DefaultTheme { get; set; }

        public virtual string DefaultCulture { get; set; }
    }

    [Serializable]
    public class EnvironmentSecurity
    {
        public virtual string SSOServerUrl { get; set; }

        public virtual string ClientSecret { get; set; }

        public virtual string ClientName { get; set; }

        public virtual string[] Scopes { get; set; }
    }

    [Serializable]
    public class EnvironmentTheme
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public class EnvironmentCulture
    {
        public virtual string Name { get; set; }

        public virtual List<EnvironmentCultureValue> Values { get; set; } = new List<EnvironmentCultureValue>();
    }

    [Serializable]
    public class EnvironmentCultureValue
    {
        public virtual string Name { get; set; }

        public virtual string Title { get; set; }
    }

    [Serializable]
    public class EnvironmentConfig
    {
        public virtual string Key { get; set; }

        public virtual object Value { get; set; }

        public virtual bool AccessibleInClientSide { get; set; }
    }
}