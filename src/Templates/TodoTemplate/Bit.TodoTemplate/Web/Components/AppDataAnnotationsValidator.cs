using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace TodoTemplate.App.Components;

public partial class AppDataAnnotationsValidator : AppComponentBase, IDisposable
{
    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    [AutoInject] private IServiceProvider _serviceProvider = default!;

    private ValidationMessageStore ValidationMessageStore = default!;

    private ValidationContext _validationContext = default!;

    private Dictionary<string, string> _displayColumns = new();

    protected override async Task OnInitAsync()
    {
        if (EditContext is null)
            throw new InvalidOperationException("EditContext is required");

        EditContext.OnValidationRequested += ValidationRequested;
        EditContext.OnFieldChanged += FieldChanged;

        ValidationMessageStore = new ValidationMessageStore(EditContext);

        _validationContext = new ValidationContext(EditContext.Model, serviceProvider: _serviceProvider, items: null);

        _displayColumns = EditContext.Model
            .GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name);

        base.OnInitialized();
    }

    void ValidationRequested(object sender, ValidationRequestedEventArgs args)
    {
        ValidationMessageStore.Clear();

        var validationResults = new List<ValidationResult>();

        if (Validator.TryValidateObject(EditContext.Model, _validationContext, validationResults, validateAllProperties: true) is false)
        {
            foreach (var validationResult in validationResults)
            {
                string memberName = string.Join(".", validationResult.MemberNames);

                var fieldIdentifier = new FieldIdentifier(EditContext.Model, memberName);

                ValidationMessageStore.Add(fieldIdentifier, Localizer.GetString(validationResult.ErrorMessage!, Localizer[_displayColumns[memberName]]));
            }
        }
    }

    void FieldChanged(object sender, FieldChangedEventArgs args)
    {
        FieldIdentifier fieldIdentifier = args.FieldIdentifier;

        ValidationMessageStore.Clear(fieldIdentifier);

        var validationResults = new List<ValidationResult>();

        if (Validator.TryValidateObject(EditContext.Model, _validationContext, validationResults, validateAllProperties: true) is false)
        {
            foreach (var validationResultOfCurrentField in validationResults)
            {
                string memberName = string.Join(".", validationResultOfCurrentField.MemberNames);

                if (memberName != fieldIdentifier.FieldName)
                    continue;

                ValidationMessageStore.Add(fieldIdentifier, Localizer.GetString(validationResultOfCurrentField.ErrorMessage!, Localizer[_displayColumns[memberName]]));
            }
        }
    }

    public void Dispose()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationRequested -= ValidationRequested;
            EditContext.OnFieldChanged -= FieldChanged;
        }
    }
}
