using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Forms;
using BlazorDual.Shared.Attributes;

namespace BlazorDual.Web.Components;

public partial class AppDataAnnotationsValidator : AppComponentBase, IDisposable
{
    private static readonly PropertyInfo _OtherPropertyNamePropertyInfo =
        typeof(CompareAttribute).GetProperty(nameof(CompareAttribute.OtherPropertyDisplayName))!;

    private bool _disposed;
    private ValidationMessageStore _validationMessageStore = default!;

    [AutoInject] private IServiceProvider _serviceProvider = default!;
    [AutoInject] private IStringLocalizerFactory _stringLocalizerFactory = default!;

    [CascadingParameter] private EditContext _editContext { get; set; } = default!;

    protected override Task OnInitAsync()
    {
        if (_editContext is null)
            throw new InvalidOperationException("EditContext is required");

        _editContext.OnFieldChanged += OnFieldChanged;
        _editContext.OnValidationRequested += OnValidationRequested;

        _validationMessageStore = new ValidationMessageStore(_editContext);

        return base.OnInitAsync();
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
    {
        var fieldIdentifier = eventArgs.FieldIdentifier;
        var propertyInfo = fieldIdentifier.Model.GetType().GetProperty(fieldIdentifier.FieldName);
        if (propertyInfo is null) return;

        var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
        var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null)
        {
            MemberName = propertyInfo.Name
        };
        var results = new List<ValidationResult>();

        var parent = propertyInfo.DeclaringType!;
        var dtoResourceTypeAttr = parent.GetCustomAttribute<DtoResourceTypeAttribute>();
        if (dtoResourceTypeAttr is null)
        {
            Validator.TryValidateProperty(propertyValue, validationContext, results);
        }
        else
        {
            var resourceType = dtoResourceTypeAttr.ResourceType;
            var stringLocalizer = _stringLocalizerFactory.Create(resourceType);
            var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();

            foreach (var attribute in validationAttributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.ErrorMessageResourceName) is false && attribute.ErrorMessageResourceType is null)
                {
                    attribute.ErrorMessageResourceType = resourceType;
                    var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
                    validationContext.DisplayName = stringLocalizer.GetString(displayAttribute?.Name ?? propertyInfo.Name);

                    if (attribute is CompareAttribute compareAttribute)
                    {
                        var otherPropertyInfoDisplayAttribute = (parent.GetProperty(compareAttribute.OtherProperty) ?? throw new InvalidOperationException($"Invalid OtherProperty {compareAttribute.OtherProperty}")).GetCustomAttribute<DisplayAttribute>();
                        _OtherPropertyNamePropertyInfo.SetValue(attribute, stringLocalizer.GetString(otherPropertyInfoDisplayAttribute?.Name ?? compareAttribute.OtherProperty).ToString());
                    }
                }

                var result = attribute.GetValidationResult(propertyValue, validationContext);

                if (result is not null)
                {
                    results.Add(result);
                }
            }

        }

        _validationMessageStore.Clear(fieldIdentifier);

        foreach (var result in CollectionsMarshal.AsSpan(results))
        {
            _validationMessageStore.Add(fieldIdentifier, result.ErrorMessage!);
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
        var results = new List<ValidationResult>();

        var objectType = validationContext.ObjectType;
        var objectInstance = validationContext.ObjectInstance;
        var dtoResourceTypeAttr = objectType.GetCustomAttribute<DtoResourceTypeAttribute>();

        _validationMessageStore.Clear();

        if (dtoResourceTypeAttr is null)
        {
            Validator.TryValidateObject(_editContext.Model, validationContext, results, true);
        }
        else
        {
            var resourceType = dtoResourceTypeAttr.ResourceType;

            var stringLocalizer = _stringLocalizerFactory.Create(resourceType);

            var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var propertyInfo in properties)
            {
                var context = new ValidationContext(objectInstance, validationContext, validationContext.Items) { MemberName = propertyInfo.Name };
                var propertyValue = propertyInfo.GetValue(objectInstance);
                var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();
                foreach (var attribute in validationAttributes)
                {
                    if (string.IsNullOrWhiteSpace(attribute.ErrorMessageResourceName) is false && attribute.ErrorMessageResourceType is null)
                    {
                        attribute.ErrorMessageResourceType = resourceType;
                        var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
                        validationContext.DisplayName = stringLocalizer.GetString(displayAttribute?.Name ?? propertyInfo.Name);
                        if (attribute is CompareAttribute compareAttribute)
                        {
                            var otherPropertyInfoDisplayAttribute = (properties.FirstOrDefault(p => p.Name == compareAttribute.OtherProperty)
                                ?? throw new InvalidOperationException($"Invalid OtherProperty {compareAttribute.OtherProperty}")).GetCustomAttribute<DisplayAttribute>();

                            _OtherPropertyNamePropertyInfo.SetValue(attribute, stringLocalizer.GetString(otherPropertyInfoDisplayAttribute?.Name
                                ?? compareAttribute.OtherProperty).ToString());
                        }
                    }

                    var result = attribute.GetValidationResult(propertyValue, context);

                    if (result is not null)
                    {
                        results.Add(result);
                    }
                }
            }
        }

        _validationMessageStore.Clear();

        foreach (var validationResult in results)
        {
            if (validationResult == null) continue;

            var hasMemberNames = false;

            foreach (var memberName in validationResult.MemberNames)
            {
                hasMemberNames = true;
                _validationMessageStore.Add(_editContext.Field(memberName), validationResult.ErrorMessage!);
            }

            if (hasMemberNames) continue;

            _validationMessageStore.Add(new FieldIdentifier(_editContext.Model, fieldName: string.Empty), validationResult.ErrorMessage!);
        }

        _editContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_editContext is not null)
        {
            _editContext.OnFieldChanged -= OnFieldChanged;
            _editContext.OnValidationRequested -= OnValidationRequested;
        }

        _disposed = true;
    }
}
