using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

/// <summary>
/// Configures the AI packages during startup.
/// </summary>
/// <remarks>
/// <para>
/// Handed to the configure callback of <c>AddBitBlazorUIAIServices</c> (Bit.BlazorUI.AI), so registration reads as one
/// fluent statement instead of a dozen unrelated service registrations.
/// </para>
/// <para>
/// What it configures here is what a session needs wherever it runs: the transport, the tools, the approval policy, the
/// models, the store, the speech services and the session defaults. The other packages add their own parts as extension methods -
/// <c>ConfigureUi</c>, <c>ConfigureRenderers</c> and <c>ConfigureGenerativeUi</c> (Bit.BlazorUI.AI),
/// <c>UseSignalRTransport</c> (Bit.BlazorUI.AI.SignalR) and <c>UseChatClient</c> (Bit.BlazorUI.AI.ChatClient) - so an
/// app only sees the ones of the packages it installed.
/// </para>
/// <para>
/// That callback is the only way to get one. To configure more after an earlier call (from a module of the app, or a
/// test), call <c>AddBitBlazorUIAIServices</c> again with a callback: it registers nothing twice, and the builder it
/// hands over adds to what is already there.
/// </para>
/// </remarks>
public sealed class BitAiBuilder
{
    internal BitAiBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers the transport that produces responses.
    /// </summary>
    /// <typeparam name="TTransport">The transport implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTransport<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>() where TTransport : class, IBitAiTransport
    {
        Services.AddScoped<IBitAiTransport, TTransport>();
        return this;
    }

