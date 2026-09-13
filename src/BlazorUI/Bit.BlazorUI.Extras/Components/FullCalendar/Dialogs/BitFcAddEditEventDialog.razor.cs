using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitFcAddEditEventDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent? ExistingEvent { get; set; }
    [Parameter] public DateTime? StartDate { get; set; }
    [Parameter] public int? StartHour { get; set; }
    [Parameter] public int? StartMinute { get; set; }
    [Parameter] public string? Resource { get; set; }

    /// <summary>
    /// When <see cref="ExistingEvent"/> is an occurrence, which part of its series the save applies to.
    /// Defaults to <see cref="BitFullCalendarRecurrenceEditScope.ThisEvent"/>, which hides the repeat fields:
    /// an occurrence edited on its own stops repeating.
    /// </summary>
    [Parameter] public BitFullCalendarRecurrenceEditScope? RecurrenceScope { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private enum MonthPatternKind
    {
        DayOfMonth,
        Weekday,
        LastWeekday
    }

    private enum RecurrenceEndKind
    {
        Never,
        OnDate,
        AfterCount
    }

    // Per-instance unique ids so multiple open dialogs don't collide on element ids, which would
    // break label-to-control association and the dialog's aria-labelledby reference.
    private readonly string _dialogTitleId = $"bfc-dlg-title-{Guid.NewGuid():N}";
    private readonly string _titleInputId = $"bfc-title-{Guid.NewGuid():N}";
    private readonly string _colorSelectId = $"bfc-color-{Guid.NewGuid():N}";
    private readonly string _descriptionInputId = $"bfc-desc-{Guid.NewGuid():N}";
    private readonly string _repeatSelectId = $"bfc-repeat-{Guid.NewGuid():N}";
    private readonly string _intervalInputId = $"bfc-interval-{Guid.NewGuid():N}";
    private readonly string _daysLabelId = $"bfc-days-{Guid.NewGuid():N}";
    private readonly string _patternSelectId = $"bfc-pattern-{Guid.NewGuid():N}";
    private readonly string _endsSelectId = $"bfc-ends-{Guid.NewGuid():N}";

    private ElementReference _dialogRef;

    private bool _isEditing;
    private bool _isOccurrence;
    private BitFullCalendarRecurrenceEditScope _scope;
    private bool _isSubmitting;
    private string _title = "";
    private string _description = "";
    private DateTime _startDate;
    private DateTime _endDate;
    private string _color = BitFullCalendarColorScheme.FallbackColorId;
    private List<BitFullCalendarAttendee> _attendees = [];
    private string _newFirstName = "";
    private string _newLastName = "";
    private string _newId = "";
    private Dictionary<string, string> _errors = new();

    private BitFullCalendarRecurrenceFrequency? _frequency;
    private int _interval = 1;
    private List<DayOfWeek> _daysOfWeek = [];
    private MonthPatternKind _monthPattern;
    private RecurrenceEndKind _end;
    private DateTime _until;
    private int _count = 10;
    private List<DateTime> _exceptionDates = [];
    private List<DateTime> _additionalDates = [];
    private DateTime _newExceptionDate;
    private DateTime _newAdditionalDate;

    private bool _initialized;
    private BitFullCalendarEvent? _lastExistingEvent;
    private DateTime? _lastStartDate;
    private DateTime _lastSelectedDate;
    private int? _lastStartHour;
    private int? _lastStartMinute;
    private string? _lastResource;
    private BitFullCalendarRecurrenceEditScope? _lastRecurrenceScope;

    /// <summary>An occurrence edited on its own leaves its series, so it has no rule of its own to edit.</summary>
    private bool ShowRecurrence => _isOccurrence is false || _scope != BitFullCalendarRecurrenceEditScope.ThisEvent;

    private string FrequencyValue
    {
        get => _frequency?.ToString() ?? "";
        set
        {
            _frequency = Enum.TryParse<BitFullCalendarRecurrenceFrequency>(value, out var frequency) ? frequency : null;
            if (_frequency == BitFullCalendarRecurrenceFrequency.Weekly && _daysOfWeek.Count == 0)
            {
                _daysOfWeek.Add(_startDate.DayOfWeek);
            }
        }
    }

    private int Interval
    {
        get => _interval;
        set => _interval = Math.Max(1, value);
    }

    private int Count
    {
        get => _count;
        set => _count = Math.Max(1, value);
    }

    private IEnumerable<DayOfWeek> OrderedWeekdays
    {
        get
        {
            var first = (int)State.Culture.DateTimeFormat.FirstDayOfWeek;
            return Enumerable.Range(0, 7).Select(i => (DayOfWeek)((first + i) % 7));
        }
    }

    private int StartDayOfMonth => State.Culture.Calendar.GetDayOfMonth(_startDate);

    /// <summary>The week of the month the start falls in, or <c>null</c> for a fifth weekday, which only "last" describes.</summary>
    private BitFullCalendarRecurrenceWeekOfMonth? StartWeekOfMonth
        => (StartDayOfMonth - 1) / 7 is var week && week < 4 ? (BitFullCalendarRecurrenceWeekOfMonth)week : null;

    private bool StartIsInLastWeek
    {
        get
        {
            var calendar = State.Culture.Calendar;
            var monthStart = _startDate.Date.AddDays(1 - StartDayOfMonth);
            var daysInMonth = (calendar.AddMonths(monthStart, 1) - monthStart).Days;
            return StartDayOfMonth + 7 > daysInMonth;
        }
    }

    /// <summary>
    /// The monthly pattern, derived from the start date: a pattern the start no longer fits (a moved start
    /// that is no longer in the last week, or is now a fifth weekday) falls back to one it does.
    /// </summary>
    private MonthPatternKind MonthPattern
    {
        get => _monthPattern switch
        {
            MonthPatternKind.Weekday when StartWeekOfMonth is null => StartIsInLastWeek ? MonthPatternKind.LastWeekday : MonthPatternKind.DayOfMonth,
            MonthPatternKind.LastWeekday when StartIsInLastWeek is false => StartWeekOfMonth is null ? MonthPatternKind.DayOfMonth : MonthPatternKind.Weekday,
            _ => _monthPattern
        };
        set => _monthPattern = value;
    }

    protected override void OnInitialized() => State.OnStateChanged += HandleStateChanged;

    /// <summary>
    /// The calendar can be switched to read-only while this dialog is open - every entry point only
    /// checks read-only when it opens the dialog, so an already-open form would otherwise stay live.
    /// Close it instead of leaving a Save button that <see cref="Submit"/> refuses.
    /// </summary>
    private void HandleStateChanged()
    {
        if (State.ReadOnly is false)
            return;

        _ = InvokeAsync(OnClose.InvokeAsync);
    }

    protected override void OnParametersSet()
    {
        // Re-run initialization whenever the parameters that drive the form change, so a reused
        // dialog instance reflects the new ExistingEvent / start parameters instead of stale values.
        // State.SelectedDate is only the fallback base date for a NEW event when no explicit
        // StartDate is supplied (see the non-editing branch below). A selected-date change must
        // therefore only force a reset while the dialog is actually using that fallback source -
        // never while editing an ExistingEvent or when an explicit StartDate was provided, otherwise
        // an unrelated calendar navigation would clobber the in-progress form.
        var usesFallbackDate = ExistingEvent is null && StartDate is null;
        var selectedDateChanged = usesFallbackDate && _lastSelectedDate != State.SelectedDate;
        var parametersChanged = !_initialized
            || !ReferenceEquals(_lastExistingEvent, ExistingEvent)
            || _lastStartDate != StartDate
            || selectedDateChanged
            || _lastStartHour != StartHour
            || _lastStartMinute != StartMinute
            || _lastResource != Resource
            || _lastRecurrenceScope != RecurrenceScope;

        if (!parametersChanged)
            return;

        _initialized = true;
        _lastExistingEvent = ExistingEvent;
        _lastStartDate = StartDate;
        _lastSelectedDate = State.SelectedDate;
        _lastStartHour = StartHour;
        _lastStartMinute = StartMinute;
        _lastResource = Resource;
        _lastRecurrenceScope = RecurrenceScope;

        // Clear transient editing state so a reused dialog instance doesn't carry over stale
        // validation errors or half-typed attendee draft inputs from a previous open.
        _errors = new();
        _newFirstName = "";
        _newLastName = "";
        _newId = "";

        _isEditing = ExistingEvent != null;
        _isOccurrence = ExistingEvent?.IsOccurrence is true;
        _scope = RecurrenceScope ?? BitFullCalendarRecurrenceEditScope.ThisEvent;
        var defaultColor = ColorScheme.Options.Count > 0
            ? ColorScheme.Options[0].Id
            : BitFullCalendarColorScheme.FallbackColorId;

        if (_isEditing)
        {
            _title = ExistingEvent!.Title;
            _description = ExistingEvent.Description;
            _startDate = ExistingEvent.StartDate;
            _endDate = ExistingEvent.EndDate;
            _color = string.IsNullOrWhiteSpace(ExistingEvent.Color) ? defaultColor : ExistingEvent.Color;
            _attendees = [.. ExistingEvent.Attendees];
        }
        else
        {
            _title = "";
            _description = "";
            _color = defaultColor;
            _attendees = [];
            var baseDate = StartDate ?? State.SelectedDate;
            _startDate = baseDate.Date.AddHours(StartHour ?? DateTime.Now.Hour).AddMinutes(StartMinute ?? 0);
            _endDate = _startDate.AddMinutes(30);
        }

        // An occurrence carries the rule of its series.
        LoadRecurrence(ExistingEvent?.Recurrence);
    }

    private void LoadRecurrence(BitFullCalendarRecurrence? rule)
    {
        _frequency = rule?.Frequency;
        _interval = rule?.Interval ?? 1;
        _daysOfWeek = rule?.DaysOfWeek.Where(d => Enum.IsDefined(d)).Distinct().ToList() ?? [];
        if (_daysOfWeek.Count == 0)
        {
            _daysOfWeek.Add(_startDate.DayOfWeek);
        }

        _monthPattern = rule?.WeekOfMonth switch
        {
            null => MonthPatternKind.DayOfMonth,
            BitFullCalendarRecurrenceWeekOfMonth.Last => MonthPatternKind.LastWeekday,
            _ => MonthPatternKind.Weekday
        };

        _end = rule?.Until is not null
            ? RecurrenceEndKind.OnDate
            : rule?.Count is not null ? RecurrenceEndKind.AfterCount : RecurrenceEndKind.Never;
        _until = rule?.Until?.Date ?? State.Culture.Calendar.AddMonths(_startDate.Date, 1);
        _count = rule?.Count ?? 10;

        _exceptionDates = rule is null ? [] : [.. rule.ExceptionDates.Select(d => d.Date).Distinct().Order()];
        _additionalDates = rule is null ? [] : [.. rule.AdditionalDates.Select(d => d.Date).Distinct().Order()];
        _newExceptionDate = _startDate.Date;
        _newAdditionalDate = _startDate.Date;
    }

    private BitFullCalendarRecurrence? BuildRecurrence()
    {
        if (_frequency is not { } frequency)
            return null;

        var rule = new BitFullCalendarRecurrence
        {
            Frequency = frequency,
            Interval = _interval,
            ExceptionDates = [.. _exceptionDates],
            AdditionalDates = [.. _additionalDates]
        };

        if (frequency == BitFullCalendarRecurrenceFrequency.Weekly)
        {
            rule.DaysOfWeek = [.. OrderedWeekdays.Where(_daysOfWeek.Contains)];
        }
        else if (frequency is BitFullCalendarRecurrenceFrequency.Monthly or BitFullCalendarRecurrenceFrequency.Yearly)
        {
            switch (MonthPattern)
            {
                case MonthPatternKind.Weekday:
                    rule.WeekOfMonth = StartWeekOfMonth;
                    rule.DaysOfWeek = [_startDate.DayOfWeek];
                    break;
                case MonthPatternKind.LastWeekday:
                    rule.WeekOfMonth = BitFullCalendarRecurrenceWeekOfMonth.Last;
                    rule.DaysOfWeek = [_startDate.DayOfWeek];
                    break;
            }
        }

        if (_end == RecurrenceEndKind.OnDate)
        {
            rule.Until = _until.Date;
        }
        else if (_end == RecurrenceEndKind.AfterCount)
        {
            rule.Count = _count;
        }

        return rule;
    }

    private string DescribeMonthPattern(BitFullCalendarRecurrenceFrequency frequency, BitFullCalendarRecurrenceWeekOfMonth? week)
    {
        var text = BitFullCalendarHelpers.DescribeRecurrenceMonthPattern(frequency, week, [_startDate.DayOfWeek], _startDate, Texts, State.Culture);
        return BitFullCalendarHelpers.Capitalize(text, State.Culture);
    }

    private void ToggleDay(DayOfWeek day)
    {
        if (_daysOfWeek.Remove(day) is false)
        {
            _daysOfWeek.Add(day);
        }
    }

    private void AddExceptionDate() => AddDate(_exceptionDates, _newExceptionDate);

    private void AddAdditionalDate() => AddDate(_additionalDates, _newAdditionalDate);

    private static void AddDate(List<DateTime> dates, DateTime value)
    {
        var date = value.Date;
        if (dates.Contains(date))
            return;

        dates.Add(date);
        dates.Sort();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Move focus into the dialog and trap Tab navigation once it has rendered; teardown in
        // DisposeAsync restores focus to the element that was focused before it opened. Mirrors
        // BitFcEventDetailsDialog so the add/edit dialog behaves like a true modal.
        if (firstRender)
            await BitFcDialogInterop.SetupAsync(JS, _dialogRef);
    }

    private void AddAttendee()
    {
        _errors.Remove("attendee");

        if (string.IsNullOrWhiteSpace(_newFirstName) && string.IsNullOrWhiteSpace(_newLastName))
        {
            _errors["attendee"] = Texts.ValidationAttendeeNameRequired;
            return;
        }

        _attendees.Add(new BitFullCalendarAttendee
        {
            FirstName = _newFirstName.Trim(),
            LastName = _newLastName.Trim(),
            Id = string.IsNullOrWhiteSpace(_newId) ? null : _newId.Trim()
        });

        _newFirstName = "";
        _newLastName = "";
        _newId = "";
    }

    private void RemoveAttendee(BitFullCalendarAttendee attendee) => _attendees.Remove(attendee);

    private Task OnStartDateChanged(DateTime value)
    {
        _startDate = value;
        return Task.CompletedTask;
    }

    private Task OnEndDateChanged(DateTime value)
    {
        _endDate = value;
        return Task.CompletedTask;
    }

    private async Task Submit()
    {
        // Last line of defense for every host of this dialog (add entry points and the details
        // dialog's edit overlay): read-only may have been switched on after the dialog opened, so
        // refuse the save rather than mutating state the calendar no longer allows to change.
        if (State.ReadOnly) return;

        // Guard against re-entrancy: a second click or Enter press while the first save is still
        // in flight would otherwise add/update the event twice before the dialog closes.
        if (_isSubmitting) return;

        _errors.Clear();
        if (string.IsNullOrWhiteSpace(_title))
            _errors["title"] = Texts.ValidationTitleRequired;
        if (string.IsNullOrWhiteSpace(_description))
            _errors["description"] = Texts.ValidationDescriptionRequired;
        if (_endDate <= _startDate)
            _errors["endDate"] = Texts.ValidationEndAfterStart;
        if (ShowRecurrence && _frequency == BitFullCalendarRecurrenceFrequency.Weekly && _daysOfWeek.Count == 0)
            _errors["days"] = Texts.ValidationRecurrenceDaysRequired;
        if (ShowRecurrence && _frequency is not null && _end == RecurrenceEndKind.OnDate && _until.Date < _startDate.Date)
            _errors["until"] = Texts.ValidationRecurrenceUntilAfterStart;

        if (_errors.Count > 0) return;

        _isSubmitting = true;
        try
        {
            var recurrence = ShowRecurrence ? BuildRecurrence() : null;

            if (_isOccurrence)
            {
                var updated = new BitFullCalendarEvent
                {
                    Id = ExistingEvent!.Id,
                    Title = _title,
                    Description = _description,
                    StartDate = _startDate,
                    EndDate = _endDate,
                    Color = _color,
                    Resource = ExistingEvent.Resource,
                    Data = ExistingEvent.Data,
                    Attendees = [.. _attendees],
                    Recurrence = recurrence
                };

                // The series decides what an occurrence edit becomes - an event of its own, a new series,
                // or an edit of the whole series - and CommitAsync rolls it all back if a notification throws.
                await Notifier.CommitAsync(State.BuildEditChanges(ExistingEvent, updated, _scope, BitFullCalendarChangeSource.Dialog));
            }
            else
            {
                await SaveEventAsync(recurrence);
            }

            // Notification succeeded and the event is committed; post-notify callbacks run outside
            // the compensation scope so an OnSaved/OnClose exception does not roll back the change.
            // Prefer the dedicated success path when provided (e.g. the details dialog closes itself
            // only on a real save), otherwise fall back to OnClose for standalone add/edit usages.
            if (OnSaved.HasDelegate)
                await OnSaved.InvokeAsync();
            else
                await OnClose.InvokeAsync();
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task SaveEventAsync(BitFullCalendarRecurrence? recurrence)
    {
        var oldSnapshot = _isEditing && ExistingEvent is not null
            ? BitFullCalendarChangeNotifier.CloneEvent(ExistingEvent)
            : null;

        var ev = new BitFullCalendarEvent
        {
            Id = _isEditing ? ExistingEvent!.Id : Guid.NewGuid().ToString("N"),
            Title = _title,
            Description = _description,
            StartDate = _startDate,
            EndDate = _endDate,
            Color = _color,
            Resource = _isEditing ? ExistingEvent!.Resource : Resource,
            Data = _isEditing ? ExistingEvent!.Data : null,
            Attendees = [.. _attendees],
            Recurrence = recurrence
        };

        if (_isEditing)
            State.UpdateEvent(ev);
        else
            State.AddEvent(ev);

        try
        {
            await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
            {
                Event = BitFullCalendarChangeNotifier.CloneEvent(ev),
                OldEvent = oldSnapshot,
                Kind = _isEditing ? BitFullCalendarChangeKind.Edit : BitFullCalendarChangeKind.Add,
                Source = BitFullCalendarChangeSource.Dialog
            });
        }
        catch
        {
            // Compensate so the dialog is safe to retry: a throwing notifier must not leave the
            // event committed to State, otherwise a second submit would add a duplicate (Add) or
            // the edit would be applied without its consumers ever being notified. Restore the
            // pre-submit snapshot on edit, or remove the just-added event on add. Only notifier
            // failures roll back - the event is committed once notification succeeds.
            if (_isEditing)
            {
                if (oldSnapshot is not null)
                    State.UpdateEvent(oldSnapshot);
            }
            else
            {
                State.RemoveEvent(ev.Id);
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        State.OnStateChanged -= HandleStateChanged;
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
