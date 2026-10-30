using Microsoft.AspNetCore.Components.Forms;

namespace Boilerplate.Workbench.Components.Pages.Showcase.Buildings;

public partial class BuildingEditor
{
    private readonly string maxYear = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);

    private bool isOpen;
    private bool isNew;
    private bool isSaving;
    private Building? model;
    private EditContext? editContext;
    private ValidationMessageStore? messages;
    private List<BitDropdownItem<DutchProvince>> provinceItems = [];
    private List<BitDropdownItem<BuildingType>> typeItems = [];

    [AutoInject] private BuildingsService buildingsService = default!;

    [Parameter] public EventCallback<Building> OnSaved { get; set; }

    protected override void OnInitialized()
    {
        provinceItems = [.. Enum.GetValues<DutchProvince>().Select(p => new BitDropdownItem<DutchProvince> { Value = p, Text = Localizer[$"Province{p}"] }).OrderBy(i => i.Text)];
        typeItems = [.. Enum.GetValues<BuildingType>().Select(t => new BitDropdownItem<BuildingType> { Value = t, Text = Localizer[$"BuildingType{t}"] })];
    }

    public Task Open(Building building, bool isNew)
    {
        this.isNew = isNew;
        model = building.Clone();
        editContext = new EditContext(model);
        messages = new ValidationMessageStore(editContext);
        editContext.OnValidationRequested += (_, _) => Validate();
        editContext.OnFieldChanged += (_, args) => { messages.Clear(args.FieldIdentifier); editContext.NotifyValidationStateChanged(); };
        isOpen = true;

        return InvokeAsync(StateHasChanged);
    }

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
            await OnSaved.InvokeAsync(saved);
        }
        finally
        {
            isSaving = false;
        }
    }

    private void Close()
    {
        isOpen = false;
    }
}
