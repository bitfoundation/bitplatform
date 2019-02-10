using Bit.Core.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    }
}