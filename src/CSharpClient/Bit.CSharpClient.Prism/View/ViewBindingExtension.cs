using System;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Forms.Xaml.Internals;

namespace Bit.View
{
    [ContentProperty(nameof(Path))]
    public class ViewBindingExtension : UnTypedBindingExtension
    {
        private static readonly FieldInfo _objectAndParentsField = typeof(SimpleValueTargetProvider).GetField("objectAndParents", BindingFlags.NonPublic | BindingFlags.Instance);

        public Type ParentType { get; set; } = typeof(Page);

        protected override object GetSource(IServiceProvider serviceProvider)
        {
            if (ParentType == null)
                throw new InvalidOperationException($"{nameof(ParentType)} may not be null");

            SimpleValueTargetProvider providedValueTarget = (SimpleValueTargetProvider)serviceProvider.GetService<IReferenceProvider>();

            foreach (object parent in (object[])_objectAndParentsField.GetValue(providedValueTarget))
            {
                if (ParentType.IsAssignableFrom(parent.GetType()))
                    return parent;
            }

            return null;
        }
    }
}
