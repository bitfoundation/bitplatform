using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards;

/// <summary>The products the model picked, as numbered cards (See AppChatbot.ShowProducts).</summary>
public partial class ProductsCard
{
    [AutoInject] private ILogger<ProductsCard> logger = default!;

    private string? title;
    private string? parsedJson;
    private List<ProductRecommendationDto> products = [];

    protected override async Task OnParamsSetAsync()
    {
        title = Message.Data.GetValueOrDefault("Title");

        // The panel re-renders on every streamed chunk; the json only changes with the card.
        var json = Message.Data.GetValueOrDefault("Products");

        if (ReferenceEquals(json, parsedJson) is false)
        {
            parsedJson = json;

            try
            {
                products = string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize(json, JsonSerializerOptions.GetTypeInfo<List<ProductRecommendationDto>>()) ?? [];
            }
            catch (JsonException exp)
            {
                // Written by another version of the app: the card shows its RawMarkdown instead.
                logger.LogWarning(exp, "Couldn't read the products of an AI chat card.");
                products = [];
            }
        }

        await base.OnParamsSetAsync();
    }

    private Task Ask(ProductDto product) => Host.SendPrompt(Localizer[nameof(AppStrings.AiChatProductsTellMeMore), product.Name!]);
}
