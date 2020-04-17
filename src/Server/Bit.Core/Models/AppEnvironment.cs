using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Bit.Core.Models
{
    public class AppEnvironment
    {
        public class KeyValues
        {
            public static readonly string HostVirtualPath = nameof(HostVirtualPath);
            public static readonly string HostVirtualPathDefaultValue = "/";

            public static readonly string IndexPagePath = nameof(IndexPagePath);
            public static readonly string IndexPagePathDefaultValue = "indexPage.html";

            public static readonly string StaticFilesRelativePath = nameof(StaticFilesRelativePath);
            public static readonly string StaticFilesRelativePathDefaultValue = "./wwwroot/";

            public static readonly string IdentityCertificatePassword = nameof(IdentityCertificatePassword);

            public static readonly string IdentityServerCertificatePath = nameof(IdentityServerCertificatePath);
            public static readonly string IdentityServerCertificatePathDefaultValue = "IdentityServerCertificate.pfx";

            public static readonly string RequireSsl = nameof(RequireSsl);
            public static readonly bool RequireSslDefaultValue = false;

            public static readonly string EventLogId = nameof(EventLogId);

            public class Data
            {
                public static readonly string DbIsolationLevel = nameof(DbIsolationLevel);
                public static readonly string DbIsolationLevelDefaultValue = nameof(IsolationLevel.ReadCommitted);

                public static readonly string LogDbConnectionstring = nameof(LogDbConnectionstring);
            }

            public class Signalr
            {
                public static readonly string SignalRAzureServiceBusConnectionString = nameof(SignalRAzureServiceBusConnectionString);

                public static readonly string SignalRSqlServerConnectionString = nameof(SignalRSqlServerConnectionString);

                public static readonly string SignalRSqlServerTableCount = nameof(SignalRSqlServerTableCount);
                public static readonly int SignalRSqlServerTableCountDefaultValue = 3;
            }

            public class IdentityServer
            {
                public static readonly string IdentityServerSiteName = nameof(IdentityServerSiteName);

                public static readonly string ActiveDirectoryName = nameof(ActiveDirectoryName);

                public static readonly string LoginPagePath = nameof(LoginPagePath);
                public static readonly string LoginPagePathDefaultValue = "loginPage.html";

                public static readonly string FacebookClientId = nameof(FacebookClientId);
                public static readonly string FacebookSecret = nameof(FacebookSecret);

                public static readonly string GoogleClientId = nameof(GoogleClientId);
                public static readonly string GoogleSecret = nameof(GoogleSecret);

                public static readonly string TwitterClientId = nameof(TwitterClientId);
                public static readonly string TwitterSecret = nameof(TwitterSecret);

                public static readonly string LinkedInClientId = nameof(LinkedInClientId);
                public static readonly string LinkedInSecret = nameof(LinkedInSecret);

                public static readonly string MicrosoftClientId = nameof(MicrosoftClientId);
                public static readonly string MicrosoftSecret = nameof(MicrosoftSecret);
            }

            public class Hangfire
            {
                public static readonly string JobSchedulerDbConnectionString = nameof(JobSchedulerDbConnectionString);
            }
        }

        public virtual string Name { get; set; } = default!;

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

        public virtual bool TryGetConfig<T>(string configKey, [MaybeNull] out T value)
        {
            if (configKey == null)
                throw new ArgumentNullException(nameof(configKey));

            if (Configs == null)
                throw new InvalidOperationException("App environment is not valid");

            EnvironmentConfig? config = Configs.ExtendedSingleOrDefault($"Finding {configKey}", c =>
                 string.Equals(c.Key, configKey, StringComparison.OrdinalIgnoreCase));

            if (config == null)
            {
                value = default!;
                return false;
            }
            else
            {
                value = (T)config.Value;
                return true;
            }
        }

        [return: MaybeNull]
        public virtual T GetConfig<T>(string configKey)
        {
            if (!TryGetConfig(configKey, out T value))
                throw new InvalidOperationException($"No config found named {configKey} in app environment named {Name}");

            return value;
        }

        [return: MaybeNull]
        public virtual T GetConfig<T>(string configKey, T defaultValueOnNotFound)
        {
            if (!TryGetConfig(configKey, out T value))
                return defaultValueOnNotFound;
            else
                return value;
        }

        [return: MaybeNull]
        public virtual T GetConfig<T>(string configKey, Func<T> defaultValueOnNotFoundProvider)
        {
            if (defaultValueOnNotFoundProvider == null)
                throw new ArgumentNullException(nameof(defaultValueOnNotFoundProvider));

            if (!TryGetConfig(configKey, out T value))
                return defaultValueOnNotFoundProvider();
            else
                return value;
        }

        public virtual string GetHostVirtualPath()
        {
            return GetConfig(KeyValues.HostVirtualPath, KeyValues.HostVirtualPathDefaultValue)!;
        }

        public virtual string? GetSsoUrl()
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

        public virtual void AddOrReplace<T>(string key, [MaybeNull] T value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            AddOrReplace(new EnvironmentConfig { Key = key, Value = value });
        }

        public virtual void AddOrReplace(EnvironmentConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!HasConfig(config.Key))
                Configs.Add(config);
            else
                Configs.ExtendedSingle($"Trying to find config with key: {config.Key}", c => string.Equals(c.Key, config.Key, StringComparison.OrdinalIgnoreCase)).Value = config.Value;
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(IsActive)}: {IsActive}, {nameof(DebugMode)}: {DebugMode}";
        }
    }

    public class EnvironmentAppInfo
    {
        public virtual string Version { get; set; } = default!;

        public virtual string Name { get; set; } = default!;

        public virtual string? DefaultTimeZone { get; set; }

        public virtual string? DefaultTheme { get; set; }

        public virtual string? DefaultCulture { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Version)}: {Version}";
        }
    }

    public class EnvironmentSecurity
    {
        public virtual string? SsoServerUrl { get; set; }

        public virtual string? IssuerName { get; set; }

        public virtual string[] Scopes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// It's used in redirects of InvokeLogin and RedirectToSsoIfNotLoggedIn middlewares to sso
        /// </summary>
        public virtual string? DefaultClientId { get; set; }

        public override string ToString()
        {
            return $"{nameof(IssuerName)}: {IssuerName}, {nameof(DefaultClientId)}: {DefaultClientId}, {nameof(Scopes)}: {Scopes}";
        }
    }

    public class EnvironmentTheme
    {
        public virtual string? Name { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}";
        }
    }

    public class EnvironmentCulture
    {
        public virtual string? Name { get; set; }

        public virtual List<EnvironmentCultureValue> Values { get; set; } = new List<EnvironmentCultureValue>();

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}";
        }
    }

    public class EnvironmentCultureValue
    {
        public virtual string Name { get; set; } = default!;

        public virtual string Title { get; set; } = default!;

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Title)}: {Title}";
        }
    }

    public class EnvironmentConfig
    {
        public virtual string Key { get; set; } = default!;

        public virtual object? Value { get; set; }

        public virtual bool AccessibleInClientSide { get; set; }

        public override string ToString()
        {
            return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
        }
    }
}