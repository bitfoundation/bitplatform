//+:cnd:noEmit
using System.ComponentModel;
//#if (module == "Sales")
using Boilerplate.Server.Api.Features.Products;
//#endif
using ModelContextProtocol.Server;
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Shared.Features.Diagnostic;
using Boilerplate.Server.Api.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Infrastructure.Services;

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

            var userDateTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);

            return $"Current date/time in user's timezone ({timeZoneId}) is {userDateTime:o}";
        }
        catch
        {
            return $"Current date/time in utc is {DateTimeOffset.UtcNow:o}";
        }
    }

    /// <summary>
    /// Saves the user's email address and the conversation history for future reference.
    /// </summary>
    [Description("Saves the user's email address and the conversation history for future reference.")]
    [McpServerTool(Name = nameof(SaveUserEmailAndConversationHistory))]
    private async Task<string?> SaveUserEmailAndConversationHistory(
        [Required, Description("User's email address")] string emailAddress,
        [Required, Description("Full conversation history")] string conversationHistory)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            // Ideally, store these in a CRM or app database,
            // but for now, we'll log them!
            scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);

            return "User email and conversation history saved successfully.";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to save user email and conversation history.";
        }
    }

    /// <summary>
    /// Navigates the user to a specific page within the application.
    /// </summary>
    [Description("Navigates the user to a specific page within the application. Use this tool when the user requests to go to a particular section or feature of the app.")]
    [McpServerTool(Name = nameof(NavigateToPage))]
    private async Task<string?> NavigateToPage(
        [Required, Description("Page URL to navigate to")] string pageUrl)
    {
        await EnsureSignalRConnectionIdIsPresent();

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.NAVIGATE_TO, pageUrl, CancellationToken.None);

            return "Navigation completed";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Navigation failed";
        }
    }

    [Description(@"Displays the sign-in modal to the user and waits for either successful sign-in or cancellation")]
    [McpServerTool(Name = nameof(ShowSignInModal))]
    public async Task<UserDto?> ShowSignInModal()
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            await EnsureSignalRConnectionIdIsPresent();

            var accessToken = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<string>(SharedAppMessages.SHOW_SIGN_IN_MODAL, CancellationToken.None);

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
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return null;
        }
    }

    /// <summary>
    /// Changes the user's culture/language setting.
    /// </summary>
    [Description("Changes the user's culture/language setting. Use this tool when the user requests to change the app language. Common LCIDs: 1033=en-US, 1065=fa-IR, 1053=sv-SE, 2057=en-GB, 1043=nl-NL, 1081=hi-IN, 2052=zh-CN, 3082=es-ES, 1036=fr-FR, 1025=ar-SA, 1031=de-DE.")]
    [McpServerTool(Name = nameof(SetCulture))]
    private async Task<string?> SetCulture(
        [Required, Description("Culture LCID (e.g., 1033 for en-US, 1065 for fa-IR)")] int cultureLcid)
    {
        await EnsureSignalRConnectionIdIsPresent();

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureLcid);

            if (CultureInfoManager.SupportedCultures.All(c => c.Culture.LCID != cultureLcid))
                return $"The requested culture is not supported. Available cultures: {string.Join(", ", CultureInfoManager.SupportedCultures.Select(c => c.Culture.NativeName))}";

            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.CHANGE_CULTURE, cultureLcid, CancellationToken.None);

            return "Culture/Language changed successfully";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to change culture/language";
        }
    }

    /// <summary>
    /// Changes the user's theme preference between light and dark mode.
    /// </summary>
    [Description("Changes the user's theme preference between light and dark mode. Use this tool when the user requests to change the app theme or appearance.")]
    [McpServerTool(Name = nameof(SetTheme))]
    private async Task<string?> SetTheme(
        [Required, Description("Theme name: 'light' or 'dark'")] string theme)
    {
        await EnsureSignalRConnectionIdIsPresent();

        if (theme != "light" && theme != "dark")
            return "Invalid theme. Use 'light' or 'dark'.";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<bool>(SharedAppMessages.CHANGE_THEME, theme, CancellationToken.None);

            return $"Theme changed to {theme} successfully";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to change theme";
        }
    }

    /// <summary>
    /// Retrieves the last error that occurred on the user's device from the diagnostic logs.
    /// </summary>
    [Description("Retrieves the last error that occurred on the user's device from the diagnostic logs. Use this tool when troubleshooting user-reported issues, investigating application crashes, or when the user mentions something isn't working.")]
    [McpServerTool(Name = nameof(CheckLastError))]
    private async Task<string?> CheckLastError()
    {
        await EnsureSignalRConnectionIdIsPresent();

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var lastError = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<DiagnosticLogDto?>(SharedAppMessages.UPLOAD_LAST_ERROR, CancellationToken.None);

            if (lastError is null)
                return "No errors found in the diagnostic logs.";

            return lastError.ToString();
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to retrieve error information from the device.";
        }
    }

    /// <summary>
    /// Clears application files on the user's device to fix issues.
    /// </summary>
    [Description("Clears application files on the user's device to fix issues.")]
    [McpServerTool(Name = nameof(ClearAppFiles))]
    private async Task<string?> ClearAppFiles()
    {
        await EnsureSignalRConnectionIdIsPresent();

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId!)
                .InvokeAsync<DiagnosticLogDto?>(SharedAppMessages.CLEAR_APP_FILES, CancellationToken.None);

            return "App files cleared successfully on the device.";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to clear app files on the device.";
        }
    }

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    /// <summary>
    /// Searches for and recommends products based on user's needs and preferences.
    /// </summary>
    [Description("This tool searches for and recommends products based on a detailed description of the user's needs and preferences and returns recommended products.")]
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
    //#endif
    //#endif
}
