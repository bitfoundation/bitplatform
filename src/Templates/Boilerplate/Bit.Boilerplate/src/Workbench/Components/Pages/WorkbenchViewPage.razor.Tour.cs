using Bit.Bonboarding;

namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// The tour of what the home page promises. Each step shows its feature for real in the views, undoes it when it is left, and
/// the end of the tour brings back the views and mode it started from, on the View tab. Its texts are the home page's own. A phone gets the
/// same steps in the wider screens' shell, which workbench.scss stacks in one column for the step shown.
/// </summary>
public partial class WorkbenchViewPage
{
    internal const string TourId = "workbench-tour";
    private const string TourTabletDevice = "galaxy-tab-s9";
    private const string TourStoreDevice = "store-apple-iphone-69";
    private const string TourDataStatesAnchor = "data-states";
    private const string TourComponentsPage = "shifts";
    private const string TourLandingPage = "landing-minimal";

    // A comment about the page as a whole: one written on the tour's live review step without pressing a spot first, or a
    // finding of the Checks tab whose element is gone.
    private static readonly WorkbenchReviewAnchor PageAnchor = new("body", null, null, 0.5, 0.5);

    private bool IsTouring => bonboardingService.FindSession(TourId) is not null;

    // Only that step puts a tour in Comment mode; once its comment is sent, the thread shows in place of the composer.
    private bool IsTourAskingForAComment => IsTouring && store.Mode == WorkbenchModes.Comment && selectedThreadId is null && isTourComposerClosed is false;

    // Cancel closes that composer until the mode changes, and pressing a spot on the page opens one all the same.
    private bool isTourComposerClosed;

    private Task StartTour()
    {
        isToolsOpen = false;

        return bonboardingService.StartAsync(CreateTour());
    }

