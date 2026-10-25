namespace Bit.Borm;

/// <summary>
/// Translates the schema's own text — labels, help, placeholders, option labels, step titles, the form's
/// title — where <see cref="IBormMessageProvider"/> translates the library's. Register one in DI
/// (<see cref="BormServiceCollectionExtensions.AddBormSchemaLocalizer{TLocalizer}"/>), typically
/// wrapping <c>IStringLocalizer</c>:
/// <code>
/// public sealed class FormText(IStringLocalizer&lt;FormText&gt; localizer) : IBormSchemaLocalizer
/// {
///     public string? Localize(string key, string? text)
///         =&gt; localizer[key] is { ResourceNotFound: false } found ? found.Value : null;
/// }
/// </code>
/// </summary>
/// <remarks>
/// <para>Keys name what is being translated:</para>
/// <list type="table">
/// <item><term><c>{field}.label</c>, <c>.help</c>, <c>.placeholder</c>, <c>.prefix</c>, <c>.suffix</c></term><description>A field's texts. <c>{field}</c> is its <see cref="BormFieldDefinition.TextKey"/> (<c>x-i18n</c>) when it has one, otherwise its path with the rows taken out — <c>Lines.Product</c> for every row's product.</description></item>
/// <item><term><c>{field}.options.{value}</c></term><description>The label of one of a field's own options. Options loaded by a provider are the provider's to translate.</description></item>
/// <item><term><c>steps.{id}.title</c>, <c>steps.{id}.description</c></term><description>A step's texts.</description></item>
/// <item><term><c>form.title</c>, <c>form.description</c></term><description>The form's.</description></item>
/// </list>
/// <para>
/// It is asked every time the text is rendered, so a page whose culture changes shows the new language on
/// its next render. Returning null keeps the text as written — or as the document's own
/// <see cref="BormDefinition.Translations"/> give it, which are consulted next.
/// </para>
/// </remarks>
public interface IBormSchemaLocalizer
{
    /// <summary>The text to show for <paramref name="key"/>, or null to keep <paramref name="text"/>, the text the schema wrote.</summary>
    string? Localize(string key, string? text);
}
