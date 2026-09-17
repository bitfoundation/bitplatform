using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

public record SmtpSettings(string Host, int Port, string UserName, string Password, bool EnableSsl)
{
    public bool HasCredentials => string.IsNullOrEmpty(UserName) is false && string.IsNullOrEmpty(Password) is false;
}

/// <summary>
/// Goes as far as <see cref="System.Net.Mail.SmtpClient"/> does before it sends: connects, upgrades with STARTTLS when
/// EnableSsl is on and signs in when credentials are set. Then it quits, so no mail is sent.
/// </summary>
public class SmtpHealthCheck(SmtpSettings settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(settings.Host, settings.Port, cancellationToken);
            Stream stream = tcpClient.GetStream();

            await ExpectReply(stream, "220", cancellationToken);
            await SendCommand(stream, $"EHLO {Dns.GetHostName()}", "250", cancellationToken);

            if (settings.EnableSsl)
            {
                await SendCommand(stream, "STARTTLS", "220", cancellationToken);
                var sslStream = new SslStream(stream);
                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = settings.Host }, cancellationToken);
                stream = sslStream;
                await SendCommand(stream, $"EHLO {Dns.GetHostName()}", "250", cancellationToken);
            }

            if (settings.HasCredentials)
            {
                // AUTH LOGIN, as SmtpClient uses. Some servers, such as Exchange Online, don't offer AUTH PLAIN.
                await SendCommand(stream, "AUTH LOGIN", "334", cancellationToken);
                await SendCommand(stream, Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.UserName)), "334", cancellationToken, commandForErrors: "the AUTH LOGIN user name");
                await SendCommand(stream, Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.Password)), "235", cancellationToken, commandForErrors: "the AUTH LOGIN password");
            }

            await SendCommand(stream, "QUIT", "221", cancellationToken);

            return HealthCheckResult.Healthy("SMTP server is healthy");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "SMTP server is unhealthy", exp);
        }
    }

    private static async Task SendCommand(Stream stream, string command, string expectedCode, CancellationToken cancellationToken, string? commandForErrors = null)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes($"{command}\r\n"), cancellationToken);
        await ExpectReply(stream, expectedCode, cancellationToken, commandForErrors ?? command);
    }

    private static async Task ExpectReply(Stream stream, string expectedCode, CancellationToken cancellationToken, string? command = null)
    {
        string line;
        do
        {
            line = await ReadLine(stream, cancellationToken);
        }
        while (line.Length > 3 && line[3] is '-'); // "250-..." lines continue a multi-line reply, "250 ..." ends it.

        if (line.StartsWith(expectedCode, StringComparison.Ordinal) is false)
            throw new InvalidOperationException($"The SMTP server replied '{line}' to {command ?? "the connection"} instead of {expectedCode}.");
    }

    /// <summary>
    /// One byte at a time: a buffered reader could swallow bytes that belong to the TLS handshake after STARTTLS.
    /// </summary>
    private static async Task<string> ReadLine(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var line = new StringBuilder();

        while (await stream.ReadAsync(buffer, cancellationToken) is 1)
        {
            if (buffer[0] is (byte)'\n')
                return line.ToString().TrimEnd('\r');

            line.Append((char)buffer[0]);
        }

        throw new IOException("The SMTP server closed the connection.");
    }
}
