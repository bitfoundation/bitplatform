namespace Boilerplate.Client.Core.Components;

public partial class AppBitParams
{
    private IBitComponentParams[] parameters = [];

    [AutoInject] private IStringLocalizer<BitBlazorUIStrings> bitBlazorUILocalizer = default!;


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
                OkText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DialogOkText)],
                CancelText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DialogCancelText)]
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
                    EmptyText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridEmptyText)],
                    LoadingText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridLoadingText)],
                    ActionsText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridActionsText)],
                    AddRowText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAddRowText)],
                    ClearFiltersText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridClearFiltersText)],
                    ExportCsvText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridExportCsvText)],
                    ExportExcelText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridExportExcelText)],
                    ColumnsText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridColumnsText)],
                    EditText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridEditText)],
                    DeleteText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridDeleteText)],
                    SaveText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridSaveText)],
                    CancelText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridCancelText)],
                    SelectAllLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridSelectAllLabel)],
                    SelectRowLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridSelectRowLabel)],
                    SelectRowFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridSelectRowFormat)],
                    FilterPlaceholder = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterPlaceholder)],
                    FilterByFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterByFormat)],
                    FilterAllText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterAllText)],
                    FilterOperatorLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterOperatorLabel)],
                    FilterOpContains = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterOpContains)],
                    FilterOpDoesNotContain = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterOpDoesNotContain)],
                    FilterOpStartsWith = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterOpStartsWith)],
                    FilterOpEndsWith = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFilterOpEndsWith)],
                    BooleanTrueText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridBooleanTrueText)],
                    BooleanFalseText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridBooleanFalseText)],
                    GroupByFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridGroupByFormat)],
                    UngroupByFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridUngroupByFormat)],
                    ExpandGroupFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridExpandGroupFormat)],
                    CollapseGroupFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridCollapseGroupFormat)],
                    PagerRangeFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridPagerRangeFormat)],
                    PagerPageFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridPagerPageFormat)],
                    PerPageFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridPerPageFormat)],
                    RowsPerPageLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridRowsPerPageLabel)],
                    FirstPageLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridFirstPageLabel)],
                    PreviousPageLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridPreviousPageLabel)],
                    NextPageLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridNextPageLabel)],
                    LastPageLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridLastPageLabel)],
                    EndOfResultsText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridEndOfResultsText)],
                    ToggleDetailsLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridToggleDetailsLabel)],
                    ToggleChildrenLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridToggleChildrenLabel)],
                    ReorderRowTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridReorderRowTitle)],
                    ReorderRowLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridReorderRowLabel)],
                    InvalidValueError = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridInvalidValueError)],
                    AnnouncementSortedAscending = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementSortedAscending)],
                    AnnouncementSortedDescending = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementSortedDescending)],
                    AnnouncementSortCleared = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementSortCleared)],
                    AnnouncementFiltered = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementFiltered)],
                    AnnouncementFilterCleared = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementFilterCleared)],
                    AnnouncementPage = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementPage)],
                    AnnouncementRowDeleted = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementRowDeleted)],
                    AnnouncementSearched = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementSearched)],
                    AnnouncementSearchCleared = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DataGridAnnouncementSearchCleared)]
                }
            }
        ];

        await base.OnInitAsync();
    }
}
