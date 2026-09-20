//+:cnd:noEmit
using System.ComponentModel;
//#if (module == "Sales")
using Boilerplate.Server.Api.Features.Products;
//#if (database == "PostgreSQL" || database == "SqlServer")
using Boilerplate.Shared.Features.Products;
//#endif
//#endif
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Shared.Features.Diagnostic;
using Boilerplate.Server.Api.Features.Identity;
using Microsoft.Agents.AI;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

[McpServerToolType]
public partial class AppChatbot
{
    /// <summary>
    /// Returns the current date and time based on the user's timezone.
    /// </summary>
    [Description("Returns the current date and time based on the user's timezone.")]
    [McpServerTool(Name = nameof(GetCurrentDateTime))]
    private string GetCurrentDateTime([Required, Description("User's timezone id")] string timeZoneId)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            var userDateTime = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);

            return $"Current date/time in user's timezone ({timeZoneId}) is {userDateTime:o}";
        }
        catch
        {
            return $"Current date/time in utc is {timeProvider.GetUtcNow():o}";
        }
    }

    /// <summary>
    /// Shows the user a card in the chat for leaving their contact details, so a human operator can follow up.
    /// No [McpServerTool]: the card needs the app's chat panel on the other end of the SignalR connection.
    /// </summary>
    [Description("Shows the user a form inside the chat for leaving their contact details, so a human operator can follow up on an issue you could not resolve. Use it instead of asking for an email address or phone number yourself.")]
    private async Task<string?> RequestHumanFollowUp(
        [Required, Description("A summary of the conversation so far in at most three sentences, written in the user's language")] string conversationSummary,
        CancellationToken cancellationToken = default)
    {
        var shown = await ShowCard(new()
        {
            ComponentType = AiChatCardComponents.HumanFollowUp,
            Data = { ["ConversationSummary"] = conversationSummary },
            RawMarkdown = $"Showed a form for leaving contact details, so a human operator can follow up on: {conversationSummary}"
        }, cancellationToken);

        return shown
            ? "The contact form was shown to the user in the chat. They fill in their contact details there, so do not ask for them yourself."
            : "Failed to show the contact form.";
    }

    /// <summary>Sent rather than invoked: nothing waits for the panel. No [McpServerTool]: the buttons need the chat panel.</summary>
    [Description("Shows the user exactly 3 things they might want to ask or do next, as buttons to tap under your answer. Each is under 60 characters, worded as the user would say it and in the language you answer in.")]
    private async Task<string> ShowFollowUpSuggestions(
        [Required, Description("The suggestions, most useful first")] string[] suggestions,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .SendAsync(SharedAppMessages.SHOW_AI_CHAT_SUGGESTIONS, suggestions, cancellationToken);

            return "The suggestions are shown to the user.";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return "Failed to show the suggestions.";
        }
    }

    /// <summary>
    /// Navigates the user to a specific page within the application.
    /// </summary>
    [Description("Navigates the user to a specific page within the application. Use this tool only when the user explicitly requests to go to a particular section or feature of the app.")]
    private async Task<string?> NavigateToPage(
        [Required, Description("Page URL to navigate to")] string pageUrl,
        CancellationToken cancellationToken = default)
    {
        if (Uri.IsAppRelativeUrl(pageUrl) is false)
            return "Invalid page url. Only app relative urls such as /dashboard are allowed.";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.NAVIGATE_TO, pageUrl, cancellationToken);

            return "Navigation completed";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return "Navigation failed";
        }
    }

    /// <summary>
    /// Returns the list of available application pages with their relative URLs and descriptions.
    /// </summary>
    [Description("Returns the list of available application pages, each with its relative URL and a short description. Call this tool whenever the user asks to find, open or navigate to a specific page/section of the app, then use the returned relative URL (e.g. /dashboard) with the NavigateToPage tool.")]
    [McpServerTool(Name = nameof(GetAppPages))]
    private object GetAppPages()
    {
        return PageUrls.GetPages();
    }

    [Description(@"Displays the sign-in modal to the user and waits for either successful sign-in or cancellation")]
    public async Task<UserDto?> ShowSignInModal(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var accessToken = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<string>(SharedAppMessages.SHOW_SIGN_IN_MODAL, cancellationToken);

            var bearerTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).BearerTokenProtector;
            var accessTokenTicket = bearerTokenProtector.Unprotect(accessToken);
            var user = accessTokenTicket!.Principal;

            return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users
                .Project()
                .FirstOrDefaultAsync(u => u.Id == user.GetUserId());
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return null;
        }
    }

    /// <summary>
    /// Changes the user's culture/language setting.
    /// </summary>
    [Description("Changes the user's culture/language setting. Use this tool only when the user explicitly requests to change the app language. Common LCIDs: 1033=en-US, 1065=fa-IR, 1053=sv-SE, 2057=en-GB, 1043=nl-NL, 1081=hi-IN, 2052=zh-CN, 3082=es-ES, 1036=fr-FR, 1025=ar-SA, 1031=de-DE.")]
    private async Task<string?> SetApplicationCulture(
        [Required, Description("Culture LCID (e.g., 1033 for en-US, 1065 for fa-IR)")] int cultureLcid,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureLcid);

            if (CultureInfoManager.SupportedCultures.All(c => c.Culture.LCID != cultureLcid))
                return $"The requested culture is not supported. Available cultures: {string.Join(", ", CultureInfoManager.SupportedCultures.Select(c => c.Culture.NativeName))}";

            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.CHANGE_CULTURE, cultureLcid, cancellationToken);

            return "Culture/Language changed successfully";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return "Failed to change culture/language";
        }
    }

    /// <summary>
    /// Changes the user's theme preference between light and dark mode.
    /// </summary>
    [Description("Changes the user's theme preference between light and dark mode. Use this tool only when the user explicitly requests to change the app theme or appearance.")]
    private async Task<string?> SetApplicationTheme(
        [Required, Description("Theme name: 'light' or 'dark'")] string theme,
        CancellationToken cancellationToken = default)
    {
        if (theme != "light" && theme != "dark")
            return "Invalid theme. Use 'light' or 'dark'.";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var themeChanged = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.CHANGE_THEME, theme, cancellationToken);

            return themeChanged ? $"Theme changed to {theme} successfully" : $"Theme is already set to {theme}";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return "Failed to change theme";
        }
    }

    /// <summary>
    /// Retrieves the last error that occurred on the user's device from the diagnostic logs.
    /// </summary>
    [Description("Retrieves the last error that occurred on the user's device from the diagnostic logs. Use this tool when troubleshooting user-reported issues, investigating application crashes, or when the user mentions something isn't working.")]
    private async Task<string?> CheckLastError(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var lastError = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<DiagnosticLogDto?>(SharedAppMessages.UPLOAD_LAST_ERROR, cancellationToken);

            if (lastError is null)
                return "No errors found in the diagnostic logs.";

            return lastError.ToString();
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return "Failed to retrieve error information from the device.";
        }
    }

    /// <summary>Clears application files on the user's device to fix issues, only once the user approves on screen (See <see cref="AwaitCard"/>).</summary>
    [Description("Clears the app's files on the user's device - local data, cache and storage - to fix corrupted local state; it also signs the user out, deletes this conversation and restarts the app. It asks the user to approve on their screen and does nothing without that approval, so don't ask for permission in the conversation yourself.")]
    private async Task<string?> ClearAppFiles(CancellationToken cancellationToken = default)
    {
        var decision = await AwaitCard(new()
        {
            ComponentType = AiChatCardComponents.UserApproval,
            Data = { ["Action"] = nameof(ClearAppFiles) },
            RawMarkdown = "Asked the user to approve clearing the app's files on this device, which signs them out, deletes this conversation and restarts the app."
        }, cancellationToken);

        if (decision is AiChatCardDecision.Declined)
            return "The user declined, so nothing was cleared. Carry on another way, and don't offer it again unless they bring it up.";

        if (decision is not AiChatCardDecision.Approved)
            return "The approval went unanswered, so nothing was cleared. Don't ask again on your own.";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var cleared = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.CLEAR_APP_FILES, cancellationToken);

            return cleared
                ? "The user approved, and the app files are being cleared: the app signs out, deletes this conversation and restarts."
                : "Failed to clear app files on the device.";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return "Failed to clear app files on the device.";
        }
    }

    /// <summary>Shows a card in the conversation and keeps what it showed in the history. False when it could not be shown.</summary>
    private async Task<bool> ShowCard(AiChatCard card, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            SignCard(card);

            var shown = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.SHOW_AI_CHAT_CARD, card, cancellationToken);

            if (shown)
            {
                RememberCard(card, cancellationToken);
            }

            return shown;
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return false;
        }
    }

    /// <summary>Shows a card and waits for the user's decision. Fails closed: an error or a cancellation is NoAnswer.</summary>
    private async Task<string> AwaitCard(AiChatCard card, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            SignCard(card);
            RememberCard(card, cancellationToken);

            var decision = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<string?>(SharedAppMessages.AWAIT_AI_CHAT_CARD, card, cancellationToken);

            return decision is AiChatCardDecision.Approved or AiChatCardDecision.Declined ? decision : AiChatCardDecision.NoAnswer;
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ApiServerExceptionHandler>().Handle(exp);
            return AiChatCardDecision.NoAnswer;
        }
    }

    /// <summary>Signed like an answer, so the panel can resend it as one. A voice call's cards stay unsigned, like its answers.</summary>
    private void SignCard(AiChatCard card)
    {
        if (isVoiceCall is false)
        {
            card.Signature = answerSigner.Sign(card.RawMarkdown!);
        }
    }

    /// <summary>Adds the card to the text chat's history as the panel resends it; not once its turn was cancelled.</summary>
    private void RememberCard(AiChatCard card, CancellationToken cancellationToken)
    {
        if (isVoiceCall || cancellationToken.IsCancellationRequested) return;

        lock (historyLock)
        {
            chatMessages.Add(new(ChatRole.Assistant, card.RawMarkdown));
        }
    }

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    /// <summary>
    /// Searches for and recommends products based on user's needs and preferences.
    /// </summary>
    [Description("This tool searches for and recommends products based on a detailed description of the user's needs and preferences and returns recommended products. Show the ones worth recommending with the ShowProducts tool.")]
    [McpServerTool(Name = nameof(GetProductRecommendations))]
    private async Task<object?> GetProductRecommendations(
        [Required, Description("Concise summary of user requirements")] string userNeeds,
        [Description("Car manufacturer's name (Optional)")] string? manufacturer,
        [Description("Car price below this value (Optional)")] decimal? maxPrice,
        [Description("Car price above this value (Optional)")] decimal? minPrice)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var productEmbeddingService = scope.ServiceProvider.GetRequiredService<ProductEmbeddingService>();
        var searchQuery = string.IsNullOrWhiteSpace(manufacturer)
            ? userNeeds
            : $"**{manufacturer}** {userNeeds}";

        // Get serverApiAddress from current context
        var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
        var context = httpContextAccessor?.HttpContext;
        var request = context?.Request;
        Uri? serverApiAddress = request?.GetBaseUrl();

        var recommendedProducts = await (await productEmbeddingService.SearchProducts(searchQuery, context?.RequestAborted ?? default))
            .WhereIf(maxPrice.HasValue, p => p.Price <= maxPrice!.Value)
            .WhereIf(minPrice.HasValue, p => p.Price >= minPrice!.Value)
            .Take(10)
            .Project()
            .Select(p => new
            {
                ProductId = p.ShortId,
                p.Name,
                p.PageUrl,
                Manufacturer = p.CategoryName,
                Price = p.FormattedPrice,
                Description = p.DescriptionText,
                PreviewImageUrl = p.GetPrimaryMediumImageUrl(serverApiAddress!) ?? "_content/Boilerplate.Client.Core/images/car_placeholder.png"
            })
            .ToArrayAsync(context?.RequestAborted ?? default);

        return recommendedProducts;
    }

    public sealed record ProductToShow(
        [property: Description("The ProductId the GetProductRecommendations tool returned")] int ProductId,
        [property: Description("Up to 3 phrases under 40 characters, in the user's language, on how it meets what the user asked for - taken from the product's details only")] string[]? Highlights);

    /// <summary>
    /// Shows products as cards. The model only picks and orders them: names, prices and pictures come from the database.
    /// No [McpServerTool]: the cards need the app's chat panel.
    /// </summary>
    [Description("Shows products to the user as numbered cards in the chat - picture, name, manufacturer, price and a link to the product page - in the order given. Pass only products the GetProductRecommendations tool returned. The cards replace a written list: afterwards don't repeat their names, prices, pictures or links; say in a sentence or two how they compare and what doesn't match the user's needs.")]
    private async Task<string> ShowProducts(
        [Required, Description("A short heading for the cards in the user's language, such as what the user is looking for")] string title,
        [Required, Description("The products to show, best match first, at most 6")] ProductToShow[] products,
        CancellationToken cancellationToken = default)
    {
        int[] ids = [.. products.Select(product => product.ProductId).Distinct().Take(6)];

        await using var scope = serviceProvider.CreateAsyncScope();

        // Only what the card renders: it is stored with the conversation on the device.
        var found = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Products
            .Where(product => ids.Contains(product.ShortId))
            .Select(product => new ProductDto
            {
                Id = product.Id,
                ShortId = product.ShortId,
                Name = product.Name,
                Price = product.Price,
                CurrencyIso = product.CurrencyIso,
                CurrencySymbol = product.CurrencySymbol,
                CategoryName = product.Category!.Name,
                HasPrimaryImage = product.HasPrimaryImage,
                PrimaryImageAltText = product.PrimaryImageAltText,
                Version = product.Version
            })
            .ToDictionaryAsync(product => product.ShortId, cancellationToken);

        List<ProductRecommendationDto> recommendations = [.. ids.Where(found.ContainsKey).Select(id => new ProductRecommendationDto
        {
            Product = found[id],
            Highlights = [.. products.First(product => product.ProductId == id).Highlights?.Where(highlight => string.IsNullOrWhiteSpace(highlight) is false).Take(3) ?? []]
        })];

        if (recommendations.Count is 0)
            return "None of these products exist, so nothing was shown.";

        var list = recommendations.Select((recommendation, index) =>
            $"{index + 1}. [{recommendation.Product.Name}]({recommendation.Product.PageUrl}) - {recommendation.Product.CategoryName} - {recommendation.Product.Price.ToString("N0", CultureInfo.InvariantCulture)} {recommendation.Product.CurrencyIso ?? "USD"}");

        var shown = await ShowCard(new()
        {
            ComponentType = AiChatCardComponents.Products,
            Data =
            {
                ["Title"] = title,
                ["Products"] = JsonSerializer.Serialize(recommendations, AppJsonContext.Default.ListProductRecommendationDto)
            },
            RawMarkdown = $"**{title}**\n\n{string.Join('\n', list)}"
        }, cancellationToken);

        return shown
            ? $"These product cards are shown:\n{string.Join('\n', list)}\nDon't repeat what they show. To open one the user picks, such as the second one, call NavigateToPage with its page url."
            : "Failed to show the products.";
    }
    //#endif
    //#endif
}
