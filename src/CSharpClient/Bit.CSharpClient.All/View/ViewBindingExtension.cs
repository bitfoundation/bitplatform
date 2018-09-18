using System;
using System.Linq;
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

        protected override object GetSource(IServiceProvider serviceProvider)
        {
            SimpleValueTargetProvider providedValueTarget = (SimpleValueTargetProvider)serviceProvider.GetService<IReferenceProvider>();

            Page parentPage = ((object[])_objectAndParentsField.GetValue(providedValueTarget))
                .OfType<Page>()
                .LastOrDefault();

            return parentPage;
        }
    }
}
