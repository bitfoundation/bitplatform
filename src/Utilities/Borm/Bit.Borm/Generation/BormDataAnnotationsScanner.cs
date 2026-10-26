using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Bit.Borm;

/// <summary>
/// Reads <see cref="System.ComponentModel.DataAnnotations"/> attributes from a property and applies
/// them to a <see cref="BormFieldDefinition"/> (labels, requiredness, length/range/pattern validators).
/// </summary>
/// <remarks>
/// Rules are added through <see cref="BormFieldDefinition.AddValidator"/>, so a field that is
/// later refined with the fluent builder ends up with one rule per constraint, not two.
/// </remarks>
public static class BormDataAnnotationsScanner
{
    public static void Apply(MemberInfo member, BormFieldDefinition field)
    {
        // Label / prompt / description
        var display = member.GetCustomAttribute<DisplayAttribute>();
        if (display is not null)
        {
            if (display.GetName() is { } name) field.Label = name;
            if (display.GetPrompt() is { } prompt) field.Placeholder = prompt;
            if (display.GetDescription() is { } desc) field.HelpText = desc;
            if (display.GetOrder() is { } order) field.Order = order;
            if (display.GetGroupName() is { } group) field.Group = group;
        }

        if (member.GetCustomAttribute<DisplayNameAttribute>() is { } displayName)
            field.Label = displayName.DisplayName;

        if (member.GetCustomAttribute<DescriptionAttribute>() is { } description && field.HelpText is null)
            field.HelpText = description.Description;

        // Required
        if (member.GetCustomAttribute<RequiredAttribute>() is { } req)
        {
            field.Required = true;
            // [Required(AllowEmptyStrings = true)] only rejects null, so an empty box is acceptable.
            field.AddValidator(req.AllowEmptyStrings
                ? new BormDelegateRule(ctx => ctx.Value is null
                    ? BormRuleResult.Fail(NullIfDefault(req.ErrorMessage) ?? ctx.Message(BormMessageKeys.Required))
                    : BormRuleResult.Success())
                : new BormRequiredRule(NullIfDefault(req.ErrorMessage)));
        }

        // String length
        if (member.GetCustomAttribute<StringLengthAttribute>() is { } sl)
        {
            field.MaxLength = sl.MaximumLength;
            field.AddValidator(new BormMaxLengthRule(sl.MaximumLength, NullIfDefault(sl.ErrorMessage)));
            if (sl.MinimumLength > 0)
            {
                field.MinLength = sl.MinimumLength;
                field.AddValidator(new BormMinLengthRule(sl.MinimumLength, NullIfDefault(sl.ErrorMessage)));
            }
        }

        // [MinLength]/[MaxLength] mean item counts on a collection, exactly as [Length] does. Mapping
        // them to the string rule on an array field made them silently do nothing: the rule reads
        // ctx.Value as a string, a List<T> is not one, and "at least one line" passed on an empty list.
        // What decides it is the member's CLR type, as it does for DataAnnotations — not the control:
        // a List<DayOfWeek> renders as a multi-select and a List<IBrowserFile> as a file picker, and
        // "pick at least one" was dropped on both because neither is an Array field.
        var isCollection = IsCollection(MemberType(member)) || field.Type == BormFieldType.Array;

        if (member.GetCustomAttribute<MinLengthAttribute>() is { } ml)
        {
            if (isCollection)
            {
                field.MinItems = ml.Length;
                field.AddValidator(new BormCollectionSizeRule(ml.Length, field.MaxItems, NullIfDefault(ml.ErrorMessage)));
            }
            else
            {
                field.MinLength = ml.Length;
                field.AddValidator(new BormMinLengthRule(ml.Length, NullIfDefault(ml.ErrorMessage)));
            }
        }

        if (member.GetCustomAttribute<MaxLengthAttribute>() is { } mxl)
        {
            if (isCollection)
            {
                field.MaxItems = mxl.Length;
                field.AddValidator(new BormCollectionSizeRule(field.MinItems, mxl.Length, NullIfDefault(mxl.ErrorMessage)));
            }
            else
            {
                field.MaxLength = mxl.Length;
                field.AddValidator(new BormMaxLengthRule(mxl.Length, NullIfDefault(mxl.ErrorMessage)));
            }
        }

        // [Length(min, max)] covers both bounds, and applies to collections as well as strings.
        if (member.GetCustomAttribute<LengthAttribute>() is { } len)
        {
            if (isCollection)
            {
                field.MinItems = len.MinimumLength;
                field.MaxItems = len.MaximumLength;
                field.AddValidator(new BormCollectionSizeRule(len.MinimumLength, len.MaximumLength, NullIfDefault(len.ErrorMessage)));
            }
            else
            {
                field.MinLength = len.MinimumLength;
                field.MaxLength = len.MaximumLength;
                field.AddValidator(new BormMinLengthRule(len.MinimumLength, NullIfDefault(len.ErrorMessage)));
                field.AddValidator(new BormMaxLengthRule(len.MaximumLength, NullIfDefault(len.ErrorMessage)));
            }
        }

        // Range. Exclusive ends are carried as the rule's own flags; the min/max attributes the input
        // renders stay inclusive, which is the most HTML can say, and the rule refuses the bound itself.
        if (member.GetCustomAttribute<RangeAttribute>() is { } range)
        {
            double? min = ToBound(range, range.Minimum);
            double? max = ToBound(range, range.Maximum);
            field.Min = min;
            field.Max = max;
            field.AddValidator(new BormRangeRule(min, max, NullIfDefault(range.ErrorMessage))
            {
                MinExclusive = range.MinimumIsExclusive,
                MaxExclusive = range.MaximumIsExclusive
            });
        }

        // Email / phone / url
        if (member.GetCustomAttribute<EmailAddressAttribute>() is { } email)
        {
            field.Type = BormFieldType.Email;
            field.Autocomplete ??= "email";
            field.AddValidator(new BormEmailRule(ExplicitMessage(email)));
        }
        if (member.GetCustomAttribute<PhoneAttribute>() is { } phone)
        {
            field.Type = BormFieldType.Tel;
            field.Autocomplete ??= "tel";
            // Judged by [Phone] itself: digits, with the spaces, dashes, dots, brackets, leading + and extension
            // people write them with. It used to choose the keyboard and nothing more, and "not a phone!!" was saved.
            var message = ExplicitMessage(phone);
            field.AddValidator(new BormDelegateRule(ctx => ctx.Value is not string { Length: > 0 } text || phone.IsValid(text)
                ? BormRuleResult.Success()
                : BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Pattern))));
        }
        if (member.GetCustomAttribute<UrlAttribute>() is { } url)
        {
            field.Type = BormFieldType.Url;
            // The schemes [Url] itself accepts. The rule's default is http and https only, so an ftp://
            // address the model's own validation was happy with was refused by the form in front of it.
            field.AddValidator(new BormUrlRule(ExplicitMessage(url),
                Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeFtp));
        }

        // Regex. [RegularExpression] matches the whole value — DataAnnotations checks that the match
        // starts at the first character and ends at the last — while a pattern rule, like JSON Schema's
        // `pattern`, matches anywhere. Copied across as it stood, [RegularExpression(@"\d{5}")] accepted
        // "abc12345xyz", which the model's own validation then rejected. Anchoring the pattern keeps
        // the attribute's meaning; the group keeps an alternation inside it ("a|b") from being anchored
        // at one end only. The rule ends at \z, because in .NET `$` also matches before a final newline
        // and "12345\n" would pass; the input's `pattern` keeps `$`, which is what the browser's (and
        // JSON Schema's) dialect understands — \z is not part of it.
        if (member.GetCustomAttribute<RegularExpressionAttribute>() is { } rx)
        {
            field.Pattern = $"^(?:{rx.Pattern})$";
            field.AddValidator(new BormPatternRule($@"^(?:{rx.Pattern})\z", NullIfDefault(rx.ErrorMessage)));
        }

        // [Compare] names a sibling property, which is exactly a form path at the same level.
        if (member.GetCustomAttribute<CompareAttribute>() is { } compare)
        {
            field.AddValidator(new BormCompareRule(compare.OtherProperty,
                compare.OtherPropertyDisplayName ?? BormFieldBuilder.Humanize(compare.OtherProperty),
                NullIfDefault(compare.ErrorMessage)));
            // The rule reads the other property, so correcting *that* one is what makes this field's
            // message wrong — and nothing would revalidate it. Same wiring MatchesField does.
            if (!field.RevalidateOn.Contains(compare.OtherProperty))
                field.RevalidateOn.Add(compare.OtherProperty);
        }

        // DataType hints
        if (member.GetCustomAttribute<DataTypeAttribute>() is { } dt)
        {
            field.Type = dt.DataType switch
            {
                DataType.Password => BormFieldType.Password,
                DataType.MultilineText => BormFieldType.TextArea,
                DataType.EmailAddress => BormFieldType.Email,
                DataType.Url or DataType.ImageUrl => BormFieldType.Url,
                DataType.PhoneNumber => BormFieldType.Tel,
                DataType.Date => BormFieldType.Date,
                DataType.Time => BormFieldType.Time,
                DataType.DateTime => BormFieldType.DateTime,
                // Only a member that can hold what a file input produces becomes one.
                DataType.Upload when IsFileMember(MemberType(member)) => BormFieldType.File,
                _ => field.Type
            };

            if (dt.DataType == DataType.Password) field.Autocomplete ??= "current-password";
        }

        // Uploads. [FileExtensions] is, first of all, a check on a *file name* — DataAnnotations
        // validates a string with it, and that is where MVC models put it. Turning such a property into
        // a file picker bound it to a string no IBrowserFile could be written to, so every selection was
        // refused. A file-typed member becomes a picker with an accept list; anything else keeps its
        // control and has the name it holds checked against the same extensions.
        if (member.GetCustomAttribute<FileExtensionsAttribute>() is { } fx && !string.IsNullOrWhiteSpace(fx.Extensions))
        {
            var accept = string.Join(",", fx.Extensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.StartsWith('.') ? e : "." + e));

            if (IsFileMember(MemberType(member)))
            {
                field.Type = BormFieldType.File;
                field.Accept = accept;
                field.AddValidator(new BormFileRule(field.MaxFileSize, accept, typeMessage: ExplicitMessage(fx)));
            }
            else
            {
                field.AddValidator(new BormFileRule(null, accept, typeMessage: ExplicitMessage(fx)));
            }
        }

        // Editable(false) => read only
        if (member.GetCustomAttribute<EditableAttribute>() is { AllowEdit: false })
            field.ReadOnly = true;
    }

    private static string? NullIfDefault(string? message) => string.IsNullOrWhiteSpace(message) ? null : message;

    /// <summary>
    /// The message an attribute was written with, or null when it carries only its own default. The
    /// DataType-based attributes ([EmailAddress], [Url], [FileExtensions]) keep their default text where
    /// <see cref="ValidationAttribute.ErrorMessage"/> reads it, so a message nobody wrote comes back as the
    /// raw template - "The {0} field is not a valid e-mail address." - and the form showed the braces.
    /// A fresh attribute of the same type tells the two apart.
    /// </summary>
    private static string? ExplicitMessage<TAttribute>(TAttribute attribute) where TAttribute : ValidationAttribute, new()
        => NullIfDefault(attribute.ErrorMessage) is { } message && message != new TAttribute().ErrorMessage ? message : null;

    private static Type? MemberType(MemberInfo member) => member switch
    {
        PropertyInfo p => p.PropertyType,
        FieldInfo f => f.FieldType,
        _ => null
    };

    /// <summary>Whether a member holds a collection (anything enumerable but text), as DataAnnotations reads "length".</summary>
    private static bool IsCollection(Type? type)
    {
        if (type is null) return false;
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t);
    }

    /// <summary>Whether a member can hold what a file input produces: one <c>IBrowserFile</c> or a collection of them.</summary>
    private static bool IsFileMember(Type? type)
    {
        if (type is null) return false;
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return BormFieldTypeResolver.IsBrowserFile(t)
               || (BormFieldTypeResolver.GetEnumerableElementType(t) is { } element
                   && BormFieldTypeResolver.IsBrowserFile(element));
    }

    /// <summary>
    /// Reads one end of a <see cref="RangeAttribute"/> the way DataAnnotations does: a bound given as
    /// text is converted to the attribute's <see cref="RangeAttribute.OperandType"/> in the
    /// <em>current</em> culture, unless <see cref="RangeAttribute.ParseLimitsInInvariantCulture"/> says
    /// otherwise. Reading it as invariant text instead turned <c>"0,01"</c> — written by someone whose
    /// culture uses a decimal comma, and read correctly by the model's own validation — into 1.
    /// </summary>
    private static double? ToBound(RangeAttribute range, object? bound)
    {
        if (bound is string text && range.OperandType is { } operandType && operandType != typeof(string))
        {
            var culture = range.ParseLimitsInInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            try
            {
                if (ToDouble(TypeDescriptor.GetConverter(operandType).ConvertFrom(null, culture, text)) is { } converted)
                    return converted;
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or NotSupportedException
                                           or InvalidCastException or OverflowException)
            {
                // Not something the operand type can read; the culture-neutral reading below may still be.
            }
        }

        return ToDouble(bound);
    }

    private static double? ToDouble(object? value)
    {
        if (value is null) return null;
        // [Range(typeof(DateTime), "2020-01-01", "2030-01-01")] carries strings; dates and times are
        // stored as OLE automation numbers so the same double-based rule and input attributes can
        // handle them. The conversion is shared with the rule that does the comparing, so a bound and
        // the value it judges can never be read on two different scales. A date too early for that
        // scale (before the year 100) bounds nothing it can express, so it is no bound at all.
        if (BormNumber.TryToDouble(value, out var d)) return d;
        return BormNumber.TryToOADate(value, out var date) && !double.IsInfinity(date) ? date : null;
    }
}
