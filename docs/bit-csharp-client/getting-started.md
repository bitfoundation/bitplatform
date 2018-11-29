## Getting Started

In order to use Bit CSharp Client Controls, install "Bit.CSharpClient.Controls" nuget package.

## Launching the controls in iOS

To launch the controls in iOS, call the  BitCSharpClientControls.Init(); method in the FinishedLaunching overridden method of the AppDelegate class after the Xamarin.Forms framework has been initialized and before the LoadApplication method is called as shown in the following code sample.
```
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

To launch the controls in iOS, call the  BitCSharpClientControls.Init(); method in the OnCreate overridden method of the MainActivity class after the Xamarin.Forms framework has been initialized.

```
protected override void OnCreate(Bundle savedInstanceState)
    {     
        …
        Forms.Init(this, savedInstanceState);

        BitCSharpClientControls.Init();

        LoadApplication(new App());
    }
```

## Launching the controls in Windows(UWP)

To launch the controls in UWP, call the  BitCSharpClientControls.Init(); method in the OnLaunched overridden method of the App.xaml.cs class.

```
protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        BitCSharpClientControls.Init();
    }
```

In xaml page add this namepsace to your page:

```
xmlns:bitControls="clr-namespace:Bit.CSharpClient.Controls;assembly=Bit.CSharpClient.Controls"
```