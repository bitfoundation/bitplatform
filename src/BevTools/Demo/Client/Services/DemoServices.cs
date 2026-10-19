using System.Net.Http.Json;
using Bit.BevTools;
using Bit.BevTools.Demo.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Demo.Client.Services;

/// <summary>Registrations shared by the server and the WebAssembly client so both hosts behave the same under Interactive Auto.</summary>
public static class DemoServices
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        services.AddHttpClient("api");
        services.AddScoped<ProductApi>();
        services.AddScoped<FilterState>();
        services.AddScoped<UserSession>();

        // State containers that integrate with BevTools through IBevStateProvider.
        services.AddBevToolsStateProvider<CartState>();
        services.AddBevToolsStateProvider<AppState>();

        // Intentional DI problem: a singleton that captures a scoped service. Never resolved by the demo (the
        // container would reject it when ValidateScopes is on) but visible to the BevTools Services panel.
        services.AddSingleton<LeakyCache>();

        // Intentional DI problem: a circular dependency between two services (also never resolved).
        services.AddScoped<ReportService>();
        services.AddScoped<ExportService>();

        return services;
    }
}

public sealed class UserSession
{
    public string UserName { get; set; } = "ada.lovelace";

    public string DisplayName { get; set; } = "Ada Lovelace";

    // Sensitive by naming convention: BevTools redacts it everywhere it appears.
    public string AccessToken { get; set; } = "eyJhbGciOiJIUzI1NiJ9.demo-token-do-not-show";

    [BevToolsSensitive]
    public string Nickname { get; set; } = "hidden by attribute";
}

public sealed class LeakyCache(UserSession session)
{
    public string Owner => session.UserName;
}

public sealed class ReportService(ExportService export)
{
    public ExportService Export => export;
}

public sealed class ExportService(ReportService report)
{
    public ReportService Report => report;
}

/// <summary>Filter state shared by the products page. Notifies on every change without coalescing (intentional).</summary>
public sealed class FilterState
{
    private string _query = "";
    private string _category = "All";
    private decimal _maxPrice = 1000;

    public event Action? Changed;

    public string Query
    {
        get => _query;
        set
        {
            _query = value;
            Changed?.Invoke();
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            _category = value;
            Changed?.Invoke();
        }
    }

    public decimal MaxPrice
    {
        get => _maxPrice;
        set
        {
            _maxPrice = value;
            Changed?.Invoke();
        }
    }

    public int ChangeCount { get; private set; }

    public void Touch()
    {
        ChangeCount++;
        Changed?.Invoke();
    }
}

/// <summary>Shopping cart exposed to BevTools as a state provider with named actions.</summary>
public sealed class CartState : IBevStateProvider
{
    public List<CartLine> Lines { get; } = [];

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public int ItemCount => Lines.Sum(l => l.Quantity);

    public string? CouponCode { get; private set; }

    public string Name => "CartState";

    public string? Description => "scoped service, one per circuit / app";

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

/// <summary>Application-wide settings exposed to BevTools; includes a sensitive member that must be redacted.</summary>
public sealed class AppState : IBevStateProvider
{
    private string _theme = "light";
    private bool _notifications = true;

    public string Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            Changed?.Invoke(new BevStateChangeInfo("SetTheme", value));
        }
    }

    public bool NotificationsEnabled
    {
        get => _notifications;
        set
        {
            _notifications = value;
            Changed?.Invoke(new BevStateChangeInfo("ToggleNotifications"));
        }
    }

    public List<string> VisitedPages { get; } = [];

    public Dictionary<string, int> Counters { get; } = new() { ["clicks"] = 0 };

    public string ApiKey { get; } = "sk-live-0000-should-never-be-visible";

    public string Name => "AppState";

    public string? Description => "scoped application state";

    public event Action<BevStateChangeInfo>? Changed;

    public object? GetSnapshot() => this;

    public void Visit(string page)
    {
        VisitedPages.Add(page);
        if (VisitedPages.Count > 20)
        {
            VisitedPages.RemoveAt(0);
        }

        Changed?.Invoke(new BevStateChangeInfo("Visit", page));
    }

    public void Click()
    {
        Counters["clicks"]++;
        Changed?.Invoke(new BevStateChangeInfo("Click"));
    }
}

/// <summary>HTTP access through IHttpClientFactory so BevTools tracks every request automatically.</summary>
public sealed class ProductApi(IHttpClientFactory factory, NavigationManager navigation)
{
    private HttpClient Client
    {
        get
        {
            var client = factory.CreateClient("api");
            client.BaseAddress = new Uri(navigation.BaseUri);
            return client;
        }
    }

    public Task<List<Product>> GetProductsAsync(CancellationToken ct = default) =>
        Client.GetFromJsonAsync<List<Product>>("api/products", ct)!;

    public Task<List<Product>> SearchAsync(string query, CancellationToken ct = default) =>
        Client.GetFromJsonAsync<List<Product>>("api/search?q=" + Uri.EscapeDataString(query), ct)!;

    public Task<List<Order>> GetOrdersAsync(int delayMs = 0, CancellationToken ct = default) =>
        Client.GetFromJsonAsync<List<Order>>("api/orders?delay=" + delayMs, ct)!;

    public async Task<string> GetFlakyAsync(CancellationToken ct = default)
    {
        var response = await Client.GetAsync("api/flaky", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public Task<Profile> GetProfileAsync(CancellationToken ct = default)
    {
        var client = Client;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "demo-token-must-be-redacted");
        return client.GetFromJsonAsync<Profile>("api/profile", ct)!;
    }

    public Task<HttpResponseMessage> GetMissingAsync(CancellationToken ct = default) => Client.GetAsync("api/does-not-exist", ct);
}
