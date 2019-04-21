using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.View.Controls
{
    [ContentProperty(nameof(Content))]
    public partial class BitCheckbox
    {
        public BitCheckbox()
        {
            InitializeComponent();

            CheckTappedCommand = new Command<BitCheckbox>(bitCheckbox =>
            {
                bitCheckbox.IsChecked = !bitCheckbox.IsChecked;
            });

            // IsCheckedProperty.DefaultValue is true.
            VisualStateManager.GoToState(this, "Checked");
        }

        public static BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(BitCheckbox), defaultValue: null, defaultBindingMode: BindingMode.OneWay);

        public virtual string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(BitCheckbox), defaultValue: Color.Black, defaultBindingMode: BindingMode.OneTime);

        public Color TextColor
        {
            get { return (Color)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        public static BindableProperty CheckColorProperty = BindableProperty.Create(nameof(CheckColor), typeof(Color), typeof(BitCheckbox), defaultValue: Color.White, defaultBindingMode: BindingMode.OneTime);

        public Color CheckColor
        {
            get { return (Color)GetValue(CheckColorProperty); }
            set { SetValue(CheckColorProperty, value); }
        }

        public static BindableProperty FillColorProperty = BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(BitCheckbox), defaultValue: Color.Blue, defaultBindingMode: BindingMode.OneTime);

        public Color FillColor
        {
            get { return (Color)GetValue(FillColorProperty); }
            set { SetValue(FillColorProperty, value); }
        }

        public static BindableProperty OutlineColorProperty = BindableProperty.Create(nameof(OutlineColor), typeof(Color), typeof(BitCheckbox), defaultValue: Color.Blue, defaultBindingMode: BindingMode.OneTime);

        public Color OutlineColor
        {
            get { return (Color)GetValue(OutlineColorProperty); }
            set { SetValue(OutlineColorProperty, value); }
        }

        public static BindableProperty IsCheckedChangedCommandProperty = BindableProperty.Create(nameof(IsCheckedChangedCommand), typeof(ICommand), typeof(BitCheckbox), defaultValue: null, defaultBindingMode: BindingMode.OneTime);

        public ICommand IsCheckedChangedCommand
        {
            get { return (ICommand)GetValue(IsCheckedChangedCommandProperty); }
            set { SetValue(IsCheckedChangedCommandProperty, value); }
        }

        public static BindableProperty IsCheckedChangedCommandParameterProperty = BindableProperty.Create(nameof(IsCheckedChangedCommandParameter), typeof(object), typeof(BitCheckbox), defaultValue: null, defaultBindingMode: BindingMode.OneTime);

        public object IsCheckedChangedCommandParameter
        {
            get { return GetValue(IsCheckedChangedCommandParameterProperty); }
            set { SetValue(IsCheckedChangedCommandParameterProperty, value); }
        }

        public static BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(Shape), typeof(BitCheckbox), defaultValue: Shape.Native, defaultBindingMode: BindingMode.OneTime);

        public Shape Shape
        {
            get { return (Shape)GetValue(ShapeProperty); }
            set { SetValue(ShapeProperty, value); }
        }

        public static BindableProperty IsCheckedProperty = BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(BitCheckbox), defaultValue: true, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            if (newValue is bool newValueAsBool)
            {
                BitCheckbox checkbox = (BitCheckbox)sender;

                if (newValueAsBool == true)
                    VisualStateManager.GoToState(checkbox, "Checked");
                else
                    VisualStateManager.GoToState(checkbox, "Unchecked");

                checkbox.IsCheckedChanged?.Invoke(checkbox, new IsCheckChangedEventArgs { IsChecked = newValueAsBool });

                if (checkbox.IsCheckedChangedCommand?.CanExecute(checkbox.IsCheckedChangedCommandParameter ?? newValueAsBool) == true)
                    checkbox.IsCheckedChangedCommand?.Execute(checkbox.IsCheckedChangedCommandParameter ?? newValueAsBool);
            }
        });

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICommand CheckTappedCommand { get; set; }

        public virtual Xamarin.Forms.View Content { get; set; }

        public event EventHandler<IsCheckChangedEventArgs> IsCheckedChanged;
    }

    public class IsCheckChangedEventArgs : EventArgs
    {
        public bool IsChecked { get; set; }
    }

    public enum Shape
    {
        Circle, Rectangle, Native
    }
}
