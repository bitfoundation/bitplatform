using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.OwinCore.Implementations
{
    public class BitTelemetryInitializer : ITelemetryInitializer
    {
        public virtual IHttpContextAccessor HttpContextAccessor { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Initialize(ITelemetry telemetry)
        {
            LogEntryAppLevelConstantInfo logEntryAppLevelConstantInfo = LogEntryAppLevelConstantInfo.GetAppConstantInfo();

            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.ApplicationName)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.ApplicationName), logEntryAppLevelConstantInfo.ApplicationName);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppVersion)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppVersion), logEntryAppLevelConstantInfo.AppVersion);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppEnvironmentName)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppEnvironmentName), logEntryAppLevelConstantInfo.AppEnvironmentName);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppWasInDebugMode)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppWasInDebugMode), logEntryAppLevelConstantInfo.AppWasInDebugMode.ToString());
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppServerName)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppServerName), logEntryAppLevelConstantInfo.AppServerName);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppServerOSVersion)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppServerOSVersion), logEntryAppLevelConstantInfo.AppServerOSVersion);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppServerAppDomainName)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppServerAppDomainName), logEntryAppLevelConstantInfo.AppServerAppDomainName);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppServerProcessId)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppServerProcessId), logEntryAppLevelConstantInfo.AppServerProcessId.ToString());
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppServerUserAccountName)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppServerUserAccountName), logEntryAppLevelConstantInfo.AppServerUserAccountName);
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppServerWas64Bit)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppServerWas64Bit), logEntryAppLevelConstantInfo.AppServerWas64Bit.ToString(CultureInfo.InvariantCulture));
            if (!telemetry.Context.GlobalProperties.ContainsKey(nameof(LogEntry.AppWas64Bit)))
                telemetry.Context.GlobalProperties.Add(nameof(LogEntry.AppWas64Bit), logEntryAppLevelConstantInfo.AppWas64Bit.ToString(CultureInfo.InvariantCulture));

            if (telemetry is RequestTelemetry requestTelemetry && HttpContextAccessor.HttpContext != null)
            {
                HttpContextAccessor.HttpContext.Items[nameof(RequestTelemetry)] = requestTelemetry;
            }
        }
    }

    public class ApplicationInsightsLogStore : ILogStore
    {
        private class KeyVal
        {
            public string Key { get; set; }

            public string Value { get; set; }

            public override string ToString()
            {
                return $"Key: {Key}, Value: {Value}";
            }
        }

        [Serializable]
        public class FatalException : Exception
        {
            public FatalException(string message)
                : base(message)
            {

            }

            public FatalException()
            {
            }

            public FatalException(string message, Exception innerException) 
                : base(message, innerException)
            {
            }
        }

        [Serializable]
        public class ErrorException : Exception
        {
            public ErrorException(string message)
                : base(message)
            {

            }

            public ErrorException()
            {
            }

            public ErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        [Serializable]
        public class WarningException : Exception
        {
            public WarningException(string message)
                : base(message)
            {

            }

            public WarningException()
            {
            }

            public WarningException(string message, Exception innerException) 
                : base(message, innerException)
            {
            }
        }

        [Serializable]
        public class InformationException : Exception
        {
            public InformationException(string message)
                : base(message)
            {

            }

            public InformationException()
            {
            }

            public InformationException(string message, Exception innerException) 
                : base(message, innerException)
            {
            }
        }

        public virtual IHttpContextAccessor HttpContextAccessor { get; set; }

        public virtual IContentFormatter Formatter { get; set; }

        public virtual void SaveLog(LogEntry logEntry)
        {
            PerformLog(logEntry);
        }

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            PerformLog(logEntry);

            return Task.CompletedTask;
        }

        private void PerformLog(LogEntry logEntry)
        {
            RequestTelemetry requestTelemetry = null;

            if (logEntry.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl)) && HttpContextAccessor.HttpContext != null)
            {
                HttpContext httpContext = HttpContextAccessor.HttpContext;

                if (!httpContext.Items.ContainsKey(nameof(RequestTelemetry)))
                    throw new InvalidOperationException($"Register app insight logger using dependencyManager.{nameof(IDependencyManagerExtensions.RegisterApplicationInsights)}();");

                requestTelemetry = (RequestTelemetry)httpContext.Items[nameof(RequestTelemetry)];

                IUserInformationProvider userInformationProvider = httpContext.RequestServices.GetRequiredService<IUserInformationProvider>();

                if (userInformationProvider.IsAuthenticated())
                    requestTelemetry.Context.User.AccountId = requestTelemetry.Context.User.AuthenticatedUserId = userInformationProvider.GetCurrentUserId();
            }

            List<KeyVal> keyValues = logEntry.LogData.Select(ld =>
            {
                string k = ld.Key;

                if (k == nameof(IRequestInformationProvider.HttpMethod)
                || k == nameof(IRequestInformationProvider.DisplayUrl)
                || k == nameof(IRequestInformationProvider.UserAgent)
                || k == "UserId"
                || k == "ResponseStatusCode"
                || k == nameof(IRequestInformationProvider.ClientIp)
                || ld.Value == null)
                    return null; // Already being logged by app insights!

                string v = null;

                if (ld.Value is string valueAsStr)
                    v = valueAsStr;

                if (k == "ClientLogs" || k == "OperationArgs")
                {
                    v = Formatter.Serialize(ld.Value);
                }
                else
                {
                    v = ld.Value.ToString();
                }

                return new KeyVal { Key = k, Value = v };
            })
            .Where(d => d != null)
            .ToList();

            keyValues.Add(new KeyVal { Key = nameof(LogEntry.MemoryUsage), Value = logEntry.MemoryUsage.ToString(CultureInfo.InvariantCulture) });

            if (logEntry.AppServerDateTime.HasValue)
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerDateTime), Value = logEntry.AppServerDateTime.ToString() });

            keyValues.Add(new KeyVal { Key = nameof(LogEntry.Severity), Value = logEntry.Severity });
            keyValues.Add(new KeyVal { Key = nameof(LogEntry.Message), Value = logEntry.Message });

            if (logEntry.Id.HasValue)
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.Id), Value = logEntry.Id.ToString() });

            if (logEntry.AppServerThreadId.HasValue)
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerThreadId), Value = logEntry.AppServerThreadId.ToString() });

            if (requestTelemetry != null)
            {
                LogData userAgent = logEntry.LogData.FirstOrDefault(ld => ld.Key == nameof(IRequestInformationProvider.UserAgent));
                if (userAgent != null)
                    requestTelemetry.Context.User.UserAgent = (string)userAgent.Value;

                foreach (KeyVal keyVal in keyValues.OrderBy(kv => kv.Key))
                {
                    if (!requestTelemetry.Properties.ContainsKey(keyVal.Key))
                        requestTelemetry.Properties.Add(keyVal.Key, keyVal.Value);
                }
            }
            else
            {
                TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);

                Dictionary<string, string> customData = new Dictionary<string, string>();

                foreach (KeyVal keyVal in keyValues.OrderBy(kv => kv.Key))
                {
                    if (!customData.ContainsKey(keyVal.Key))
                        customData.Add(keyVal.Key, keyVal.Value);
                }

                Exception ex = null;

                try
                {
                    customData.TryGetValue("ExceptionTypeAssemblyQualifiedName", out string exceptionTypeAssemblyQualifiedName);

                    if (!string.IsNullOrEmpty(exceptionTypeAssemblyQualifiedName))
                        ex = (Exception)Activator.CreateInstance(Type.GetType(exceptionTypeAssemblyQualifiedName) ?? throw new InvalidOperationException($"{exceptionTypeAssemblyQualifiedName} could not be found"), args: new object[] { logEntry.Message });
                }
                catch { }

                if (ex == null)
                {
                    ex = logEntry.Severity switch
                    {
                        "Information" => new InformationException(logEntry.Message),
                        "Warning" => new WarningException(logEntry.Message),
                        "Error" => new ErrorException(logEntry.Message),
                        "Fatal" => new FatalException(logEntry.Message),
                        _ => new Exception(logEntry.Message),
                    };
                }

                telemetryClient.TrackException(ex, customData);
            }
        }
    }
}
