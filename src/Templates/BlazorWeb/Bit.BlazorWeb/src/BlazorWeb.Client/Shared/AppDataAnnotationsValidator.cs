﻿using System.Reflection;
using System.Runtime.InteropServices;
using BlazorWeb.Shared.Attributes;
using BlazorWeb.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorWeb.Client.Shared;

/// <summary>
/// To implement forms where each error is displayed according to the language chosen by the user, you can use the <see cref="DtoResourceTypeAttribute"/>
/// on the corresponding class instead of using `ErrorResourceType` on each property. Check out <see cref="SignUpRequestDto"/> for an example.
/// However, you need to use <see cref="AppDataAnnotationsValidator"/> instead of <see cref="DataAnnotationsValidator"/> in Blazor EditForms for this method to work.
/// </summary>
public partial class AppDataAnnotationsValidator : AppComponentBase, IDisposable
{
    private static readonly PropertyInfo _OtherPropertyNamePropertyInfo = typeof(CompareAttribute).GetProperty(nameof(CompareAttribute.OtherPropertyDisplayName))!;

    private bool _disposed;
    private ValidationMessageStore _validationMessageStore = default!;

    [AutoInject] private IServiceProvider _serviceProvider = default!;
    [AutoInject] private IStringLocalizerFactory _stringLocalizerFactory = default!;

    [CascadingParameter] public EditContext EditContext { get; set; } = default!;

    protected override Task OnInitAsync()
    {
        if (EditContext is null)
            throw new InvalidOperationException("EditContext is required");

        EditContext.OnFieldChanged += OnFieldChanged;
        EditContext.OnValidationRequested += OnValidationRequested;

        _validationMessageStore = new ValidationMessageStore(EditContext);

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
        if (dtoResourceTypeAttr is not null)
        {
            var resourceType = dtoResourceTypeAttr.ResourceType;
            var stringLocalizer = _stringLocalizerFactory.Create(resourceType);
            var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();

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
        else
        {
            Validator.TryValidateProperty(propertyValue, validationContext, results);
        }

        _validationMessageStore.Clear(fieldIdentifier);
        foreach (var result in CollectionsMarshal.AsSpan(results))
        {
            _validationMessageStore.Add(fieldIdentifier, result.ErrorMessage!);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        var validationContext = new ValidationContext(EditContext.Model, _serviceProvider, items: null);
        var results = new List<ValidationResult>();

        var objectType = validationContext.ObjectType;
        var objectInstance = validationContext.ObjectInstance;
        var dtoResourceTypeAttr = objectType.GetCustomAttribute<DtoResourceTypeAttribute>();

        _validationMessageStore.Clear();
        if (dtoResourceTypeAttr is not null)
        {
            var resourceType = dtoResourceTypeAttr.ResourceType;

            var stringLocalizer = _stringLocalizerFactory.Create(resourceType);

            var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var propertyInfo in properties)
            {
                var context = new ValidationContext(objectInstance, validationContext, validationContext.Items);
                context.MemberName = propertyInfo.Name;
                var propertyValue = propertyInfo.GetValue(objectInstance);
                var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();
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
                        var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
                        validationContext.DisplayName = stringLocalizer.GetString(displayAttribute?.Name ?? propertyInfo.Name);
                        if (attribute is CompareAttribute compareAttribute)
                        {
                            var otherPropertyInfoDisplayAttribute = (properties.FirstOrDefault(p => p.Name == compareAttribute.OtherProperty) ?? throw new InvalidOperationException($"Invalid OtherProperty {compareAttribute.OtherProperty}")).GetCustomAttribute<DisplayAttribute>();
                            _OtherPropertyNamePropertyInfo.SetValue(attribute, stringLocalizer.GetString(otherPropertyInfoDisplayAttribute?.Name ?? compareAttribute.OtherProperty).ToString());
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
        else
        {
            Validator.TryValidateObject(EditContext.Model, validationContext, results, true);
        }

        _validationMessageStore.Clear();
        foreach (var validationResult in results)
        {
            if (validationResult == null) continue;

            var hasMemberNames = false;
            foreach (var memberName in validationResult.MemberNames)
            {
                hasMemberNames = true;
                _validationMessageStore.Add(EditContext.Field(memberName), validationResult.ErrorMessage!);
            }

            if (hasMemberNames) continue;

            _validationMessageStore.Add(new FieldIdentifier(EditContext.Model, fieldName: string.Empty), validationResult.ErrorMessage!);
        }

        EditContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (EditContext is not null)
        {
            EditContext.OnFieldChanged -= OnFieldChanged;
            EditContext.OnValidationRequested -= OnValidationRequested;
        }

        _disposed = true;
    }
}
