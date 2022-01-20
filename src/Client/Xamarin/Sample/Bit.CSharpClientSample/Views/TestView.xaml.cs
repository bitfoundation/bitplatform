using Bit.View;
using System;
using System.Globalization;

namespace Bit.CSharpClientSample.Views
{
    public partial class TestView
    {
        public TestView()
        {
            InitializeComponent();
        }
    }

    public class TestConverter : ValueConverter<int, int>
    {
        public override int Convert(int value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
