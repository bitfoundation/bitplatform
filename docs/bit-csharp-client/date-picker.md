# Bit Date Picker

**BitDatePicker** is a fully customizable control that supports any calendar system that [noda time supports](https://nodatime.org/1.3.x/userguide/calendars) and any locale that .NET supports. It supports Android/iOS/UWP.

This calendar is dependent on [RgPluginsPopup](https://github.com/rotorgames/Rg.Plugins.Popup/wiki/Getting-started).

### How to Use BitDatePicker in Xaml:

```xml
<bitControls:BitDatePicker
    Text="Please select a date" 
    SelectedColor="LightBlue" 
    TodayColor="Blue" />
```

You can also customize **FontFamily**, **DateDisplayFormat**, **CalendarSystem** and **Culture**. For example, to change calendar system and locale to Solar (Persian) you can use following sample:

```xml

xmlns:noda="clr-namespace:NodaTime;assembly=NodaTime"

<bitControls:BitDatePicker
    Text="لطفا یک روز را انتخاب کنید" 
    Culture="Fa"
    DateDisplayFormat="dd MMM yyyy"
    CalendarSystem="{x:Static noda:CalendarSystem.PersianArithmetic}"
    FlowDirection="RightToLeft" />
```

In order to have a DatePicker with (Arabic-Persian) numbers, you can use a font with those numbers such as [Vazir-FD-WOL](https://github.com/rastikerdar/vazir-font/tree/master/dist/Farsi-Digits-Without-Latin) (FD=>Farsi Digits) (WOL=> Without Latin)

There is a **SelectedDate** property which is bindable.

In order to customize picker's button you can have followings:

```xml
<bitControls:BitDatePicker Text="Please Select a date">
    <bitControls:BitDatePicker.ControlTemplate>
        <ControlTemplate>
            <Frame HasShadow="False" Padding="5" BorderColor="LightGray">
                <StackLayout Orientation="Horizontal">
                    <StackLayout.GestureRecognizers>
                        <TapGestureRecognizer Command="{TemplateBinding OpenPopupCommand}"/>
                    </StackLayout.GestureRecognizers>
                    <Image
                        HeightRequest="15"
                        Source="Calendar.png"
                        WidthRequest="15" />
                        <Label FontFamily="{TemplateBinding FontFamily}" 
                        Text="{TemplateBinding DisplayText}" />
                    </StackLayout>
            </Frame>
        </ControlTemplate>
    </bitControls:BitDatePicker.ControlTemplate>
</bitControls:BitDatePicker>
```