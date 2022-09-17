using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace TodoTemplate.Client.Components;

public partial class AppDataAnnotationsValidator : AppComponentBase, IDisposable
{
    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    [AutoInject] private IServiceProvider _serviceProvider = default!;

    [AutoInject] private IStringLocalizerFactory _stringLocalizerFactory = default!;

    private ValidationMessageStore _validationMessageStore = default!;

    private ValidationContext _validationContext = default!;

    private Dictionary<string, string> _displayColumns = new();

    private IStringLocalizer _localizer = default!;

    protected override async Task OnInitAsync()
    {
        if (EditContext is null)
            throw new InvalidOperationException("EditContext is required");

        EditContext.OnValidationRequested += ValidationRequested;
        EditContext.OnFieldChanged += FieldChanged;

        _validationMessageStore = new ValidationMessageStore(EditContext);

        _validationContext = new ValidationContext(EditContext.Model, serviceProvider: _serviceProvider, items: null);

        _displayColumns = EditContext.Model
            .GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name);

        _localizer = StringLocalizerProvider.ProvideLocalizer(EditContext.Model.GetType(), _stringLocalizerFactory);

        base.OnInitialized();
    }

    void ValidationRequested(object sender, ValidationRequestedEventArgs args)
    {
        _validationMessageStore.Clear();

        var validationResults = new List<ValidationResult>();

        if (Validator.TryValidateObject(EditContext.Model, _validationContext, validationResults, validateAllProperties: true) is false)
        {
            foreach (var validationResult in validationResults)
            {
                string memberName = string.Join(".", validationResult.MemberNames);

                var fieldIdentifier = new FieldIdentifier(EditContext.Model, memberName);

                _validationMessageStore.Add(fieldIdentifier, _localizer.GetString(validationResult.ErrorMessage!, _localizer[_displayColumns[memberName]]));
            }
        }
    }

    void FieldChanged(object sender, FieldChangedEventArgs args)
    {
        FieldIdentifier fieldIdentifier = args.FieldIdentifier;

        _validationMessageStore.Clear(fieldIdentifier);

        var validationResults = new List<ValidationResult>();

        if (Validator.TryValidateObject(EditContext.Model, _validationContext, validationResults, validateAllProperties: true) is false)
        {
            foreach (var validationResultOfCurrentField in validationResults)
            {
                string memberName = string.Join(".", validationResultOfCurrentField.MemberNames);

                if (memberName != fieldIdentifier.FieldName)
                    continue;

                _validationMessageStore.Add(fieldIdentifier, _localizer.GetString(validationResultOfCurrentField.ErrorMessage!, _localizer[_displayColumns[memberName]]));
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
