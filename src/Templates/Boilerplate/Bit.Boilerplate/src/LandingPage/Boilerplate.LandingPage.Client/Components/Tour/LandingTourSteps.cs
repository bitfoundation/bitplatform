using Bit.Bonboarding;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.LandingPage.Client.Components.Tour;

public static class LandingTourSteps
{
    public const string Id = "landing-tour";

    public static BonboardingTourDefinition Create(IStringLocalizer<TourStrings> t)
    {
        var tour = new BonboardingTourDefinition
        {
            Id = Id,
            Persist = false,
            Progress = BonboardingProgressStyle.Text,
            WaitTimeout = TimeSpan.FromSeconds(3),
            WaitTimeoutBehavior = BonboardingMissingTargetBehavior.Center,
            Labels = new()
            {
                Next = t[nameof(TourStrings.Next)],
                Previous = t[nameof(TourStrings.Back)],
                Skip = t[nameof(TourStrings.Skip)],
                Done = t[nameof(TourStrings.Done)],
                Close = t[nameof(TourStrings.Close)],
                DialogLabel = t[nameof(TourStrings.DialogLabel)],
                ProgressFormat = t[nameof(TourStrings.ProgressFormat)],
                StepAnnouncementFormat = t[nameof(TourStrings.StepAnnouncementFormat)],
                WaitingAnnouncement = t[nameof(TourStrings.WaitingAnnouncement)],
                ProgressLabel = t[nameof(TourStrings.ProgressLabel)]
            }
        };

        var count = LandingSites.All.Length;

        Add(tour, "intro", LandingRoutes.Home, null, t[nameof(TourStrings.IntroTitle)], t[nameof(TourStrings.IntroBody), count]);
        Add(tour, "modes", LandingRoutes.Home, ".gallery-filters", t[nameof(TourStrings.ModesTitle)], t[nameof(TourStrings.ModesBody)]);

        // Each sample's steps are added only while the sample exists, so deleting one leaves the tour working
        if (LandingSites.Find("ecommerce") is { } shop)
        {
            Add(tour, "shop-offer", shop.Path, ".lp-announcement", t[nameof(TourStrings.ShopOfferTitle), shop.Name], t[nameof(TourStrings.ShopOfferBody)]);
            Try(Add(tour, "shop-demo", shop.Path, ".shop-demo", t[nameof(TourStrings.DemoTitle)], t[nameof(TourStrings.DemoBody)]));
            Add(tour, "shop-stores", shop.Path, ".ecommerce-hero .lp-badges", t[nameof(TourStrings.StoresTitle)], t[nameof(TourStrings.StoresBody)]);
            Add(tour, "shop-rating", shop.Path, ".lp-rating", t[nameof(TourStrings.RatingTitle)], t[nameof(TourStrings.RatingBody)]);
            Add(tour, "shop-faq", shop.Path, ".lp-faq", t[nameof(TourStrings.FaqTitle)], t[nameof(TourStrings.FaqBody)]);
            Open(Add(tour, "shop-chat", shop.Path, ".lp-chat-card", t[nameof(TourStrings.ChatTitle)], t[nameof(TourStrings.ChatBody), shop.Name]), ".lp-chat-bubble");
        }

        if (LandingSites.Find("restaurant") is { } cafe)
        {
            Add(tour, "cafe-waitlist", cafe.Path, ".lp-waitlist", t[nameof(TourStrings.WaitlistTitle)], t[nameof(TourStrings.WaitlistBody)]);
        }

        if (LandingSites.Find("retail") is { } grocer)
        {
            Add(tour, "grocer-shops", $"{grocer.Path}/stores", ".retail-store", t[nameof(TourStrings.PagesTitle)], t[nameof(TourStrings.PagesBody), grocer.Name]);
            Add(tour, "grocer-concierge", $"{grocer.Path}/stores", ".lp-chat-concierge-tab", t[nameof(TourStrings.ConciergeTitle)], t[nameof(TourStrings.ConciergeBody)]);
        }

        if (LandingSites.Find("fitness") is { } workout)
        {
            Add(tour, "fit-languages", workout.Path, ".lp-nav .lp-lang", t[nameof(TourStrings.LanguagesTitle)], t[nameof(TourStrings.LanguagesBody)]);
            Add(tour, "fit-theme", workout.Path, ".lp-nav .lp-theme-toggle", t[nameof(TourStrings.ThemeTitle)], t[nameof(TourStrings.ThemeBody)]);
            Add(tour, "fit-search", workout.Path, null, t[nameof(TourStrings.SearchTitle)], t[nameof(TourStrings.SearchBody)]);
        }

        if (LandingSites.Find("accounting") is { } books)
        {
            Try(Add(tour, "acc-invoice", books.Path, ".acc-desk", t[nameof(TourStrings.InvoiceTitle), books.Name], t[nameof(TourStrings.InvoiceBody)]));
            Try(Add(tour, "acc-vat", $"{books.Path}/pricing", ".acc-plans", t[nameof(TourStrings.VatTitle)], t[nameof(TourStrings.VatBody)]));
        }

        if (LandingSites.Find("hotel-software") is { } lodge)
        {
            Press(Add(tour, "lodge-desk", lodge.Path, ".hs-desk", t[nameof(TourStrings.DeskTitle), lodge.Name], t[nameof(TourStrings.DeskBody)]), ".hs-desk-list:first-child .hs-stay:not(.is-done) .hs-stay-action", 2);
            Add(tour, "lodge-press", $"{lodge.Path}/press", ".hs-press-assets", t[nameof(TourStrings.PressTitle)], t[nameof(TourStrings.PressBody)]);
        }

        if (LandingSites.Find("minimal") is { } notch)
        {
            Add(tour, "notch-route", notch.Path, ".minimal-route", t[nameof(TourStrings.RouteTitle)], t[nameof(TourStrings.RouteBody)]);
            Add(tour, "notch-privacy", notch.Path, ".lp-footer-link-button", t[nameof(TourStrings.PrivacyTitle)], t[nameof(TourStrings.PrivacyBody)]);
            Add(tour, "notch-legal", $"{notch.Path}/privacy", ".lp-legal-header", t[nameof(TourStrings.LegalTitle)], t[nameof(TourStrings.LegalBody)]);
            Add(tour, "notch-missing", $"{notch.Path}/no-such-page", ".lp-not-found-art", t[nameof(TourStrings.MissingTitle)], t[nameof(TourStrings.MissingBody)]);
        }

        Add(tour, "offline", LandingRoutes.Home, null, t[nameof(TourStrings.OfflineTitle)], t[nameof(TourStrings.OfflineBody)]);
        Add(tour, "light", LandingRoutes.Home, null, t[nameof(TourStrings.LightTitle)], t[nameof(TourStrings.LightBody)]);
        Add(tour, "yours", LandingRoutes.Home, ".gallery-how", t[nameof(TourStrings.YoursTitle)], t[nameof(TourStrings.YoursBody), count]);

        return tour;
    }

