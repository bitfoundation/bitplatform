# bit Butil
bit Blazor Utilities for JavaScript

---

To start using `Butil` you first need to install the NuGet package:

```
dotnet add package Bit.Butil
```

then add its script tag to your app:

```html
<script src="_content/Bit.Butil/bit-butil.js"></script>
```

you also need to add its services like this:

```csharp
using Bit.Butil;

...

builder.Services.AddBitButilServices();

...
```

Now you can inject its classes to use the utilities.


### Window

To use a representation of the DOM's `window` object in C# you can inject the `Bit.Butil.Window` class:

```razor
@inject Bit.Butil.Window window

...

@code {

    ...

    await window.AddEventListener(ButilEvents.KeyDown, args => { ... });

    ...
}
```


### Document

To use a representation of the DOM's `document` object in C# you can inject the `Bit.Butil.Document` class:

```razor
@inject Bit.Butil.Document document

...

@code {

    ...

    await document.AddEventListener(ButilEvents.Click, args => { ... });

    ...
}
```


### Keyboard
In Butil there is a special class to work with keyboard and short keys. you can use this class by inejcting the `Bit.Butil.Keyboard` class:

```razor
@inject Bit.Butil.Keyboard keyboard

...

@code {
    ...

    await keyboard.Add("F10", args => { ... }, , ButilModifiers.Alt | ButilModifiers.Ctrl);

    ...
}
```


### Console

To use the browser console features you can try injecting the `Bit.Butil.Console` class like this:

```razor
@inject Bit.Butil.Console console

...

@code {
    ...

    console.Log("This is a test log:", someValue);
    console.Assert(condition, "The condition failed!", testedValue);
    console.Error("This is a test error:", value);

    ...
}
```