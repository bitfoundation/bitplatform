namespace Bit.Butil;

/// <summary>
/// One item as the store describes it - the title, price and billing period the store itself holds,
/// which is why a storefront should render these rather than a copy kept in your own database.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalGoodsService/getDetails">DigitalGoodsService.getDetails()</see>
/// </summary>
public class DigitalGoodsItem
{
    /// <summary>The store's identifier for the item - what you pass back to buy or consume it.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>The item's title, localized by the store.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The item's description, localized by the store.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The price in the user's currency, already converted by the store. Show this, not a price of
    /// your own: it is the number the user will be charged.
    /// </summary>
    public PaymentCurrencyAmount? Price { get; set; }

    /// <summary><c>"product"</c> for a one-off purchase, <c>"subscription"</c> for a recurring one.</summary>
    public string? Type { get; set; }

    /// <summary>Icon URLs the store holds for the item, largest first where it offers several.</summary>
    public string[] IconUrls { get; set; } = [];

    /// <summary>The billing period of a subscription, as an ISO 8601 duration (<c>"P1M"</c>).</summary>
    public string? SubscriptionPeriod { get; set; }

    /// <summary>The free trial length, as an ISO 8601 duration.</summary>
    public string? FreeTrialPeriod { get; set; }

    /// <summary>The introductory price, when the item has one.</summary>
    public PaymentCurrencyAmount? IntroductoryPrice { get; set; }

    /// <summary>How long the introductory price lasts, as an ISO 8601 duration.</summary>
    public string? IntroductoryPricePeriod { get; set; }

    /// <summary>How many billing cycles the introductory price covers.</summary>
    public int? IntroductoryPriceCycles { get; set; }
}