    private static BonboardingStepDefinition Add(BonboardingTourDefinition tour, string id, string path, string? selector, string title, string description)
    {
        var step = new BonboardingStepDefinition
        {
            Id = id,
            // Kept in the visitor's language, as a route in the other one would reload the page and end the tour
            Route = LandingCultures.Localize(path, CultureInfo.CurrentUICulture),
            Target = selector is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css(selector),
            Title = title,
            Description = description
        };

        tour.Steps.Add(step);

        return step;
    }

    // The spotlight lets clicks through, so the visitor can use the working demo under it
    private static void Try(BonboardingStepDefinition step) => step.Interaction = BonboardingInteractionMode.TargetOnly;

    private static void Open(BonboardingStepDefinition step, string toggle)
    {
        step.OnBeforeShow = context => Run(context, "landing.setExpanded", toggle, true);
        step.OnBeforeLeave = context => Run(context, "landing.setExpanded", toggle, false);
    }

    private static void Press(BonboardingStepDefinition step, string buttons, int count)
    {
        Try(step);
        step.OnShown = context =>
        {
            // Not awaited, as Bonboarding holds the step's transition until a hook returns
            _ = Run(context, "landing.press", buttons, count, 1100).AsTask();
            return ValueTask.CompletedTask;
        };
    }

    private static async ValueTask Run(BonboardingStepContext context, string identifier, params object?[] args)
    {
        try
        {
            await context.Services.GetRequiredService<IJSRuntime>().InvokeVoidAsync(identifier, args);
        }
        catch (JSException)
        {
            // Bonboarding ends the whole tour when a step's hook throws, so a failed script call is let go
        }
    }
}
