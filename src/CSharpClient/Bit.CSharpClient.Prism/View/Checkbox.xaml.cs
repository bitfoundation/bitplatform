using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.View
{
    [ContentProperty(nameof(Content))]
    public partial class Checkbox : TemplatedView
    {
        public Checkbox()
        {
            InitializeComponent();

            CheckTappedCommand = new Command<Checkbox>(checkbox =>
            {
                checkbox.IsChecked = !checkbox.IsChecked;
            });

            // IsCheckedProperty.DefaultValue is true.
            VisualStateManager.GoToState(this, "Checked");
        }

        public static BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Checkbox), defaultValue: null, defaultBindingMode: BindingMode.OneWay);

        public virtual string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(Checkbox), defaultValue: Color.Black, defaultBindingMode: BindingMode.OneTime);

        public Color TextColor
        {
            get { return (Color)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        public static BindableProperty CheckColorProperty = BindableProperty.Create(nameof(CheckColor), typeof(Color), typeof(Checkbox), defaultValue: Color.White, defaultBindingMode: BindingMode.OneTime);

        public Color CheckColor
        {
            get { return (Color)GetValue(CheckColorProperty); }
            set { SetValue(CheckColorProperty, value); }
        }

        public static BindableProperty FillColorProperty = BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(Checkbox), defaultValue: Color.Blue, defaultBindingMode: BindingMode.OneTime);

        public Color FillColor
        {
            get { return (Color)GetValue(FillColorProperty); }
            set { SetValue(FillColorProperty, value); }
        }

        public static BindableProperty OutlineColorProperty = BindableProperty.Create(nameof(OutlineColor), typeof(Color), typeof(Checkbox), defaultValue: Color.Blue, defaultBindingMode: BindingMode.OneTime);

        public Color OutlineColor
        {
            get { return (Color)GetValue(OutlineColorProperty); }
            set { SetValue(OutlineColorProperty, value); }
        }

        public static BindableProperty IsCheckedChangedCommandProperty = BindableProperty.Create(nameof(IsCheckedChangedCommand), typeof(ICommand), typeof(Checkbox), defaultValue: null, defaultBindingMode: BindingMode.OneTime);

        public ICommand IsCheckedChangedCommand
        {
            get { return (ICommand)GetValue(IsCheckedChangedCommandProperty); }
            set { SetValue(IsCheckedChangedCommandProperty, value); }
        }

        public static BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(Shape), typeof(Checkbox), defaultValue: Shape.Native, defaultBindingMode: BindingMode.OneTime);

        public Shape Shape
        {
            get { return (Shape)GetValue(ShapeProperty); }
            set { SetValue(ShapeProperty, value); }
        }

        public static BindableProperty IsCheckedProperty = BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(Checkbox), defaultValue: true, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            if (newValue is bool newValueAsBool)
            {
                Checkbox checkbox = (Checkbox)sender;

                if (newValueAsBool == true)
                    VisualStateManager.GoToState(checkbox, "Checked");
                else
                    VisualStateManager.GoToState(checkbox, "Unchecked");

                checkbox.IsCheckedChanged?.Invoke(checkbox, new IsCheckChangedEventArgs { IsChecked = newValueAsBool });

                if (checkbox.IsCheckedChangedCommand?.CanExecute(newValueAsBool) == true)
                    checkbox.IsCheckedChangedCommand?.Execute(newValueAsBool);
            }
        });

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICommand CheckTappedCommand { get; protected set; }

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
