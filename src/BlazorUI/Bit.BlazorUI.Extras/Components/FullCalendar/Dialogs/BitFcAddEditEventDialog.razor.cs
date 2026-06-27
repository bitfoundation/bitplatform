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
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    // Per-instance unique ids so multiple open dialogs don't collide on element ids, which would
    // break label-to-control association and the dialog's aria-labelledby reference.
    private readonly string _dialogTitleId = $"bfc-dlg-title-{Guid.NewGuid():N}";
    private readonly string _titleInputId = $"bfc-title-{Guid.NewGuid():N}";
    private readonly string _colorSelectId = $"bfc-color-{Guid.NewGuid():N}";
    private readonly string _descriptionInputId = $"bfc-desc-{Guid.NewGuid():N}";

    private ElementReference _dialogRef;

    private bool _isEditing;
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

        if (_errors.Count > 0) return;

        _isSubmitting = true;
        try
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
                Attendees = [.. _attendees]
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

    public async ValueTask DisposeAsync()
    {
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
