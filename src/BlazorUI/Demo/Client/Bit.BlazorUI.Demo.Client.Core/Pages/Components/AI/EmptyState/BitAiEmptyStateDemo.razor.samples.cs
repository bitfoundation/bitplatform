namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.EmptyState;

public partial class BitAiEmptyStateDemo
{
    private readonly string example1RazorCode = @"
<BitAiEmptyState />";

    private readonly string example2RazorCode = @"
<BitAiEmptyState Title=""Ask the docs assistant"" Body=""It knows every component of bit BlazorUI."" />

<BitAiEmptyState Title=""Start a new thread"">
    Questions are answered from the <BitLink Href=""/components"">component gallery</BitLink> and nothing else.
</BitAiEmptyState>";

    private readonly string example3RazorCode = @"
<BitAiEmptyState Title=""What can I help with?"" Suggestions=""suggestions"" OnSuggestion=""HandleSuggestion"" />

<div>Chosen: <b>@(chosenSuggestion ?? ""-"")</b></div>";
    private readonly string example3CsharpCode = @"
private string? chosenSuggestion;
private readonly string[] suggestions = [""Summarize this page"", ""Write a release note"", ""Explain render modes""];

// Submit it to a session here: await session.SubmitAsync(suggestion);
private void HandleSuggestion(string suggestion)
{
    chosenSuggestion = suggestion;
}";

    private readonly string example4RazorCode = @"
<BitAiEmptyState IconName=""@BitIconName.Robot"" Title=""Hi, I am Bit"" />

<BitAiEmptyState Title=""Welcome back"">
    <IconTemplate>
        <span style=""font-size:2.5rem"">👋</span>
    </IconTemplate>
</BitAiEmptyState>";

    private readonly string example5RazorCode = @"
<BitAiEmptyState Disabled Title=""The assistant is offline"" Suggestions=""suggestions"" />";
    private readonly string example5CsharpCode = @"
private readonly string[] suggestions = [""Summarize this page"", ""Write a release note"", ""Explain render modes""];";

    private readonly string example6RazorCode = @"
<BitParams Parameters=""emptyStateParams"">
    <BitAiEmptyState />

    <BitAiEmptyState Title=""Its own title wins"" />
</BitParams>";
    private readonly string example6CsharpCode = @"
private readonly BitAiEmptyStateParams[] emptyStateParams =
[
    new()
    {
        Title = ""Ask the docs"",
        Body = ""Both empty states take their words and glyph from the BitParams around them."",
        IconName = BitIconName.ChatBot,
    }
];";

    private readonly string example7RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.3/font/bootstrap-icons.min.css"" />

<BitAiEmptyState Icon=""@BitIconInfo.Fa(""solid wand-magic-sparkles"")"" Title=""FontAwesome"" />

<BitAiEmptyState Icon=""@BitIconInfo.Bi(""chat-dots"")"" Title=""Bootstrap Icons"" />";

    private readonly string example8RazorCode = @"
<style>
    .custom-empty {
        border-radius: 1rem;
        border: 1px dashed var(--bit-clr-pri);
    }

    .custom-empty-icon {
        transform: rotate(-12deg);
    }

    .custom-empty-title {
        text-transform: uppercase;
        letter-spacing: 0.1em;
    }
</style>

<BitAiEmptyState Title=""Styled""
                 Suggestions=""suggestions""
                 Style=""border-radius:1rem;background:linear-gradient(180deg, var(--bit-clr-pri-tint), transparent)""
                 Styles=""@(new() { Title = ""font-style:italic"", Suggestion = new() { Root = ""border-radius:0.25rem"" } })"" />

<BitAiEmptyState Title=""Classy""
                 Class=""custom-empty""
                 Classes=""@(new() { Icon = ""custom-empty-icon"", Title = ""custom-empty-title"" })"" />

<BitAiEmptyState Title=""CSS variables""
                 Suggestions=""suggestions""
                 Style=""--bit-AiEmptyState-gap:0.5rem;--bit-AiEmptyState-padding:1.5rem;--bit-AiEmptyState-color:var(--bit-clr-fg-pri);--bit-AiEmptyState-icon-color:var(--bit-clr-suc);--bit-AiEmptyState-icon-size:4rem;--bit-AiEmptyState-title-color:var(--bit-clr-suc);--bit-AiEmptyState-title-font-size:2rem;--bit-AiEmptyState-title-font-weight:300;--bit-AiEmptyState-suggestions-gap:1rem"" />";
    private readonly string example8CsharpCode = @"
private readonly string[] suggestions = [""Summarize this page"", ""Write a release note"", ""Explain render modes""];";

    private readonly string example9RazorCode = @"
<BitAiEmptyState Dir=""BitDir.Rtl""
                 Title=""چطور می‌توانم کمک کنم؟""
                 Body=""پیامی بنویسید یا یکی از پیشنهادها را انتخاب کنید.""
                 Suggestions=""rtlSuggestions"" />";
    private readonly string example9CsharpCode = @"
private readonly string[] rtlSuggestions = [""این صفحه را خلاصه کن"", ""یک یادداشت انتشار بنویس""];";
}
