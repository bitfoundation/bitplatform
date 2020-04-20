## Controls Config

In order to use Bit CSharp Client Controls, install "Bit.CSharpClient.Controls" nuget package.

## Launching the controls in iOS

To launch the controls in iOS, call the  BitCSharpClientControls.Init(); method in the FinishedLaunching overridden method of the AppDelegate class after the Xamarin.Forms framework has been initialized and before the LoadApplication method is called as shown in the following code sample:

```cs
public override bool FinishedLaunching(UIApplication app, NSDictionary options) 
{ 
    … 
    global::Xamarin.Forms.Forms.Init();

    BitCSharpClientControls.Init();

    LoadApplication(new App()); 
    …
}
```

## Launching the controls in Android

To launch the controls in Android, call the  BitCSharpClientControls.Init(); method in the OnCreate overridden method of the MainActivity class after the Xamarin.Forms framework has been initialized.

```cs
protected override void OnCreate(Bundle savedInstanceState)
{     
    …
    Forms.Init(this, savedInstanceState);

    BitCSharpClientControls.Init();

    LoadApplication(new App());
    …
}
```

You'll also need following LinkerConfig.xml

```xml
<assembly fullname="Bit.CSharpClient.Controls">
    <type fullname="*" />
  </assembly>
  <assembly fullname="System.Core">
    <type fullname="*" />
  </assembly>
  <assembly fullname="Rg.Plugins.Popup">
    <type fullname="*" />
  </assembly>
  <assembly fullname="NodaTime">
    <type fullname="*" />
  </assembly>
  <assembly fullname="mscorlib">
    <type fullname="System.Globalization.*" />
    <type fullname="System.DateTime">
      <method name="AddYears"></method>
      <method name="AddMonths"></method>
      <method name="AddDays"></method>
      <method name="AddHours"></method>
      <method name="AddMinutes"></method>
      <method name="AddSeconds"></method>
    </type>
  </assembly>
```

## Launching the controls in Windows(UWP)

To launch the controls in UWP, call the BitCSharpClientControls.Init(); method in the OnLaunched overridden method of the App.xaml.cs class.

```cs
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    …
    BitCSharpClientControls.Init();
    …
}
```

## Shared project / .NET Standard project config:

In xaml page add following namepsace to your page:

```xml
xmlns:bit="https://bitframework.com"
```

And put following in App.xaml.cs after InitializeComponent();

```cs
BitCSharpClientControls.XamlInit();
```

To have better understanding see [samples](/Samples/CSharpClientSamples/Controls.Samples).

Feedbacks/issues/questions are also welcomed in [stackoverflow](http://stackoverflow.com/questions/tagged/bit-framework) or [github](https://github.com/bit-foundation/bit-framework/issues/new?labels=&template=bug_report.md).
