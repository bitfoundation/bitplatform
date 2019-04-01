using Bit.Core.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bit.Core.Models
{
    public class LogEntryAppLevelConstantInfo
    {
        public virtual string ApplicationName { get; set; }

        public virtual string AppVersion { get; set; }

        public virtual string AppEnvironmentName { get; set; }

        public virtual bool? AppWasInDebugMode { get; set; }

        public virtual string AppServerName { get; set; }

        public virtual string AppServerOSVersion { get; set; }

        public virtual string AppServerAppDomainName { get; set; }

        public virtual int? AppServerProcessId { get; set; }

        public virtual string AppServerUserAccountName { get; set; }

        public virtual bool AppServerWas64Bit { get; set; }

        public virtual bool AppWas64Bit { get; set; }

        public static LogEntryAppLevelConstantInfo GetAppConstantInfo()
        {
            AppEnvironment appEnvironment = DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment();
            Process currentProcess = Process.GetCurrentProcess();

            return new LogEntryAppLevelConstantInfo
            {
                ApplicationName = appEnvironment.AppInfo.Name,
                AppVersion = appEnvironment.AppInfo.Version,
                AppEnvironmentName = appEnvironment.Name,
                AppWasInDebugMode = appEnvironment.DebugMode,
                AppServerName = Environment.MachineName,
                AppServerOSVersion = Environment.OSVersion.ToString(),
                AppServerAppDomainName = AppDomain.CurrentDomain.FriendlyName,
                AppServerProcessId = currentProcess.Id,
                AppServerUserAccountName = $"{Environment.UserDomainName}\\{Environment.UserName}",
                AppServerWas64Bit = Environment.Is64BitOperatingSystem,
                AppWas64Bit = Environment.Is64BitProcess
            };
        }
    }

    public class LogEntry : LogEntryAppLevelConstantInfo
    {
        public virtual Guid? Id { get; set; }

        public virtual string Message { get; set; }

        public virtual string Severity { get; set; }

        public virtual IEnumerable<LogData> LogData { get; set; }

        public virtual DateTimeOffset? AppServerDateTime { get; set; }

        public virtual int? AppServerThreadId { get; set; }

        public virtual long MemoryUsage { get; set; }

        public override string ToString()
        {
            return $"{nameof(Message)}: {Message}, {nameof(Severity)}: {Severity}";
        }

        public virtual Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> values = new Dictionary<string, object>
            {
                { nameof(Id), Id },
                { nameof(Severity), Severity },
                { nameof(AppServerDateTime), AppServerDateTime },
                { nameof(Message), Message },

                { nameof(AppServerProcessId), AppServerProcessId },
                { nameof(AppServerThreadId), AppServerThreadId },

                { nameof(ApplicationName), ApplicationName },
                { nameof(AppEnvironmentName), AppEnvironmentName },
                { nameof(AppVersion), AppVersion },
                { nameof(AppWasInDebugMode), AppWasInDebugMode },

                { nameof(AppServerAppDomainName), AppServerAppDomainName },
                { nameof(AppServerName), AppServerName },
                { nameof(AppServerOSVersion), AppServerOSVersion },
                { nameof(AppServerUserAccountName), AppServerUserAccountName },
                { nameof(AppServerWas64Bit), AppServerWas64Bit },
                { nameof(AppWas64Bit), AppWas64Bit },
                { nameof(MemoryUsage), MemoryUsage }
            };

            foreach (IGrouping<string, LogData> logData in LogData.GroupBy(ld => ld.Key))
            {
                values.Add(logData.Key, logData.First().Value);
            }

            return values;
        }
    }
}