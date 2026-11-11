using System.Net;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The Extras section of the Theme page: the components of the Bit.BlazorUI.Extras package in the order the bit BlazorUI
/// demo lists them, each in every state that changes its look, or in one sample that shows it the way an app uses it.
/// BitAppShell is left out: whenever it is the page's only shell it takes the page's scroll keys for its own box.
/// </summary>
public partial class ExtrasSection
{
    /// <summary>The questions the Several selected sample of the accordion list opens together.</summary>
    private static readonly string[] multipleKeys = ["returns", "payment"];

    /// <summary>The orders of the grid's loading and empty samples.</summary>
    private static readonly ExtrasOrder[] noOrders = [];

    /// <summary>The order the grid's Selected sample selects.</summary>
    private static readonly IReadOnlyList<ExtrasOrder> selectedOrders = [ExtrasSamples.Orders[1]];

    /// <summary>The countries the flag list names.</summary>
    private static readonly BitCountry[] flagCountries = [BitCountries.Netherlands, BitCountries.Germany, BitCountries.Japan, BitCountries.Sweden];

    /// <summary>Every order of the virtualized list, as indexes that ExtrasOrderRow draws from.</summary>
    private static readonly int[] allOrders = [.. Enumerable.Range(0, 5000)];

    /// <summary>The invoice of the first order, for the PDF viewer.</summary>
    private static readonly BitPdfSource invoice = BitPdfSource.FromBytes(ExtrasSamplePdf.Bytes, "invoice-8042.pdf");

    /// <summary>The week view shows the working day only.</summary>
    private readonly BitFullCalendarSettings calendarSettings = new() { VisibleStartHour = 8, VisibleEndHour = 19 };

    private readonly TimeProvider sampleClock = new SampleClock();

    /// <summary>The Dutch government's base map, where the three stores are.</summary>
    private readonly BitLeafletMapProvider mapProvider = new()
    {
        TileUrl = "https://service.pdok.nl/brt/achtergrondkaart/wmts/v2_0/standaard/EPSG:3857/{z}/{x}/{y}.png",
        TileAttribution = "Kaartgegevens &copy; <a href=\"https://www.kadaster.nl\">Kadaster</a>",
        TileMaxZoom = 19,
        Center = new(52.14, 4.8),
        Zoom = 8,
        MinZoom = 7,
        MaxBounds = new(new(50.4, 2.6), new(53.9, 7.6))
    };

    // Each accordion list sample gets lists of its own, so expanding one never opens another.
    private List<BitAccordionListItem>[] faqLists = [];
    private BitChartData revenueData = new();
    private BitChartData visitorsData = new();
    private BitChartData paymentsData = new();
    private BitChartData emptyData = new();
    private BitChartOptions barOptions = new();
    private BitChartOptions lineOptions = new();
    private BitChartOptions doughnutOptions = new();
    private List<BitFullCalendarEvent> calendarEvents = [];
    private BitFullCalendarTexts calendarTexts = new();
    private List<BitMapMarker> mapMarkers = [];
    private List<BitNavItem> openNavItems = [];
    private List<BitNavItem> railNavItems = [];
    private (string NameKey, BitColor Color, string Title, string Body)[] messageBoxes = [];
    private string markdownDraft = string.Empty;
    private string markdownDocument = string.Empty;
    private string richText = string.Empty;
    private BitResourceTimelineConfig[] schedules = [];
    private BitResourceTimelineOptions timelineOptions = new();
    private BitResourceTimelineTexts timelineTexts = new();
    private BitErrorBoundary? failedBoundary;
    private bool isPhone;
    // The calendar view the user picked; until then the default for the screen.
    private BitFullCalendarView? pickedView;

    // Texts the samples share.
    private string SaveText => AppLocalizer[nameof(AppStrings.Save)];

    private string CancelText => AppLocalizer[nameof(AppStrings.Cancel)];

    private string OkText => AppLocalizer[nameof(AppStrings.Ok)];

    private string SearchText => Localizer[nameof(WorkbenchStrings.ThemeSearch)];

    private string CountryLabel => Localizer[nameof(WorkbenchStrings.ThemeExtrasCountry)];

    private string CountryPlaceholder => Localizer[nameof(WorkbenchStrings.ThemeExtrasPickACountry)];

