namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts;

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

    [Parameter] public IReadOnlyList<Shift> Shifts { get; set; } = [];

    [Parameter] public Func<Shift, string?>? Validate { get; set; }

    [Parameter] public EventCallback<Shift> OnSaved { get; set; }

    [Parameter] public EventCallback<Shift> OnDeleted { get; set; }

    protected override void OnInitialized()
    {
        staffItems = [.. ShiftsService.Staff.Select(member => new BitDropdownItem<string> { Value = member.NameKey, Text = Localizer[member.NameKey] })];
        roleItems = [.. Enum.GetValues<ShiftRole>().Select(value => new BitDropdownItem<ShiftRole> { Value = value, Text = Localizer[$"ShiftsRole{value}"] })];

        if (store.Draft is { } draft)
        {
            Begin(draft, store.IsDraftNew);
        }
    }

    public Task Open(Shift shift, bool isNew)
    {
        Begin(shift, isNew);
        store.Edit(shift, isNew);

        return InvokeAsync(StateHasChanged);
    }

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

    private void SetStaff(string? key)
    {
        Update(() =>
        {
            staffKey = key;
            role = ShiftsService.Staff.FirstOrDefault(member => member.NameKey == key)?.Role ?? role;
        });
    }

    private void Update(Action change)
    {
        change();
        error = null;
        store.Edit(ToShift(), isNew);
    }

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

    private void Close()
    {
        isOpen = false;
        store.CloseEditor();
    }
}
