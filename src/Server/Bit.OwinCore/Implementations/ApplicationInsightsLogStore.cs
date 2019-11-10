using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.OwinCore.Implementations
{
    internal class AppInsightsLogKeyVal
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return $"Key: {Key}, Value: {Value}";
        }
    }

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
                if (!HttpContextAccessor.HttpContext.Items.TryGetValue("LogKeyValues", out object logKeyValuesAsObj))
                    return; // based on logger's policy, requests without any issue (ok responses) won't have log key values and we don't have anything to provide to app insights here.

                List<AppInsightsLogKeyVal> logKeyValues = (List<AppInsightsLogKeyVal>)logKeyValuesAsObj;

                AppInsightsLogKeyVal userAgent = logKeyValues.FirstOrDefault(ld => ld.Key == nameof(IRequestInformationProvider.UserAgent));

                if (userAgent != null)
                    requestTelemetry.Context.User.UserAgent = userAgent.Value;

                AppInsightsLogKeyVal userId = logKeyValues.FirstOrDefault(ld => ld.Key == "UserId");

                if (userId != null)
                    requestTelemetry.Context.User.AccountId = requestTelemetry.Context.User.Id = requestTelemetry.Context.User.AuthenticatedUserId = requestTelemetry.Context.User.AuthenticatedUserId = userId.Value;

                foreach (AppInsightsLogKeyVal keyVal in logKeyValues.OrderBy(kv => kv.Key))
                {
                    if (keyVal.Key == nameof(IRequestInformationProvider.UserAgent) || keyVal.Key == "UserId")
                        continue;
                    if (!requestTelemetry.Properties.ContainsKey(keyVal.Key))
                        requestTelemetry.Properties.Add(keyVal.Key, keyVal.Value);
                }
            }
        }
    }

    public class ApplicationInsightsLogStore : ILogStore
    {
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

        public TelemetryClient TelemetryClient { get; set; }

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
            List<AppInsightsLogKeyVal> logKeyValues = PopulateLogKeyValues(logEntry);

            if (logEntry.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl)) && HttpContextAccessor.HttpContext != null)
            {
                if (!HttpContextAccessor.HttpContext.Items.ContainsKey("LogKeyValues"))
                    HttpContextAccessor.HttpContext.Items.Add("LogKeyValues", logKeyValues); // someone might register app insights logger multiple times!
                return; // The rest of things will be handled with BitTelemetryInitializer.
            }

            Dictionary<string, string> customData = new Dictionary<string, string>();

            foreach (AppInsightsLogKeyVal keyVal in logKeyValues.OrderBy(kv => kv.Key))
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

            TelemetryClient.TrackException(ex, customData);
        }

        List<AppInsightsLogKeyVal> PopulateLogKeyValues(LogEntry logEntry)
        {
            List<AppInsightsLogKeyVal> keyValues = logEntry.LogData.Select(ld =>
            {
                string k = ld.Key;

                if (k == nameof(IRequestInformationProvider.HttpMethod)
                || k == nameof(IRequestInformationProvider.DisplayUrl)
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

                return new AppInsightsLogKeyVal { Key = k, Value = v };
            })
            .Where(d => d != null)
            .ToList();

            keyValues.Add(new AppInsightsLogKeyVal { Key = nameof(LogEntry.MemoryUsage), Value = logEntry.MemoryUsage.ToString(CultureInfo.InvariantCulture) });

            if (logEntry.AppServerDateTime.HasValue)
                keyValues.Add(new AppInsightsLogKeyVal { Key = nameof(LogEntry.AppServerDateTime), Value = logEntry.AppServerDateTime.ToString() });

            keyValues.Add(new AppInsightsLogKeyVal { Key = nameof(LogEntry.Severity), Value = logEntry.Severity });
            keyValues.Add(new AppInsightsLogKeyVal { Key = nameof(LogEntry.Message), Value = logEntry.Message });

            if (logEntry.Id.HasValue)
                keyValues.Add(new AppInsightsLogKeyVal { Key = nameof(LogEntry.Id), Value = logEntry.Id.ToString() });

            if (logEntry.AppServerThreadId.HasValue)
                keyValues.Add(new AppInsightsLogKeyVal { Key = nameof(LogEntry.AppServerThreadId), Value = logEntry.AppServerThreadId.ToString() });

            return keyValues;
        }
    }
}
