namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts;

/// <summary>
/// The dialog that adds or edits a shift. Each change is kept as the store's draft, and Save checks the times, the
/// calendar's rules and the person's other shifts before handing the shift to the page.
/// </summary>
public partial class ShiftEditor
{
    private bool isOpen;
    private bool isNew;
    private string? id;
    private string? staffKey;
    private string? title;
    private string? titleKey;
    private ShiftRole role;
    private DateTimeOffset? day;
    private TimeSpan? start;
    private TimeSpan? end;
    private string? error;
    private List<BitDropdownItem<string>> staffItems = [];
    private List<BitDropdownItem<ShiftRole>> roleItems = [];

    [AutoInject] private ShiftsStore store = default!;

    /// <summary>The planner's shifts, to refuse one that overlaps the same person's.</summary>
    [Parameter] public IReadOnlyList<Shift> Shifts { get; set; } = [];

    /// <summary>The calendar's check of opening hours and blocked times: a message, or null when the shift fits.</summary>
    [Parameter] public Func<Shift, string?>? Validate { get; set; }

    [Parameter] public EventCallback<Shift> OnSaved { get; set; }

    [Parameter] public EventCallback<Shift> OnDeleted { get; set; }

    protected override async Task OnInitializedAsync()
    {
        staffItems = [.. ShiftsService.Staff.Select(member => new BitDropdownItem<string> { Value = member.NameKey, Text = Localizer[member.NameKey] })];
        roleItems = [.. Enum.GetValues<ShiftRole>().Select(value => new BitDropdownItem<ShiftRole> { Value = value, Text = Localizer[$"ShiftsRole{value}"] })];

        // The draft is read once the store holds what the browser saved.
        await WhenStateRestored();

        // Reopens on the draft the store kept.
        if (store.Draft is { } draft)
        {
            Begin(draft, store.IsDraftNew);
        }

        // A draft dropped elsewhere, like by a reset, closes the editor.
        OwnsSubscription(store.Subscribe(() => store.Draft, stored => OnStoredDraftChanged(stored)));
    }

    /// <summary>Closes the editor once its draft is gone from the store.</summary>
    private void OnStoredDraftChanged(Shift? draft)
    {
        if (draft is not null || isOpen is false)
            return;

        _ = InvokeAsync(() =>
        {
            isOpen = false;
            StateHasChanged();
        });
    }

    /// <summary>Opens the editor on a shift and keeps it as the draft.</summary>
    public Task Open(Shift shift, bool isNew)
    {
        Begin(shift, isNew);
        store.Edit(shift, isNew);

        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Fills the fields from a shift.</summary>
    private void Begin(Shift shift, bool isNew)
    {
        this.isNew = isNew;
        id = shift.Id;
        staffKey = shift.StaffKey;
        role = shift.Role;
        titleKey = shift.TitleKey;
        title = shift.Title ?? (shift.TitleKey is null ? null : Localizer[shift.TitleKey].Value);
        day = new DateTimeOffset(shift.Start.Date);
        start = shift.Start.TimeOfDay;
        end = shift.End.TimeOfDay;
        error = null;
        isOpen = true;
    }

    /// <summary>Picks the team member, and their usual role with them.</summary>
    private void SetStaff(string? key)
    {
        Update(() =>
        {
            staffKey = key;
            role = ShiftsService.Staff.FirstOrDefault(member => member.NameKey == key)?.Role ?? role;
        });
    }

    /// <summary>Applies a field's change and keeps the result as the draft.</summary>
    private void Update(Action change)
    {
        change();
        error = null;
        store.Edit(ToShift(), isNew);
    }

    /// <summary>The shift the fields describe; a sample title key stays while its text is unchanged.</summary>
    private Shift ToShift()
    {
        var date = (day ?? DateTimeOffset.Now).Date;
        var keepsTitleKey = titleKey is not null && title == Localizer[titleKey].Value;

        return new(id ?? Guid.NewGuid().ToString("N"),
                   staffKey ?? ShiftsService.Staff[0].NameKey,
                   role,
                   date + (start ?? TimeSpan.FromHours(ShiftsService.OpeningHour)),
                   date + (end ?? TimeSpan.FromHours(ShiftsService.ClosingHour)),
                   keepsTitleKey ? titleKey : null,
                   keepsTitleKey || string.IsNullOrWhiteSpace(title) ? null : title.Trim());
    }

    /// <summary>Saves unless the shift ends before it starts, breaks the calendar's rules or overlaps the person's other shifts.</summary>
    private async Task Save()
    {
        var shift = ToShift();

        if (shift.End <= shift.Start)
        {
            error = Localizer[nameof(WorkbenchStrings.ShiftsEndBeforeStart)];
            return;
        }

        if (Validate?.Invoke(shift) is { } refusal)
        {
            error = refusal;
            return;
        }

        if (Shifts.Any(other => other.Id != shift.Id && other.StaffKey == shift.StaffKey && other.Start < shift.End && shift.Start < other.End))
        {
            error = Localizer[nameof(WorkbenchStrings.ShiftsOverlap), Localizer[shift.StaffKey]];
            return;
        }

        isOpen = false;
        store.CloseEditor();
        await OnSaved.InvokeAsync(shift);
    }

    private async Task Delete()
    {
        var shift = ToShift();

        isOpen = false;
        store.CloseEditor();
        await OnDeleted.InvokeAsync(shift);
    }

    /// <summary>Closes without saving and drops the draft.</summary>
    private void Close()
    {
        isOpen = false;
        store.CloseEditor();
    }
}
