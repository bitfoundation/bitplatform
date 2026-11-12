using System.Collections.Concurrent;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;

namespace Boilerplate.LandingPage.Tests.Infrastructure;

internal sealed class RecordingEmailSender : ISender
{
    public ConcurrentQueue<EmailData> Sent { get; } = new();

    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        Sent.Enqueue(email.Data);
        return new SendResponse();
    }

    public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null) => Task.FromResult(Send(email, token));

    public EmailData[] To(string address) => [.. Sent.Where(email => email.ToAddresses.Any(to => string.Equals(to.EmailAddress, address, StringComparison.OrdinalIgnoreCase)))];
}
