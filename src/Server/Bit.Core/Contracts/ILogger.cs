using Bit.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    public interface ILogger
    {
        Task LogExceptionAsync(Exception exp, string message);

        void LogException(Exception exp, string message);

        Task LogInformationAsync(string message);

        void LogInformation(string message);

        Task LogFatalAsync(string message);

        void LogFatal(string message);

        Task LogWarningAsync(string message);

        void LogWarning(string message);

        IEnumerable<LogData> LogData { get; }

        void AddLogData(string key, object value);
    }
}
