//+:cnd:noEmit
using System.Text.RegularExpressions;
using Boilerplate.Shared.Features.Chatbot;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Identity.Services;
//#endif

namespace Boilerplate.Server.Api.Features.Chatbot;

/// <summary>The current tenant's system prompt, shared by the AI agents and voice calls.</summary>
public static partial class SystemPromptProvider
{
    /// <summary>
    /// A client supplied prompt variable (e.g. "Samsung Android 14", "Asia/Tehran"), kept to one short line of plain
    /// characters so it can't close its quotes or add instructions of its own.
    /// </summary>
    public static string? SanitizeVariable(string? value, int maxLength = 64)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var sanitized = UnsafeVariableCharacters().Replace(value, " ").Trim();

        return sanitized.Length is 0 ? null : sanitized[..Math.Min(sanitized.Length, maxLength)].TrimEnd();
    }

    /// <summary>For a value that can't be sanitized away (e.g. an email with a quoted local part, or a url): escapes quotes and line breaks.</summary>
    public static string EscapeVariable(string? value)
        => System.Text.Json.JsonEncodedText.Encode(value ?? "null", System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping).Value;

    /// <summary>The id if this server knows the time zone (ICU resolves both IANA and Windows ids on either OS), otherwise null.</summary>
    public static string? KnownTimeZoneId(string? timeZoneId)
        => SanitizeVariable(timeZoneId) is { } id && id.Contains("..") is false && TimeZoneInfo.TryFindSystemTimeZoneById(id, out _) ? id : null;

    [GeneratedRegex(@"[^\p{L}\p{N}._\-/()+]+")]
    private static partial Regex UnsafeVariableCharacters();

    public static string GetSystemPrompt(PromptKind promptKind, IServiceProvider sp)
    {
        var cache = sp.GetRequiredService<IFusionCache>();
        var dbContext = sp.GetRequiredService<AppDbContext>();
        //#if (multitenant == true)
        var tenantId = sp.GetRequiredService<TenantProvider>().GetCurrentTenantId();
        var cacheKey = $"SystemPrompt_{tenantId}_{promptKind}";
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        var cacheKey = $"SystemPrompt_{promptKind}";
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        var result = cache.GetOrSet(
            cacheKey, _ =>
            {
                var prompt = dbContext.SystemPrompts.FirstOrDefault(p => p.PromptKind == promptKind);
                return prompt?.Markdown ?? throw new ResourceNotFoundException().WithData("Reason", $"System prompt for '{promptKind}' not found.");
            },
            options => options.SetDuration(TimeSpan.FromHours(1)).SetPriority(CacheItemPriority.High));
        return result;
    }
}
