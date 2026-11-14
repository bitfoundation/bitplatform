using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Layout.AiChat;

/// <summary>The products the model picked, as numbered cards: what a ShowProducts call showed (See AppChatbot.ShowProducts).</summary>
public partial class ProductsCard
{
    [Parameter] public ShownProductsDto? Shown { get; set; }

    /// <summary>Asks the chat about one of the products, as the user's next message.</summary>
    [Parameter] public EventCallback<string> OnAsk { get; set; }

    private List<ProductRecommendationDto> products = [];

    protected override async Task OnParamsSetAsync()
    {
        // A null entry, which another version of the app could have stored, would break rendering
        products = [.. Shown?.Products.Where(p => p?.Product is not null) ?? []];

        await base.OnParamsSetAsync();
    }

    private Task Ask(ProductDto product) => OnAsk.InvokeAsync(Localizer[nameof(AppStrings.AiChatProductsTellMeMore), product.Name!]);
}
