using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Foundation.Api.Exceptions;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using System.Linq;

namespace Foundation.Api.Implementations
{
    public class DefaultLogger : ILogger
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEnumerable<ILogStore> _logStores;

        protected DefaultLogger()
        {
        }

        public DefaultLogger(IEnumerable<ILogStore> logStores, IAppEnvironmentProvider appEnvironmentProvider,
            IDateTimeProvider dateTimeProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (dateTimeProvider == null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            if (logStores == null)
                throw new ArgumentNullException(nameof(logStores));

            _dateTimeProvider = dateTimeProvider;
            _logStores = logStores;
            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        public virtual void LogWarning(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Warning";

            LogEntry logEntry = CreateLogEntry(message, severity);
        }

        private void SaveLogEntryUsingAllLogStores(LogEntry logEntry)
        {
            List<Exception> logExceptions = new List<Exception>();

            foreach (ILogStore logStore in _logStores)
            {
                try
                {
                    logStore.SaveLog(logEntry);
                }
                catch (Exception exp)
                {
                    logExceptions.Add(exp);
                }
            }

            if (logExceptions.Any())
                throw new AggregateException(logExceptions);
        }

        private async Task SaveLogEntryUsingAllLogStoresAsync(LogEntry logEntry)
        {
            List<Exception> logExceptions = new List<Exception>();

            foreach (ILogStore logStore in _logStores)
            {
                try
                {
                    await logStore.SaveLogAsync(logEntry);
                }
                catch (Exception exp)
                {
                    logExceptions.Add(exp);
                }
            }

            if (logExceptions.Any())
                throw new AggregateException(logExceptions);
        }

        public virtual IEnumerable<LogData> LogData { get; set; } = new Collection<LogData>();

        public virtual void AddLogData(string key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (LogData.Any(ld => ld.Value == value))
                return;

            ((Collection<LogData>)LogData).Add(new LogData
            {
                Key = key,
                Value = value
            });
        }

        public virtual void LogFatal(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        public virtual async Task LogWarningAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Warning";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await SaveLogEntryUsingAllLogStoresAsync(logEntry);
        }

        public virtual void LogInformation(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Information";

            LogEntry logEntry = CreateLogEntry(message, severity);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        public virtual async Task LogFatalAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await SaveLogEntryUsingAllLogStoresAsync(logEntry);
        }

        public virtual void LogException(Exception exp, string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (exp == null)
                throw new ArgumentNullException(nameof(exp));

            string severity = "Error";

            if (!(exp is AppException))
                severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            AddLogData("Exception", exp);
            AddLogData("ExceptionType", exp.GetType().FullName);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        public virtual async Task LogInformationAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Information";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await SaveLogEntryUsingAllLogStoresAsync(logEntry);
        }

        public virtual async Task LogExceptionAsync(Exception exp, string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (exp == null)
                throw new ArgumentNullException(nameof(exp));

            string severity = "Error";

            if (!(exp is AppException))
                severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            AddLogData("Exception", exp);
            AddLogData("ExceptionType", exp.GetType().FullName);

            await SaveLogEntryUsingAllLogStoresAsync(logEntry);
        }

        private LogEntry CreateLogEntry(string message, string severity)
        {
            LogEntry logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Message = message,
                Severity = severity,
                LogData = LogData,
                ApplicationName = _activeAppEnvironment.AppInfo.Name,
                AppVersion = _activeAppEnvironment.AppInfo.Version,
                AppEnvironmentName = _activeAppEnvironment.Name,
                AppWasInDebugMode = _activeAppEnvironment.DebugMode,
                AppServerName = Environment.MachineName,
                AppServerDateTime = _dateTimeProvider.GetCurrentUtcDateTime(),
                AppServerOSVersion = Environment.OSVersion.ToString(),
                AppServerAppDomainName = AppDomain.CurrentDomain.FriendlyName,
                AppServerProcessId = Process.GetCurrentProcess().Id,
                AppServerThreadId = Environment.CurrentManagedThreadId,
                AppServerUserAccountName = $"{Environment.UserDomainName}\\{Environment.UserName}"
            };

            return logEntry;
        }
    }
}