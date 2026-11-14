namespace Boilerplate.Client.Core.Components.Layout.AiChat;

/// <summary>
/// Shown when the chatbot could not resolve the user's issue (See <c>AppChatbot.RequestHumanFollowUp</c>), drafted with the
/// model's summary of the conversation. A signed-in user is already known, so only an anonymous one is asked for contact
/// details.
/// </summary>
public partial class HumanFollowUpCard
{
    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    [AutoInject] private ILogger<HumanFollowUpCard> logger = default!;

    /// <summary>The model's draft, in the user's language.</summary>
    [Parameter] public string? ConversationSummary { get; set; }

    /// <summary>Sent already, in this visit or one before it: the form gives way to its confirmation.</summary>
    [Parameter] public bool IsSent { get; set; }

    /// <summary>Keeps that it was sent with the conversation, so it isn't offered again once the app reloads it.</summary>
    [Parameter] public EventCallback OnSent { get; set; }

    private readonly FollowUpRequest request = new();

    /// <summary>Sent from this card just now, ahead of <see cref="IsSent"/> coming back from the conversation.</summary>
    private bool sent;

    protected override async Task OnInitAsync()
    {
        request.ConversationSummary = ConversationSummary;

        await base.OnInitAsync();
    }

    protected override async Task OnParamsSetAsync()
    {
        // CurrentUser cascades, so a sign in or out while the card is open changes what it asks for.
        request.IsSignedIn = CurrentUser is not null;

        await base.OnParamsSetAsync();
    }

    private async Task Send()
    {
        // Starter code: replace the log with your own delivery, e.g. an http call that stores the request in your CRM.
        logger.LogWarning("Human follow-up requested. User id: {UserId}, Email: {Email}, Phone number: {PhoneNumber}, Conversation summary: {ConversationSummary}",
                          CurrentUser?.Id,
                          CurrentUser is null ? request.Email : CurrentUser.Email,
                          CurrentUser is null ? request.PhoneNumber : CurrentUser.PhoneNumber,
                          request.ConversationSummary);

        sent = true;

        await OnSent.InvokeAsync();
    }

    [DtoResourceType(typeof(AppStrings))]
    public class FollowUpRequest : IValidatableObject
    {
        /// <summary>Checked in <see cref="Validate"/> rather than with [Required], whose message would need a new resx key.</summary>
        public string? ConversationSummary { get; set; }

        [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
        [Display(Name = nameof(AppStrings.Email))]
        public string? Email { get; set; }

        [Phone(ErrorMessage = nameof(AppStrings.PhoneAttribute_ValidationError))]
        [Display(Name = nameof(AppStrings.PhoneNumber))]
        public string? PhoneNumber { get; set; }

        /// <summary>A signed-in user's own contact details are used, so none are asked for.</summary>
        public bool IsSignedIn { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ConversationSummary))
                yield return new ValidationResult(
                    errorMessage: "The conversation summary is required.",
                    memberNames: [nameof(ConversationSummary)]
                );

            if (IsSignedIn is false && string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(PhoneNumber))
                yield return new ValidationResult(
                    errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber),
                    memberNames: [nameof(Email), nameof(PhoneNumber)]
                );
        }
    }
}
