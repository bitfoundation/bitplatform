namespace Bit.Butil;

/// <summary>
/// A purchase the store has on file for this user.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalGoodsService/listPurchases">DigitalGoodsService.listPurchases()</see>
/// </summary>
public class DigitalGoodsPurchase
{
    /// <summary>The <see cref="DigitalGoodsItem.ItemId"/> that was bought.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The store's token for this purchase. Send it to your server and have the server verify it
    /// with the store's own API before unlocking anything - the client's word that a purchase
    /// happened is not evidence that it did.
    /// </summary>
    public string PurchaseToken { get; set; } = string.Empty;
}
