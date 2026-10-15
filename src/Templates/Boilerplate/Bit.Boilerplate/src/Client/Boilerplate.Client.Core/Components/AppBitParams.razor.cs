namespace Boilerplate.Client.Core.Components;

public partial class AppBitParams
{
    private IBitComponentParams[] parameters = [];


    [Parameter] public RenderFragment? ChildContent { get; set; }


    protected override async Task OnInitAsync()
    {
        parameters =
        [
            new BitOtpInputParams
            {
                Length = 6,
                NormalizeDigits = true,
                Size = BitSize.Large,
                Type = BitInputType.Number,
                InputMode = BitInputMode.Tel
            },
            new BitNumberFieldParams { NormalizeDigits = true },
            new BitPhoneInputParams
            {
                FullWidth = true,
                DefaultCountry = BitCountries.Current
            },
            new BitMarkdownViewerParams
            {
                StripBidiControlCharacters = true,
                Pipeline = BitMarkdownPipelines.Advanced
            },
            new BitNavParams { Accent = BitColor.SecondaryBackground },
            new BitNavPanelParams { Accent = BitColor.SecondaryBackground },
            new BitDialogParams
            {
                AutoFocusButton = BitDialogButton.Cancel,
                OkText = Localizer[nameof(AppStrings.Yes)],
                CancelText = Localizer[nameof(AppStrings.No)]
            },
            new BitDataGridParams
            {
                Pageable = true,
                Filterable = true,
                Resizable = true,
                Reorderable = true,
                ShowToolbar = true,
                ShowColumnChooser = true,
                ShowCsvExport = true,
                ShowExcelExport = true,
                PageSize = 10,
                Strings = new()
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
                    AnnouncementRowDeleted = Localizer[nameof(AppStrings.DataGridAnnouncementRowDeleted)],
                    AnnouncementSearched = Localizer[nameof(AppStrings.DataGridAnnouncementSearched)],
                    AnnouncementSearchCleared = Localizer[nameof(AppStrings.DataGridAnnouncementSearchCleared)]
                }
            }
        ];

        await base.OnInitAsync();
    }
}
