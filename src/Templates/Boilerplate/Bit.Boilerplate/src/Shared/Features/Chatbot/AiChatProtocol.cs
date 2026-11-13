//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// What the AI chat panel and the server agree on beyond Bit.BlazorUI.AI's own wire format: the conversation travels as a
/// BitAiRequest to <c>AppHub</c> (a BitAiHub), and its answer comes back as BitAiEvents, so all that's left to agree on is
/// what the request carries besides the conversation, what the server writes on an answer, and the tools' names.
/// </summary>
public static class AiChatProtocol
{
    /// <summary>How many of the newest messages the model is shown, and so all the panel keeps and sends (See <c>AppChatbot.TrimChatHistory</c>).</summary>
    public const int MaxHistory = 40;

    #region What a question carries besides the conversation (BitAiRequestOptions.Extra, which the session sends with every request)

    /// <summary>The LCID of the user's culture, which the answer is written in.</summary>
    public const string CultureId = "cultureId";

    public const string TimeZoneId = "timeZoneId";

    /// <summary>The device the app runs on, for the system prompt's variables.</summary>
    public const string DeviceInfo = "deviceInfo";

    #endregion

    #region What the server writes on an answer (BitAiGenerationMetadata.Extra)

    /// <summary>The answer the model wrote, word for word: what read aloud speaks.</summary>
    public const string Answer = "answer";

    /// <summary>The server's signature over <see cref="Answer"/>, so read aloud and the conversation sent back can be trusted (See <c>ChatbotAnswerSigner</c>).</summary>
    public const string Signature = "signature";

    /// <summary>
    /// What the model reads of this answer on the next turn when it showed something besides its words - the products it
    /// showed, the form it offered - with what it wrote. Missing when it's the same as <see cref="Answer"/>.
    /// </summary>
    public const string Replay = "replay";

    /// <summary>The server's signature over <see cref="Replay"/>.</summary>
    public const string ReplaySignature = "replaySignature";

    #endregion

    #region The attachments of a message (BitAiAttachment.Metadata)

    /// <summary>The id an image was stored under (See <c>AttachmentController.UploadAiChatImage</c>): all of it the server reads.</summary>
    public const string AttachmentId = "attachmentId";

    #endregion

    /// <summary>The tools the panel draws in a way of their own, by the name the model calls them (See <c>AppChatbot.Tools.cs</c>).</summary>
    public static class Tools
    {
        public const string GetCurrentDateTime = nameof(GetCurrentDateTime);
        public const string RequestHumanFollowUp = nameof(RequestHumanFollowUp);
        public const string ShowFollowUpSuggestions = nameof(ShowFollowUpSuggestions);
        public const string GetAppPages = nameof(GetAppPages);
        public const string NavigateToPage = nameof(NavigateToPage);
        public const string ShowSignInModal = nameof(ShowSignInModal);
        public const string SetApplicationCulture = nameof(SetApplicationCulture);
        public const string SetApplicationTheme = nameof(SetApplicationTheme);
        public const string CheckLastError = nameof(CheckLastError);
        public const string ClearAppFiles = nameof(ClearAppFiles);
        //#if (module == "Sales")
        //#if (database == "PostgreSQL" || database == "SqlServer")
        public const string GetProductRecommendations = nameof(GetProductRecommendations);
        public const string ShowProducts = nameof(ShowProducts);
        //#endif
        //#endif
    }
}