    private BonboardingTourDefinition CreateTour()
    {
        WorkbenchScenario[] viewsBefore = [.. store.Panes.Select(pane => pane.Scenario)];
        var modeBefore = store.Mode;
        var phone = new WorkbenchScenario();
        // The building sample shows what a flow is; without its page, the list of flows does.
        var hasBuildingsSample = reviewSession.AllFlows.Any(flow => flow.Id == WorkbenchSampleFlows.BuildingsId);
        var flowsUrl = hasBuildingsSample ? WorkbenchUrls.Flow(WorkbenchSampleFlows.BuildingsId) : WorkbenchUrls.Flows;
        // The sample's arrow editor, which opens only where arrows can be changed.
        var showsArrowEditor = hasBuildingsSample && reviewSession.CanWrite;
        // Set by the step being left when the next one is on the board too, which it then leaves as it is.
        var staysOnBoard = false;
        // Set by the Progress step when Next leaves it, as the Flows step goes there from the Progress page by itself.
        var leavesProgressForward = false;
        var pageBefore = pageInfo!.Key;
        // Another page than the one the tour starts on, for a change; without it, that page.
        var componentsPage = WorkbenchPageCatalog.Find(TourComponentsPage)?.Key ?? pageBefore;
        // A page with a theme other than the app's, to show one Workbench holding several apps; without it, that page.
        var landingPage = WorkbenchPageCatalog.Find(TourLandingPage)?.Key ?? pageBefore;

        // Replaces the address, as the phone's Back from a page the tour opened by itself would lead nowhere useful.
        void ShowPage(string pageKey) => NavigationManager.NavigateTo($"{WorkbenchUrls.Workbench(pageKey)}{store.QueryString}", replace: true);

        // Back to the views, unless the next step is on the board too: then only the arrow's editor closes.
        void LeaveBoard(string stayingOn)
        {
            if (staysOnBoard)
            {
                NavigationManager.NavigateTo(stayingOn, replace: true);
            }
            else
            {
                ShowPage(pageBefore);
            }

            staysOnBoard = false;
        }

        BonboardingTourDefinition tour = new()
        {
            Id = TourId,
            Persist = false,
            MissingTarget = BonboardingMissingTargetBehavior.Center,
            OnStarted = _ =>
            {
                SetMode(WorkbenchModes.Browse);
                store.ShowColumn(WorkbenchModes.ViewColumn);
                store.ShowViews([phone]);

                if (store.IsNavigatorCollapsed)
                {
                    store.ToggleNavigator();
                }

                if (store.IsInspectorCollapsed)
                {
                    store.ToggleInspector();
                }

                return ValueTask.CompletedTask;
            },
            OnEnded = async _ =>
            {
                SetMode(isSmallScreen && IsDesktopOnly(modeBefore) ? WorkbenchModes.Browse : modeBefore);
                // The tab the user looks at first, whichever one the tour started from.
                store.ShowColumn(WorkbenchModes.ViewColumn);
                store.ShowViews(viewsBefore);

                var address = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

                if (IsOnFlowsPage || IsOnProgressPage || BonboardingRouteMatcher.Matches(WorkbenchUrls.Workbench(componentsPage), address) || BonboardingRouteMatcher.Matches(WorkbenchUrls.Workbench(landingPage), address))
                {
                    ShowPage(pageBefore);
                }

                try
                {
                    await JSRuntime.InvokeVoidAsync("workbench.setTourStep", (string?)null);
                }
                catch (JSDisconnectedException)
                {
                    // The tab was closed during the tour, and the mark went with it.
                }
            },
            Steps =
            {
                new BonboardingStepDefinition
                {
                    Id = "agents",
                    Title = nameof(WorkbenchStrings.HomeWhyAgentsTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyAgentsText)
                },
                new BonboardingStepDefinition
                {
                    Id = "head-start",
                    Target = BonboardingStepTarget.Css("#wb-navigator"),
                    Title = nameof(WorkbenchStrings.HomeWhyHeadStartTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyHeadStartText),
                    Placement = BonboardingPlacement.InlineEnd
                },
                new BonboardingStepDefinition
                {
                    Id = "devices",
                    Target = BonboardingStepTarget.Css(".canvas"),
                    Title = nameof(WorkbenchStrings.HomeWhyDevicesTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyDevicesText),
                    Placement = BonboardingPlacement.InlineStart,
                    OnBeforeShow = _ => ShowInTour(() => store.ShowViews([phone, phone with { Device = TourTabletDevice, Rotated = true, Theme = WorkbenchThemeMode.Dark }])),
                    OnBeforeLeave = _ => ShowInTour(() => store.RemovePane(store.Panes[^1].Id))
                },
                new BonboardingStepDefinition
                {
                    Id = "states",
                    Target = BonboardingStepTarget.Css(".canvas"),
                    AdditionalTargets = [BonboardingStepTarget.Anchor(TourDataStatesAnchor)],
                    Title = nameof(WorkbenchStrings.HomeWhyStatesTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyStatesText),
                    Placement = BonboardingPlacement.InlineStart,
                    Interaction = BonboardingInteractionMode.TargetOnly,
                    // workbench.scss dims all of the spotlight's rectangle but the phone and the Data section, by this much.
                    PopoverClass = "tour-states",
                    OverlayOpacity = 0.6,
                    SpotlightPadding = 0,
                    SpotlightRadius = 0,
                    OnBeforeShow = async _ =>
                    {
                        store.ChangeScenario(phone with { Data = WorkbenchDataState.Loading });
                        await JSRuntime.InvokeVoidAsync("workbench.scrollIntoSight", $"[data-bonboarding-anchor='{TourDataStatesAnchor}']");
                    },
                    OnBeforeLeave = _ => ShowInTour(() => store.ChangeScenario(phone))
                },
                new BonboardingStepDefinition
                {
                    Id = "stores",
                    Target = BonboardingStepTarget.Anchor("screenshot"),
                    AdditionalTargets = [BonboardingStepTarget.Css(".canvas"), BonboardingStepTarget.Css("#wb-inspector")],
                    Title = nameof(WorkbenchStrings.HomeWhyStoresTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyStoresText),
                    Placement = BonboardingPlacement.InlineStart,
                    Interaction = BonboardingInteractionMode.TargetOnly,
                    OnBeforeShow = _ => ShowInTour(() => store.ChangeScenario(phone with { Device = TourStoreDevice, Backdrop = WorkbenchBackdrop.Ocean, Caption = Localizer[nameof(WorkbenchStrings.TourCaption)] })),
                    OnBeforeLeave = _ => ShowInTour(() => store.ChangeScenario(phone))
                },
                new BonboardingStepDefinition
                {
                    Id = "real-components",
                    Target = BonboardingStepTarget.Css(".modes"),
                    AdditionalTargets = [BonboardingStepTarget.Css(".canvas"), BonboardingStepTarget.Css("#wb-inspector")],
                    Title = nameof(WorkbenchStrings.HomeWhyRealTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyRealText),
                    Placement = BonboardingPlacement.InlineStart,
                    Interaction = BonboardingInteractionMode.TargetOnly,
                    OnBeforeShow = _ => ShowInTour(() =>
                    {
                        ShowPage(componentsPage);
                        SetMode(WorkbenchModes.Inspect);
                    }),
                    OnBeforeLeave = _ => ShowInTour(() =>
                    {
                        SetMode(WorkbenchModes.Browse);
                        ShowPage(pageBefore);
                    })
                },
                new BonboardingStepDefinition
                {
                    Id = "live-review",
                    Target = BonboardingStepTarget.Css(".modes"),
                    AdditionalTargets = [BonboardingStepTarget.Css(".canvas"), BonboardingStepTarget.Css("#wb-inspector")],
                    Title = nameof(WorkbenchStrings.HomeWhyLiveTitle),
                    Description = nameof(WorkbenchStrings.TourLiveText),
                    Placement = BonboardingPlacement.InlineStart,
                    Interaction = BonboardingInteractionMode.TargetOnly,
                    OnBeforeShow = _ => ShowInTour(() =>
                    {
                        selectedThreadId = null;
                        SetMode(WorkbenchModes.Comment);
                    }),
                    OnBeforeLeave = _ => ShowInTour(() =>
                    {
                        SetMode(WorkbenchModes.Browse);
                        store.ShowColumn(WorkbenchModes.ViewColumn);
                    })
                },
                new BonboardingStepDefinition
                {
                    Id = "progress",
                    Route = WorkbenchUrls.Progress,
                    Target = BonboardingStepTarget.Anchor("progress-list"),
                    Title = nameof(WorkbenchStrings.HomeWhyProgressTitle),
                    Description = nameof(WorkbenchStrings.TourProgressText),
                    Placement = BonboardingPlacement.InlineEnd,
                    OnBeforeShow = _ => ShowInTour(() => leavesProgressForward = false),
                    ResolveNext = _ =>
                    {
                        leavesProgressForward = true;
                        return ValueTask.FromResult<string?>(null);
                    },
                    // Back goes to the views; Next goes straight on to the Flows page.
                    OnBeforeLeave = _ => ShowInTour(() =>
                    {
                        if (leavesProgressForward is false)
                        {
                            ShowPage(pageBefore);
                        }

                        leavesProgressForward = false;
                    })
                },
                new BonboardingStepDefinition
                {
                    Id = "flows",
                    Route = flowsUrl,
                    // The building sample's screens, which leaves room beside them for the card; without the sample, the list of flows.
                    Target = BonboardingStepTarget.Css(".screen-card:has([data-screen-id='list']), .flow-list"),
                    AdditionalTargets = [BonboardingStepTarget.Css(".screen-card:has([data-screen-id='form'])"), BonboardingStepTarget.Css(".screen-card:has([data-screen-id='map'])")],
                    Title = nameof(WorkbenchStrings.Flows),
                    Description = nameof(WorkbenchStrings.AllFlowsHint),
                    Placement = BonboardingPlacement.InlineStart,
                    // A leave hook that never ran, for the browser's Back, leaves no mark for the next one.
                    OnBeforeShow = _ => ShowInTour(() => staysOnBoard = false),
                    // Runs before the leave hook, so the board stays for the arrow's step.
                    ResolveNext = _ =>
                    {
                        staysOnBoard = showsArrowEditor;
                        return ValueTask.FromResult<string?>(null);
                    },
                    // Back goes to the Progress page from here, never through the views, whose first render would take a
                    // tour that waits for a page as the browser's Back and step it back once more.
                    ResolvePrevious = _ =>
                    {
                        staysOnBoard = true;
                        return ValueTask.FromResult<string?>(null);
                    },
                    OnBeforeLeave = _ => ShowInTour(() => LeaveBoard(stayingOn: flowsUrl))
                },
                new BonboardingStepDefinition
                {
                    Id = "flow-arrows",
                    Route = flowsUrl,
                    When = _ => ValueTask.FromResult(showsArrowEditor),
                    // The editor of the sample's Add building arrow, opened the way the board's address opens it.
                    Target = BonboardingStepTarget.Dynamic(_ =>
                    {
                        NavigationManager.NavigateTo(WorkbenchUrls.FlowArrow(WorkbenchSampleFlows.BuildingsId, WorkbenchSampleFlows.BuildingsAddArrowId), replace: true);

                        return BonboardingStepTarget.Css(".label-editor");
                    }),
                    Title = nameof(WorkbenchStrings.TourArrowsTitle),
                    Description = nameof(WorkbenchStrings.TourArrowsText),
                    Placement = BonboardingPlacement.Bottom,
                    OnBeforeShow = _ => ShowInTour(() => staysOnBoard = false),
                    ResolvePrevious = _ =>
                    {
                        staysOnBoard = true;
                        return ValueTask.FromResult<string?>(null);
                    },
                    OnBeforeLeave = _ => ShowInTour(() => LeaveBoard(stayingOn: flowsUrl))
                },
                new BonboardingStepDefinition
                {
                    Id = "many-apps",
                    Target = BonboardingStepTarget.Css(".canvas"),
                    Title = nameof(WorkbenchStrings.HomeWhyManyAppsTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyManyAppsText),
                    Placement = BonboardingPlacement.InlineStart,
                    OnBeforeShow = _ => ShowInTour(() => ShowPage(landingPage)),
                    OnBeforeLeave = _ => ShowInTour(() => ShowPage(pageBefore))
                },
                new BonboardingStepDefinition
                {
                    Id = "safe",
                    Title = nameof(WorkbenchStrings.HomeWhySafeTitle),
                    Description = nameof(WorkbenchStrings.HomeWhySafeText)
                },
                new BonboardingStepDefinition
                {
                    Id = "customize",
                    Title = nameof(WorkbenchStrings.HomeWhyCustomizeTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyCustomizeText)
                }
            }
        };

        // Marked on the document before the step measures its targets, as the page below can change during the tour.
        foreach (var step in tour.Steps)
        {
            var show = step.OnBeforeShow;

            step.OnBeforeShow = async context =>
            {
                await JSRuntime.InvokeVoidAsync("workbench.setTourStep", step.Id);

                if (show is not null)
                {
                    await show(context);
                }
            };
        }

        return tour;
    }

    private bool IsOnFlowsPage => BonboardingRouteMatcher.Matches($"{WorkbenchUrls.Flows}*", NavigationManager.ToBaseRelativePath(NavigationManager.Uri));

    private bool IsOnProgressPage => BonboardingRouteMatcher.Matches($"{WorkbenchUrls.Progress}*", NavigationManager.ToBaseRelativePath(NavigationManager.Uri));

    private static ValueTask ShowInTour(Action show)
    {
        show();

        return ValueTask.CompletedTask;
    }
}
