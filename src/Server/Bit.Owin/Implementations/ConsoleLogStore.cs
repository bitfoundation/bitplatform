using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class ConsoleLogStore : ILogStore
    {
        public virtual IContentFormatter Formatter { get; set; }

        public virtual void SaveLog(LogEntry logEntry)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                switch (logEntry.Severity)
                {
                    case "Information":
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case "Warning":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                Console.WriteLine(Formatter.Serialize(logEntry) + Environment.NewLine);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            SaveLog(logEntry);
            return Task.CompletedTask;
        }
    }
}
