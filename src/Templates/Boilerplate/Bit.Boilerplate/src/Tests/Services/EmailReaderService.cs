using Boilerplate.Server.Api.Resources;
using System.Text.RegularExpressions;
using MsgReader.Mime;

namespace Boilerplate.Tests.Services;
public static partial class EmailReaderService
{
    public static string GetLastEmailFor(string toMailAddress, string emailSubject)
    {
        var emailsDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "sent-emails");
        var messages = new DirectoryInfo(emailsDirectory).GetFiles().Select(Message.Load);
        var message = messages
            .Where(m => m.Headers.To[0].Address == toMailAddress)
            .MaxBy(m => m.Headers.DateSent);

        Assert.IsNotNull(message, "Email has not sent");
        Assert.AreEqual("info@Boilerplate.com", message.Headers.From.Address); //TODO: read from AppSettings.Email.DefaultFromEmail
        Assert.AreEqual(EmailStrings.DefaultFromName, message.Headers.From.DisplayName);
        Assert.IsTrue(Regex.IsMatch(message.Headers.Subject, emailSubject), "Email subject does not match.");

        return message.HtmlBody.GetBodyAsText();
    }
}
