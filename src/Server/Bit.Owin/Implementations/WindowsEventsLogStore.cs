using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Owin.Implementations
{
    public class WindowsEventsLogStore : ILogStore
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IContentFormatter _contentFormatter;

#if DEBUG
        protected WindowsEventsLogStore()
        {
        }
#endif

        public WindowsEventsLogStore(IContentFormatter contentFormatter, IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (contentFormatter == null)
                throw new ArgumentNullException(nameof(contentFormatter));

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _contentFormatter = contentFormatter;
            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        public virtual async Task SaveLogAsync(LogEntry logEntry)
        {
            SaveLog(logEntry);
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

            string logContents = _contentFormatter.Serialize(logEntry);

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