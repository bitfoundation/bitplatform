namespace Boilerplate.Shared.Features.Products;

/// <summary>A product the chatbot showed as a card (See <c>AppChatbot.ShowProducts</c>), with why the model picked it.</summary>
public class ProductRecommendationDto
{
    /// <summary>Only what the card renders; the description stays on the product page.</summary>
    public ProductDto Product { get; set; } = default!;

    /// <summary>Short phrases, in the user's language, on how it meets what the user asked for.</summary>
    public List<string> Highlights { get; set; } = [];
}
