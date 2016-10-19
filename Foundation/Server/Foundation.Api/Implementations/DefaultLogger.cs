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
        private readonly ILogStore _logStore;

        protected DefaultLogger()
        {
        }

        public DefaultLogger(ILogStore logStore, IAppEnvironmentProvider appEnvironmentProvider,
            IDateTimeProvider dateTimeProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (dateTimeProvider == null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            if (logStore == null)
                throw new ArgumentNullException(nameof(logStore));

            _dateTimeProvider = dateTimeProvider;
            _logStore = logStore;
            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        public virtual void LogWarning(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Warning";

            LogEntry logEntry = CreateLogEntry(message, severity);

            _logStore.SaveLog(logEntry);
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

            _logStore.SaveLog(logEntry);
        }

        public virtual async Task LogWarningAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Warning";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await _logStore.SaveLogAsync(logEntry);
        }

        public virtual void LogInformation(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Information";

            LogEntry logEntry = CreateLogEntry(message, severity);

            _logStore.SaveLog(logEntry);
        }

        public virtual async Task LogFatalAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await _logStore.SaveLogAsync(logEntry);
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

            _logStore.SaveLog(logEntry);
        }

        public virtual async Task LogInformationAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Information";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await _logStore.SaveLogAsync(logEntry);
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

            await _logStore.SaveLogAsync(logEntry);
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