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

        if (LandingSites.Find("ecommerce") is { } shop)
        {
            Add(tour, "shop-offer", shop.Path, ".lp-announcement", t[nameof(TourStrings.ShopOfferTitle), shop.Name], t[nameof(TourStrings.ShopOfferBody)]);
            Try(Add(tour, "shop-demo", shop.Path, ".shop-demo", t[nameof(TourStrings.DemoTitle)], t[nameof(TourStrings.DemoBody)]));
            Add(tour, "shop-stores", shop.Path, ".ecommerce-hero .lp-badges", t[nameof(TourStrings.StoresTitle)], t[nameof(TourStrings.StoresBody)]);
            Add(tour, "shop-rating", shop.Path, ".lp-rating", t[nameof(TourStrings.RatingTitle)], t[nameof(TourStrings.RatingBody)]);
            Add(tour, "shop-press", shop.Path, ".ecommerce-press", t[nameof(TourStrings.ProofTitle)], t[nameof(TourStrings.ProofBody)]);
            Add(tour, "shop-faq", shop.Path, ".lp-faq", t[nameof(TourStrings.FaqTitle)], t[nameof(TourStrings.FaqBody)]);
            Open(Add(tour, "shop-chat", shop.Path, ".lp-chat-card", t[nameof(TourStrings.ChatTitle)], t[nameof(TourStrings.ChatBody), shop.Name]), ".lp-chat-bubble");
            Add(tour, "shop-coupon", shop.Path, ".ecommerce-coupon", t[nameof(TourStrings.CouponTitle)], t[nameof(TourStrings.CouponBody)]);
            Add(tour, "shop-about", $"{shop.Path}/about", ".ecommerce-team", t[nameof(TourStrings.AboutTitle)], t[nameof(TourStrings.AboutBody)]);
        }

        if (LandingSites.Find("restaurant") is { } cafe)
        {
            Add(tour, "cafe-countdown", cafe.Path, ".lp-countdown", t[nameof(TourStrings.CountdownTitle), cafe.Name], t[nameof(TourStrings.CountdownBody)]);
            Add(tour, "cafe-preorder", cafe.Path, ".restaurant-soon", t[nameof(TourStrings.PreOrderTitle)], t[nameof(TourStrings.PreOrderBody)]);
            Try(Add(tour, "cafe-order", cafe.Path, ".order-demo", t[nameof(TourStrings.OrderTitle)], t[nameof(TourStrings.OrderBody)]));
            Add(tour, "cafe-ask", cafe.Path, ".lp-chat-inline", t[nameof(TourStrings.InlineChatTitle)], t[nameof(TourStrings.InlineChatBody)]);
            Add(tour, "cafe-waitlist", cafe.Path, ".lp-waitlist", t[nameof(TourStrings.WaitlistTitle)], t[nameof(TourStrings.WaitlistBody)]);
        }

        if (LandingSites.Find("retail") is { } grocer)
        {
            Try(Add(tour, "grocer-collect", grocer.Path, ".slot-demo", t[nameof(TourStrings.CollectTitle), grocer.Name], t[nameof(TourStrings.CollectBody)]));
            Add(tour, "grocer-card", grocer.Path, ".loyalty", t[nameof(TourStrings.CardTitle)], t[nameof(TourStrings.CardBody)]);
            Add(tour, "grocer-shops", $"{grocer.Path}/stores", ".retail-store", t[nameof(TourStrings.PagesTitle)], t[nameof(TourStrings.PagesBody), grocer.Name]);
            Open(Add(tour, "grocer-concierge", $"{grocer.Path}/stores", ".lp-chat-panel-body", t[nameof(TourStrings.ConciergeTitle)], t[nameof(TourStrings.ConciergeBody)]), ".lp-chat-concierge-tab", ".bit-pnl:has(.lp-chat-panel-body) .bit-pnl-cls");
        }

        if (LandingSites.Find("fitness") is { } workout)
        {
            Try(Add(tour, "fit-session", workout.Path, ".fit-stage", t[nameof(TourStrings.SessionTitle), workout.Name], t[nameof(TourStrings.SessionBody)]));
            Add(tour, "fit-launch", workout.Path, ".fit-devices .is-android", t[nameof(TourStrings.LaunchTitle)], t[nameof(TourStrings.LaunchBody)]);
            Add(tour, "fit-news", workout.Path, ".fit-news", t[nameof(TourStrings.NewsTitle)], t[nameof(TourStrings.NewsBody)]);
            Add(tour, "fit-languages", workout.Path, ".lp-nav .lp-lang", t[nameof(TourStrings.LanguagesTitle)], t[nameof(TourStrings.LanguagesBody)]);
            Add(tour, "fit-theme", workout.Path, ".lp-nav .lp-theme-toggle", t[nameof(TourStrings.ThemeTitle)], t[nameof(TourStrings.ThemeBody)]);
            Add(tour, "fit-search", workout.Path, null, t[nameof(TourStrings.SearchTitle)], t[nameof(TourStrings.SearchBody)]);
        }

        if (LandingSites.Find("crm") is { } crm)
        {
            Try(Add(tour, "crm-board", crm.Path, ".crm-board", t[nameof(TourStrings.BoardTitle), crm.Name], t[nameof(TourStrings.BoardBody)]));
            Add(tour, "crm-stats", crm.Path, ".crm-stats", t[nameof(TourStrings.SourcesTitle)], t[nameof(TourStrings.SourcesBody)]);
            Try(Add(tour, "crm-currency", $"{crm.Path}/pricing", ".crm-plans", t[nameof(TourStrings.CurrencyTitle)], t[nameof(TourStrings.CurrencyBody)]));
            Add(tour, "crm-sheets", $"{crm.Path}/vs-spreadsheets", ".crm-sheets-table", t[nameof(TourStrings.SheetsTitle)], t[nameof(TourStrings.SheetsBody)]);
        }

        if (LandingSites.Find("accounting") is { } books)
        {
            Try(Add(tour, "acc-invoice", books.Path, ".acc-desk", t[nameof(TourStrings.InvoiceTitle), books.Name], t[nameof(TourStrings.InvoiceBody)]));
            Try(Add(tour, "acc-savings", books.Path, ".acc-savings", t[nameof(TourStrings.SavingsTitle)], t[nameof(TourStrings.SavingsBody)]));
            Add(tour, "acc-desktop", books.Path, ".acc-hero .lp-badges", t[nameof(TourStrings.DesktopTitle)], t[nameof(TourStrings.DesktopBody)]);
            Try(Add(tour, "acc-vat", $"{books.Path}/pricing", ".acc-plans", t[nameof(TourStrings.VatTitle)], t[nameof(TourStrings.VatBody)]));
        }

        if (LandingSites.Find("hotel") is { } hotel)
        {
            Try(Add(tour, "hotel-stay", hotel.Path, ".stay-demo", t[nameof(TourStrings.StayTitle), hotel.Name], t[nameof(TourStrings.StayBody)]));
            Add(tour, "hotel-direct", hotel.Path, ".hotel-direct", t[nameof(TourStrings.DirectTitle)], t[nameof(TourStrings.DirectBody)]);
            Try(Add(tour, "hotel-rooms", $"{hotel.Path}/rooms", ".hotel-gallery", t[nameof(TourStrings.RoomsTitle)], t[nameof(TourStrings.RoomsBody)]));
        }

        if (LandingSites.Find("hotel-software") is { } lodge)
        {
            Press(Add(tour, "lodge-desk", lodge.Path, ".hs-desk", t[nameof(TourStrings.DeskTitle), lodge.Name], t[nameof(TourStrings.DeskBody)]), ".hs-desk-list:first-child .hs-stay:not(.is-done) .hs-stay-action", 2);
            Try(Add(tour, "lodge-calc", $"{lodge.Path}/pricing", ".hs-calc", t[nameof(TourStrings.CalcTitle)], t[nameof(TourStrings.CalcBody)]));
            Add(tour, "lodge-press", $"{lodge.Path}/press", ".hs-press-assets", t[nameof(TourStrings.PressTitle)], t[nameof(TourStrings.PressBody)]);
        }

        if (LandingSites.Find("minimal") is { } notch)
        {
            Try(Add(tour, "notch-page", notch.Path, ".notch-app", t[nameof(TourStrings.NotchTitle), notch.Name], t[nameof(TourStrings.NotchBody)]));
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
            Route = LandingCultures.Localize(path, CultureInfo.CurrentUICulture),
            Target = selector is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css(selector),
            Title = title,
            Description = description
        };

        tour.Steps.Add(step);

        return step;
    }

    private static void Try(BonboardingStepDefinition step) => step.Interaction = BonboardingInteractionMode.TargetOnly;

    private static void Open(BonboardingStepDefinition step, string toggle, string? close = null)
    {
        step.OnBeforeShow = context => Scripts(context).InvokeVoidAsync("landing.setExpanded", toggle, true);
        step.OnBeforeLeave = context => close is null
            ? Scripts(context).InvokeVoidAsync("landing.setExpanded", toggle, false)
            : Scripts(context).InvokeVoidAsync("landing.press", close, 1, 0);
    }

    private static void Press(BonboardingStepDefinition step, string buttons, int count)
    {
        Try(step);
        step.OnShown = context =>
        {
            _ = Scripts(context).InvokeVoidAsync("landing.press", buttons, count, 1100).AsTask();
            return ValueTask.CompletedTask;
        };
    }

    private static IJSRuntime Scripts(BonboardingStepContext context) => context.Services.GetRequiredService<IJSRuntime>();
}
