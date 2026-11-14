using Bit.BlazorUI;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using static Boilerplate.Tests.Features.Chatbot.TestAiChatPanel;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// <c>chatbot.active_conversations</c> is an up-down counter: <c>AppHub.StreamAsync</c> adds one as an answer starts and
/// takes it back in the <c>finally</c> of its stream. A path that ends an answer without reaching that <c>finally</c>
/// would leave the dashboard counting answers that are long gone, and nothing else would notice.
/// <para>
/// Read in process with a <see cref="MeterListener"/> - what the OpenTelemetry exporter and <c>dotnet-counters</c> read
/// too. <c>DoNotParallelize</c>: the counter is process wide, so another test's answer would move it.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public partial class ChatbotConversationMetricsTests
{
    private const string instrumentName = "chatbot.active_conversations";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Once stopped in the panel, once by the connection going away - the two ways an answer ends before the model is
    /// done. The model writes a word and waits there, so each answer stays open until one of them ends it.
    /// </summary>
    [TestMethod]
    public async Task OngoingConversationsCount_Should_RiseForAnAnswerBeingWritten_AndFallBackWhenItEnds()
    {
        var chatClient = new TestChatClient
        {
            StreamingChunks = _ => ["bit platform is", " a set of tools"],
            PauseAfterFirstChunk = new TaskCompletionSource()
        };

        await using var server = BuildServerWith(chatClient);
        await server.Start(TestContext.CancellationToken);

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

        // ---- Stopped in the panel ----
        await using (var connection = await Connect(server))
        {
            using var answer = new CancellationTokenSource();
            var reading = ReadAnswer(connection, answer.Token);

            await WaitFor(() => Interlocked.Read(ref ongoing) is 1, () => $"An answer being written should count as one ongoing conversation, the counter says {Interlocked.Read(ref ongoing)}.");

            await answer.CancelAsync();
            await reading;

            await WaitFor(() => Interlocked.Read(ref ongoing) is 0, () => $"An answer the panel stopped should no longer be counted, the counter says {Interlocked.Read(ref ongoing)}.");
        }

        // ---- Ended by the connection going away ----
        var droppedConnection = await Connect(server);
        var droppedReading = ReadAnswer(droppedConnection, CancellationToken.None);

        await WaitFor(() => Interlocked.Read(ref ongoing) is 1, () => $"The second answer should count as one ongoing conversation, the counter says {Interlocked.Read(ref ongoing)}.");

        await droppedConnection.DisposeAsync();
        await droppedReading;

        await WaitFor(() => Interlocked.Read(ref ongoing) is 0, () => $"An answer whose connection went away should no longer be counted, the counter says {Interlocked.Read(ref ongoing)}.");
    }

    private async Task<HubConnection> Connect(AppTestServer server)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.ApiAppAddress, "app-hub"), options => options.Transports = HttpTransportType.WebSockets)
            .Build();

        await connection.StartAsync(TestContext.CancellationToken);

        return connection;
    }

    /// <summary>Asks a question over the connection the way the panel does (See AppAiChat), and reads its answer until the token or the connection ends it.</summary>
    private static Task ReadAnswer(HubConnection connection, CancellationToken cancellationToken)
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "what is bit platform?");

        var transport = new BitAiSignalRTransport(connection);

        return Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in transport.StreamAsync(new BitAiRequest([.. conversation.Messages]), cancellationToken))
                {
                }
            }
            catch (Exception)
            {
                // Ending the answer is the point: a canceled stream or a closed connection both surface here.
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
