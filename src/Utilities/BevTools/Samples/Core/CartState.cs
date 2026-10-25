namespace Bit.BevTools.Samples.Core;

/// <summary>
/// A state container the State panel lists by name, with a snapshot and a diff for every named action. Implementing
/// <see cref="IBevStateProvider"/> is all it takes; IServiceCollectionExtensions registers it.
/// </summary>
public sealed class CartState : IBevStateProvider
{
    public List<CartLine> Lines { get; } = [];

    public decimal Total => Lines.Sum(l => l.Product.Price * l.Quantity);

    public string? CouponCode { get; private set; }

    public string Name => "CartState";

    public string? Description => "scoped: one per circuit, WebAssembly app or BlazorWebView";

    public event Action<BevStateChangeInfo>? Changed;

    public object? GetSnapshot() => this;

    public void Add(Product product)
    {
        var line = Lines.FirstOrDefault(l => l.Product.Id == product.Id);
        if (line is null)
        {
            Lines.Add(new CartLine { Product = product, Quantity = 1 });
        }
        else
        {
            line.Quantity++;
        }

        Changed?.Invoke(new BevStateChangeInfo("Add", product.Name));
    }

    public void Remove(int productId)
    {
        Lines.RemoveAll(l => l.Product.Id == productId);
        Changed?.Invoke(new BevStateChangeInfo("Remove", "product " + productId));
    }

    public void ApplyCoupon(string code)
    {
        CouponCode = code;
        Changed?.Invoke(new BevStateChangeInfo("ApplyCoupon", code));
    }

    public void Clear()
    {
        Lines.Clear();
        CouponCode = null;
        Changed?.Invoke(new BevStateChangeInfo("Clear"));
    }
}

public sealed class CartLine
{
    public required Product Product { get; init; }

    public int Quantity { get; set; }
}
