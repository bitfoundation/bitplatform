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

        public virtual EnvironmentSecurity Security { get; set; } = new EnvironmentSecurity
        {
            Scopes = new [] { "openid", "profile", "user_info" }
        };

        public virtual EnvironmentCulture[] Cultures { get; set; } = new EnvironmentCulture[] { };

        public virtual EnvironmentTheme[] Themes { get; set; } = new EnvironmentTheme[] { };

        public virtual List<EnvironmentConfig> Configs { get; set; } = new List<EnvironmentConfig>();

        public virtual bool TryGetConfig<T>(string configKey, out T value)
        {
            if (configKey == null)
                throw new ArgumentNullException(nameof(configKey));

            if (Configs == null)
                throw new InvalidOperationException("App environment is not valid");

            EnvironmentConfig config = Configs.ExtendedSingleOrDefault($"Finding {configKey}", c =>
                 string.Equals(c.Key, configKey, StringComparison.OrdinalIgnoreCase));

            if (config == null)
            {
                value = default(T);
                return false;
            }
            else
            {
                value = (T)config.Value;
                return true;
            }
        }

        public virtual T GetConfig<T>(string configKey)
        {
            if (!TryGetConfig(configKey, out T value))
                throw new InvalidOperationException($"No config found named {configKey} in app environment named {Name}");

            return value;
        }

        public virtual T GetConfig<T>(string configKey, T defaultValueOnNotFound)
        {
            if (!TryGetConfig(configKey, out T value))
                return defaultValueOnNotFound;
            else
                return value;
        }

        public virtual string GetHostVirtualPath()
        {
            return GetConfig("HostVirtualPath", "/");
        }

        public virtual string GetSsoUrl()
        {
            return Security?.SSOServerUrl ?? $"{GetHostVirtualPath()}core";
        }

        public virtual string GetSsoIssuerName()
        {
            return Security?.IssuerName ?? AppInfo.Name;
        }

        public virtual bool HasConfig(string configKey)
        {
            if (configKey == null)
                throw new ArgumentNullException(nameof(configKey));

            return Configs.Any(c => string.Equals(c.Key, configKey, StringComparison.OrdinalIgnoreCase));
        }

        public virtual void AddOrReplace(EnvironmentConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!HasConfig(config.Key))
                Configs.Add(config);
            else
                Configs.Find(c => string.Equals(c.Key, config.Key, StringComparison.OrdinalIgnoreCase)).Value = config.Value;
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

        public virtual string IssuerName { get; set; }

        public virtual string ClientSecret { get; set; }

        public virtual string ClientId { get; set; }

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