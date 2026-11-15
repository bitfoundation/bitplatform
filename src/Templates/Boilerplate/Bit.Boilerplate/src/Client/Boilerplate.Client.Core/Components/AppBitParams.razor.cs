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
            },
            // The pickers' own texts: their buttons, their tooltips and what a screen reader announces. The two date pickers
            // share the calendar's texts, and the time buttons of BitDatePicker share BitTimePicker's.
            new BitDatePickerParams
            {
                CalloutAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarAriaLabel)],
                GoToNextMonthTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToNextMonthTitle)],
                GoToPrevMonthTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToPrevMonthTitle)],
                GoToNextYearTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToNextYearTitle)],
                GoToPrevYearTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToPrevYearTitle)],
                GoToNextYearRangeTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToNextYearRangeTitle)],
                GoToPrevYearRangeTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToPrevYearRangeTitle)],
                GoToTodayTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToTodayTitle)],
                MonthPickerToggleTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarMonthPickerToggleTitle)],
                YearPickerToggleTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarYearPickerToggleTitle)],
                YearRangePickerToggleTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarYearRangePickerToggleTitle)],
                WeekNumbersHeaderTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarWeekNumbersHeaderTitle)],
                WeekNumberTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarWeekNumberTitle)],
                ShowTimePickerTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarShowTimePickerTitle)],
                HideTimePickerTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarHideTimePickerTitle)],
                ClearButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DatePickerClearButtonTitle)],
                CloseButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DatePickerCloseButtonTitle)],
                DateFormatAriaDescription = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DatePickerDateFormatAriaDescription)],
                HighlightedDateAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DatePickerHighlightedDateAriaLabel)],
                NowButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DatePickerNowButtonTitle)],
                SelectedDateAriaAtomic = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DatePickerSelectedDateAriaAtomic)],
                TimePickerIncreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeIncreaseHourTitle)],
                TimePickerDecreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeDecreaseHourTitle)],
                TimePickerIncreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeIncreaseMinuteTitle)],
                TimePickerDecreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeDecreaseMinuteTitle)],
                TimePickerIncreaseSecondTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeIncreaseSecondTitle)],
                TimePickerDecreaseSecondTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeDecreaseSecondTitle)],
                TimePickerHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeHourLabel)],
                TimePickerMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeMinuteLabel)],
                TimePickerSecondTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeSecondLabel)]
            },
            new BitDateRangePickerParams
            {
                CalloutAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarAriaLabel)],
                GoToNextMonthTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToNextMonthTitle)],
                GoToPrevMonthTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToPrevMonthTitle)],
                GoToNextYearTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToNextYearTitle)],
                GoToPrevYearTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToPrevYearTitle)],
                GoToNextYearRangeTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToNextYearRangeTitle)],
                GoToPrevYearRangeTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToPrevYearRangeTitle)],
                GoToTodayTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarGoToTodayTitle)],
                MonthPickerToggleTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarMonthPickerToggleTitle)],
                YearPickerToggleTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarYearPickerToggleTitle)],
                YearRangePickerToggleTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarYearRangePickerToggleTitle)],
                WeekNumbersHeaderTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarWeekNumbersHeaderTitle)],
                WeekNumberTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarWeekNumberTitle)],
                ShowTimePickerTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarShowTimePickerTitle)],
                HideTimePickerTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.CalendarHideTimePickerTitle)],
                ApplyButtonText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerApplyButtonText)],
                CancelButtonText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerCancelButtonText)],
                ClearButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerClearButtonTitle)],
                CloseButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerCloseButtonTitle)],
                PresetsAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerPresetsAriaLabel)],
                SelectedDateAriaAtomic = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerSelectedDateAriaAtomic)],
                StartTimeIncreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerStartTimeIncreaseHourTitle)],
                StartTimeDecreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerStartTimeDecreaseHourTitle)],
                StartTimeIncreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerStartTimeIncreaseMinuteTitle)],
                StartTimeDecreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerStartTimeDecreaseMinuteTitle)],
                EndTimeIncreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerEndTimeIncreaseHourTitle)],
                EndTimeDecreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerEndTimeDecreaseHourTitle)],
                EndTimeIncreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerEndTimeIncreaseMinuteTitle)],
                EndTimeDecreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerEndTimeDecreaseMinuteTitle)],
                StartTimeHourInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerStartHourLabel)],
                StartTimeMinuteInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerStartMinuteLabel)],
                EndTimeHourInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerEndHourLabel)],
                EndTimeMinuteInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerEndMinuteLabel)],
                ValueFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.DateRangePickerValueFormat)]
            },
            new BitTimePickerParams
            {
                CalloutAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimePickerCalloutAriaLabel)],
                ClearButtonText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimePickerClearButtonText)],
                ClearButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimePickerClearButtonTitle)],
                CloseButtonTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimePickerCloseButtonTitle)],
                NowButtonText = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimePickerNowButtonText)],
                IncreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeIncreaseHourTitle)],
                DecreaseHourTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeDecreaseHourTitle)],
                IncreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeIncreaseMinuteTitle)],
                DecreaseMinuteTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeDecreaseMinuteTitle)],
                IncreaseSecondTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeIncreaseSecondTitle)],
                DecreaseSecondTitle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeDecreaseSecondTitle)],
                HourInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeHourLabel)],
                MinuteInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeMinuteLabel)],
                SecondInputAriaLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.TimeSecondLabel)]
            }
        ];

        await base.OnInitAsync();
    }
}
