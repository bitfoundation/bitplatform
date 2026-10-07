using System.Globalization;
using System.Text.Json;
using Bit.Cli.Tests.Infrastructure;
using Bit.Cli.Translation;
using Microsoft.Extensions.AI;

namespace Bit.Cli.Tests;

[TestClass]
public class TranslationTests
{
    private const string Config = """
        {
          "DefaultLanguage": "en",
          "SupportedLanguages": [ "fa", "de" ],
          "ResxPaths": [ "/src/**/*.resx" ],
          "ChatOptions": { "Temperature": "0" },
          "OpenAI": { "Model": "test-model", "Endpoint": "https://api.openai.com/v1", "ApiKey": "test-key" }
        }
        """;

    [TestMethod]
    public void BaseFiles_Should_SkipCultureFilesButKeepDottedNames()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx"), new() { ["Hello"] = "سلام" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Strings.zh-Hans.resx"), new() { ["Hello"] = "你好" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Nested", "Some.Api.resx"), new() { ["Ok"] = "OK" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "bin", "Debug", "Copied.resx"), new() { ["Ok"] = "OK" });

        var settings = ResxTranslatorSettings.Load(Path.Combine(host.WorkingDirectory, ResxTranslatorSettings.FileName), new Dictionary<string, string>());
        var files = ResxCatalog.FindBaseFiles(settings).Select(Path.GetFileName).ToArray();

        CollectionAssert.AreEquivalent(new[] { "Strings.resx", "Some.Api.resx" }, files);
    }

    [TestMethod]
    public void Plan_Should_FindOnlyMissingKeys()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello", ["Bye"] = "Bye {0}" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx"), new() { ["Hello"] = "سلام" });

        var settings = ResxTranslatorSettings.Load(Path.Combine(host.WorkingDirectory, ResxTranslatorSettings.FileName), new Dictionary<string, string>());
        var work = ResxCatalog.Pairs(settings, ResxCatalog.FindBaseFiles(settings)).Select(ResxCatalog.Plan).ToDictionary(w => w.Pair.Target.Name);

        CollectionAssert.AreEquivalent(new[] { "Bye" }, work["fa"].Missing.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { "Hello", "Bye" }, work["de"].Missing.Keys.ToArray());
    }

    [TestMethod]
    public async Task Translate_Should_FillTheGapsAndKeepExistingTranslations()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello", ["Bye"] = "Bye {0}" });
        var fa = Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx");
        WriteResx(fa, new() { ["Hello"] = "سلام دستی" });
        var client = new FakeTranslationClient();

        var exitCode = await new TranslationRun(host.Services, _ => client).RunAsync(new TranslateRequest(null, [], false, false), CancellationToken.None);

        Assert.AreEqual(CliApp.ExitOk, exitCode, host.Output);

        var faValues = ResxCatalog.Read(fa);
        Assert.AreEqual("سلام دستی", faValues["Hello"]);
        Assert.AreEqual("fa:Bye {0}", faValues["Bye"]);

        var deValues = ResxCatalog.Read(Path.Combine(host.WorkingDirectory, "src", "Strings.de.resx"));
        Assert.AreEqual("de:Hello", deValues["Hello"]);
        Assert.AreEqual("de:Bye {0}", deValues["Bye"]);
        Assert.HasCount(2, client.Batches);
    }

    [TestMethod]
    public async Task APlaceholderMismatch_Should_BeRetriedOnceThenLeftOut()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello", ["Bye"] = "Bye {0}" });
        var client = new FakeTranslationClient { BreakPlaceholders = int.MaxValue };

        var exitCode = await new TranslationRun(host.Services, _ => client).RunAsync(new TranslateRequest(null, ["fa"], false, false), CancellationToken.None);

        Assert.AreEqual(CliApp.ExitOk, exitCode, host.Output);
        Assert.HasCount(2, client.Batches);
        Assert.IsTrue(client.Batches[1].Reminder);
        CollectionAssert.AreEqual(new[] { "Bye {0}" }, client.Batches[1].Values.ToArray());

        var values = ResxCatalog.Read(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx"));
        Assert.AreEqual("fa:Hello", values["Hello"]);
        Assert.IsFalse(values.ContainsKey("Bye"));
        StringAssert.Contains(host.Output, "kept out");
    }

    [TestMethod]
    public async Task ARetriedTranslation_Should_BeKeptWhenItsRightTheSecondTime()
    {
        using var host = CreateProject(new() { ["Bye"] = "Bye {0}" });
        var client = new FakeTranslationClient { BreakPlaceholders = 1 };

        await new TranslationRun(host.Services, _ => client).RunAsync(new TranslateRequest(null, ["fa"], false, false), CancellationToken.None);

        Assert.AreEqual("fa:Bye {0}", ResxCatalog.Read(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx"))["Bye"]);
    }

    [TestMethod]
    public async Task Check_Should_FailWithoutCallingAModel()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        var client = new FakeTranslationClient();

        var exitCode = await new TranslationRun(host.Services, _ => client).RunAsync(new TranslateRequest(null, [], true, false), CancellationToken.None);

        Assert.AreEqual(CliApp.ExitCheckFailed, exitCode);
        Assert.IsEmpty(client.Batches);
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx")));
    }

    [TestMethod]
    public async Task Check_Should_PassWhenEverythingIsTranslated()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx"), new() { ["Hello"] = "سلام" });
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Strings.de.resx"), new() { ["Hello"] = "Hallo" });

        var exitCode = await new TranslationRun(host.Services, _ => new FakeTranslationClient()).RunAsync(new TranslateRequest(null, [], true, false), CancellationToken.None);

        Assert.AreEqual(CliApp.ExitOk, exitCode);
        StringAssert.Contains(host.Output, "up to date");
    }

    [TestMethod]
    public async Task DryRun_Should_WriteNothing()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        var client = new FakeTranslationClient();

        var exitCode = await new TranslationRun(host.Services, _ => client).RunAsync(new TranslateRequest(null, [], false, true), CancellationToken.None);

        Assert.AreEqual(CliApp.ExitOk, exitCode);
        Assert.IsEmpty(client.Batches);
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "src", "Strings.fa.resx")));
    }

    [TestMethod]
    public async Task NoApiKey_Should_TranslateNothingWithoutFailing()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" }, Config.Replace("\"test-key\"", "null", StringComparison.Ordinal));
        var client = new FakeTranslationClient();

        var exitCode = await new TranslationRun(host.Services, _ => client).RunAsync(new TranslateRequest(null, [], false, false), CancellationToken.None);

        Assert.AreEqual(CliApp.ExitOk, exitCode);
        Assert.IsEmpty(client.Batches);
        StringAssert.Contains(host.Output, "OpenAI__ApiKey");
    }

    [TestMethod]
    public async Task TheConfig_Should_BeFoundAboveTheCurrentFolder()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        var nested = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "deep")).FullName;

        Assert.AreEqual(Path.Combine(host.WorkingDirectory, ResxTranslatorSettings.FileName), ResxTranslatorSettings.FindConfig(nested));
        Assert.IsNull(ResxTranslatorSettings.FindConfig(Path.GetPathRoot(host.WorkingDirectory)!));
        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task AMissingConfigOrLanguage_Should_BeAUsageError()
    {
        using var empty = new TestHost();
        Assert.AreEqual(CliApp.ExitUsage, await new TranslationRun(empty.Services, _ => new FakeTranslationClient()).RunAsync(new TranslateRequest(null, [], false, false), CancellationToken.None));

        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        Assert.AreEqual(CliApp.ExitUsage, await new TranslationRun(host.Services, _ => new FakeTranslationClient()).RunAsync(new TranslateRequest(null, ["it"], false, false), CancellationToken.None));
        StringAssert.Contains(host.Output, "--language it");
    }

    [TestMethod]
    public void EnvironmentVariables_Should_OverrideTheFile()
    {
        using var host = CreateProject(new() { ["Hello"] = "Hello" });
        var settings = ResxTranslatorSettings.Load(Path.Combine(host.WorkingDirectory, ResxTranslatorSettings.FileName), new Dictionary<string, string> { ["OpenAI__ApiKey"] = "from-env", ["OpenAI__Model"] = "other-model" });

        Assert.AreEqual("from-env", settings.OpenAI.ApiKey);
        Assert.AreEqual("other-model", settings.OpenAI.Model);
        Assert.AreEqual(0F, settings.ChatOptions.Temperature);
        Assert.AreEqual("openai", settings.OpenAI.ProviderKind);
    }

    [TestMethod]
    [DataRow("Hello {0}", "Salut {0}", true)]
    [DataRow("{0} of {1}", "{1} van {0}", true)]
    [DataRow("Hello {0}", "Salut", false)]
    [DataRow("Hello {0}", "Salut {1}", false)]
    [DataRow("Total {0:N2}", "Totaal {0:N2}", true)]
    [DataRow("{{literal}}", "{{letterlijk}}", true)]
    [DataRow("No placeholders", "Geen", true)]
    public void Placeholders_Should_BeComparedBySet(string source, string translation, bool expected)
    {
        Assert.AreEqual(expected, ChatTranslationClient.PlaceholdersMatch(source, translation));
    }

    [TestMethod]
    public async Task TheChatClient_Should_SendTheStringsAndReadTheJsonAnswer()
    {
        var chat = new FakeChatClient();
        var client = new ChatTranslationClient(chat, new ChatOptions { Temperature = 0 });

        var output = await client.TranslateAsync(new TranslationBatch(CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("fa"), ["Hello", "Bye {0}"], false), CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "[Hello]", "[Bye {0}]" }, output.Translations.ToArray());
        Assert.AreEqual(12, output.InputTokens);
        StringAssert.Contains(chat.LastSystemPrompt, "فارسی");
        Assert.AreEqual(0F, chat.LastOptions!.Temperature);
        Assert.IsNotNull(chat.LastOptions.ResponseFormat);
    }

    private static TestHost CreateProject(Dictionary<string, string> strings, string config = Config)
    {
        var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, ResxTranslatorSettings.FileName), config);
        WriteResx(Path.Combine(host.WorkingDirectory, "src", "Strings.resx"), strings);
        return host;
    }

    private static void WriteResx(string path, Dictionary<string, string> values)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var data = string.Join("\n", values.Select(kvp => $"""  <data name="{kvp.Key}" xml:space="preserve">{"\n"}    <value>{System.Security.SecurityElement.Escape(kvp.Value)}</value>{"\n"}  </data>"""));
        File.WriteAllText(path, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <resheader name="resmimetype">
                <value>text/microsoft-resx</value>
              </resheader>
            {data}
            </root>
            """);
    }

    private sealed class FakeTranslationClient : ITranslationClient
    {
        public List<TranslationBatch> Batches { get; } = [];

        public int BreakPlaceholders { get; set; }

        public Task<TranslationOutput> TranslateAsync(TranslationBatch batch, CancellationToken cancellationToken)
        {
            Batches.Add(batch);

            var translations = batch.Values.Select(v =>
            {
                if (BreakPlaceholders > 0 && v.Contains('{'))
                {
                    BreakPlaceholders--;
                    return $"{batch.Target.Name}:broken";
                }

                return $"{batch.Target.Name}:{v}";
            }).ToList();

            return Task.FromResult(new TranslationOutput(translations, 10, 20));
        }
    }

    private sealed class FakeChatClient : IChatClient
    {
        public string? LastSystemPrompt { get; private set; }

        public ChatOptions? LastOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var list = messages.ToList();
            LastSystemPrompt = list.First(m => m.Role == ChatRole.System).Text;
            LastOptions = options;

            var user = list.Last(m => m.Role == ChatRole.User && m.Text.Contains("Translate the following", StringComparison.Ordinal)).Text;
            var values = JsonSerializer.Deserialize<string[]>(user[user.IndexOf('[')..])!;
            var json = JsonSerializer.Serialize(new { translations = values.Select(v => $"[{v}]") });

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)) { Usage = new UsageDetails { InputTokenCount = 12, OutputTokenCount = 34 } });
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