    private string PhoneLabel => AppLocalizer[nameof(AppStrings.PhoneNumber)];

    private string PhonePlaceholder => AppLocalizer[nameof(AppStrings.PhoneNumberPlaceholder)];

    private string TimeZoneLabel => AppLocalizer[nameof(AppStrings.TimeZone)];

    private string TimeZonePlaceholder => AppLocalizer[nameof(AppStrings.SelectTimeZone)];

    private string TimeZoneSearch => AppLocalizer[nameof(AppStrings.FindTimeZone)];

    private string NotesLabel => Localizer[nameof(WorkbenchStrings.ThemeExtrasWeeklyUpdate)];

    private string NotesPlaceholder => Localizer[nameof(WorkbenchStrings.ThemeExtrasWriteTheUpdateInMarkdown)];

    private string NewsletterLabel => Localizer[nameof(WorkbenchStrings.ThemeExtrasNewsletter)];

    private string NewsletterPlaceholder => Localizer[nameof(WorkbenchStrings.ThemeExtrasWriteSomethingWorthReading)];

    private string ReportTitle => Localizer[nameof(WorkbenchStrings.ThemeExtrasTodaySSales)];

    private string ScheduleLabel => Localizer[nameof(WorkbenchStrings.ShiftsHeading)];

    /// <summary>A day on phones, where a week doesn't fit, a week elsewhere; used until the user picks a view.</summary>
    private BitFullCalendarView CalendarView => pickedView ?? (isPhone ? BitFullCalendarView.Day : BitFullCalendarView.Week);

