using Xamarin.Forms;

namespace Bit.View
{
    [ContentProperty(nameof(Path))]
    public class ViewModelBindingExtension : ViewBindingExtension
    {
        public override string Path
        {
            get
            {
                string basePath = base.Path;
                if (basePath == ".")
                    return nameof(BindableObject.BindingContext);
                return $"{nameof(BindableObject.BindingContext)}.{basePath}";
            }
            set => base.Path = value;
        }
    }
}
