namespace Bit.Minifier.Tests;

[TestClass]
public class MapDecoderTests
{
    /// <summary>
    /// What the minifier writes for an assembly whose namespace went with its type names: Worker -> _a, its state
    /// machine renamed twice (once as a generated name, once as a type), and its members renamed before it was.
    /// </summary>
    private static readonly string[] App =
    [
        "App\tM\tApp.Services.Worker::Process\taa",
        "App\tF\tApp.Services.Worker::_queue\tab",
        "App\tM\tApp.Services.Worker/<a>d__3::ReportAsync\tac",
        "App\tG\tApp.Services.Worker::aa<TItem>\ta",
        "App\tT\tApp.Services.Worker/<Run>d__3\t<a>d__3",
        "App\tT\tApp.Services.Worker\t_a",
        "App\tT\tApp.Services.Worker/<Run>d__3\t_b",
        "App\tT\tApp.Services.Options\tApp.Services._c",
    ];

    [TestMethod]
    public void ReadsTypesTheirMembersAndTheirGenericParameters()
    {
        var decoder = new MapDecoder(App);

        var decoded = decoder.Decode("""
            System.InvalidOperationException: nothing to do
               at _a.aa[a](String name) in /_/src/Worker.cs:line 42
               at App.Services._c.ToString()
               at System.Threading.Tasks.Task.ThrowAsync(Exception exception)
            """);

        Assert.AreEqual("""
            System.InvalidOperationException: nothing to do
               at App.Services.Worker.Process[TItem](String name) in /_/src/Worker.cs:line 42
               at App.Services.Options.ToString()
               at System.Threading.Tasks.Task.ThrowAsync(Exception exception)
            """, decoded);
    }

    [TestMethod]
    public void ReadsANestedTypeThroughTheTypeItIsNestedIn()
    {
        var decoder = new MapDecoder(App);

        // the state machine was renamed twice, and its method before either time
        var decoded = decoder.Decode("   at _a+_b.MoveNext()\n   at _a+_b.ac()");

        Assert.AreEqual("   at App.Services.Worker+<Run>d__3.MoveNext()\n   at App.Services.Worker+<Run>d__3.ReportAsync()", decoded);
    }

    [TestMethod]
    public void ReadsANameThatStandsOnItsOwn()
    {
        var decoder = new MapDecoder(App);

        var decoded = decoder.Decode("   at App.Host.Start(_a worker, Int32 count)");

        Assert.AreEqual("   at App.Host.Start(App.Services.Worker worker, Int32 count)", decoded);
    }

    [TestMethod]
    public void KeepsWhatTheMapDoesNotName()
    {
        var text = """
            System.IO.FileNotFoundException: Could not load file or assembly 'App.Services'
               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
               at App.Services.Worker.Report(String message) in /_/src/Worker.cs:line 7
            """;

        Assert.AreEqual(text, new MapDecoder(App).Decode(text));
        Assert.AreEqual(text, new MapDecoder(["not a map", "", "App\tT\ttoo\tmany\tcolumns"]).Decode(text));
    }

    [TestMethod]
    public void NamesEveryAssemblyAShortNameCouldBelongTo()
    {
        var decoder = new MapDecoder([.. App, "Lib\tT\tLib.Handler\t_a", "Lib\tM\tLib.Handler::Handle\taa"]);

        var decoded = decoder.Decode("   at _a.aa(String name)");

        Assert.AreEqual("   at App.Services.Worker.Process(String name)  [Bit.Minifier: _a.aa is also Lib.Handler.Handle (Lib)]", decoded);
    }

    [TestMethod]
    public void ReadsTheSameAnswerFromSeveralAssembliesOnce()
    {
        // a type both assemblies see, renamed the same way in each
        var decoder = new MapDecoder([.. App, "Lib\tT\tApp.Services.Worker\t_a", "Lib\tM\tApp.Services.Worker::Process\taa"]);

        Assert.AreEqual("   at App.Services.Worker.Process()", decoder.Decode("   at _a.aa()"));
    }
}
