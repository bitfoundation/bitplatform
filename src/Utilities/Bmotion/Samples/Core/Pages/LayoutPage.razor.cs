namespace Bit.Bmotion.Samples.Core.Pages;

public partial class LayoutPage
{
    private static readonly string[] Tabs = ["Spring", "Summer", "Autumn", "Winter"];

    private List<int> _numbers = [.. Enumerable.Range(1, 12)];
    private int _arrangement;
    private string _tab = Tabs[0];
    private List<string> _tasks = ["Write the draft", "Ask for a review", "Fix the typos", "Publish", "Tell everyone"];
    private int _reorders;

    private string LayoutNote => RendererInfo.Name switch
    {
        "Server" => "On Server the measuring happens in the browser, a round trip after the render that moved an element; until then each element is held where it was, so nothing flashes at its new place first.",
        "WebView" => "In the MAUI app the measuring happens in the WebView, after the render that moved an element; until then each element is held where it was.",
        _ => "",
    };

    private void Shuffle() => Arrange([.. _numbers.OrderBy(_ => Random.Shared.Next())]);

    private void Sort() => Arrange([.. _numbers.Order()]);

    private void Arrange(List<int> numbers)
    {
        _numbers = numbers;
        _arrangement++;
    }

    private void Reverse() => _tasks = [.. Enumerable.Reverse(_tasks)];

    private static string TabText(string tab) => tab switch
    {
        "Spring" => "Longer days, and everything starts again.",
        "Summer" => "The longest days, and the slowest evenings.",
        "Autumn" => "The leaves turn and fall, and the evenings draw in.",
        _ => "The shortest days, and the year turns over.",
    };
}
