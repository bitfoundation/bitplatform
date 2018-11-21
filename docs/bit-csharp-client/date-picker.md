# Bit Date Picker

**BitDatePicker** is a fully customizable control that supports any calendar system that [noda time supports](https://nodatime.org/1.3.x/userguide/calendars) and any locale that .NET supports. It supports all Xamarin Forms backends, including but not limited to Android-iOS-Windows(UWP) etc.

### Getting Started

In order to use BitDatePicker, install "Bit.CSharpClient.Controls" nuget package.

### Use BitDatePicker in Xaml

First of all, add a namepsace to your page:

```
xmlns:bitControls="clr-namespace:Bit.CSharpClient.Controls;assembly=Bit.CSharpClient.Controls"
```

And you're ready to go! No extra configuration is required!

```xml
<bitControls:BitCalendarPicker
    Text="Please select a date" 
    SelectedColor="LightBlue" 
    TodayColor="Blue" />
```

You can also customize **FontFamily**, **DateDisplayFormat**, **CalendarSystem** and **Culture**. For example, to change calendar system and locale to Solar (Persian) you can use following sample:

```xml

xmlns:noda="clr-namespace:NodaTime;assembly=NodaTime"

<bitControls:BitCalendarPicker
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
    <bitControls:BitCalendarPicker Text="Please Select a date">
            <bitControls:BitCalendarPicker.ControlTemplate>
                <ControlTemplate>
                    <StackLayout Orientation="Horizontal">
                        <StackLayout.GestureRecognizers>
                            <TapGestureRecognizer Command="{TemplateBinding OpenPopupCommand}" />
                        </StackLayout.GestureRecognizers>
                        <Image
                            HeightRequest="15"
                            Source="Calendar.png"
                            WidthRequest="15" />
                        <Label FontFamily="{TemplateBinding FontFamily}" Text="{TemplateBinding DisplayText}" />
                    </StackLayout>
                </ControlTemplate>
            </bitControls:BitCalendarPicker.ControlTemplate>
    </bitControls:BitCalendarPicker>
```

To have better understanding see samples. Feedbacks/issues/questions are also welcomed in [stackoverflow](http://stackoverflow.com/questions/tagged/bit-framework) or [github](https://github.com/bit-foundation/bit-framework/issues/new?labels=&template=bug_report.md).

ToDo => Take some photos from BitDatePicker + move stuffs to bit repository + Create samples project!