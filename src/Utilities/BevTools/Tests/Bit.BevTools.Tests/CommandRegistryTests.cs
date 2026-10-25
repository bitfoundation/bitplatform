
namespace Bit.BevTools.Tests;

[TestClass]
public class CommandRegistryTests
{
    private static CommandRegistry Create()
    {
        var registry = new CommandRegistry();
        registry.AddRange(BuiltInCommands.Create());
        return registry;
    }

    [TestMethod]
    public void Exact_and_prefix_matches_rank_first()
    {
        var registry = Create();
        var results = registry.Search("> show errors");
        Assert.AreEqual("errors.show", results[0].Command.Id);

        results = registry.Search("show slow");
        Assert.AreEqual("profiler.slow", results[0].Command.Id);
    }

    [TestMethod]
    public void Argument_commands_capture_trailing_text()
    {
        var registry = Create();
        var results = registry.Search("Find component ProductList");
        Assert.AreEqual("components.find", results[0].Command.Id);
        Assert.AreEqual("ProductList", results[0].Argument);

        results = registry.Search("find ProductList");
        Assert.AreEqual("components.find", results[0].Command.Id);
        Assert.AreEqual("ProductList", results[0].Argument);

        results = registry.Search("show components rendering more than 40");
        Assert.AreEqual("profiler.renders-over", results[0].Command.Id);
        Assert.AreEqual("40", results[0].Argument);
    }

    [TestMethod]
    public void Keywords_and_subsequences_still_match()
    {
        var registry = Create();
        Assert.Contains(r => r.Command.Id == "network.show", registry.Search("http"));
        Assert.Contains(r => r.Command.Id == "services.show", registry.Search("di"));
        Assert.IsTrue(CommandRegistry.IsSubsequence("sce", "Show capabilities and overhead"));
        Assert.IsEmpty(registry.Search("zzzzqqq"));
    }

    [TestMethod]
    public void Empty_query_lists_everything_grouped_by_category()
    {
        var registry = Create();
        var results = registry.Search("", limit: 100);
        Assert.AreEqual(registry.Commands.Count, results.Count);
        Assert.AreSequenceEqual(results.Select(r => r.Command.Category).OrderBy(c => c), results.Select(r => r.Command.Category));
    }

    [TestMethod]
    public async Task Theme_command_cycles_through_auto_dark_and_light()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var command = Assert.ContainsSingle(c => c.Id == "ui.theme", Create().Commands);
            var context = new CommandContext(scope.ServiceProvider);

            Assert.AreEqual(BevToolsTheme.Auto, session.Ui.Theme);
            await command.ExecuteAsync(context, null);
            Assert.AreEqual(BevToolsTheme.Dark, session.Ui.Theme);
            await command.ExecuteAsync(context, null);
            Assert.AreEqual(BevToolsTheme.Light, session.Ui.Theme);
            await command.ExecuteAsync(context, null);
            Assert.AreEqual(BevToolsTheme.Auto, session.Ui.Theme);
            Assert.AreEqual(3, context.PersistPreferencesCalls);

            session.Ui.IsOpen = true;
            var close = Create().Commands.Single(c => c.Id == "ui.close");
            await close.ExecuteAsync(context, null);
            Assert.IsFalse(session.Ui.IsOpen);
            Assert.AreEqual(4, context.PersistPreferencesCalls);
        }
    }

    private sealed class CommandContext(IServiceProvider services) : IBevToolsCommandContext
    {
        public IServiceProvider Services => services;

        public int ShowPanelCalls { get; private set; }

        public void ShowPanel(string panelId, string? query = null) => ShowPanelCalls++;

        public int PersistPreferencesCalls { get; private set; }

        public void PersistPreferences() => PersistPreferencesCalls++;

        public void SelectComponent(long instanceId) { }

        public void SelectEvent(long eventId) { }

        public void Notify(string message) { }
    }
}
