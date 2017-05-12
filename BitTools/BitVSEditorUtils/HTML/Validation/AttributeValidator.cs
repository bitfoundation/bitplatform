using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Html.Core.Tree.Nodes;
using Microsoft.Html.Editor.Validation.Def;
using Microsoft.Html.Editor.Validation.Errors;
using Microsoft.Html.Editor.Validation.Validators;
using Microsoft.VisualStudio.Utilities;

namespace BitVSEditorUtils.Html
{
    [Export(typeof(IHtmlElementValidatorProvider))]
    [ContentType("htmlx")]
    public class AttributeValidatorProvider : BaseHtmlElementValidatorProvider<AttributeValidator>
    { }

    public class AttributeValidator : BaseValidator
    {
        private static readonly HtmlElement _global = HtmlElementsContainer.Elements.Single(e => e.Name == "*");

        public override IList<IHtmlValidationError> ValidateElement(ElementNode element)
        {
            ValidationErrorCollection results = new ValidationErrorCollection();
            List<HtmlAttribute> attributes = _global.Attributes.ToList();

            HtmlElement match = HtmlElementsContainer.Elements.SingleOrDefault(e => e.Name.Equals(element.Name, StringComparison.OrdinalIgnoreCase));

            if (match != null && match.Attributes != null)
                attributes.AddRange(match.Attributes);

            foreach (HtmlAttribute htmlAttr in attributes)
            {
                AttributeNode attrNode = element.GetAttribute(htmlAttr.Name);

                if (attrNode == null)
                    continue;

                int index = element.GetAttributeIndex(attrNode.Name);
                string error;

                if (!IsTypeValid(attrNode.Value, htmlAttr, out error))
                {
                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeValue, index);
                }

                if (!IsRequireValid(htmlAttr, element, out error))
                {
                    results.AddAttributeError(element, error, HtmlValidationErrorLocation.AttributeName, index);
                }
            }

            return results;
        }

        private static bool IsRequireValid(HtmlAttribute attribute, ElementNode element, out string error)
        {
            error = string.Empty;

            if (!string.IsNullOrEmpty(attribute.Require) && element.GetAttribute(attribute.Require) == null)
            {
                error = $"When using \"{attribute.Name}\" you must also specify \"{attribute.Require}\".";
                return false;
            }

            return true;
        }

        private static bool IsTypeValid(string value, HtmlAttribute attribute, out string error)
        {
            error = string.Empty;

            if (attribute.Type == "boolean")
            {
                error = $"The value \"{value}\" must be either \"true\" or \"false\".";
                return value == "true" || value == "false";
            }

            if (attribute.Type == "number")
            {
                error = $"The value \"{value}\" is not a valid number.";
                double number;
                return double.TryParse(value, out number);
            }

            if (attribute.Type == "enum")
            {
                error = $"The value \"{value}\" doesn't match any of the allowed enum values.";
                return attribute.Values.Contains(value);
            }

            return true;
        }
    }
}
