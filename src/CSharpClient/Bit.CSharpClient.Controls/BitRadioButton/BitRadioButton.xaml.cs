using System;
using System.Collections.Generic;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.View.Controls
{
    [ContentProperty(nameof(Content))]
    public partial class BitRadioButton
    {
        internal static List<WeakReference<BitRadioButton>> BitRadioButtons = new List<WeakReference<BitRadioButton>>();

        internal static void UnSelectOtherBitRadioButtons(BitRadioButton currentRadioButtonView)
        {
            foreach (WeakReference<BitRadioButton> radioButtonWeakReference in BitRadioButtons)
            {
                if (radioButtonWeakReference.TryGetTarget(out BitRadioButton radioButtonView) && radioButtonView != currentRadioButtonView && radioButtonView.Key.GetType() == currentRadioButtonView.Key.GetType())
                {
                    radioButtonView.IsSelected = false;
                }
            }
        }

        public BitRadioButton()
        {
            BitRadioButtons.Add(new WeakReference<BitRadioButton>(this));

            InitializeComponent();

            SelectedTappedCommand = new Command<BitRadioButton>(radioButton =>
            {
                Value = Key;
                IsSelected = true;
                UnSelectOtherBitRadioButtons(radioButton);
            });
        }

        public virtual void SelectedItemChanged()
        {
            if (Value.Equals(Key))
            {
                IsSelected = true;
            }
        }

        public virtual Xamarin.Forms.View Content { get; set; }

        public bool IsSelected { get; protected set; }

        public ICommand SelectedTappedCommand { get; protected set; }

        public static BindableProperty BorderColorProperty = BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(BitRadioButton), defaultValue: Color.Gray, defaultBindingMode: BindingMode.OneWay);

        public virtual Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }

        public static BindableProperty InerCircleColorProperty = BindableProperty.Create(nameof(InerCircleColor), typeof(Color), typeof(BitRadioButton), defaultValue: Color.Blue, defaultBindingMode: BindingMode.OneWay);

        public virtual Color InerCircleColor
        {
            get => (Color)GetValue(InerCircleColorProperty);
            set => SetValue(InerCircleColorProperty, value);
        }

        public static BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(BitRadioButton), defaultValue: null, defaultBindingMode: BindingMode.OneWay);

        public virtual string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(BitRadioButton), defaultValue: Color.Black, defaultBindingMode: BindingMode.OneWay);

        public Color TextColor
        {
            get { return (Color)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        public static BindableProperty KeyProperty = BindableProperty.Create(nameof(Key), typeof(object), typeof(BitRadioButton), defaultValue: null, defaultBindingMode: BindingMode.OneWay);

        public virtual object Key
        {
            get => GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public static BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(object), typeof(BitRadioButton), defaultValue: null, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            BitRadioButton bitRadioButton = (BitRadioButton)sender;
            bitRadioButton.SelectedItemChanged();
        });

        public virtual object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
