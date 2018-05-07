using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Exceptions;

namespace Bit.Owin.Implementations
{
    public class DefaultLogger : ILogger
    {
        public virtual IDateTimeProvider DateTimeProvider { get; set; }
        public virtual IEnumerable<ILogStore> LogStores { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

        private void SaveLogEntryUsingAllLogStores(LogEntry logEntry)
        {
            List<Exception> logExceptions = new List<Exception>();

            foreach (ILogStore logStore in LogStores)
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

            foreach (ILogStore logStore in LogStores)
            {
                try
                {
                    await logStore.SaveLogAsync(logEntry).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    logExceptions.Add(exp);
                }
            }

            if (logExceptions.Any())
                throw new AggregateException(logExceptions);
        }

        public virtual IEnumerable<LogData> LogData { get; protected set; } = new Collection<LogData>();

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

        public virtual async Task LogWarningAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            const string severity = "Warning";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await SaveLogEntryUsingAllLogStoresAsync(logEntry).ConfigureAwait(false);
        }

        public virtual void LogWarning(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            const string severity = "Warning";

            LogEntry logEntry = CreateLogEntry(message, severity);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        public virtual async Task LogFatalAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            await SaveLogEntryUsingAllLogStoresAsync(logEntry).ConfigureAwait(false);
        }

        public virtual void LogFatal(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            const string severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        public virtual Task LogInformationAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string severity = "Information";

            LogEntry logEntry = CreateLogEntry(message, severity);

            return SaveLogEntryUsingAllLogStoresAsync(logEntry);
        }

        public virtual void LogInformation(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            const string severity = "Information";

            LogEntry logEntry = CreateLogEntry(message, severity);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        public virtual Task LogExceptionAsync(Exception exp, string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (exp == null)
                throw new ArgumentNullException(nameof(exp));

            string severity = "Error";

            if (!(exp is AppException))
                severity = "Fatal";

            LogEntry logEntry = CreateLogEntry(message, severity);

            AddLogData("Exception", exp.ToString());
            AddLogData("ExceptionAdditionalMessage", message);
            AddLogData("ExceptionType", exp.GetType().FullName);
            AddLogData("ExceptionTypeAssemblyQualifiedName", exp.GetType().AssemblyQualifiedName);

            return SaveLogEntryUsingAllLogStoresAsync(logEntry);
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

            AddLogData("Exception", exp.ToString());
            AddLogData("ExceptionAdditionalMessage", message);
            AddLogData("ExceptionType", exp.GetType().FullName);
            AddLogData("ExceptionTypeAssemblyQualifiedName", exp.GetType().AssemblyQualifiedName);

            SaveLogEntryUsingAllLogStores(logEntry);
        }

        private LogEntry CreateLogEntry(string message, string severity)
        {
            LogEntry logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Message = message,
                Severity = severity,
                LogData = LogData,
                ApplicationName = AppEnvironment.AppInfo.Name,
                AppVersion = AppEnvironment.AppInfo.Version,
                AppEnvironmentName = AppEnvironment.Name,
                AppWasInDebugMode = AppEnvironment.DebugMode,
                AppServerName = Environment.MachineName,
                AppServerDateTime = DateTimeProvider.GetCurrentUtcDateTime(),
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