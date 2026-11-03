using Bit.Bonboarding;

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
            Progress = BonboardingProgressStyle.Dots,
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
            Add(tour, "rating", shop.Path, ".lp-rating", t[nameof(TourStrings.RatingTitle), shop.Name], t[nameof(TourStrings.RatingBody)]);
            Add(tour, "chat", shop.Path, ".lp-chat-bubble", t[nameof(TourStrings.ChatTitle)], t[nameof(TourStrings.ChatBody), shop.Name]);
        }

        if (LandingSites.Find("restaurant") is { } cafe)
        {
            Add(tour, "waitlist", cafe.Path, ".lp-waitlist", t[nameof(TourStrings.WaitlistTitle), cafe.Name], t[nameof(TourStrings.WaitlistBody)]);
        }

        if (LandingSites.Find("retail") is { } grocer)
        {
            Add(tour, "pages", $"{grocer.Path}/stores", ".retail-store", t[nameof(TourStrings.PagesTitle)], t[nameof(TourStrings.PagesBody), grocer.Name]);
        }

        if (LandingSites.Find("fitness") is { } workout)
        {
            Add(tour, "launch", workout.Path, ".fit-devices .is-android", t[nameof(TourStrings.LaunchTitle), workout.Name], t[nameof(TourStrings.LaunchBody)]);
            Add(tour, "languages", workout.Path, ".lp-nav .lp-lang", t[nameof(TourStrings.LanguagesTitle)], t[nameof(TourStrings.LanguagesBody)]);
            Add(tour, "search", workout.Path, null, t[nameof(TourStrings.SearchTitle)], t[nameof(TourStrings.SearchBody)]);
        }

        Add(tour, "yours", LandingRoutes.Home, ".gallery-how", t[nameof(TourStrings.YoursTitle)], t[nameof(TourStrings.YoursBody), count]);

        return tour;
    }

    private static void Add(BonboardingTourDefinition tour, string id, string path, string? selector, string title, string description)
    {
        tour.Steps.Add(new()
        {
            Id = id,
            Route = LandingCultures.Localize(path, CultureInfo.CurrentUICulture),
            Target = selector is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css(selector),
            Title = title,
            Description = description
        });
    }
}
