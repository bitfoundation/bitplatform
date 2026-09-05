namespace Bit.Butil;

/// <summary>
/// The operations a
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Private_State_Token_API">Private State Token</see>
/// fetch can carry, matching the strings the <c>privateToken</c> fetch option accepts.
/// </summary>
public enum PrivateStateTokenOperation
{
    /// <summary>Ask the issuer for tokens. Done once, on a site where the user has already proved they are trustworthy.</summary>
    TokenRequest,

    /// <summary>Spend a token at the issuer and get a redemption record back.</summary>
    TokenRedemption,

    /// <summary>Attach the redemption record to a request, so the far end learns "trusted" and nothing else.</summary>
    SendRedemptionRecord,
}
