using Bit.View;
using System;
using System.Globalization;
using System.Linq;

namespace Bit.CSharpClientSample.Views
{
    public partial class LoginView
    {
        public LoginView()
        {
            InitializeComponent();
        }
    }

    public class TestMultiValueConverter1 : MultiValueConverter<string, string, bool, object>
    {
        public override bool Convert(string source1, string source2, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(source1) && !string.IsNullOrEmpty(source2);
        }
    }

    public class TestMultiValueConverter2 : MultiValueConverter<bool, object>
    {
        public override bool Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Cast<string>().All(str => !string.IsNullOrEmpty(str));
        }
    }
}
