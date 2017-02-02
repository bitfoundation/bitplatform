using System.Threading.Tasks;
using Foundation.Core.Models;

namespace Foundation.Core.Contracts
{
    public interface ILogStore
    {
        Task SaveLogAsync(LogEntry logEntry);
        void SaveLog(LogEntry logEntry);
    }
}
