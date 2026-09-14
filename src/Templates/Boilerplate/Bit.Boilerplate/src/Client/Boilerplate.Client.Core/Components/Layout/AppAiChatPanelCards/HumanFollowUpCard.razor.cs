namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards;

/// <summary>
/// Shown when the chatbot could not resolve the user's issue (See <c>AppChatbot.RequestHumanFollowUp</c>). A signed-in
/// user is already known, so only an anonymous one is asked for contact details.
/// </summary>
public partial class HumanFollowUpCard
{
    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    [AutoInject] private ILogger<HumanFollowUpCard> logger = default!;

    private readonly FollowUpRequest request = new();

    private bool IsSent => Message.Data.ContainsKey("Sent");

    protected override async Task OnInitAsync()
    {
        // The model's draft, in the user's language.
        request.ConversationSummary = Message.Data.GetValueOrDefault("ConversationSummary");

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

        // Kept on the card, so it stays sent after a reload.
        Message.Data["Sent"] = "true";

        await Host.Save(Message);
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
