namespace Bit.BlazorUI;

public partial class BitFcAddEditEventDialog
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent? ExistingEvent { get; set; }
    [Parameter] public DateTime? StartDate { get; set; }
    [Parameter] public int? StartHour { get; set; }
    [Parameter] public int? StartMinute { get; set; }
    [Parameter] public string? Resource { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    // Per-instance unique ids so multiple open dialogs don't collide on element ids, which would
    // break label-to-control association and the dialog's aria-labelledby reference.
    private readonly string _dialogTitleId = $"bfc-dlg-title-{Guid.NewGuid():N}";
    private readonly string _titleInputId = $"bfc-title-{Guid.NewGuid():N}";
    private readonly string _colorSelectId = $"bfc-color-{Guid.NewGuid():N}";
    private readonly string _descriptionInputId = $"bfc-desc-{Guid.NewGuid():N}";

    private bool _isEditing;
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

    private bool _initialized;
    private BitFullCalendarEvent? _lastExistingEvent;
    private DateTime? _lastStartDate;
    private DateTime _lastSelectedDate;
    private int? _lastStartHour;
    private int? _lastStartMinute;
    private string? _lastResource;

    protected override void OnParametersSet()
    {
        // Re-run initialization whenever the parameters that drive the form change, so a reused
        // dialog instance reflects the new ExistingEvent / start parameters instead of stale values.
        // State.SelectedDate is tracked too because it is the fallback base date for a new event
        // when StartDate is null (see the non-editing branch below).
        var parametersChanged = !_initialized
            || !ReferenceEquals(_lastExistingEvent, ExistingEvent)
            || _lastStartDate != StartDate
            || _lastSelectedDate != State.SelectedDate
            || _lastStartHour != StartHour
            || _lastStartMinute != StartMinute
            || _lastResource != Resource;

        if (!parametersChanged)
            return;

        _initialized = true;
        _lastExistingEvent = ExistingEvent;
        _lastStartDate = StartDate;
        _lastSelectedDate = State.SelectedDate;
        _lastStartHour = StartHour;
        _lastStartMinute = StartMinute;
        _lastResource = Resource;

        // Clear transient editing state so a reused dialog instance doesn't carry over stale
        // validation errors or half-typed attendee draft inputs from a previous open.
        _errors = new();
        _newFirstName = "";
        _newLastName = "";
        _newId = "";

        _isEditing = ExistingEvent != null;
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
        _errors.Clear();
        if (string.IsNullOrWhiteSpace(_title))
            _errors["title"] = Texts.ValidationTitleRequired;
        if (string.IsNullOrWhiteSpace(_description))
            _errors["description"] = Texts.ValidationDescriptionRequired;
        if (_endDate <= _startDate)
            _errors["endDate"] = Texts.ValidationEndAfterStart;

        if (_errors.Count > 0) return;

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
            Attendees = [.. _attendees]
        };

        if (_isEditing)
            State.UpdateEvent(ev);
        else
            State.AddEvent(ev);

        await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
        {
            Event = BitFullCalendarChangeNotifier.CloneEvent(ev),
            OldEvent = oldSnapshot,
            Kind = _isEditing ? BitFullCalendarChangeKind.Edit : BitFullCalendarChangeKind.Add,
            Source = BitFullCalendarChangeSource.Dialog
        });

        await OnClose.InvokeAsync();
    }
}
