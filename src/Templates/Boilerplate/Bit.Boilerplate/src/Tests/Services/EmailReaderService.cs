using Boilerplate.Server.Api.Resources;
using System.Text.RegularExpressions;
using MsgReader.Mime;

namespace Boilerplate.Tests.Services;
public static partial class EmailReaderService
{
    public static string GetLastEmailFor(string toMailAddress, string subjectPattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(toMailAddress);
        ArgumentException.ThrowIfNullOrEmpty(subjectPattern);

        var emailsDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "sent-emails");
        var messages = new DirectoryInfo(emailsDirectory).GetFiles().Select(Message.Load);
        var message = messages
            .Where(m => m.Headers.To[0].Address == toMailAddress)
            .MaxBy(m => m.Headers.DateSent);

        Assert.IsNotNull(message, "Email has not sent");
        Assert.AreEqual("info@Boilerplate.com", message.Headers.From.Address); //TODO: read from AppSettings.Email.DefaultFromEmail
        Assert.AreEqual(EmailStrings.DefaultFromName, message.Headers.From.DisplayName);

        if (subjectPattern is not null && Regex.IsMatch(message.Headers.Subject, subjectPattern) is false)
        {
            throw new AssertFailedException($@"
            Email subject does not match.
            expected pattern: {subjectPattern}
            actual subject: {message.Headers.Subject}
            ");
        }

        return message.HtmlBody.GetBodyAsText();
    }
}
