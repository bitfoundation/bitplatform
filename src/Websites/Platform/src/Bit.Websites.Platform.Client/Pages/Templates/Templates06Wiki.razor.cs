namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates06Wiki
{
    private static string AskUrl(string question) => $"{Urls.Ask}?question={Uri.EscapeDataString(question)}";
}
