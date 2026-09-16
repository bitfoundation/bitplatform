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

    /// <summary>
    /// The event being edited, or <c>null</c> to add one. A recurring occurrence
    /// (<see cref="BitFullCalendarEvent.IsOccurrence"/>) is edited on its own: saving skips its date in
    /// the series and adds a one-off event with the edited fields in its place. To edit the whole
    /// series, pass the series master instead.
    /// </summary>
    [Parameter] public BitFullCalendarEvent? ExistingEvent { get; set; }
    [Parameter] public DateTime? StartDate { get; set; }
    [Parameter] public int? StartHour { get; set; }
    [Parameter] public int? StartMinute { get; set; }

    /// <summary>
    /// Length of the draft event in minutes. Supplied when the user selected a range on the time
    /// grid; without it a new event lasts one slot.
    /// </summary>
    [Parameter] public int? DurationMinutes { get; set; }

    [Parameter] public string? Resource { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    // Per-instance unique ids so multiple open dialogs don't collide on element ids, which would
    // break label-to-control association and the dialog's aria-labelledby reference.
    private readonly string _dialogTitleId = $"bfc-dlg-title-{Guid.NewGuid():N}";
    private readonly string _titleInputId = $"bfc-title-{Guid.NewGuid():N}";
    private readonly string _colorSelectId = $"bfc-color-{Guid.NewGuid():N}";
    private readonly string _resourceSelectId = $"bfc-resource-{Guid.NewGuid():N}";
    private readonly string _descriptionInputId = $"bfc-desc-{Guid.NewGuid():N}";
    private readonly string _repeatSelectId = $"bfc-repeat-{Guid.NewGuid():N}";
    private readonly string _intervalInputId = $"bfc-interval-{Guid.NewGuid():N}";
    private readonly string _positionSelectId = $"bfc-position-{Guid.NewGuid():N}";
    private readonly string _daysLabelId = $"bfc-days-{Guid.NewGuid():N}";
    private readonly string _endsSelectId = $"bfc-ends-{Guid.NewGuid():N}";

    private ElementReference _dialogRef;

    private bool _isEditing;
    private bool _isOccurrenceEdit;
    private bool _isSubmitting;
    private string _title = "";
    private string _description = "";
    private DateTime _startDate;
    private DateTime _endDate;
    private string _color = BitFullCalendarColorScheme.FallbackColorId;
    private string _resource = string.Empty;
    private bool _isAllDay;

    // The timed range the form held before "all day" snapped it onto whole-day boundaries, so
    // clearing the checkbox puts the hours back instead of leaving a midnight-to-midnight range.
    private (DateTime Start, DateTime End)? _timedRangeBeforeAllDay;
    private List<BitFullCalendarAttendee> _attendees = [];
    private string _newFirstName = "";
    private string _newLastName = "";
    private string _newId = "";
    private Dictionary<string, string> _errors = new();

    // The repeat rule as the form holds it; BuildRecurrence turns it back into a rule on save.
    private BitFullCalendarRecurrence? _originalRule;
    private BitFullCalendarRecurrenceFrequency? _frequency;
    private int _interval = 1;
    private HashSet<DayOfWeek> _repeatDays = [];
    private BitFullCalendarWeekOfMonth? _weekOfMonth;
    private RepeatEnd _repeatEnd;
    private bool _repeatEndTouched;
    private DateTime _until;
    private int _count = 10;
    private List<DateTime> _exceptionDates = [];
    private List<DateTime> _additionalDates = [];
    private DateTime _exceptionDraft;

    private bool _initialized;
    private BitFullCalendarEvent? _lastExistingEvent;
    private DateTime? _lastStartDate;
    private DateTime _lastSelectedDate;
    private int? _lastStartHour;
    private int? _lastStartMinute;
    private int? _lastDurationMinutes;
    private string? _lastResource;

    private enum RepeatEnd { Never, OnDate, AfterCount }

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
            || _lastDurationMinutes != DurationMinutes
            || _lastResource != Resource;

        if (!parametersChanged)
            return;

        _initialized = true;
        _lastExistingEvent = ExistingEvent;
        _lastStartDate = StartDate;
        _lastSelectedDate = State.SelectedDate;
        _lastStartHour = StartHour;
        _lastStartMinute = StartMinute;
        _lastDurationMinutes = DurationMinutes;
        _lastResource = Resource;

        // Clear transient editing state so a reused dialog instance doesn't carry over stale
        // validation errors or half-typed attendee draft inputs from a previous open.
        _errors = new();
        _newFirstName = "";
        _newLastName = "";
        _newId = "";
        _timedRangeBeforeAllDay = null;

        _isEditing = ExistingEvent != null;
        _isOccurrenceEdit = ExistingEvent?.IsOccurrence is true;
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
            _resource = ExistingEvent.Resource ?? string.Empty;
            _isAllDay = ExistingEvent.IsAllDay;
            _attendees = [.. ExistingEvent.Attendees];
        }
        else
        {
            _title = "";
            _description = "";
            _color = defaultColor;
            _resource = Resource ?? string.Empty;
            _isAllDay = false;
            _attendees = [];
            var baseDate = StartDate ?? State.SelectedDate;
            // With no slot to seed from, the draft opens at the calendar's start-of-day hour - the
            // same hour the external OnAddClick draft carries - rather than at the wall-clock hour,
            // which has nothing to do with the day being scheduled.
            _startDate = baseDate.Date.AddHours(StartHour ?? State.StartOfDayHour).AddMinutes(StartMinute ?? 0);
            // A range the user selected on the grid decides the length; without one a new event
            // lasts a single slot, so a 15-minute grid creates 15-minute events.
            _endDate = _startDate.AddMinutes(Math.Max(1, DurationMinutes ?? State.SlotDurationMinutes));
        }

        InitializeRepeat(_isEditing && _isOccurrenceEdit is false ? ExistingEvent!.Recurrence : null);
    }

    /// <summary>Loads the repeat fields from the rule being edited, or resets them for a one-off.</summary>
    private void InitializeRepeat(BitFullCalendarRecurrence? rule)
    {
        _originalRule = rule;
        _frequency = rule is not null && Enum.IsDefined(rule.Frequency) ? rule.Frequency : null;
        _interval = rule?.Interval ?? 1;
        _weekOfMonth = rule?.ResolveWeekOfMonth();
        _repeatDays = rule?.DaysOfWeek is { Count: > 0 } days ? days.Where(Enum.IsDefined).ToHashSet() : [];
        if (_repeatDays.Count == 0)
            _repeatDays.Add(_startDate.DayOfWeek);

        // A rule carrying both an end date and a count shows its count; the end date is kept on save
        // unless the user changes how the series ends (see BuildRecurrence).
        _repeatEnd = rule?.Count is not null ? RepeatEnd.AfterCount
                   : rule?.Until is not null ? RepeatEnd.OnDate
                   : RepeatEnd.Never;
        _repeatEndTouched = false;
        _count = rule?.Count is { } count && count > 0 ? count : 10;
        _until = rule?.Until?.Date ?? _startDate.Date.AddMonths(3);

        _exceptionDates = rule?.ExceptionDates?.Select(d => d.Date).Distinct().Order().ToList() ?? [];
        _additionalDates = rule?.AdditionalDates?.Select(d => d.Date).Distinct().Order().ToList() ?? [];
        _exceptionDraft = _startDate.Date;
    }

    private void OnFrequencyChanged(ChangeEventArgs e)
    {
        _frequency = Enum.TryParse<BitFullCalendarRecurrenceFrequency>(e.Value?.ToString(), out var frequency) && Enum.IsDefined(frequency)
            ? frequency
            : null;

        // Only a monthly or yearly series can sit on a weekday of the month.
        if (_frequency is not (BitFullCalendarRecurrenceFrequency.Monthly or BitFullCalendarRecurrenceFrequency.Yearly))
            _weekOfMonth = null;
    }

    private void OnWeekOfMonthChanged(ChangeEventArgs e)
    {
        var previous = _weekOfMonth;
        _weekOfMonth = Enum.TryParse<BitFullCalendarWeekOfMonth>(e.Value?.ToString(), out var week) && Enum.IsDefined(week)
            ? week
            : null;

        // Moving off the day number starts from the start date's own weekday, the one a "third
        // Tuesday" series opened on a Tuesday means, rather than whatever a weekly rule had selected.
        if (previous is null && _weekOfMonth is not null)
            _repeatDays = [_startDate.DayOfWeek];
    }

    private void ToggleRepeatDay(DayOfWeek day)
    {
        // A series needs at least one weekday, so the last one selected stays selected.
        if (_repeatDays.Contains(day))
        {
            if (_repeatDays.Count > 1)
                _repeatDays.Remove(day);
        }
        else
        {
            _repeatDays.Add(day);
        }
    }

    private void OnRepeatEndChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<RepeatEnd>(e.Value?.ToString(), out var end) && Enum.IsDefined(end))
        {
            _repeatEnd = end;
            _repeatEndTouched = true;
        }
    }

    private void OnUntilChanged(DateTime value) => _until = value.Date;

    private void OnExceptionDraftChanged(DateTime value) => _exceptionDraft = value.Date;

    private void SkipDraftDate()
    {
        // A date is either skipped or added, never both.
        _additionalDates.Remove(_exceptionDraft);
        if (_exceptionDates.Contains(_exceptionDraft) is false)
        {
            _exceptionDates.Add(_exceptionDraft);
            _exceptionDates.Sort();
        }
    }

    private void AddDraftDate()
    {
        _exceptionDates.Remove(_exceptionDraft);
        if (_additionalDates.Contains(_exceptionDraft) is false)
        {
            _additionalDates.Add(_exceptionDraft);
            _additionalDates.Sort();
        }
    }

    /// <summary>The seven weekdays in the order the calendar's own week runs.</summary>
    private IEnumerable<DayOfWeek> GetOrderedWeekDays()
    {
        var first = (int)State.FirstDayOfWeek;
        return Enumerable.Range(0, 7).Select(i => (DayOfWeek)((first + i) % 7));
    }

    private string GetIntervalUnit(BitFullCalendarRecurrenceFrequency frequency) => frequency switch
    {
        BitFullCalendarRecurrenceFrequency.Weekly => Texts.RepeatWeeksUnit,
        BitFullCalendarRecurrenceFrequency.Monthly => Texts.RepeatMonthsUnit,
        BitFullCalendarRecurrenceFrequency.Yearly => Texts.RepeatYearsUnit,
        _ => Texts.RepeatDaysUnit
    };

    /// <summary>The option that keeps a monthly or yearly series on the start date's own day.</summary>
    private string GetDayOfPeriodLabel(BitFullCalendarRecurrenceFrequency frequency)
        => frequency is BitFullCalendarRecurrenceFrequency.Yearly
            ? _startDate.ToString("M", State.Culture)
            : string.Format(State.Culture, Texts.RepeatOnDayOfMonthFormat, _startDate.Day);

    /// <summary>The rule the form describes, or <c>null</c> when the event does not repeat.</summary>
    private BitFullCalendarRecurrence? BuildRecurrence()
    {
        if (_isOccurrenceEdit || _frequency is not { } frequency)
            return null;

        var weekOfMonth = frequency is BitFullCalendarRecurrenceFrequency.Monthly or BitFullCalendarRecurrenceFrequency.Yearly
            ? _weekOfMonth
            : null;
        var usesDays = frequency is BitFullCalendarRecurrenceFrequency.Weekly || weekOfMonth is not null;

        return new BitFullCalendarRecurrence
        {
            Frequency = frequency,
            Interval = _interval,
            DaysOfWeek = usesDays ? [.. _repeatDays.OrderBy(d => (int)d)] : null,
            WeekOfMonth = weekOfMonth,
            Count = _repeatEnd is RepeatEnd.AfterCount ? _count : null,
            // The form shows one way of ending a series; a rule that had both keeps its end date
            // until the user picks how the series ends.
            Until = _repeatEnd is RepeatEnd.OnDate ? _until.Date
                  : _repeatEnd is RepeatEnd.AfterCount && _repeatEndTouched is false ? _originalRule?.Until
                  : null,
            ExceptionDates = _exceptionDates.Count > 0 ? [.. _exceptionDates] : null,
            AdditionalDates = _additionalDates.Count > 0 ? [.. _additionalDates] : null
        };
    }

    /// <summary>
    /// Turning "all day" on snaps the range onto whole-day boundaries so the saved event covers the
    /// dates it says it does; turning it off puts the times it replaced back, or falls back to a
    /// working time range when the dialog opened on an all-day event and there are none to put back.
    /// </summary>
    private void OnAllDayChanged(ChangeEventArgs e)
    {
        _isAllDay = e.Value is bool value && value;

        if (_isAllDay)
        {
            // Remembered so turning the checkbox back off restores the times it is about to discard.
            _timedRangeBeforeAllDay = (_startDate, _endDate);

            var start = _startDate.Date;
            // The last covered day is read the way the rest of the calendar reads it
            // (GetInclusiveEndDate): a range ending at 00:00 ends the previous day, so a 22:00-00:00
            // event becomes one all-day day rather than two. The end is then exclusive midnight of
            // the day after it.
            var lastDay = BitFullCalendarHelpers.GetInclusiveEndDate(_startDate, _endDate);
            if (lastDay < start)
                lastDay = start;
            var end = lastDay.AddDays(1);
            _startDate = start;
            _endDate = end;
        }
        else if (_timedRangeBeforeAllDay is { } timed)
        {
            _startDate = timed.Start;
            _endDate = timed.End;
            _timedRangeBeforeAllDay = null;
        }
        // An all-day range with no remembered times - one the dialog opened on - carries no hours to
        // restore. A single covered day becomes the shortest timed event the grid holds, starting at
        // the calendar's start-of-day hour; a longer range keeps the days it already spans.
        else if (_endDate <= _startDate.Date.AddDays(1))
        {
            _startDate = _startDate.Date.AddHours(State.StartOfDayHour);
            _endDate = _startDate.AddMinutes(Math.Max(1, State.SlotDurationMinutes));
        }
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

    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        // Escape is the standard way out of a modal. A save in flight is left alone so the dialog
        // can't be dismissed out from under the change it is committing.
        if (e.Key is "Escape" or "Esc" && _isSubmitting is false)
            await OnClose.InvokeAsync();
    }

    private Task OnStartDateChanged(DateTime value)
    {
        // Moving the start carries the end with it, the way every calendar editor behaves: the user
        // is rescheduling the event, not silently shortening it (or inverting the range and being
        // told so only on save). The end picker still sets the length independently.
        var duration = _endDate > _startDate
            ? _endDate - _startDate
            : TimeSpan.FromMinutes(Math.Max(1, State.SlotDurationMinutes));

        _startDate = value;
        _endDate = value + duration;
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
        // A description is optional unless the consumer asked for it (Settings.RequireEventDescription).
        if (State.RequireEventDescription && string.IsNullOrWhiteSpace(_description))
            _errors["description"] = Texts.ValidationDescriptionRequired;
        if (_endDate <= _startDate)
            _errors["endDate"] = Texts.ValidationEndAfterStart;

        if (_isOccurrenceEdit is false && _frequency is not null)
        {
            if (_interval < 1)
                _errors["interval"] = Texts.ValidationRepeatAtLeastOne;
            if (_repeatEnd is RepeatEnd.AfterCount && _count < 1)
                _errors["count"] = Texts.ValidationRepeatAtLeastOne;
            if (_repeatEnd is RepeatEnd.OnDate && _until.Date < _startDate.Date)
                _errors["until"] = Texts.ValidationUntilBeforeStart;
        }

        var resourceId = string.IsNullOrWhiteSpace(_resource) ? null : _resource;
        // An occurrence edited on its own is measured as part of its series, so the slot it is
        // leaving (still occupied by the series until the save commits) never counts as taken.
        var editingId = _isOccurrenceEdit ? ExistingEvent!.SeriesId ?? string.Empty
                      : _isEditing ? ExistingEvent!.Id
                      : string.Empty;
        // The save passes through the same gate a drop and a resize do, and says why next to the
        // action instead of silently creating what the calendar would refuse from a drag: an event
        // outside the navigable window could never be reached again, one outside the business hours
        // breaks the constraint the consumer asked for, and one on a taken slot is a double booking.
        if (_errors.Count == 0)
        {
            var refusal = State.ValidateRange(editingId, _startDate, _endDate, resourceId);
            switch (refusal)
            {
                case BitFullCalendarChangeRefusal.OutOfRange:
                    _errors["endDate"] = Texts.OutOfRangeMessage;
                    break;
                case BitFullCalendarChangeRefusal.OutsideBusinessHours:
                    _errors["overlap"] = Texts.OutsideBusinessHoursMessage;
                    break;
                case BitFullCalendarChangeRefusal.Overlap:
                    _errors["overlap"] = Texts.EventOverlapMessage;
                    break;
            }
        }

        if (_errors.Count > 0) return;

        _isSubmitting = true;
        try
        {
            if (_isOccurrenceEdit)
                await SaveOccurrenceAsync(resourceId);
            else
                await SaveEventAsync(resourceId);

            // Notification succeeded and the change is committed; post-notify callbacks run outside
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

    private BitFullCalendarEvent BuildEvent(string id, string? resourceId) => new()
    {
        Id = id,
        Title = _title,
        Description = _description,
        StartDate = _startDate,
        EndDate = _endDate,
        Color = _color,
        Resource = resourceId,
        Data = _isEditing ? ExistingEvent!.Data : null,
        Attendees = [.. _attendees],
        IsAllDay = _isAllDay,
        // An occurrence is read-only only so it cannot be dragged; the one-off it becomes is not.
        IsReadOnly = _isEditing && _isOccurrenceEdit is false && ExistingEvent!.IsReadOnly,
        CssClass = _isEditing ? ExistingEvent!.CssClass : null,
        Recurrence = BuildRecurrence()
    };

    private async Task SaveEventAsync(string? resourceId)
    {
        var oldSnapshot = _isEditing && ExistingEvent is not null
            ? BitFullCalendarChangeNotifier.CloneEvent(ExistingEvent)
            : null;

        var ev = BuildEvent(_isEditing ? ExistingEvent!.Id : Guid.NewGuid().ToString("N"), resourceId);

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

    /// <summary>
    /// Saves an occurrence edited on its own, leaving the rest of its series as it was: the series
    /// skips the occurrence's date (an Edit of the master) and a one-off event carrying the edited
    /// fields takes its place (an Add). Both are rolled back if either notification throws.
    /// </summary>
    private async Task SaveOccurrenceAsync(string? resourceId)
    {
        var occurrence = ExistingEvent!;
        var master = State.AllEvents.FirstOrDefault(e => string.Equals(e.Id, occurrence.SeriesId, StringComparison.Ordinal));
        // The series was removed while the dialog was open, so there is no occurrence left to change.
        if (master is null || occurrence.OccurrenceDate is not { } occurrenceDate)
            return;

        var masterSnapshot = BitFullCalendarChangeNotifier.CloneEvent(master);
        var updatedMaster = BitFullCalendarHelpers.SkipOccurrence(master, occurrenceDate);
        var detached = BuildEvent(Guid.NewGuid().ToString("N"), resourceId);

        State.UpdateEvent(updatedMaster);
        State.AddEvent(detached);

        try
        {
            await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
            {
                Event = BitFullCalendarChangeNotifier.CloneEvent(updatedMaster),
                OldEvent = masterSnapshot,
                Kind = BitFullCalendarChangeKind.Edit,
                Source = BitFullCalendarChangeSource.Dialog
            });
            await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
            {
                Event = BitFullCalendarChangeNotifier.CloneEvent(detached),
                Kind = BitFullCalendarChangeKind.Add,
                Source = BitFullCalendarChangeSource.Dialog
            });
        }
        catch
        {
            State.RemoveEvent(detached.Id);
            State.UpdateEvent(master);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        State.OnStateChanged -= HandleStateChanged;
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
