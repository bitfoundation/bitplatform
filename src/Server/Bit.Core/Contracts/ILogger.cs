using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Models;

namespace Bit.Core.Contracts
{
    public enum LogPolicy
    {
        /// <summary>
        /// Performs log only when there is an error. For example when a response is not succeded.
        /// </summary>
        InCaseOfScopeFailure,
        /// <summary>
        /// Store logs anyway. (Default)
        /// </summary>
        Always
    }

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

        /// <summary>
        /// Default <see cref="LogPolicy.InCaseOfScopeFailure"/>
        /// </summary>
        LogPolicy Policy { get; set; }
    }
}
