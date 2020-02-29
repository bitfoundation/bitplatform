using System.Threading.Tasks;

namespace Bit.Tests.Core.Contracts
{
    public interface IEmailService
    {
        Task SendEmail(string to, string title, string message);
    }
}
