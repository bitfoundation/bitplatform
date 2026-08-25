namespace Boilerplate.Client.Core.Components.Pages;

public abstract partial class AppPageBase : AppComponentBase
{
    [Parameter] public string? culture { get; set; }

    protected BitDataGridStrings DataGridStrings { get; private set; } = default!;

    protected override async Task OnInitAsync()
    {
        DataGridStrings = new()
        {
            EmptyText = Localizer[nameof(AppStrings.DataGridEmptyText)],
            LoadingText = Localizer[nameof(AppStrings.DataGridLoadingText)],
            ActionsText = Localizer[nameof(AppStrings.DataGridActionsText)],
            AddRowText = Localizer[nameof(AppStrings.DataGridAddRowText)],
            ClearFiltersText = Localizer[nameof(AppStrings.DataGridClearFiltersText)],
            ExportCsvText = Localizer[nameof(AppStrings.DataGridExportCsvText)],
            ExportExcelText = Localizer[nameof(AppStrings.DataGridExportExcelText)],
            ColumnsText = Localizer[nameof(AppStrings.DataGridColumnsText)],
            EditText = Localizer[nameof(AppStrings.DataGridEditText)],
            DeleteText = Localizer[nameof(AppStrings.DataGridDeleteText)],
            SaveText = Localizer[nameof(AppStrings.DataGridSaveText)],
            CancelText = Localizer[nameof(AppStrings.DataGridCancelText)],
            SelectAllLabel = Localizer[nameof(AppStrings.DataGridSelectAllLabel)],
            SelectRowLabel = Localizer[nameof(AppStrings.DataGridSelectRowLabel)],
            SelectRowFormat = Localizer[nameof(AppStrings.DataGridSelectRowFormat)],
            FilterPlaceholder = Localizer[nameof(AppStrings.DataGridFilterPlaceholder)],
            FilterByFormat = Localizer[nameof(AppStrings.DataGridFilterByFormat)],
            FilterAllText = Localizer[nameof(AppStrings.DataGridFilterAllText)],
            FilterOperatorLabel = Localizer[nameof(AppStrings.DataGridFilterOperatorLabel)],
            FilterOpContains = Localizer[nameof(AppStrings.DataGridFilterOpContains)],
            FilterOpDoesNotContain = Localizer[nameof(AppStrings.DataGridFilterOpDoesNotContain)],
            FilterOpStartsWith = Localizer[nameof(AppStrings.DataGridFilterOpStartsWith)],
            FilterOpEndsWith = Localizer[nameof(AppStrings.DataGridFilterOpEndsWith)],
            BooleanTrueText = Localizer[nameof(AppStrings.DataGridBooleanTrueText)],
            BooleanFalseText = Localizer[nameof(AppStrings.DataGridBooleanFalseText)],
            GroupByFormat = Localizer[nameof(AppStrings.DataGridGroupByFormat)],
            UngroupByFormat = Localizer[nameof(AppStrings.DataGridUngroupByFormat)],
            ExpandGroupFormat = Localizer[nameof(AppStrings.DataGridExpandGroupFormat)],
            CollapseGroupFormat = Localizer[nameof(AppStrings.DataGridCollapseGroupFormat)],
            PagerRangeFormat = Localizer[nameof(AppStrings.DataGridPagerRangeFormat)],
            PagerPageFormat = Localizer[nameof(AppStrings.DataGridPagerPageFormat)],
            PerPageFormat = Localizer[nameof(AppStrings.DataGridPerPageFormat)],
            RowsPerPageLabel = Localizer[nameof(AppStrings.DataGridRowsPerPageLabel)],
            FirstPageLabel = Localizer[nameof(AppStrings.DataGridFirstPageLabel)],
            PreviousPageLabel = Localizer[nameof(AppStrings.DataGridPreviousPageLabel)],
            NextPageLabel = Localizer[nameof(AppStrings.DataGridNextPageLabel)],
            LastPageLabel = Localizer[nameof(AppStrings.DataGridLastPageLabel)],
            EndOfResultsText = Localizer[nameof(AppStrings.DataGridEndOfResultsText)],
            ToggleDetailsLabel = Localizer[nameof(AppStrings.DataGridToggleDetailsLabel)],
            ToggleChildrenLabel = Localizer[nameof(AppStrings.DataGridToggleChildrenLabel)],
            ReorderRowTitle = Localizer[nameof(AppStrings.DataGridReorderRowTitle)],
            ReorderRowLabel = Localizer[nameof(AppStrings.DataGridReorderRowLabel)],
            InvalidValueError = Localizer[nameof(AppStrings.DataGridInvalidValueError)],
            AnnouncementSortedAscending = Localizer[nameof(AppStrings.DataGridAnnouncementSortedAscending)],
            AnnouncementSortedDescending = Localizer[nameof(AppStrings.DataGridAnnouncementSortedDescending)],
            AnnouncementSortCleared = Localizer[nameof(AppStrings.DataGridAnnouncementSortCleared)],
            AnnouncementFiltered = Localizer[nameof(AppStrings.DataGridAnnouncementFiltered)],
            AnnouncementFilterCleared = Localizer[nameof(AppStrings.DataGridAnnouncementFilterCleared)],
            AnnouncementPage = Localizer[nameof(AppStrings.DataGridAnnouncementPage)],
            AnnouncementRowDeleted = Localizer[nameof(AppStrings.DataGridAnnouncementRowDeleted)]
        };

        await base.OnInitAsync();
    }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (InPrerenderSession) return;

        if (string.IsNullOrEmpty(culture)) return;

        if (CultureInfoManager.InvariantGlobalization || CultureInfoManager.SupportedCultures.Any(sc => string.Equals(sc.Culture.Name, culture, StringComparison.InvariantCultureIgnoreCase)) is false)
        {
            // Because Blazor router doesn't support regex, the '/{culture?}/' captures some irrelevant routes
            // such as non existing routes like /some-invalid-url, we need to make sure that the first segment of the route is a valid culture name.
            NavigationManager.NavigateTo($"{PageUrls.NotFound}?url={Uri.EscapeDataString(NavigationManager.GetRelativePath())}", replace: true);
        }
    }
}
