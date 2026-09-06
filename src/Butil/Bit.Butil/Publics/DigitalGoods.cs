using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Digital_Goods_API">Digital Goods API</see>
/// (<c>window.getDigitalGoodsService()</c>): reads an app store's catalogue and the user's
/// entitlements from inside an installed PWA.
/// </summary>
/// <remarks>
/// This half only queries - it lists items, purchases and history, and consumes a purchase. Buying
/// is a <see cref="PaymentRequest"/> with the store's payment method identifier
/// (<c>"https://play.google.com/billing"</c>) and the item id in its data, and the purchase shows up
/// here afterwards.
/// <br/>
/// Chromium only, and only in an app installed from the store it is asking - a browser tab has no
/// store to connect to, so <see cref="Connect"/> returns false there. Every entitlement decision
/// belongs on your server, verified against the store's API with a
/// <see cref="DigitalGoodsPurchase.PurchaseToken"/>; a client that says it paid is a claim, not a
/// receipt.
/// </remarks>
[ButilService(typeof(DigitalGoods))]
public class DigitalGoods(IJSRuntime js)
{
    /// <summary>
    /// The payment method identifier for Google Play billing - the service provider these calls
    /// default to, and the method identifier a <see cref="PaymentRequest"/> buys through.
    /// </summary>
    public const string GooglePlayBilling = "https://play.google.com/billing";

    /// <summary>
    /// True when the runtime exposes <c>window.getDigitalGoodsService</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/getDigitalGoodsService">Window.getDigitalGoodsService()</see>
    /// </summary>
    /// <remarks>
    /// Being exposed is not the same as being connectable: an uninstalled app has the function and
    /// no store behind it. <see cref="Connect"/> is the check that answers for real.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.digitalGoods.isSupported");

    /// <summary>
    /// Connects to a store's billing service, returning false when there is none - the usual answer
    /// in a browser tab. The connection is cached, so the other members can be called straight away
    /// and this is only worth calling to decide whether to show a store UI at all.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/getDigitalGoodsService">Window.getDigitalGoodsService()</see>
    /// </summary>
    /// <param name="serviceProvider">The store's payment method identifier. Defaults to <see cref="GooglePlayBilling"/>.</param>
    public ValueTask<bool> Connect(string serviceProvider = GooglePlayBilling)
        => js.Invoke<bool>("BitButil.digitalGoods.connect", serviceProvider);

    /// <summary>
    /// Looks up the store's own title, description and price for each item id, skipping ids the
    /// store does not know. Empty when there is no store to ask.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalGoodsService/getDetails">DigitalGoodsService.getDetails()</see>
    /// </summary>
    /// <param name="itemIds">The item ids configured in the store's console.</param>
    /// <param name="serviceProvider">The store's payment method identifier. Defaults to <see cref="GooglePlayBilling"/>.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalGoodsItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PaymentCurrencyAmount))]
    public ValueTask<DigitalGoodsItem[]> GetDetails(string[] itemIds, string serviceProvider = GooglePlayBilling)
        => js.Invoke<DigitalGoodsItem[]>("BitButil.digitalGoods.getDetails", itemIds, serviceProvider);

    /// <summary>
    /// The purchases that are currently active - unconsumed one-offs and live subscriptions. This is
    /// what a client restores entitlements from after a reinstall, and what it re-checks on launch.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalGoodsService/listPurchases">DigitalGoodsService.listPurchases()</see>
    /// </summary>
    /// <param name="serviceProvider">The store's payment method identifier. Defaults to <see cref="GooglePlayBilling"/>.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalGoodsPurchase))]
    public ValueTask<DigitalGoodsPurchase[]> ListPurchases(string serviceProvider = GooglePlayBilling)
        => js.Invoke<DigitalGoodsPurchase[]>("BitButil.digitalGoods.listPurchases", serviceProvider);

    /// <summary>
    /// The most recent purchase per item, including ones already consumed or expired - the history
    /// behind <see cref="ListPurchases"/> rather than the current state.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalGoodsService/listPurchaseHistory">DigitalGoodsService.listPurchaseHistory()</see>
    /// </summary>
    /// <param name="serviceProvider">The store's payment method identifier. Defaults to <see cref="GooglePlayBilling"/>.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DigitalGoodsPurchase))]
    public ValueTask<DigitalGoodsPurchase[]> ListPurchaseHistory(string serviceProvider = GooglePlayBilling)
        => js.Invoke<DigitalGoodsPurchase[]>("BitButil.digitalGoods.listPurchaseHistory", serviceProvider);

    /// <summary>
    /// Marks a purchase as used up, so the item can be bought again - the coin pack the user spent.
    /// Returns false when the store refused or there was no store.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalGoodsService/consume">DigitalGoodsService.consume()</see>
    /// </summary>
    /// <param name="purchaseToken">The <see cref="DigitalGoodsPurchase.PurchaseToken"/> to consume.</param>
    /// <param name="serviceProvider">The store's payment method identifier. Defaults to <see cref="GooglePlayBilling"/>.</param>
    /// <remarks>
    /// Consume only after your server has recorded what the purchase bought. Consuming first and
    /// crediting second loses the purchase entirely if anything in between fails.
    /// </remarks>
    public ValueTask<bool> Consume(string purchaseToken, string serviceProvider = GooglePlayBilling)
        => js.Invoke<bool>("BitButil.digitalGoods.consume", purchaseToken, serviceProvider);
}
