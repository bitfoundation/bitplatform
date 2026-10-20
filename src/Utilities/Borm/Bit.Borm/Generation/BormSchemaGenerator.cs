using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Bit.Borm;

/// <summary>
/// Generates a <see cref="BormDefinition"/> from a CLR type using reflection and DataAnnotations.
/// This gives "zero-config" forms from any POCO, while remaining fully customisable afterwards.
/// </summary>
public static class BormSchemaGenerator
{
    public static BormDefinition Generate<TModel>(BormSchemaGeneratorOptions? options = null)
        => Generate(typeof(TModel), options);

    public static BormDefinition Generate(Type modelType, BormSchemaGeneratorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        options ??= new BormSchemaGeneratorOptions();

        var form = new BormDefinition { ModelType = modelType };
        if (modelType.GetCustomAttribute<DisplayAttribute>() is { } display)
        {
            form.Title = display.GetName();
            form.Description = display.GetDescription();
        }

        foreach (var field in BuildFields(modelType, options, depth: 0, new HashSet<Type>()))
            form.Fields.Add(field);
        return form;
    }

    private static IEnumerable<BormFieldDefinition> BuildFields(
        Type type, BormSchemaGeneratorOptions options, int depth, HashSet<Type> ancestry)
    {
        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // A public getter, not merely CanRead: a property whose getter is private is not part of the
            // model's public surface, and a form has no business displaying it.
            .Where(p => BormModelDataAccessor.IsPubliclyReadable(p) && p.GetIndexParameters().Length == 0)
            .Where(p => !ShouldSkip(p, options))
            // Inherited properties come first, then the type's own — the order the class reads in, and
            // the order anyone declaring `Article : Document` expects. Metadata tokens alone cannot say
            // this: they run in file order, so moving the base class below the derived one in the same
            // file silently reordered the form, and across assemblies they are two unrelated sequences
            // whose interleaving means nothing at all.
            .OrderBy(p => InheritanceDistance(type, p.DeclaringType))
            .ThenBy(p => p.MetadataToken);

        var list = new List<BormFieldDefinition>();
        foreach (var prop in props)
            if (BuildField(prop, options, depth, ancestry) is { } field)
                list.Add(field);

        // OrderBy is stable, so fields that share an order keep their declaration sequence.
        return list.OrderBy(f => f.Order);
    }

    /// <summary>
    /// Builds the field for <paramref name="prop"/>, or returns null when it cannot be rendered at this
    /// depth.
    /// </summary>
    private static BormFieldDefinition? BuildField(
        PropertyInfo prop, BormSchemaGeneratorOptions options, int depth, HashSet<Type> ancestry)
    {
        var propType = prop.PropertyType;
        var fieldType = BormFieldTypeResolver.Resolve(propType);

        var field = new BormFieldDefinition(prop.Name, fieldType)
        {
            ValueType = propType,
            Label = BormFieldBuilder.Humanize(prop.Name),
            // A public setter, not merely CanWrite, which is also true of a private or internal one — and
            // only where editing goes through it: a class-typed group or list is edited in place.
            ReadOnly = BormModelDataAccessor.IsReadOnlyForForm(prop, fieldType)
        };

        var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

        if (underlying.IsEnum)
        {
            // Resolve() already chose Select or MultiSelect depending on [Flags].
            foreach (var option in BormEnumOptions.For(underlying))
                field.Options.Add(option);
        }
        else if (BormFieldTypeResolver.GetEnumElementType(underlying) is { } elementEnum)
        {
            // A collection of enum members: Resolve() made it a multi-select, and the choices are the
            // element type's, not the collection's.
            foreach (var option in BormEnumOptions.For(elementEnum))
                field.Options.Add(option);
        }
        else if (fieldType == BormFieldType.File)
        {
            // A collection of files is one control that accepts several, not a repeater of file pickers.
            field.Multiple = BormFieldTypeResolver.GetEnumerableElementType(underlying) is not null;
        }
        else if (fieldType == BormFieldType.Array)
        {
            // A repeater cannot render a row without a template. A list of plain values gets one without
            // going any deeper, so it is built at any depth; a list of objects past MaxDepth cannot be,
            // and is left off the form. Generated without a template it was a repeater that threw the
            // moment it had a row to show — on a server, a dropped circuit.
            var elementType = BormFieldTypeResolver.GetEnumerableElementType(propType) ?? typeof(string);
            if (depth >= options.MaxDepth && BormFieldTypeResolver.Resolve(elementType) == BormFieldType.Object)
                return null;
            field.ItemTemplate = BuildItemTemplate(elementType, options, depth + 1, ancestry);
        }
        else if (fieldType == BormFieldType.Object && depth < options.MaxDepth && !ancestry.Contains(underlying))
        {
            ancestry.Add(underlying);
            foreach (var child in BuildFields(underlying, options, depth + 1, ancestry))
                field.Children.Add(child);
            ancestry.Remove(underlying);
        }

        BormDataAnnotationsScanner.Apply(prop, field);
        options.ConfigureField?.Invoke(field);
        return field;
    }

