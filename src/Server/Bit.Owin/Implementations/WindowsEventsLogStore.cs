using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Owin.Implementations
{
    public class WindowsEventsLogStore : ILogStore
    {
        private AppEnvironment _activeAppEnvironment;

        public virtual IContentFormatter ContentFormatter { get; set; }

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(AppEnvironmentProvider));
                _activeAppEnvironment = value.GetActiveAppEnvironment();
            }
        }

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            SaveLog(logEntry);

            return Task.CompletedTask;
        }

        public virtual void SaveLog(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            EventLog appLog = new EventLog("Application")
            {
                Source = _activeAppEnvironment.AppInfo.Name
            };

            EventLogEntryType eventLogsSeverity;

            if (logEntry.Severity == "Warning")
                eventLogsSeverity = EventLogEntryType.Warning;
            else if (logEntry.Severity == "Information")
                eventLogsSeverity = EventLogEntryType.Information;
            else
                eventLogsSeverity = EventLogEntryType.Error;

            string logContents = ContentFormatter.Serialize(logEntry);

            if (logContents.Length >= 30000)
                logContents = logContents.Substring(0, 29999);

            if (_activeAppEnvironment.TryGetConfig("EventLogId", out long eventLogId))
            {
                appLog.WriteEntry(logContents, eventLogsSeverity, Convert.ToInt32(eventLogId));
            }
            else
            {
                appLog.WriteEntry(logContents, eventLogsSeverity);
            }
        }
    }

}
