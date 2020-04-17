using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.OwinCore.Implementations
{
    public class AspNetCoreAppEnvironmentsProvider : IAppEnvironmentsProvider
    {
        public static AspNetCoreAppEnvironmentsProvider Current { get; } = new AspNetCoreAppEnvironmentsProvider();

        private AspNetCoreAppEnvironmentsProvider()
        {

        }

        public IConfiguration Configuration { get; set; } = default!;

#if DotNet
        public Microsoft.AspNetCore.Hosting.IHostingEnvironment HostingEnvironment { get; set; } = default!;
#else
        public IWebHostEnvironment WebHostEnvironment { get; set; } = default!;
#endif

        private AppEnvironment? _appEnvironment;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public virtual void Init()
        {
            List<EnvironmentConfig> configs = new List<EnvironmentConfig> { };

            void TryReadConfig<T>(IConfiguration configuration, string key)
            {
                if (configuration != null && configuration[key] != null)
                    configs.Add(new EnvironmentConfig { Key = key, Value = configuration.GetValue<T>(key) });
            }

            void TryReadConnectionString(string key)
            {
                if (configs.Any(c => c.Key == key))
                    return;
                string connectionString = Configuration.GetConnectionString(key);
                if (connectionString != null)
                    configs.Add(new EnvironmentConfig { Key = key, Value = connectionString });
            }

            TryReadConfig<string>(Configuration, AppEnvironment.KeyValues.HostVirtualPath);
            TryReadConfig<string>(Configuration, AppEnvironment.KeyValues.IndexPagePath);
            TryReadConfig<string>(Configuration, AppEnvironment.KeyValues.StaticFilesRelativePath);
            TryReadConfig<string>(Configuration, AppEnvironment.KeyValues.IdentityCertificatePassword);
            TryReadConfig<bool>(Configuration, AppEnvironment.KeyValues.RequireSsl);
            TryReadConfig<long>(Configuration, AppEnvironment.KeyValues.EventLogId);

            IConfiguration? data = Configuration.GetChildren().ExtendedSingleOrDefault("Finding data config", c => c.Key == nameof(AppEnvironment.KeyValues.Data));
            if (data != null)
            {
                TryReadConfig<string>(data, AppEnvironment.KeyValues.Data.DbIsolationLevel);
                TryReadConfig<string>(data, AppEnvironment.KeyValues.Data.LogDbConnectionstring);
            }

            IConfiguration? signalr = Configuration.GetChildren().ExtendedSingleOrDefault("Finding signalr config", c => c.Key == nameof(AppEnvironment.KeyValues.Signalr));
            if (signalr != null)
            {
                TryReadConfig<string>(signalr, AppEnvironment.KeyValues.Signalr.SignalRAzureServiceBusConnectionString);
                TryReadConfig<string>(signalr, AppEnvironment.KeyValues.Signalr.SignalRSqlServerConnectionString);
                TryReadConfig<int>(signalr, AppEnvironment.KeyValues.Signalr.SignalRSqlServerTableCount);
            }

            IConfiguration? identityServer = Configuration.GetChildren().ExtendedSingleOrDefault("Finding identityServer config", c => c.Key == nameof(AppEnvironment.KeyValues.IdentityServer));
            if (identityServer != null)
            {
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.IdentityServerSiteName);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.ActiveDirectoryName);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.LoginPagePath);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.FacebookClientId);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.FacebookSecret);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.GoogleClientId);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.GoogleSecret);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.TwitterClientId);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.TwitterSecret);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.LinkedInClientId);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.LinkedInSecret);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.MicrosoftClientId);
                TryReadConfig<string>(identityServer, AppEnvironment.KeyValues.IdentityServer.MicrosoftSecret);
            }

            IConfiguration? hangfire = Configuration.GetChildren().ExtendedSingleOrDefault("Finding hangfire config", c => c.Key == nameof(AppEnvironment.KeyValues.Hangfire));
            if (hangfire != null)
            {
                TryReadConfig<string>(hangfire, AppEnvironment.KeyValues.Hangfire.JobSchedulerDbConnectionString);
            }

            TryReadConnectionString(AppEnvironment.KeyValues.Signalr.SignalRSqlServerConnectionString);
            TryReadConnectionString(AppEnvironment.KeyValues.Hangfire.JobSchedulerDbConnectionString);
            TryReadConnectionString(AppEnvironment.KeyValues.Data.LogDbConnectionstring);

            IConfiguration? appInfo = Configuration.GetChildren().ExtendedSingleOrDefault("Finding appInfo config", c => c.Key == nameof(AppEnvironment.AppInfo));

            _appEnvironment = new AppEnvironment
            {
#if DotNet
                Name = HostingEnvironment.EnvironmentName,
#else
                Name = WebHostEnvironment.EnvironmentName,
#endif
                IsActive = true,
#if DotNet
                DebugMode = HostingEnvironment.IsDevelopment(),
#else
                DebugMode = WebHostEnvironment.IsDevelopment(),
#endif
                AppInfo = new EnvironmentAppInfo
                {
#if DotNet
                    Name = HostingEnvironment.ApplicationName,
#else
                    Name = WebHostEnvironment.ApplicationName,
#endif
                    Version = (Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()!).Version,
                    DefaultTimeZone = appInfo?.GetValue<string?>(nameof(EnvironmentAppInfo.DefaultTimeZone), defaultValue: null)
                },
                Configs = configs
            };

            IConfiguration? security = Configuration.GetChildren().ExtendedSingleOrDefault("Finding security config", c => c.Key == nameof(AppEnvironment.Security));

            _appEnvironment.Security.DefaultClientId = security?.GetValue<string?>(nameof(EnvironmentSecurity.DefaultClientId), defaultValue: null) ?? _appEnvironment.Security.DefaultClientId;
            _appEnvironment.Security.IssuerName = security?.GetValue<string?>(nameof(EnvironmentSecurity.IssuerName), defaultValue: null) ?? _appEnvironment.Security.IssuerName;
            _appEnvironment.Security.Scopes = security?.GetValue<string[]?>(nameof(EnvironmentSecurity.Scopes), defaultValue: null) ?? _appEnvironment.Security.Scopes;
            _appEnvironment.Security.SsoServerUrl = security?.GetValue<string?>(nameof(EnvironmentSecurity.SsoServerUrl), defaultValue: null) ?? _appEnvironment.Security.SsoServerUrl;

            DefaultAppEnvironmentsProvider.Current = this;
        }

        public virtual (bool success, string? message) TryGetActiveAppEnvironment(out AppEnvironment? activeAppEnvironment)
        {
            activeAppEnvironment = _appEnvironment;

            if (activeAppEnvironment != null)
                return (true, null);

            return (false, $"Call {nameof(Init)} first");
        }

        public virtual AppEnvironment GetActiveAppEnvironment()
        {
            var (success, message) = TryGetActiveAppEnvironment(out _appEnvironment);

            if (success == true)
                return _appEnvironment!;

            throw new InvalidOperationException(message);
        }
    }
}
