namespace Foundation.Test.Core.Contracts
{
    public interface IEmailService
    {
        void SendEmail(string to, string title, string message);
    }
}
