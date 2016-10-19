using System;
using System.Collections.Generic;

namespace Foundation.Core.Models
{
    [Serializable]
    public class LogEntry
    {
        public virtual Guid? Id { get; set; }

        public virtual string Message { get; set; }

        public virtual string Severity { get; set; }

        public virtual IEnumerable<LogData> LogData { get; set; }


        public virtual string ApplicationName { get; set; }

        public virtual string AppVersion { get; set; }

        public virtual string AppEnvironmentName { get; set; }

        public virtual bool? AppWasInDebugMode { get; set; }


        public virtual string AppServerName { get; set; }

        public virtual DateTimeOffset? AppServerDateTime { get; set; }

        public virtual string AppServerOSVersion { get; set; }

        public virtual string AppServerAppDomainName { get; set; }

        public virtual int? AppServerProcessId { get; set; }

        public virtual int? AppServerThreadId { get; set; }

        public virtual string AppServerUserAccountName { get; set; }
    }
}