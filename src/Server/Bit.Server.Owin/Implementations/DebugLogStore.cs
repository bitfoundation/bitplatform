#define DEBUG
using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class DebugLogStore : ILogStore
    {
        public virtual IContentFormatter ContentFormatter { get; set; } = default!;

        public virtual void SaveLog(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            if (Debugger.IsAttached)
                Debug.WriteLine(ContentFormatter.Serialize(logEntry.ToDictionary()) + Environment.NewLine);
        }

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            if (Debugger.IsAttached)
                Debug.WriteLine(ContentFormatter.Serialize(logEntry.ToDictionary()) + Environment.NewLine);

            return Task.CompletedTask;
        }
    }
}