    protected override void OnInitialized()
    {
        faqLists = [.. Enumerable.Range(0, 6).Select(_ => CreateFaqItems())];
        openNavItems = CreateNavItems();
        railNavItems = CreateNavItems();

        CreateCharts();

        calendarEvents = CreateCalendarEvents();
        calendarTexts = CreateCalendarTexts();

        schedules = [.. Enumerable.Range(0, 3).Select(_ => CreateSchedule())];

        // The axis is drawn in UTC, the samples' zone, and in the page's language rather than the browser's.
        timelineOptions = new()
        {
            TimeZone = "UTC",
            Locale = CultureInfo.CurrentCulture.Name,
            ResourceHeight = 32,
            WorkingHoursStart = 7 * 60,
            WorkingHoursEnd = 19 * 60
        };

        timelineTexts = new() { Loading = Localizer[nameof(WorkbenchStrings.ThemeExtrasLoadingTheSchedule)] };

        mapMarkers = [.. ExtrasSamples.Stores.Select(store => new BitMapMarker
        {
            Id = store.NameKey,
            Position = new(store.Latitude, store.Longitude),
            Title = Localizer[store.NameKey],
            Alt = Localizer[store.NameKey],
            TooltipText = Localizer[store.NameKey],
            Color = $"var({BitCss.Var.Color.Primary.Main})",
            RiseOnHover = true
        })];

        messageBoxes =
        [
            (nameof(WorkbenchStrings.ThemeInfo), BitColor.Info, Localizer[nameof(WorkbenchStrings.ThemeExtrasANewVersionIsReady)], Localizer[nameof(WorkbenchStrings.ThemeExtrasRestartTheAppToStart)]),
            (nameof(WorkbenchStrings.ThemeSuccess), BitColor.Success, Localizer[nameof(WorkbenchStrings.ThemeExtrasPaymentReceived)], Localizer[nameof(WorkbenchStrings.ThemeExtrasOrder8042IsPaidAnd)]),
            (nameof(WorkbenchStrings.ThemeWarning), BitColor.Warning, Localizer[nameof(WorkbenchStrings.ThemeExtrasStorageAlmostFull)], Localizer[nameof(WorkbenchStrings.ThemeExtrasDeleteOldExportsToKeep)]),
            (nameof(WorkbenchStrings.ThemeSevereWarning), BitColor.SevereWarning, Localizer[nameof(WorkbenchStrings.ThemeExtrasYourPlanEndsTomorrow)], Localizer[nameof(WorkbenchStrings.ThemeExtrasRenewItTodayToKeep)]),
            (nameof(WorkbenchStrings.ThemeError), BitColor.Error, Localizer[nameof(WorkbenchStrings.ThemeExtrasPaymentFailed)], Localizer[nameof(WorkbenchStrings.ThemeExtrasTheCardWasDeclinedAsk)])
        ];

        markdownDraft = string.Join('\n',
        [
            $"## {Localizer[nameof(WorkbenchStrings.ThemeExtrasWeeklyUpdate)]}",
            string.Empty,
            $"{Localizer[nameof(WorkbenchStrings.ThemeExtrasSalesGrewAgainThisWeek)]}",
            string.Empty,
            $"- **{Localizer[nameof(WorkbenchStrings.ThemeExtrasRevenue)]}**: {ExtrasSamples.Money(26500)}",
            $"- **{Localizer[nameof(WorkbenchStrings.ThemeExtrasNewCustomers)]}**: 48",
            $"- *{Localizer[nameof(WorkbenchStrings.ThemeExtrasNextStep)]}*: {Localizer[nameof(WorkbenchStrings.ThemeExtrasPlanTheSummerMenu)]}"
        ]);

        markdownDocument = string.Join('\n',
        [
            $"# {Localizer[nameof(WorkbenchStrings.ThemeExtrasReleaseNotes)]}",
            string.Empty,
            $"{Localizer[nameof(WorkbenchStrings.ThemeExtrasVersion24IsOut)]}",
            string.Empty,
            $"## {Localizer[nameof(WorkbenchStrings.ThemeExtrasWhatSNew)]}",
            string.Empty,
            $"- **{Localizer[nameof(WorkbenchStrings.ThemeExtrasOfflineOrders)]}**: {Localizer[nameof(WorkbenchStrings.ThemeExtrasKeepSellingWhenTheConnection)]}",
            $"- **{Localizer[nameof(WorkbenchStrings.ThemeExtrasFasterDashboard)]}**: {Localizer[nameof(WorkbenchStrings.ThemeExtrasReportsOpenInUnderA)]}",
            $"- `{Localizer[nameof(WorkbenchStrings.ThemeExtrasExport)]}`: {Localizer[nameof(WorkbenchStrings.ThemeExtrasNowWritesExcelFilesToo)]}",
            string.Empty,
            $"> {Localizer[nameof(WorkbenchStrings.ThemeExtrasTipSearchTheWholeApp)]}",
            string.Empty,
            $"| {Localizer[nameof(WorkbenchStrings.ThemeExtrasPlan)]} | {Localizer[nameof(WorkbenchStrings.ThemeExtrasStores)]} | {Localizer[nameof(WorkbenchStrings.ThemeExtrasPricePerMonth)]} |",
            "| --- | ---: | ---: |",
            $"| {Localizer[nameof(WorkbenchStrings.ThemeExtrasStarter)]} | 1 | {ExtrasSamples.Money(9)} |",
            $"| {Localizer[nameof(WorkbenchStrings.PosTeam)]} | 5 | {ExtrasSamples.Money(29)} |",
            string.Empty,
            $"[{Localizer[nameof(WorkbenchStrings.ThemeExtrasReadTheFullChangelog)]}](https://bitplatform.dev)"
        ]);

        richText = $"<h3>{Html(Localizer[nameof(WorkbenchStrings.ThemeExtrasSummerMenu)])}</h3>"
                 + $"<p>{Html(Localizer[nameof(WorkbenchStrings.ThemeExtrasFromJuneEveryStoreServes)])}</p>"
                 + $"<ul><li><strong>{Html(Localizer[nameof(WorkbenchStrings.PosProductColdBrew)])}</strong></li>"
                 + $"<li><em>{Html(Localizer[nameof(WorkbenchStrings.ThemeExtrasIcedOatMilkLatte)])}</em></li>"
                 + $"<li>{Html(Localizer[nameof(WorkbenchStrings.ThemeExtrasFreshLemonade)])}</li></ul>";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        // The error state comes from the boundary's own API, which takes an exception that was never thrown; the
        // exception's text is not shown, since the samples leave ShowException off.
        failedBoundary?.Capture(new InvalidOperationException("The sales report could not be loaded."));

        try
        {
            // The section may render after the page asked for its forced states, so it asks again for its own.
            await JSRuntime.InvokeVoidAsync("workbench.forceStates");
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
            // The view went away while the browser was being asked.
        }
    }

