using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Bit.View
{
    [ContentProperty(nameof(Template))]
    public class InlineDataTemplate
    {
        public Type ItemType { get; set; }

        public DataTemplate Template { get; set; }
    }

    public class InlineDataTemplateSelector : DataTemplateSelector
    {
        public List<InlineDataTemplate> Templates { get; set; } = new List<InlineDataTemplate> { };

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            foreach (InlineDataTemplate inlineDataTemplate in Templates)
            {
                if (item.GetType() == inlineDataTemplate.ItemType)
                    return inlineDataTemplate.Template;
            }

            throw new InvalidOperationException($"{nameof(InlineDataTemplate)} could not be found for item with type: {item.GetType().FullName}");
        }
    }
}
