using Bit.ResxTranslator;

using Microsoft.Extensions.AI;
using Bit.ResxTranslator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.ClientModel.Primitives;

using Azure.AI.Inference;
using Azure.Core.Pipeline;

using OpenAI.Chat;
using System.Globalization;

var services = new ServiceCollection();
var configuration = new ConfigurationManager();

configuration.AddJsonFile(Path.Combine(Environment.CurrentDirectory, "resx-translator.json"), optional: false);
configuration.AddEnvironmentVariables();
#if DEBUG
configuration.AddUserSecrets("a7e11fa7-16df-4c99-b309-1616a8db9c37");
#endif
configuration.AddCommandLine(args);

services.AddOptions<ResxTranslatorSettings>()
    .Bind(configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

ResxTranslatorSettings settings = new();
configuration.Bind(settings);
services.AddSingleton(settings);

services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
});

services.AddScoped<ResxFilesManager>();

services.AddHttpClient("AI", c =>
{
    c.DefaultRequestVersion = HttpVersion.Version20;
    c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
}).ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler()
{
    EnableMultipleHttp2Connections = true,
    EnableMultipleHttp3Connections = true
});

if (string.IsNullOrEmpty(settings.OpenAI?.ApiKey) is false)
{
    // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.OpenAI#microsoftextensionsaiopenai
    services.AddChatClient(sp => new ChatClient(model: settings.OpenAI.Model, credential: new(settings.OpenAI.ApiKey), options: new()
    {
        Endpoint = settings.OpenAI.Endpoint,
        Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
    }).AsIChatClient())
    .UseLogging()
    .UseFunctionInvocation();
}
else if (string.IsNullOrEmpty(settings.AzureOpenAI?.ApiKey) is false)
{
    // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.AzureAIInference#microsoftextensionsaiazureaiinference
    services.AddChatClient(sp => new ChatCompletionsClient(endpoint: settings.AzureOpenAI.Endpoint,
        credential: new Azure.AzureKeyCredential(settings.AzureOpenAI.ApiKey),
        options: new()
        {
            Transport = new HttpClientTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
        }).AsIChatClient(settings.AzureOpenAI.Model))
    .UseLogging()
    .UseFunctionInvocation();
}

await using var serviceProvider = services.BuildServiceProvider();

var resxFilesManager = serviceProvider.GetRequiredService<ResxFilesManager>();

foreach (var resxGroup in resxFilesManager.GetResxGroups())
{
    CultureInfo culture = new CultureInfo(resxGroup.Path);
}
