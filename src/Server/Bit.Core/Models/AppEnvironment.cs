using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Core.Models
{
    public class AppEnvironment
    {
        public virtual string Name { get; set; }

        public virtual bool IsActive { get; set; }

        public virtual bool DebugMode { get; set; }

        public virtual EnvironmentAppInfo AppInfo { get; set; } = new EnvironmentAppInfo { };

        public virtual EnvironmentSecurity Security { get; set; } = new EnvironmentSecurity
        {
            Scopes = new[] { "openid", "profile", "user_info" }
        };

        public virtual EnvironmentCulture[] Cultures { get; set; } = Array.Empty<EnvironmentCulture>();

        public virtual EnvironmentTheme[] Themes { get; set; } = Array.Empty<EnvironmentTheme>();

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
                value = default;
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

        public virtual T GetConfig<T>(string configKey, Func<T> defaultValueOnNotFoundProvider)
        {
            if (!TryGetConfig(configKey, out T value))
                return defaultValueOnNotFoundProvider();
            else
                return value;
        }

        public virtual string GetHostVirtualPath()
        {
            return GetConfig("HostVirtualPath", "/");
        }

        public virtual string GetSsoUrl()
        {
            return Security?.SsoServerUrl ?? $"{GetHostVirtualPath()}core";
        }

        public virtual string GetSsoIssuerName()
        {
            return Security?.IssuerName ?? AppInfo.Name;
        }

        public virtual string GetSsoDefaultClientId()
        {
            return Security?.DefaultClientId ?? AppInfo.Name;
        }

        public virtual bool HasConfig(string configKey)
        {
            if (configKey == null)
                throw new ArgumentNullException(nameof(configKey));

            return Configs.Any(c => string.Equals(c.Key, configKey, StringComparison.OrdinalIgnoreCase));
        }

        public virtual void AddOrReplace<T>(string key, T value)
        {
            AddOrReplace(new EnvironmentConfig { Key = key, Value = value });
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

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(IsActive)}: {IsActive}, {nameof(DebugMode)}: {DebugMode}";
        }
    }

    public class EnvironmentAppInfo
    {
        public virtual string Version { get; set; }

        public virtual string Name { get; set; }

        public virtual string DefaultTimeZone { get; set; }

        public virtual string DefaultTheme { get; set; }

        public virtual string DefaultCulture { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Version)}: {Version}";
        }
    }

    public class EnvironmentSecurity
    {
        public virtual string SsoServerUrl { get; set; }

        public virtual string IssuerName { get; set; }

        public virtual string[] Scopes { get; set; }

        /// <summary>
        /// It's used in redirects of InvokeLogin and RedirectToSsoIfNotLoggedIn middlewares to sso
        /// </summary>
        public virtual string DefaultClientId { get; set; }

        public override string ToString()
        {
            return $"{nameof(IssuerName)}: {IssuerName}, {nameof(DefaultClientId)}: {DefaultClientId}, {nameof(Scopes)}: {Scopes}";
        }
    }

    public class EnvironmentTheme
    {
        public virtual string Name { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}";
        }
    }

    public class EnvironmentCulture
    {
        public virtual string Name { get; set; }

        public virtual List<EnvironmentCultureValue> Values { get; set; } = new List<EnvironmentCultureValue>();

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}";
        }
    }

    public class EnvironmentCultureValue
    {
        public virtual string Name { get; set; }

        public virtual string Title { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Title)}: {Title}";
        }
    }

    public class EnvironmentConfig
    {
        public virtual string Key { get; set; }

        public virtual object Value { get; set; }

        public virtual bool AccessibleInClientSide { get; set; }

        public override string ToString()
        {
            return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
        }
    }
}