using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class WindowsEventsLogStore : ILogStore
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual IContentFormatter ContentFormatter { get; set; } = default!;

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            SaveLog(logEntry);

            return Task.CompletedTask;
        }

        public virtual void SaveLog(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            using (EventLog appLog = new EventLog("Application")
            {
                Source = AppEnvironment.AppInfo.Name
            })
            {
                EventLogEntryType eventLogsSeverity;

                if (logEntry.Severity == "Warning")
                    eventLogsSeverity = EventLogEntryType.Warning;
                else if (logEntry.Severity == "Information")
                    eventLogsSeverity = EventLogEntryType.Information;
                else
                    eventLogsSeverity = EventLogEntryType.Error;

                string logContents = ContentFormatter.Serialize(logEntry.ToDictionary());

                if (logContents.Length >= 30000)
                    logContents = logContents.Substring(0, 29999);

                if (AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.EventLogId, out long eventLogId))
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

}
