using Microsoft.AspNetCore.Components.Forms;

namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings;

/// <summary>
/// The dialog that adds or edits a building. The draft is kept in <c>BuildingsStore</c> as it is typed, so a reload or
/// a screenshot shows the dialog open with the same values.
/// </summary>
public partial class BuildingEditor
{
    private bool isOpen;
    private bool isNew;
    private bool isSaving;
    private Building? model;
    private EditContext? editContext;
    private ValidationMessageStore? messages;
    private List<BitDropdownItem<DutchProvince>> provinceItems = [];
    private List<BitDropdownItem<BuildingType>> typeItems = [];

    [AutoInject] private BuildingsStore store = default!;
    [AutoInject] private BuildingsService buildingsService = default!;

    [Parameter] public EventCallback<Building> OnSaved { get; set; }

    protected override void OnInitialized()
    {
        provinceItems = [.. Enum.GetValues<DutchProvince>().Select(p => new BitDropdownItem<DutchProvince> { Value = p, Text = Localizer[$"Province{p}"] }).OrderBy(i => i.Text)];
        typeItems = [.. Enum.GetValues<BuildingType>().Select(t => new BitDropdownItem<BuildingType> { Value = t, Text = Localizer[$"BuildingType{t}"] })];

        // Reopens the dialog a reload or a screenshot found open.
        if (store.Draft is { } draft)
        {
            Begin(draft, store.IsDraftNew);
        }
    }

    /// <summary>Opens the dialog on a copy of the building and keeps it as the draft.</summary>
    public Task Open(Building building, bool isNew)
    {
        Begin(building, isNew);
        store.Edit(building.Clone(), isNew);

        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Starts editing a copy of the building; every field change is kept as the store's draft.</summary>
    private void Begin(Building building, bool isNew)
    {
        this.isNew = isNew;
        model = building.Clone();
        editContext = new EditContext(model);
        messages = new ValidationMessageStore(editContext);
        editContext.OnValidationRequested += (_, _) => Validate();
        editContext.OnFieldChanged += (_, args) =>
        {
            messages.Clear(args.FieldIdentifier);
            editContext.NotifyValidationStateChanged();
            store.Edit(model.Clone(), isNew);
        };
        isOpen = true;
    }

    /// <summary>Checks the fields by hand, as <c>Building</c> carries no validation attributes.</summary>
    private void Validate()
    {
        if (model is null || messages is null || editContext is null)
            return;

        messages.Clear();

        if (string.IsNullOrWhiteSpace(model.Name))
            messages.Add(() => model.Name, Localizer[nameof(WorkbenchStrings.BuildingsNameRequired)]);

        if (string.IsNullOrWhiteSpace(model.City))
            messages.Add(() => model.City, Localizer[nameof(WorkbenchStrings.BuildingsCityRequired)]);

        if (model.Year is < 1000 || model.Year > DateTime.Now.Year)
            messages.Add(() => model.Year, Localizer[nameof(WorkbenchStrings.BuildingsYearInvalid)]);

        if (NetherlandsBoundary.Contains(model.Latitude, model.Longitude) is false)
            messages.Add(() => model.Latitude, Localizer[nameof(WorkbenchStrings.BuildingsOutsideBody)]);

        editContext.NotifyValidationStateChanged();
    }

    /// <summary>Validates, saves through the sample service and hands the saved building to the page.</summary>
    private async Task Submit()
    {
        if (model is null || editContext is null)
            return;

        //#if (framework == 'net11.0')
        if (await editContext.ValidateAsync(CurrentCancellationToken) is false)
            return;
        //#else
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        if (editContext.Validate() is false)
            return;
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif

        isSaving = true;

        try
        {
            var saved = await buildingsService.Save(model, CurrentCancellationToken);
            isOpen = false;
            store.CloseEditor();
            await OnSaved.InvokeAsync(saved);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isSaving = false;
        }
    }

    private void Close()
    {
        isOpen = false;
        store.CloseEditor();
    }
}
