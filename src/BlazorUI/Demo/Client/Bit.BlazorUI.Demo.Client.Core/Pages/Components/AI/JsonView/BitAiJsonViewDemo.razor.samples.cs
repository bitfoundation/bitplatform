namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.JsonView;

public partial class BitAiJsonViewDemo
{
    private const string OrderSampleCode = """"
private readonly JsonElement order = JsonDocument.Parse("""
    {
      "id": "ord_4821",
      "status": "shipped",
      "total": 129.5,
      "paid": true,
      "coupon": null,
      "customer": { "name": "Ada Lovelace", "email": "ada@example.com", "address": { "city": "London", "zip": "N1 9GU" } },
      "items": [
        { "sku": "KB-01", "name": "Keyboard", "quantity": 1, "price": 89.5 },
        { "sku": "MS-02", "name": "Mouse", "quantity": 2, "price": 20 }
      ]
    }
    """).RootElement;
"""";

    private readonly string example1RazorCode = """
<BitAiJsonView Value="order" />
""";
    private readonly string example1CsharpCode = OrderSampleCode;

    private readonly string example2RazorCode = """
<BitSlider Label="DefaultExpandDepth" Min="0" Max="4" @bind-Value="expandDepth" />

<BitAiJsonView Value="order" DefaultExpandDepth="(int)expandDepth" />
""";
    private readonly string example2CsharpCode = OrderSampleCode + """

private double expandDepth = 1;
""";

    private readonly string example3RazorCode = """
<BitAiJsonView Value="readings" />

<BitAiJsonView Value="readings" LargeContainerSize="50" />
""";
    private readonly string example3CsharpCode = """
private readonly JsonElement readings =
    JsonDocument.Parse($"[{string.Join(", ", Enumerable.Range(1, 30).Select(i => 18 + i % 7))}]").RootElement;
""";

    private readonly string example4RazorCode = """
<BitButton OnClick="StreamResultAsync" Disabled="isStreaming">Stream a tool result</BitButton>

<BitAiJsonView Value="streamedValue" />
""";
    private readonly string example4CsharpCode = """"
private const string ToolResult = """
    {
      "query": "render modes",
      "took_ms": 42,
      "hits": [
        { "title": "Render modes", "score": 0.94, "url": "https://example.com/docs/render-modes" },
        { "title": "Circuit lifetime", "score": 0.81, "url": "https://example.com/docs/circuits" },
        { "title": "Prerendering", "score": 0.66, "url": "https://example.com/docs/prerendering" }
      ],
      "truncated": false
    }
    """;

private bool isStreaming;
private JsonElement? streamedValue;

private async Task StreamResultAsync()
{
    isStreaming = true;

    // Nine characters at a time, the way a model writes it, repaired into a valid document on every step.
    for (var length = 9; length < ToolResult.Length + 9; length += 9)
    {
        streamedValue = BitAiPartialJson.TryParse(ToolResult[..Math.Min(length, ToolResult.Length)]);
        StateHasChanged();

        await Task.Delay(60);
    }

    isStreaming = false;
}
"""";

    private readonly string example5RazorCode = """
<BitAiJsonView Value="order" Disabled />
""";
    private readonly string example5CsharpCode = OrderSampleCode;

    private readonly string example6RazorCode = """
<style>
    .custom-json {
        padding: 0.75rem;
        border-radius: 0.5rem;
        border: 1px dashed #8a2be2;
    }

    .custom-json-key {
        font-weight: bold;
        text-decoration: underline;
    }
</style>

<BitAiJsonView Value="order"
               Style="padding: 0.75rem; border-radius: 0.5rem; background-color: #1e1e2e; color: #cdd6f4"
               Styles="@(new() { Key = "color: #89b4fa", String = "color: #a6e3a1", Number = "color: #fab387", Boolean = "color: #cba6f7", Null = "color: #9399b2", Toggle = "color: #cdd6f4" })" />

<BitAiJsonView Value="order" Classes="@(new() { Root = "custom-json", Key = "custom-json-key" })" />

<div style="--bit-AiJsonView-key-color: light-dark(#7b2cbf, #c77dff); --bit-AiJsonView-string-color: light-dark(#a0522d, #f4a460);--bit-AiJsonView-indent: 2rem; --bit-AiJsonView-font-size: 0.75rem">
    <BitAiJsonView Value="order" />
</div>
""";
    private readonly string example6CsharpCode = OrderSampleCode;

    private readonly string example7RazorCode = """
<BitAiJsonView Dir="BitDir.Rtl" Value="persianOrder" />

<div dir="rtl">
    <BitAiJsonView Value="persianOrder" DefaultExpandDepth="1" />
</div>
""";
    private readonly string example7CsharpCode = """"
private readonly JsonElement persianOrder = JsonDocument.Parse("""
    {
      "شناسه": "ord_4821",
      "وضعیت": "ارسال شده",
      "جمع": 129.5,
      "پرداخت شده": true,
      "مشتری": { "نام": "آدا لاولیس", "شهر": "لندن" },
      "اقلام": [ { "نام": "صفحه کلید", "تعداد": 1 }, { "نام": "ماوس", "تعداد": 2 } ]
    }
    """).RootElement;
"""";
}
