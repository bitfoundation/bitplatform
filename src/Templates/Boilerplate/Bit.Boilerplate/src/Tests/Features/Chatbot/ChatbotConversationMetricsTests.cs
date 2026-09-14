using System.Threading.Channels;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// <c>appHub.ongoing_conversations_count</c> is an up-down counter: <c>AppHub.StartChat</c> adds one once a chat is
/// open and takes it back in the <c>finally</c> of the stream. A path that ends a chat without reaching that
/// <c>finally</c> would leave the dashboard counting conversations that are long gone, and nothing else would notice.
/// <para>
/// Read in process with a <see cref="MeterListener"/> - what the OpenTelemetry exporter and <c>dotnet-counters</c> read
/// too. <c>DoNotParallelize</c>: the counter is process wide, so another test's chat would move it.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public partial class ChatbotConversationMetricsTests
{
    private const string instrumentName = "appHub.ongoing_conversations_count";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Once ended by the client, once by the connection going away - the two ways a chat really ends. StartChat asks
    /// nothing of the model, so no AI provider is needed for the count to move.
    /// </summary>
    [TestMethod]
    public async Task OngoingConversationsCount_Should_RiseForAnOpenChat_AndFallBackWhenItEnds()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        long ongoing = 0;

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name is instrumentName)
                    meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => Interlocked.Add(ref ongoing, measurement));
        listener.Start();

        // ---- Ended by the client ----
        await using (var connection = await Connect(server))
        {
            using var chat = new CancellationTokenSource();
            var reading = ReadChat(connection, chat.Token);

            await WaitFor(() => Interlocked.Read(ref ongoing) is 1, () => $"An open chat should count as one ongoing conversation, the counter says {Interlocked.Read(ref ongoing)}.");

            await chat.CancelAsync();
            await reading;

            await WaitFor(() => Interlocked.Read(ref ongoing) is 0, () => $"A chat the client ended should no longer be counted, the counter says {Interlocked.Read(ref ongoing)}.");
        }

        // ---- Ended by the connection going away ----
        var droppedConnection = await Connect(server);
        var droppedReading = ReadChat(droppedConnection, CancellationToken.None);

        await WaitFor(() => Interlocked.Read(ref ongoing) is 1, () => $"The second chat should count as one ongoing conversation, the counter says {Interlocked.Read(ref ongoing)}.");

        await droppedConnection.DisposeAsync();
        await droppedReading;

        await WaitFor(() => Interlocked.Read(ref ongoing) is 0, () => $"A chat whose connection went away should no longer be counted, the counter says {Interlocked.Read(ref ongoing)}.");
    }

    private async Task<HubConnection> Connect(AppTestServer server)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.WebAppServerAddress, "app-hub"), options => options.Transports = HttpTransportType.WebSockets)
            .Build();

        await connection.StartAsync(TestContext.CancellationToken);

        return connection;
    }

    /// <summary>
    /// Opens a chat nobody has spoken in yet: the user's side is a stream the client keeps open and never writes to, so
    /// the chat stays open until the token or the connection ends it.
    /// </summary>
    private static Task ReadChat(HubConnection connection, CancellationToken cancellationToken)
    {
        var userMessages = Channel.CreateUnbounded<AiChatMessage>();

        return Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in connection.StreamAsync<string>(SharedAppMessages.StartChat, new StartChatRequest(), userMessages.Reader, cancellationToken))
                {
                }
            }
            catch (Exception)
            {
                // Ending the chat is the point: a canceled stream or a closed connection both surface here.
            }
        }, CancellationToken.None);
    }

    private async Task WaitFor(Func<bool> condition, Func<string> failure)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);

        while (condition() is false)
        {
            if (DateTimeOffset.UtcNow >= deadline)
                Assert.Fail(failure());

            await Task.Delay(50, TestContext.CancellationToken);
        }
    }
}