    /// <summary>
    /// The next page of today's orders, as the indexes ExtrasOrderRow draws from, after a short wait that shows the
    /// feed's loading row the way a real one does. The feed stops at its MaxItems.
    /// </summary>
    private static async ValueTask<IEnumerable<int>> LoadOrders(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(400, request.CancellationToken);

        return Enumerable.Range(request.Skip, request.Count);
    }

    /// <summary>A feed with no orders, for the empty sample.</summary>
    private static ValueTask<IEnumerable<int>> LoadNoOrders(BitInfiniteScrollingItemsProviderRequest request) => ValueTask.FromResult(Enumerable.Empty<int>());

    /// <summary>Three shop questions with their answers; each sample gets new ones, as the list keeps state on them.</summary>
    private List<BitAccordionListItem> CreateFaqItems() =>
    [
        new()
        {
            Key = "delivery",
            IconName = BitIconName.DeliveryTruck,
            Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasWhenDoesMyOrderArrive)],
            Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasDelivery)],
            Body = BodyText(Localizer[nameof(WorkbenchStrings.ThemeExtrasOrdersPlacedBefore4Pm)])
        },
        new()
        {
            Key = "returns",
            IconName = BitIconName.ReturnToSession,
            Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasCanIReturnSomething)],
            Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasReturns)],
            Body = BodyText(Localizer[nameof(WorkbenchStrings.ThemeExtrasSendItBackWithin30)])
        },
        new()
        {
            Key = "payment",
            IconName = BitIconName.PaymentCard,
            Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasHowCanIPay)],
            Description = Localizer[nameof(WorkbenchStrings.PosPayment)],
            Body = BodyText(Localizer[nameof(WorkbenchStrings.ThemeExtrasByCardMobileWalletOr)])
        }
    ];

    /// <summary>A panel body that is just a line of text.</summary>
    private static RenderFragment<BitAccordionListItem> BodyText(string text) => _ => builder => builder.AddContent(0, text);

    /// <summary>The app's menu, with the catalog group open; each nav panel gets its own, as the nav keeps state on them.</summary>
    private List<BitNavItem> CreateNavItems() =>
    [
        new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], IconName = BitIconName.Home },
        new() { Key = "dashboard", Text = Localizer[nameof(WorkbenchStrings.ThemeExtrasDashboard)], IconName = BitIconName.BarChartVerticalFill },
        new()
        {
            Key = "catalog",
            Text = Localizer[nameof(WorkbenchStrings.ThemeExtrasCatalog)],
            IconName = BitIconName.Shop,
            IsExpanded = true,
            ChildItems =
            [
                new() { Key = "categories", Text = Localizer[nameof(WorkbenchStrings.ThemeExtrasCategories)], IconName = BitIconName.BuildQueue },
                new() { Key = "products", Text = Localizer[nameof(WorkbenchStrings.ThemeExtrasProducts)], IconName = BitIconName.Product }
            ]
        },
        new() { Key = "orders", Text = Localizer[nameof(WorkbenchStrings.ThemeExtrasOrders)], IconName = BitIconName.ShoppingCart, Badge = "3" },
        new() { Key = "users", Text = AppLocalizer[nameof(AppStrings.Users)], IconName = BitIconName.People },
        new() { Key = "settings", Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Settings }
    ];

    /// <summary>The chart samples' data and options, colored with the theme's own colors.</summary>
    private void CreateCharts()
    {
        var months = Enumerable.Range(0, ExtrasSamples.RevenueThisYear.Length)
                               .Select(month => ThemeSamples.Today.AddMonths(month - ExtrasSamples.RevenueThisYear.Length + 1).ToString("MMM", CultureInfo.CurrentCulture));
        // The samples' day is a Monday, the first of the week the line chart shows.
        var days = Enumerable.Range(0, ExtrasSamples.VisitorsThisWeek.Length)
                             .Select(day => ThemeSamples.Today.AddDays(day).ToString("ddd", CultureInfo.CurrentCulture));

        revenueData = new()
        {
            Labels = [.. months],
            Datasets =
            {
                new BitChartDataset
                {
                    Label = Localizer[nameof(WorkbenchStrings.ThemeExtrasThisYear)],
                    Data = [.. ExtrasSamples.RevenueThisYear.Select(value => (double?)value)],
                    BackgroundColor = ThemeColor(BitCss.Var.Color.Primary.Main),
                    BorderRadius = 6,
                    MaxBarThickness = 22
                },
                new BitChartDataset
                {
                    Label = Localizer[nameof(WorkbenchStrings.ThemeExtrasLastYear)],
                    Data = [.. ExtrasSamples.RevenueLastYear.Select(value => (double?)value)],
                    BackgroundColor = ThemeColor(BitCss.Var.Color.Secondary.Main),
                    BorderRadius = 6,
                    MaxBarThickness = 22
                }
            }
        };

        visitorsData = new()
        {
            Labels = [.. days],
            Datasets =
            {
                new BitChartDataset
                {
                    Label = Localizer[nameof(WorkbenchStrings.NotchDemoWeek)],
                    Data = [.. ExtrasSamples.VisitorsThisWeek.Select(value => (double?)value)],
                    BorderColor = ThemeColor(BitCss.Var.Color.Primary.Main),
                    BackgroundColor = ThemeColor(BitCss.Var.Color.Primary.Main),
                    BorderWidth = 2,
                    Tension = 0.4,
                    PointRadius = 3
                },
                new BitChartDataset
                {
                    Label = Localizer[nameof(WorkbenchStrings.ThemeExtrasLastWeek)],
                    Data = [.. ExtrasSamples.VisitorsLastWeek.Select(value => (double?)value)],
                    BorderColor = ThemeColor(BitCss.Var.Color.Warning.Main),
                    BackgroundColor = ThemeColor(BitCss.Var.Color.Warning.Main),
                    BorderDash = [5, 4],
                    BorderWidth = 2,
                    Tension = 0.4,
                    PointRadius = 0
                }
            }
        };

        paymentsData = new()
        {
            Labels = [.. ExtrasSamples.Payments.Select(payment => Localizer[payment.MethodKey].Value)],
            Datasets =
            {
                new BitChartDataset
                {
                    Label = Localizer[nameof(WorkbenchStrings.ThemeExtrasSalesByPaymentMethod)],
                    Data = [.. ExtrasSamples.Payments.Select(payment => (double?)payment.Amount)],
                    BackgroundColors =
                    [
                        ThemeColor(BitCss.Var.Color.Primary.Main),
                        ThemeColor(BitCss.Var.Color.Success.Main),
                        ThemeColor(BitCss.Var.Color.Warning.Main),
                        ThemeColor(BitCss.Var.Color.Info.Main)
                    ],
                    BorderWidth = 0,
                    SpacingArc = 2
                }
            }
        };

        barOptions = new()
        {
            MaintainAspectRatio = false,
            Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Placement = BitPlacement.Bottom } },
            Scales =
            {
                ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Grid = new BitChartGridOptions { Display = false } },
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true, Ticks = new BitChartTickOptions { Prefix = "€", MaxTicksLimit = 5 } }
            }
        };

        lineOptions = new()
        {
            MaintainAspectRatio = false,
            Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Placement = BitPlacement.Bottom } },
            Scales =
            {
                ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Grid = new BitChartGridOptions { Display = false } },
                ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true, Ticks = new BitChartTickOptions { MaxTicksLimit = 5 } }
            }
        };

        doughnutOptions = new()
        {
            MaintainAspectRatio = false,
            CutoutPercentage = 64,
            Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Placement = BitPlacement.Bottom } }
        };
    }

    /// <summary>A theme color for a chart, which draws its marks with the CSS variable as it is.</summary>
    private static string ThemeColor(string variable) => $"var({variable})";

    /// <summary>A week of the coffee shop's appointments, in the calendar's built-in event colors.</summary>
    private List<BitFullCalendarEvent> CreateCalendarEvents()
    {
        var monday = ThemeSamples.Today.Date;

        return
        [
            new() { Id = "stand-up", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasTeamStandUp)], Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasPlansForTheDay)], StartDate = monday.AddHours(9), EndDate = monday.AddHours(9.5), Color = "blue" },
            new() { Id = "tasting", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasMenuTasting)], Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasThreeNewSummerDrinks)], StartDate = monday.AddHours(11), EndDate = monday.AddHours(12.5), Color = "purple" },
            new() { Id = "supplier", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasSupplierCall)], Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasBeansForTheNextQuarter)], StartDate = monday.AddDays(1).AddHours(10), EndDate = monday.AddDays(1).AddHours(11), Color = "green" },
            new() { Id = "training", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasBaristaTraining)], Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasMilkTextureAndLatteArt)], StartDate = monday.AddDays(2).AddHours(14), EndDate = monday.AddDays(2).AddHours(16), Color = "orange" },
            new() { Id = "delivery", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasStockDelivery)], Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasCupsLidsAndOatMilk)], StartDate = monday.AddDays(3).AddHours(8.5), EndDate = monday.AddDays(3).AddHours(9.25), Color = "yellow" },
            new() { Id = "inspection", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasFoodSafetyInspection)], Description = Localizer[nameof(WorkbenchStrings.PosStoreAll)], StartDate = monday.AddDays(4), EndDate = monday.AddDays(4), IsAllDay = true, Color = "red" },
            new() { Id = "workshop", Title = Localizer[nameof(WorkbenchStrings.ThemeExtrasLatteArtWorkshop)], Description = Localizer[nameof(WorkbenchStrings.ThemeExtrasForCustomersTwelveSeats)], StartDate = monday.AddDays(5).AddHours(15), EndDate = monday.AddDays(5).AddHours(17), Color = "purple" }
        ];
    }

    /// <summary>The calendar's texts from the Calendar resources the shift planner uses; a text without one keeps the calendar's own.</summary>
    private BitFullCalendarTexts CreateCalendarTexts()
    {
        var texts = new BitFullCalendarTexts();

        foreach (var property in typeof(BitFullCalendarTexts).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(string) && p.CanWrite))
        {
            var text = Localizer[$"Calendar{property.Name}"];

            if (text.ResourceNotFound is false)
            {
                property.SetValue(texts, text.Value);
            }
        }

        return texts;
    }

    /// <summary>
    /// A week of the three stores' shifts from the samples' Monday, each store a group of its staff; Utrecht starts
    /// collapsed, so the open and the closed group both show.
    /// </summary>
    private BitResourceTimelineConfig CreateSchedule()
    {
        var monday = new DateTimeOffset(ThemeSamples.Today.Date, TimeSpan.Zero);

        List<BitResourceTimelineResource> resources = [];
        List<BitResourceTimelineAllocation> shifts = [];

        foreach (var store in ExtrasSamples.Staff.Select(member => member.StoreKey).Distinct())
        {
            resources.Add(new() { Id = store, Name = Localizer[store], Collapsed = store == nameof(WorkbenchStrings.PosStoreUtrecht) });

            foreach (var (nameKey, _, week) in ExtrasSamples.Staff.Where(member => member.StoreKey == store))
            {
                resources.Add(new() { Id = nameKey, Name = Localizer[nameKey], ParentId = store });

                for (var day = 0; day < week.Length; day++)
                {
                    if (week[day] is '-')
                        continue;

                    var (start, end, shiftKey) = ExtrasSamples.Shift(week[day]);

                    shifts.Add(new()
                    {
                        Id = FormattableString.Invariant($"{nameKey}-{day}"),
                        ResourceId = nameKey,
                        StartTime = monday.AddDays(day).AddHours(start),
                        EndTime = monday.AddDays(day).AddHours(end),
                        TextAbove = Localizer[shiftKey]
                    });
                }
            }
        }

        return new() { Resources = resources, StartDate = monday, EndDate = monday.AddDays(7), Allocations = shifts };
    }

    /// <summary>The shifts of the loading sample, which never arrive, so the timeline keeps showing its loading state.</summary>
    private async Task<IReadOnlyList<BitResourceTimelineAllocation>> LoadShiftsForever(BitResourceTimelineWindow window)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, CurrentCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The section went away.
        }

        return [];
    }

    private void OnPhoneChanged(bool phone) => isPhone = phone;

    private void ShowCalendarView(BitFullCalendarView view) => pickedView = view;

    private static string Html(string text) => WebUtility.HtmlEncode(text);

    /// <summary>A clock stopped at the samples' day, so the calendar marks the same today and time on every run.</summary>
    private sealed class SampleClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ThemeSamples.Today;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
