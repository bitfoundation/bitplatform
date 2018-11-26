# Bit Check Box

**BitCheckBox** is a fully customizable control .It supports all Xamarin Forms backends, including but not limited to Android-iOS-Windows(UWP) etc.

### Getting Started

In order to use BitCheckBox, install "Bit.CSharpClient.Controls" nuget package.

### Use BitCheckBox in Xaml

First of all, add a namepsace to your page:

```
xmlns:bitControls="clr-namespace:Bit.CSharpClient.Controls;assembly=Bit.CSharpClient.Controls"
```

And you're ready to go! No extra configuration is required!
```Xml
<bitControls:BitCheckbox
            IsChecked="False"
            Text="Rectangle check box" />
```
you can also customise **CheckColor** , **Shape** ,**TextColor** ,**FillColor** ,**OutlineColor** and **OutlineColor** and also you have accses to **IsCheckedChangedCommand** .

A visualStateGroup is defined by name of **CheckboxStates** for BitCheckBox which has two staits by name of Checked and Unchecked . By following codes you would be able to give styles for difrent staits.

```xml
<Style x:Key="ExtendedCheckboxUI" TargetType="bitControls:BitCheckbox">
            <Setter Property="VisualStateManager.VisualStateGroups">
                <VisualStateGroupList>
                    <VisualStateGroup x:Name="CheckboxStates">
                        <VisualState x:Name="Checked" />
                        <VisualState x:Name="Unchecked">
                            <VisualState.Setters>
                                <Setter Property="FillColor" Value="Yellow" />
                                <Setter Property="CheckColor" Value="Transparent" />
                            </VisualState.Setters>
                        </VisualState>
                    </VisualStateGroup>
                </VisualStateGroupList>
            </Setter>
```
In order to customise text of checkbox :
```xml
<bitView:BitCheckbox>
    <Label FontAttributes="Bold" 
        Text="custom label for check box. You can also put any custom control here!" />
</bitView:BitCheckbox>
```
To have better understanding see samples. Feedbacks/issues/questions are also welcomed in [stackoverflow](http://stackoverflow.com/questions/tagged/bit-framework) or [github](https://github.com/bit-foundation/bit-framework/issues/new?labels=&template=bug_report.md).

