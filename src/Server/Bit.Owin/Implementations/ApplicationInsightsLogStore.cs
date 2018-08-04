using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
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
        private class FatalException : Exception
        {
            public FatalException(string message)
                : base(message)
            {

            }
        }

        [Serializable]
        private class ErrorException : Exception
        {
            public ErrorException(string message)
                : base(message)
            {

            }
        }

        [Serializable]
        private class WarningException : Exception
        {
            public WarningException(string message)
                : base(message)
            {

            }
        }

        [Serializable]
        private class InformationException : Exception
        {
            public InformationException(string message)
                : base(message)
            {

            }
        }

        public virtual Lazy<IOwinContext> OwinContext { get; set; }

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
            TelemetryClient telemetryClient = null;
            IUserInformationProvider userInformationProvider = null;
            bool isPerRequestTelemetryClient = false;

            if (logEntry.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.RequestUri)))
            {
                IOwinContext owinContext = OwinContext.Value;

                IDependencyResolver resolver = owinContext.GetDependencyResolver();

                telemetryClient = resolver.Resolve<TelemetryClient>();

                userInformationProvider = resolver.Resolve<IUserInformationProvider>();

                isPerRequestTelemetryClient = true;
            }

            List<KeyVal> keyValues = logEntry.LogData.Select(ld =>
            {
                string k = ld.Key;

                if (k == nameof(IRequestInformationProvider.HttpMethod)
                || k == nameof(IRequestInformationProvider.RequestUri)
                || k == nameof(IRequestInformationProvider.UserAgent)
                || k == "UserId"
                || k == "ResponseStatusCode"
                || k == nameof(IRequestInformationProvider.ClientIp)
                || ld.Value == null)
                    return null;

                string v = null;

                if (ld.Value is string valueAsStr)
                    v = valueAsStr;

                if (k == "ClientLogs" || k == "OperationArgs")
                    v = Formatter.Serialize(ld.Value);
                else
                    v = ld.Value.ToString();

                return new KeyVal { Key = k, Value = v };
            })
            .Where(d => d != null)
            .ToList();

            try
            {
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppEnvironmentName), Value = logEntry.AppEnvironmentName });

                if (logEntry.AppServerProcessId.HasValue)
                    keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerProcessId), Value = logEntry.AppServerProcessId.ToString() });

                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerAppDomainName), Value = logEntry.AppServerAppDomainName });

                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerOSVersion), Value = logEntry.AppServerOSVersion });

                if (logEntry.AppServerDateTime.HasValue)
                    keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerDateTime), Value = logEntry.AppServerDateTime.ToString() });

                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerName), Value = logEntry.AppServerName });

                if (logEntry.AppWasInDebugMode.HasValue)
                    keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppWasInDebugMode), Value = logEntry.AppWasInDebugMode.ToString() });

                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerUserAccountName), Value = logEntry.AppServerUserAccountName });
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppVersion), Value = logEntry.AppVersion });
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.ApplicationName), Value = logEntry.ApplicationName });
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.Severity), Value = logEntry.Severity });
                keyValues.Add(new KeyVal { Key = nameof(LogEntry.Message), Value = logEntry.Message });

                if (logEntry.Id.HasValue)
                    keyValues.Add(new KeyVal { Key = nameof(LogEntry.Id), Value = logEntry.Id.ToString() });

                if (logEntry.AppServerThreadId.HasValue)
                    keyValues.Add(new KeyVal { Key = nameof(LogEntry.AppServerThreadId), Value = logEntry.AppServerThreadId.ToString() });

                if (isPerRequestTelemetryClient)
                {
                    if (userInformationProvider.IsAuthenticated())
                        telemetryClient.Context.User.AccountId = telemetryClient.Context.User.AuthenticatedUserId = userInformationProvider.GetCurrentUserId();

                    LogData userAgent = logEntry.LogData.FirstOrDefault(ld => ld.Key == nameof(IRequestInformationProvider.UserAgent));
                    if (userAgent != null)
                        telemetryClient.Context.User.UserAgent = (string)userAgent.Value;

                    foreach (KeyVal keyVal in keyValues.OrderBy(kv => kv.Key))
                    {
                        if (!telemetryClient.Context.Properties.ContainsKey(keyVal.Key))
                            telemetryClient.Context.Properties.Add(keyVal.Key, keyVal.Value);
                    }
                }
                else
                {
                    telemetryClient = new TelemetryClient();

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
                            ex = (Exception)Activator.CreateInstance(Type.GetType(exceptionTypeAssemblyQualifiedName), args: new object[] { logEntry.Message });
                    }
                    catch { }

                    if (ex == null)
                    {
                        switch (logEntry.Severity)
                        {
                            case "Information":
                                ex = new InformationException(logEntry.Message);
                                break;
                            case "Warning":
                                ex = new WarningException(logEntry.Message);
                                break;
                            case "Error":
                                ex = new ErrorException(logEntry.Message);
                                break;
                            case "Fatal":
                                ex = new FatalException(logEntry.Message);
                                break;
                            default:
                                ex = new Exception(logEntry.Message);
                                break;
                        }
                    }

                    telemetryClient.TrackException(ex, customData);
                }
            }
            finally
            {
                telemetryClient.Flush();
            }
        }
    }
}
