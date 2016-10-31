using Foundation.Core.Contracts;
using Foundation.Core.Models;
using System;
using System.Threading.Tasks;

namespace Foundation.Api.Implementations
{
    public class ConsoleLogStore : ILogStore
    {
        private readonly IContentFormatter _formatter;

        public ConsoleLogStore(IContentFormatter formatter)
        {
            _formatter = formatter;
        }

        public void SaveLog(LogEntry logEntry)
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

        public async Task SaveLogAsync(LogEntry logEntry)
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
