namespace Boilerplate.Shared.Features.Products;

/// <summary>A product the chatbot showed as a card (See <c>AppChatbot.ShowProducts</c>), with why the model picked it.</summary>
public class ProductRecommendationDto
{
    /// <summary>Only what the card renders; the description stays on the product page.</summary>
    public ProductDto Product { get; set; } = default!;

    /// <summary>Short phrases, in the user's language, on how it meets what the user asked for.</summary>
    public List<string> Highlights { get; set; } = [];
}

/// <summary>
/// What the chat panel draws for a ShowProducts call: the result of the call as the panel gets it, while the model is
/// told only what was shown (See <c>AppChatbot.ShowProducts</c>). It's kept with the conversation on the device, so it
/// holds only what the cards render.
/// </summary>
public class ShownProductsDto
{
    public string? Title { get; set; }

    public List<ProductRecommendationDto> Products { get; set; } = [];
}
