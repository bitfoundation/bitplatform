using System.Threading.Tasks;
using Bit.Core.Models;

namespace Bit.Core.Contracts
{
    public interface ILogStore
    {
        Task SaveLogAsync(LogEntry logEntry);
        void SaveLog(LogEntry logEntry);
    }
}
