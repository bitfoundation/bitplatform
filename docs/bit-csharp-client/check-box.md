# Bit Check Box

**BitCheckBox** is a fully customizable control which supports all Xamarin Forms backends, including but not limited to Android-iOS-Windows(UWP) etc.

### How to use BitCheckBox in Xaml:

```xml
<bitControls:BitCheckbox
            IsChecked="False"
            Text="Rectangle check box" />
```
you can also customise **CheckColor** , **Shape** ,**TextColor** ,**FillColor** ,**OutlineColor** and **OutlineColor** and also you have accses to **IsCheckedChangedCommand** .

A visualStateGroup is defined by name of **CheckboxStates** for BitCheckBox which has two staits by name of Checked and Unchecked . By following codes you would be able to give styles for different staits.

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
</Style>
```

In order to customise text of checkbox :

```xml
<bitControls:BitCheckbox>
    <Label FontAttributes="Bold" 
        Text="custom label for check box. You can also put any custom control here!" />
</bitControls:BitCheckbox>
```