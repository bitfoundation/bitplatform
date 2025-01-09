using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Attributes;
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Components.Forms;

namespace Boilerplate.Client.Core.Components;

/// <summary>
/// To implement forms where each error is displayed according to the language chosen by the user, you can use the <see cref="DtoResourceTypeAttribute"/>
/// on the corresponding class instead of using `ErrorResourceType` on each property. Check out <see cref="SignUpRequestDto"/> for an example.
/// However, you need to use <see cref="AppDataAnnotationsValidator"/> instead of <see cref="DataAnnotationsValidator"/> in Blazor EditForms for this method to work.
/// </summary>
public partial class AppDataAnnotationsValidator : AppComponentBase
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_OtherPropertyDisplayName")]
    static extern void SetOtherPropertyDisplayName(CompareAttribute valAttribute, string name);

    private bool disposed;
    private ValidationMessageStore validationMessageStore = default!;

    [AutoInject] private SnackBarService snackbarService = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IStringLocalizerFactory stringLocalizerFactory = default!;

    [CascadingParameter] public EditContext EditContext { get; set; } = default!;

    protected override Task OnInitAsync()
    {
        if (EditContext is null)
            throw new InvalidOperationException("EditContext is required");

        EditContext.OnFieldChanged += OnFieldChanged;
        EditContext.OnValidationRequested += OnValidationRequested;

        validationMessageStore = new ValidationMessageStore(EditContext);

        return base.OnInitAsync();
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
    {
        var fieldIdentifier = eventArgs.FieldIdentifier;
        var propertyInfo = fieldIdentifier.Model.GetType().GetProperty(fieldIdentifier.FieldName);
        if (propertyInfo is null) return;

        var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
        var propValidationContext = new ValidationContext(fieldIdentifier.Model, serviceProvider, items: null)
        {
            MemberName = propertyInfo.Name
        };
        var results = new List<ValidationResult>();

        var parent = propertyInfo.DeclaringType!;
        var dtoResourceTypeAttr = parent.GetCustomAttribute<DtoResourceTypeAttribute>(inherit: true);
        if (dtoResourceTypeAttr is not null)
        {
            var resourceType = dtoResourceTypeAttr.ResourceType;
            var stringLocalizer = stringLocalizerFactory.Create(resourceType);
            var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();
            var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute is not null)
            {
                displayAttribute.ResourceType ??= resourceType;
            }

            foreach (var attribute in validationAttributes)
            {
                if (string.IsNullOrEmpty(attribute.ErrorMessage) is false)
                {
                    attribute.ErrorMessageResourceName = attribute.ErrorMessage;
                    attribute.ErrorMessage = null;
                }

                if (string.IsNullOrWhiteSpace(attribute.ErrorMessageResourceName) is false && attribute.ErrorMessageResourceType is null)
                {
                    attribute.ErrorMessageResourceType = resourceType;
                    propValidationContext.DisplayName = stringLocalizer.GetString(displayAttribute?.Name ?? propertyInfo.Name);

                    if (attribute is CompareAttribute compareAttribute)
                    {
                        var otherPropertyInfoDisplayAttribute = (parent.GetProperty(compareAttribute.OtherProperty) ?? throw new InvalidOperationException($"Invalid OtherProperty {compareAttribute.OtherProperty}")).GetCustomAttribute<DisplayAttribute>();
                        if (otherPropertyInfoDisplayAttribute is not null)
                        {
                            otherPropertyInfoDisplayAttribute.ResourceType ??= resourceType;
                        }
                        SetOtherPropertyDisplayName(compareAttribute, stringLocalizer.GetString(otherPropertyInfoDisplayAttribute?.Name ?? compareAttribute.OtherProperty).ToString());
                    }
                }

                var result = attribute.GetValidationResult(propertyValue, propValidationContext);

                if (result is not null)
                {
                    results.Add(result);
                }
            }
        }
        else
        {
            Validator.TryValidateProperty(propertyValue, propValidationContext, results);
        }

        validationMessageStore.Clear(fieldIdentifier);
        foreach (var result in CollectionsMarshal.AsSpan(results))
        {
            validationMessageStore.Add(fieldIdentifier, result.ErrorMessage!);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        var modelValidationContext = new ValidationContext(EditContext.Model, serviceProvider, items: null);
        var results = new List<ValidationResult>();

        var objectType = modelValidationContext.ObjectType;
        var objectInstance = modelValidationContext.ObjectInstance;
        var dtoResourceTypeAttr = objectType.GetCustomAttribute<DtoResourceTypeAttribute>(inherit: true);

        validationMessageStore.Clear();
        if (dtoResourceTypeAttr is not null)
        {
            var resourceType = dtoResourceTypeAttr.ResourceType;

            var stringLocalizer = stringLocalizerFactory.Create(resourceType);

            var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var propertyInfo in properties)
            {
                var propValidationContext = new ValidationContext(objectInstance, modelValidationContext, modelValidationContext.Items)
                {
                    MemberName = propertyInfo.Name
                };
                var propertyValue = propertyInfo.GetValue(objectInstance);
                var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();
                var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute is not null)
                {
                    displayAttribute.ResourceType ??= resourceType;
                }
                foreach (var attribute in validationAttributes)
                {
                    if (string.IsNullOrEmpty(attribute.ErrorMessage) is false)
                    {
                        attribute.ErrorMessageResourceName = attribute.ErrorMessage;
                        attribute.ErrorMessage = null;
                    }

                    if (string.IsNullOrWhiteSpace(attribute.ErrorMessageResourceName) is false && attribute.ErrorMessageResourceType is null)
                    {
                        attribute.ErrorMessageResourceType = resourceType;
                        propValidationContext.DisplayName = stringLocalizer.GetString(displayAttribute?.Name ?? propertyInfo.Name);
                        if (attribute is CompareAttribute compareAttribute)
                        {
                            var otherPropertyInfoDisplayAttribute = (properties.FirstOrDefault(p => p.Name == compareAttribute.OtherProperty) ?? throw new InvalidOperationException($"Invalid OtherProperty {compareAttribute.OtherProperty}")).GetCustomAttribute<DisplayAttribute>();
                            if (otherPropertyInfoDisplayAttribute is not null)
                            {
                                otherPropertyInfoDisplayAttribute.ResourceType ??= resourceType;
                            }
                            SetOtherPropertyDisplayName(compareAttribute, stringLocalizer.GetString(otherPropertyInfoDisplayAttribute?.Name ?? compareAttribute.OtherProperty).ToString());
                        }
                    }

                    var result = attribute.GetValidationResult(propertyValue, propValidationContext);

                    if (result is not null)
                    {
                        results.Add(result);
                    }
                }
            }

            if (EditContext.Model is IValidatableObject validatableObject)
            {
                foreach (var item in validatableObject.Validate(modelValidationContext))
                {
                    item.ErrorMessage = stringLocalizer.GetString(item.ErrorMessage!);

                    results.Add(item);
                }
            }
        }
        else
        {
            Validator.TryValidateObject(EditContext.Model, modelValidationContext, results, true);
        }

        validationMessageStore.Clear();
        foreach (var validationResult in results)
        {
            if (validationResult == null) continue;

            var hasMemberNames = false;
            foreach (var memberName in validationResult.MemberNames)
            {
                hasMemberNames = true;
                validationMessageStore.Add(EditContext.Field(memberName), validationResult.ErrorMessage!);
            }

            if (hasMemberNames) continue;

            validationMessageStore.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), validationResult.ErrorMessage!);
        }

        EditContext.NotifyValidationStateChanged();
    }

    public void DisplayErrors(ResourceValidationException exception)
    {
        foreach (var detail in exception.Payload.Details)
        {
            if (detail.Name is "*")
            {
                snackbarService.Error(string.Join(Environment.NewLine, detail.Errors.Select(e => e.Message)));
                continue;
            }
            foreach (var err in detail.Errors)
            {
                validationMessageStore.Add(EditContext.Field(detail.Name!), err.Message!);
            }
        }

        EditContext.NotifyValidationStateChanged();
    }

    public void ClearErrors()
    {
        validationMessageStore.Clear();
        EditContext.NotifyValidationStateChanged();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        if (EditContext is not null)
        {
            EditContext.OnFieldChanged -= OnFieldChanged;
            EditContext.OnValidationRequested -= OnValidationRequested;
        }

        disposed = true;
    }
}
