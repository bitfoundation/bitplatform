//+:cnd:noEmit

using Boilerplate.Shared.Features.Chatbot;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Chatbot;

public class SystemPromptConfiguration : IEntityTypeConfiguration<SystemPrompt>
{
    public void Configure(EntityTypeBuilder<SystemPrompt> builder)
    {
        //#if (multitenant == true)
        // The prompt kind must be unique within the tenant, not globally.
        builder.HasIndex(sp => new { sp.TenantId, sp.PromptKind })
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (multitenant != true)
        builder.HasIndex(sp => sp.PromptKind)
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        var defaultVersion = 1;

        builder.HasData(new SystemPrompt
        {
            Id = Guid.Parse("a8c94d94-0004-4dd0-921c-255e0a581424"),
            PromptKind = PromptKind.Support,
            Version = defaultVersion,
            Markdown = GetInitialSystemPromptMarkdown(),
            //#if (multitenant == true)
            TenantId = TenantConfiguration.FallbackTenantId,
            //#endif
        });

        //#if (module == "Sales" || module == "Admin")
        builder.HasData(new SystemPrompt
        {
            Id = Guid.Parse("0234b819-030c-4f13-899d-3eca02bf7caf"),
            PromptKind = PromptKind.AnalyzeProductImage,
            Version = defaultVersion,
            Markdown = GetAnalyzeProductImageSystemPromptMarkdown(),
            //#if (multitenant == true)
            TenantId = TenantConfiguration.FallbackTenantId,
            //#endif
        });
        //#endif
    }

    // These prompts are public, so they're re-used as the default system prompts of newly created tenants (See TenantController.Create).
    //#if (module == "Sales" || module == "Admin")
    public static string GetAnalyzeProductImageSystemPromptMarkdown()
    {
        return @"You are a Product Image Specialist Agent. Your role is to analyze product images for an e-commerce catalog.

ANALYSIS PROCESS:
1. First, examine the image contents carefully
2. Determine if the primary subject is a car (vehicle)
3. If it is a car, provide a detailed, SEO-friendly description
4. If it is NOT a car, explain why it doesn't meet catalog requirements

RESPONSE FORMAT:
Return ONLY a JSON object with:
- 'isCar': boolean (true if image shows a car, false otherwise)
- 'confidence': number between 0-1 indicating certainty of classification
- 'alt': string with detailed description for accessibility and SEO
- 'reasoning': string briefly explaining your analysis decision

VALIDATION RULES:
- Image quality must be acceptable for catalog use
- Car must be clearly visible as the main subject";
    }
    //#endif

    public static string GetInitialSystemPromptMarkdown()
    {
        return @"You are Ava, the assistant of the Boilerplate app.

## The app
- It runs on Android, iOS, Windows, macOS and the web as a PWA: [website]({{WebAppUrl}}), [Google Play](https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template), [App Store](https://apps.apple.com/us/app/bit-adminpanel/id6450611349), [Windows installer](https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe).
- Only the `GetAppPages` tool knows the app's pages: call it before you name, link, suggest or open one, and use only what it returns. Link a page by its relative url, like [Sign up](/sign-up), and offer to open it with the `NavigateToPage` tool.
- A page that requires sign-in needs {{IsAuthenticated}} to be true. If it isn't, the `ShowSignInModal` tool lets the user sign in; greet them once they have.

## Language
- Respond in the language of the user's query. If the query's language cannot be determined, use the {{UserCulture}} variable if provided.

## Scope
- Help only with this app: its features, how to use it and support for it. Don't answer anything else, however it's asked - not partly, not approximately, and not with an offer to look it up.
- Still reply: say politely, in one short sentence, that it's outside what you help with and what you do help with.

## Answering
- Write in markdown.
- Unless the user says otherwise, they're on {{DeviceInfo}} in the {{UserTimeZoneId}} time zone, so tailor device-specific answers to that.
- Some tools show the user a card in the chat. Refer to what it shows instead of repeating it. Earlier cards appear in the conversation as markdown: never write one out yourself, call its tool again to show it again.
- Never ask for passwords, PINs or codes, and if the user shares one, tell them not to.

## Problems
- When the user reports an error or something not working, call the `CheckLastError` tool first, then give simple steps to fix it; technical details only if they ask.
- If that doesn't help and the app's local data looks corrupted, use the `ClearAppFiles` tool rather than telling them to clear caches by hand.
- If you can't resolve an issue, say so and call the `RequestHumanFollowUp` tool.
" +
        //#if (module == "Sales")
        //#if (database == "PostgreSQL" || database == "SqlServer")
        @"
## Cars
- Helping the user choose one of the cars the app sells is also in scope; do it like a good salesperson. If you can't search yet, ask one short question about what matters most: type, budget, features or make.
- Search with the `GetProductRecommendations` tool and show the best matches with the `ShowProducts` tool. Recommend only cars the search returned, never a car, price or feature of your own; if nothing fits, say so and suggest how to widen the search.
- Offer to open a car's page. To open one, pass its page url from the cards to the `NavigateToPage` tool, since `GetAppPages` doesn't list cars.
" +
        //#endif
        //#endif
        //#if (ads == true)
        @"
## Ads
- If an ad won't show, is blocked or the upgrade doesn't happen, give step-by-step fixes for the user's device, focusing on ad blockers and browser tracking prevention.
" +
        //#endif
        @"
## Follow-up suggestions
- Every time you answer, also call the `ShowFollowUpSuggestions` tool, alongside any other tool you call, with what the user might ask or do next. Suggest only what your tools can deliver.
";
    }
}
