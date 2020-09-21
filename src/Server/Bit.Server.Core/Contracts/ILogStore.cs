using Bit.Core.Models;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    public interface ILogStore
    {
        Task SaveLogAsync(LogEntry logEntry);
        void SaveLog(LogEntry logEntry);
    }
}
