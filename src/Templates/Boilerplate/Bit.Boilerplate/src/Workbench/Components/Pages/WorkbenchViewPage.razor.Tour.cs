using Bit.Bonboarding;

namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// The tour of what the home page promises. Each step shows its feature for real in the views, undoes it when it is left, and
/// the end of the tour brings back the views, mode and tab it started from. Its texts are the home page's own.
/// </summary>
public partial class WorkbenchViewPage
{
    private const string TourId = "workbench-tour";
    private const string TourTabletDevice = "galaxy-tab-s9";
    private const string TourStoreDevice = "store-apple-iphone-69";
    private const string TourDataStatesAnchor = "data-states";

    private Task StartTour() => bonboardingService.StartAsync(CreateTour());

    private BonboardingTourDefinition CreateTour()
    {
        WorkbenchScenario[] viewsBefore = [.. store.Panes.Select(pane => pane.Scenario)];
        var modeBefore = store.Mode;
        var columnBefore = store.Column;
        var phone = new WorkbenchScenario();
        // The building sample shows what a flow is; without its page, the list of flows does.
        var flowsUrl = reviewSession.AllFlows.Any(flow => flow.Id == WorkbenchSampleFlows.BuildingsId) ? WorkbenchUrls.Flow(WorkbenchSampleFlows.BuildingsId) : WorkbenchUrls.Flows;

        return new()
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

                return ValueTask.CompletedTask;
            },
            OnEnded = _ =>
            {
                SetMode(modeBefore);
                store.ShowColumn(columnBefore);
                store.ShowViews(viewsBefore);

                if (IsOnFlowsPage)
                {
                    BackToTheViews();
                }

                return ValueTask.CompletedTask;
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
                    OnBeforeShow = _ => ShowInTour(() => SetMode(WorkbenchModes.Inspect)),
                    OnBeforeLeave = _ => ShowInTour(() => SetMode(WorkbenchModes.Browse))
                },
                new BonboardingStepDefinition
                {
                    Id = "live-review",
                    Target = BonboardingStepTarget.Css("#wb-inspector"),
                    Title = nameof(WorkbenchStrings.HomeWhyLiveTitle),
                    Description = nameof(WorkbenchStrings.HomeWhyLiveText),
                    Placement = BonboardingPlacement.InlineStart,
                    OnBeforeShow = _ => ShowInTour(() => SetMode(WorkbenchModes.Comment)),
                    OnBeforeLeave = _ => ShowInTour(() =>
                    {
                        SetMode(WorkbenchModes.Browse);
                        store.ShowColumn(WorkbenchModes.ViewColumn);
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
                    OnBeforeLeave = _ => ShowInTour(BackToTheViews)
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
    }

    private bool IsOnFlowsPage => BonboardingRouteMatcher.Matches($"{WorkbenchUrls.Flows}*", NavigationManager.ToBaseRelativePath(NavigationManager.Uri));

    private void BackToTheViews() => NavigationManager.NavigateTo($"{WorkbenchUrls.Workbench(pageInfo!.Key)}{store.QueryString}");

    private static ValueTask ShowInTour(Action show)
    {
        show();

        return ValueTask.CompletedTask;
    }
}
