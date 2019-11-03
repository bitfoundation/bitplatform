using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Bit.View
{
    // https://github.com/xamarin/Xamarin.Forms/issues/3606
    [ContentProperty(nameof(Path))]
    public class UnTypedBindingExtension : IMarkupExtension<BindingBase>
    {
        public virtual string Path { get; set; } = ".";
        public BindingMode Mode { get; set; } = BindingMode.Default;
        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        public string StringFormat { get; set; }
        public object Source { get; set; }
        public string UpdateSourceEventName { get; set; }
        public object TargetNullValue { get; set; }
        public object FallbackValue { get; set; }

        BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider serviceProvider)
        {
            Source ??= GetSource(serviceProvider);

            return new Binding(Path, Mode, Converter, ConverterParameter, StringFormat, Source)
            {
                UpdateSourceEventName = UpdateSourceEventName,
                FallbackValue = FallbackValue,
                TargetNullValue = TargetNullValue
            };
        }

        protected virtual object GetSource(IServiceProvider serviceProvider)
        {
            return null;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
        }
    }
}
