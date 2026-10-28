namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ConversationList;

public partial class BitAiConversationListDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiConversationListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the conversation list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversations",
            Type = "IReadOnlyList<BitAiConversationSummary>?",
            DefaultValue = "null",
            Description = "The conversations to list, in any order: they are sorted by UpdatedAt, newest first, and grouped into today, yesterday, the previous seven days, the previous thirty days, then by month - counted in the TimeZone, else in the TimeZone of the BitAiUiOptions in force, else in the browser's zone.",
            LinkType = LinkType.Link,
            Href = "#conversation-summary",
        },
        new()
        {
            Name = "OnDelete",
            Type = "EventCallback<BitAiConversationSummary>",
            DefaultValue = "",
            Description = "The callback for when the delete button of a conversation is pressed. The delete buttons are shown only while it is set; removing the conversation (and handing the list the new set) is the app's.",
            LinkType = LinkType.Link,
            Href = "#conversation-summary",
        },
        new()
        {
            Name = "OnNew",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the new-conversation button is pressed.",
        },
        new()
        {
            Name = "OnSelect",
            Type = "EventCallback<BitAiConversationSummary>",
            DefaultValue = "",
            Description = "The callback for when a conversation is chosen. Opening it - loading it, pointing the chat at its session - is the app's.",
            LinkType = LinkType.Link,
            Href = "#conversation-summary",
        },
        new()
        {
            Name = "SelectedId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ConversationId of the conversation currently open, which is marked with aria-current and set apart.",
        },
        new()
        {
            Name = "ShowNewButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the new-conversation button at the top of the list.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiConversationListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the conversation list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "TimeZone",
            Type = "TimeZoneInfo?",
            DefaultValue = "null",
            Description = "The time zone the conversations are grouped by day in - what counts as today, yesterday, the previous seven and thirty days, and the month of an older one. While it is null, the TimeZone of the BitAiUiOptions in force, else the browser's: under Blazor Server the list is drawn in the zone of the server until the browser has said which zone it runs in.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiConversationList-gap", DefaultValue = "spacing(0.25)", Description = "Room between two conversations, and between the parts of the list." },
        new() { Name = "--bit-AiConversationList-padding", DefaultValue = "spacing(1)", Description = "Inset of the list." },
        new() { Name = "--bit-AiConversationList-group-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Text of the heading of a group." },
        new() { Name = "--bit-AiConversationList-item-radius", DefaultValue = "var(--bit-shp-radius-control)", Description = "Corners of a conversation." },
        new() { Name = "--bit-AiConversationList-item-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text of a conversation." },
        new() { Name = "--bit-AiConversationList-item-background", DefaultValue = "transparent", Description = "Background of a conversation." },
        new() { Name = "--bit-AiConversationList-item-hover-background", DefaultValue = "var(--bit-clr-bg-pri-hover)", Description = "Background of a conversation under the pointer." },
        new() { Name = "--bit-AiConversationList-selected-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Text of the conversation that is open." },
        new() { Name = "--bit-AiConversationList-selected-background", DefaultValue = "var(--bit-clr-pri-tint)", Description = "Background of the conversation that is open." },
        new() { Name = "--bit-AiConversationList-selected-hover-background", DefaultValue = "var(--bit-clr-bg-pri-hover)", Description = "Background of the conversation that is open, under the pointer." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiConversationListClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiConversationList."
                },
                new()
                {
                    Name = "NewButton",
                    Type = "BitActionButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the new-conversation button of the BitAiConversationList.",
                    LinkType = LinkType.Link,
                    Href = "/components/actionbutton/#class-styles"
                },
                new()
                {
                    Name = "GroupHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the heading of each group of the BitAiConversationList."
                },
                new()
                {
                    Name = "List",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the list of each group of the BitAiConversationList."
                },
                new()
                {
                    Name = "Item",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each item of the BitAiConversationList."
                },
                new()
                {
                    Name = "ItemButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button each conversation of the BitAiConversationList is chosen with."
                },
                new()
                {
                    Name = "SelectedItemButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the conversation currently open in the BitAiConversationList, added to those of ItemButton."
                },
                new()
                {
                    Name = "ItemTitle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of each conversation of the BitAiConversationList."
                },
                new()
                {
                    Name = "DeleteButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the delete button of each conversation of the BitAiConversationList.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                }
            ]
        },
        new()
        {
            Id = "conversation-summary",
            Title = "BitAiConversationSummary",
            Description = "The lightweight projection of a conversation a list renders without loading its messages: a positional record, new BitAiConversationSummary(conversationId, title, updatedAt, preview?, messageCount?). BitAiConversation.ToSummary() and IBitAiConversationStore.ListAsync() return them.",
            Parameters =
            [
                new()
                {
                    Name = "ConversationId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the conversation, which SelectedId is compared with."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "",
                    Description = "The display title. A conversation without one is listed under the Untitled string."
                },
                new()
                {
                    Name = "UpdatedAt",
                    Type = "DateTimeOffset",
                    DefaultValue = "",
                    Description = "When the conversation last changed, which sorts the list and picks the group it is listed in."
                },
                new()
                {
                    Name = "Preview",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A snippet of the last message, shown as the tooltip of the conversation."
                },
                new()
                {
                    Name = "MessageCount",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "How many messages the conversation holds."
                }
            ]
        },
    ];



    private static IReadOnlyList<BitAiConversationSummary> CreateSummaries()
    {
        var now = DateTimeOffset.Now;

        return
        [
            new("c1", "Plan a weekend in Lisbon", now.AddMinutes(-20), "Three days, mostly on foot, with one day trip to Sintra.", 6),
            new("c2", "Draft the release notes", now.AddHours(-2), "Grouped by package, breaking changes first.", 4),
            new("c3", "Why does my circuit drop?", now.AddDays(-1), "Most likely an idle timeout on the proxy.", 8),
            new("c4", "Compare the pricing tiers", now.AddDays(-4), "The middle tier covers most teams.", 10),
            new("c5", "Explain render modes", now.AddDays(-12), "Server, WebAssembly and Auto, per component.", 2),
            new("c6", "Name the new component", now.AddDays(-45), "BitAiChat won the vote.", 12),
        ];
    }


    private string? selectedId = "c2";
    private readonly IReadOnlyList<BitAiConversationSummary> summaries = CreateSummaries();

    private void HandleSelect(BitAiConversationSummary summary)
    {
        selectedId = summary.ConversationId;
    }


    private int newCount;
    private string? editableSelectedId = "c1";
    private IReadOnlyList<BitAiConversationSummary> editableSummaries = CreateSummaries();

    private void HandleEditableSelect(BitAiConversationSummary summary)
    {
        editableSelectedId = summary.ConversationId;
    }

    private void HandleNew()
    {
        var summary = new BitAiConversationSummary($"new-{++newCount}", $"New conversation {newCount}", DateTimeOffset.Now);

        editableSummaries = [summary, .. editableSummaries];
        editableSelectedId = summary.ConversationId;
    }

    private void HandleDelete(BitAiConversationSummary summary)
    {
        editableSummaries = editableSummaries.Where(s => s.ConversationId != summary.ConversationId).ToList();

        if (editableSelectedId == summary.ConversationId) editableSelectedId = editableSummaries.FirstOrDefault()?.ConversationId;
    }


    // Never called: a disabled list raises none of its callbacks.
    private void HandleDisabledNew() { }

    private void HandleDisabledDelete(BitAiConversationSummary summary) { }


    // One live session per thread, so a reply keeps streaming in a thread that was switched away from.
    private readonly Dictionary<string, BitAiChatSession> chatSessions = new(StringComparer.Ordinal);
    private IReadOnlyList<BitAiConversationSummary> chatSummaries = [];
    private BitAiChatSession? chatSession;

    [AutoInject] private BitAiChatSessionFactory sessionFactory = default!;

    private void OpenChat(BitAiConversation conversation)
    {
        if (chatSessions.TryGetValue(conversation.ConversationId, out var session) is false)
        {
            session = sessionFactory.Create(conversation);
            conversation.MessageAdded += OnChatMessageAdded;
            conversation.Changed += OnChatChanged;
            chatSessions[conversation.ConversationId] = session;
        }

        chatSession = session;
    }

    private void NewChat()
    {
        // An untouched thread is reused rather than stacking empty ones.
        if (chatSession?.Conversation.Messages.Count == 0) return;

        OpenChat(new BitAiConversation());
    }

    private void SelectChat(BitAiConversationSummary summary)
    {
        if (chatSessions.TryGetValue(summary.ConversationId, out var session)) OpenChat(session.Conversation);
    }

    private async Task DeleteChat(BitAiConversationSummary summary)
    {
        if (chatSessions.Remove(summary.ConversationId, out var removed) is false) return;

        removed.Conversation.MessageAdded -= OnChatMessageAdded;
        removed.Conversation.Changed -= OnChatChanged;
        await removed.DisposeAsync();

        RefreshChatSummaries();

        if (ReferenceEquals(chatSession, removed) is false) return;

        var next = chatSessions.Values.FirstOrDefault(s => s.Conversation.Messages.Count > 0);

        OpenChat(next?.Conversation ?? new BitAiConversation());
    }

    // Only the threads with a message in them are listed, newest first.
    private void RefreshChatSummaries()
    {
        chatSummaries = chatSessions.Values.Select(s => s.Conversation)
                                           .Where(c => c.Messages.Count > 0)
                                           .Select(c => c.ToSummary())
                                           .ToList();
    }

    // Raised from whatever thread the stream is on, so the list is refreshed on the renderer's.
    private void OnChatMessageAdded(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(() =>
    {
        RefreshChatSummaries();
        StateHasChanged();
    });

    private void OnChatChanged(BitAiConversation conversation) => _ = InvokeAsync(() =>
    {
        RefreshChatSummaries();
        StateHasChanged();
    });


    // Two zones twelve hours apart and with no daylight saving time, so a new day has always begun in one of them and not
    // yet in the other for twelve hours, whatever the time is.
    private static readonly TimeZoneInfo tokyoZone = TimeZoneInfo.CreateCustomTimeZone("Tokyo", TimeSpan.FromHours(9), "Tokyo (UTC+9)", "Tokyo (UTC+9)");
    private static readonly TimeZoneInfo saoPauloZone = TimeZoneInfo.CreateCustomTimeZone("Sao Paulo", TimeSpan.FromHours(-3), "São Paulo (UTC-3)", "São Paulo (UTC-3)");
    private readonly IReadOnlyList<BitAiConversationSummary> zoneSummaries = CreateZoneSummaries();

    private static IReadOnlyList<BitAiConversationSummary> CreateZoneSummaries()
    {
        var now = DateTimeOffset.UtcNow;

        // Halfway between the midnights the two cities passed last: after the later one in one city, before it in the other.
        var sinceTokyoMidnight = TimeZoneInfo.ConvertTime(now, tokyoZone).TimeOfDay;
        var sinceSaoPauloMidnight = TimeZoneInfo.ConvertTime(now, saoPauloZone).TimeOfDay;
        var ago = (sinceTokyoMidnight + sinceSaoPauloMidnight) / 2;

        return
        [
            new("z1", "Check in for the trip", now.AddMinutes(-10), "Seats 14A and 14B.", 2),
            new("z2", "Book the flights", now - ago, "The direct one on Friday morning.", 6),
            new("z3", "Pack for the weather", now.AddDays(-3), "Light layers and an umbrella.", 4),
        ];
    }


    private readonly BitAiConversationListParams[] conversationListParams =
    [
        new()
        {
            ShowNewButton = false,
            TimeZone = TimeZoneInfo.Utc,
            Styles = new() { GroupHeader = "color:var(--bit-clr-pri)", Root = "border:1px solid var(--bit-clr-brd-sec);border-radius:0.5rem" },
        }
    ];


    private string? rtlSelectedId = "r1";
    private readonly IReadOnlyList<BitAiConversationSummary> rtlSummaries =
    [
        new("r1", "برنامه‌ی سفر آخر هفته", DateTimeOffset.Now.AddHours(-1), "سه روز، بیشتر پیاده.", 6),
        new("r2", "یادداشت‌های انتشار", DateTimeOffset.Now.AddDays(-1), "دسته‌بندی بر اساس بسته.", 4),
        new("r3", "مقایسه‌ی طرح‌های قیمت", DateTimeOffset.Now.AddDays(-5), "طرح میانی برای بیشتر تیم‌ها کافی است.", 10),
    ];

    private void HandleRtlSelect(BitAiConversationSummary summary)
    {
        rtlSelectedId = summary.ConversationId;
    }



    protected override Task OnInitAsync()
    {
        OpenChat(new BitAiConversation());

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        foreach (var session in chatSessions.Values)
        {
            session.Conversation.MessageAdded -= OnChatMessageAdded;
            session.Conversation.Changed -= OnChatChanged;
            await session.DisposeAsync();
        }

        chatSessions.Clear();

        await base.DisposeAsync(disposing);
    }
}