    /// <summary>
    /// Registers a transport instance.
    /// </summary>
    /// <param name="transport">The transport.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTransport(IBitAiTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        Services.AddSingleton(transport);
        return this;
    }

    /// <summary>
    /// Registers a transport built from services.
    /// </summary>
    /// <param name="factory">Creates the transport.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTransport(Func<IServiceProvider, IBitAiTransport> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.AddScoped(factory);
        return this;
    }

    /// <summary>
    /// Registers a <see cref="BitAiSseTransport"/>: each request is posted to a server endpoint (<c>MapBitAiSse</c> of
    /// the Bit.BlazorUI.AI.Server package) and the reply read from its server-sent events.
    /// </summary>
    /// <param name="httpClient">Gets the client the requests are sent with, whose base address the endpoints are relative to.</param>
    /// <param name="options">The endpoints and the serializer settings.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseSseTransport(Func<IServiceProvider, HttpClient> httpClient, BitAiSseTransportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        return UseTransport(sp => new BitAiSseTransport(httpClient(sp), options));
    }

    /// <summary>
    /// Registers a <see cref="BitAiWebSocketTransport"/>: each request opens a WebSocket to a server endpoint
    /// (<c>MapBitAiWebSocket</c> of the Bit.BlazorUI.AI.Server package), which streams the reply and takes the decisions
    /// on the tool calls it holds over that same socket.
    /// </summary>
    /// <param name="endpoint">
    /// Gets the address of the endpoint: <c>ws</c> / <c>wss</c>, or <c>http</c> / <c>https</c> for the matching WebSocket
    /// scheme, so an address built on the app's base address works as it is.
    /// </param>
    /// <param name="options">The socket and the serializer settings.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseWebSocketTransport(Func<IServiceProvider, Uri> endpoint, BitAiWebSocketTransportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return UseTransport(sp => new BitAiWebSocketTransport(endpoint(sp), options));
    }

    /// <summary>
    /// Adds a tool the model may call.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder AddTool(IBitAiTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        Services.AddSingleton(tool);
        return this;
    }

    /// <summary>
    /// Adds a tool resolved from services, for tools that need dependencies.
    /// </summary>
    /// <typeparam name="TTool">The tool implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder AddTool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTool>() where TTool : class, IBitAiTool
    {
        Services.AddSingleton<IBitAiTool, TTool>();
        return this;
    }

    /// <summary>
    /// Replaces the policy that decides which tool calls need a human decision.
    /// </summary>
    /// <typeparam name="TPolicy">The policy implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseApprovalPolicy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPolicy>() where TPolicy : class, IBitAiToolApprovalPolicy
    {
        // Scoped so a policy can depend on per-user services such as the current user.
        Services.RemoveAll<IBitAiToolApprovalPolicy>();
        Services.AddScoped<IBitAiToolApprovalPolicy, TPolicy>();
        return this;
    }

    /// <summary>
    /// Registers the models the user can choose between.
    /// </summary>
    /// <param name="models">The models.</param>
    /// <param name="providers">The providers serving them.</param>
    /// <param name="defaultModelId">The model selected by default.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder AddModels(
        IEnumerable<BitAiModel> models,
        IEnumerable<BitAiProvider>? providers = null,
        string? defaultModelId = null)
    {
        ArgumentNullException.ThrowIfNull(models);
        Services.RemoveAll<IBitAiModelCatalog>();
        Services.AddSingleton<IBitAiModelCatalog>(new BitAiModelCatalog(models, providers, defaultModelId));
        return this;
    }

    /// <summary>
    /// Registers persistence for conversations.
    /// </summary>
    /// <typeparam name="TStore">The store implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseConversationStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>() where TStore : class, IBitAiConversationStore
    {
        Services.RemoveAll<IBitAiConversationStore>();
        Services.AddScoped<IBitAiConversationStore, TStore>();
        return this;
    }

    /// <summary>
    /// Registers the speech-to-text service the voice inputs dictate through: they record the microphone in the browser
    /// and hand each take to it, in place of the speech API of the browser (see <see cref="IBitAiSpeechToText"/>).
    /// </summary>
    /// <typeparam name="TSpeechToText">The service implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseSpeechToText<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSpeechToText>() where TSpeechToText : class, IBitAiSpeechToText
    {
        // Scoped, like the transport: a service posting to the app's server typically takes its HttpClient, and on Blazor
        // Server a per-user one.
        Services.RemoveAll<IBitAiSpeechToText>();
        Services.AddScoped<IBitAiSpeechToText, TSpeechToText>();
        return this;
    }

    /// <summary>
    /// Registers a speech-to-text service instance the voice inputs dictate through (see <see cref="IBitAiSpeechToText"/>).
    /// </summary>
    /// <param name="speechToText">The service.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseSpeechToText(IBitAiSpeechToText speechToText)
    {
        ArgumentNullException.ThrowIfNull(speechToText);
        Services.RemoveAll<IBitAiSpeechToText>();
        Services.AddSingleton(speechToText);
        return this;
    }

    /// <summary>
    /// Registers a speech-to-text service built from services, which the voice inputs dictate through (see
    /// <see cref="IBitAiSpeechToText"/>).
    /// </summary>
    /// <param name="factory">Creates the service.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseSpeechToText(Func<IServiceProvider, IBitAiSpeechToText> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.RemoveAll<IBitAiSpeechToText>();
        Services.AddScoped(factory);
        return this;
    }

    /// <summary>
    /// Registers the text-to-speech service replies are read aloud through (see <see cref="IBitAiTextToSpeech"/>).
    /// </summary>
    /// <typeparam name="TTextToSpeech">The service implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTextToSpeech<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTextToSpeech>() where TTextToSpeech : class, IBitAiTextToSpeech
    {
        Services.RemoveAll<IBitAiTextToSpeech>();
        Services.AddScoped<IBitAiTextToSpeech, TTextToSpeech>();
        return this;
    }

    /// <summary>
    /// Registers a text-to-speech service instance replies are read aloud through (see <see cref="IBitAiTextToSpeech"/>).
    /// </summary>
    /// <param name="textToSpeech">The service.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTextToSpeech(IBitAiTextToSpeech textToSpeech)
    {
        ArgumentNullException.ThrowIfNull(textToSpeech);
        Services.RemoveAll<IBitAiTextToSpeech>();
        Services.AddSingleton(textToSpeech);
        return this;
    }

    /// <summary>
    /// Registers a text-to-speech service built from services, which replies are read aloud through (see
    /// <see cref="IBitAiTextToSpeech"/>).
    /// </summary>
    /// <param name="factory">Creates the service.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTextToSpeech(Func<IServiceProvider, IBitAiTextToSpeech> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.RemoveAll<IBitAiTextToSpeech>();
        Services.AddScoped(factory);
        return this;
    }

    /// <summary>
    /// Sets the default options every session starts with.
    /// </summary>
    /// <param name="configure">
    /// Receives the current defaults and returns the ones to use. A transform rather than a
    /// mutation, because the options are an immutable record, which is what makes one instance safe
    /// to share across every session.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// ai.ConfigureSessions(options =&gt; options with { MaxToolRoundtrips = 5 });
    /// </code>
    /// </example>
    public BitAiBuilder ConfigureSessions(Func<BitAiChatSessionOptions, BitAiChatSessionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var current = (BitAiChatSessionOptions?)Services
            .LastOrDefault(d => d.ServiceType == typeof(BitAiChatSessionOptions))?.ImplementationInstance;

        Services.RemoveAll<BitAiChatSessionOptions>();
        Services.AddSingleton(configure(current ?? new BitAiChatSessionOptions()));
        return this;
    }
}