    private static BormFieldDefinition BuildItemTemplate(
        Type elementType, BormSchemaGeneratorOptions options, int depth, HashSet<Type> ancestry)
    {
        var elementFieldType = BormFieldTypeResolver.Resolve(elementType);
        if (elementFieldType == BormFieldType.Object && !ancestry.Contains(elementType))
        {
            var template = new BormFieldDefinition("item", BormFieldType.Object) { ValueType = elementType };
            ancestry.Add(elementType);
            foreach (var child in BuildFields(elementType, options, depth + 1, ancestry))
                template.Children.Add(child);
            ancestry.Remove(elementType);
            return template;
        }

        var scalar = new BormFieldDefinition("item", elementFieldType) { ValueType = elementType };
        var underlying = Nullable.GetUnderlyingType(elementType) ?? elementType;
        if (underlying.IsEnum)
        {
            foreach (var option in BormEnumOptions.For(underlying))
                scalar.Options.Add(option);
        }
        return scalar;
    }

    /// <summary>
    /// How many levels above <paramref name="type"/> a property was declared. Larger means further up
    /// the hierarchy, so sorting descending puts the root base class's properties first. An unknown
    /// declaring type (an interface's property on a type that never implemented it explicitly) sorts
    /// with the type's own, which is where the rest of the sort leaves it anyway.
    /// </summary>
    private static int InheritanceDistance(Type type, Type? declaring)
    {
        if (declaring is null) return 0;

        var depth = 0;
        for (var current = type; current is not null; current = current.BaseType, depth++)
            if (current == declaring) return -depth;

        return 0;
    }

    private static bool ShouldSkip(PropertyInfo prop, BormSchemaGeneratorOptions options)
    {
        if (options.IgnoredProperties.Contains(prop.Name)) return true;

        if (options.HonorScaffoldColumn &&
            prop.GetCustomAttribute<ScaffoldColumnAttribute>() is { Scaffold: false })
            return true;

        // [Display(AutoGenerateField = false)] is the DataAnnotations way of saying "not on a form".
        if (prop.GetCustomAttribute<DisplayAttribute>()?.GetAutoGenerateField() == false)
            return true;

        // Raw content (byte[], Stream, a list of either) has no control that can fill it: a file input
        // produces an IBrowserFile, which has to be read asynchronously before it is bytes. Generated as
        // a file field it was a picker that refused every file; generated as anything else it is worse.
        // A model that wants an upload declares an IBrowserFile and reads it in its submit handler.
        if (BormFieldTypeResolver.IsBinaryContent(prop.PropertyType)) return true;

        // A get-only scalar cannot be edited; whether it is shown read-only or dropped is configurable.
        // Containers stay regardless — their children may well be writable. "Get-only" means no
        // public setter: a private or internal one is not the form's to call.
        if (!BormModelDataAccessor.IsPubliclyWritable(prop) && options.ReadOnlyProperties == BormReadOnlyPropertyHandling.Skip)
        {
            var fieldType = BormFieldTypeResolver.Resolve(prop.PropertyType);
            if (fieldType is not (BormFieldType.Object or BormFieldType.Array)) return true;
        }

        return false;
    }
}
