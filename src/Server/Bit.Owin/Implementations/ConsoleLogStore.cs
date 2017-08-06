using System;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Owin.Implementations
{
    public class ConsoleLogStore : ILogStore
    {
        private readonly IContentFormatter _formatter;

        public ConsoleLogStore(IContentFormatter formatter)
        {
            _formatter = formatter;
        }

#if DEBUG
        protected ConsoleLogStore()
        {
        }
#endif

        public virtual void SaveLog(LogEntry logEntry)
        {
            SaveLogAsync(logEntry).GetAwaiter().GetResult();
        }

        public virtual async Task SaveLogAsync(LogEntry logEntry)
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

                Console.WriteLine(_formatter.Serialize(logEntry) + Environment.NewLine);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
