# Butil
Blazor Utilities for JavaScript

---

To start using `Butil` you first need to add its services like this:

```csharp
using Bit.Butil;

...

builder.Services.AddBitButilServices();

...
```

Then you can inject its classes for each tool. 

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


### Document

To use a representation of the DOM's `document` object in C# you can inject the `Bit.Butil.Document` class:

```razor
@@inject Bit.Butil.Document document

...

@@code {

    ...

    await document.AddEventListener(ButilEvents.Click, args => { ... });

    ...
}
```


### Window

To use a representation of the DOM's `window` object in C# you can inject the `Bit.Butil.Window` class:

```razor
@@inject Bit.Butil.Window window

...

@@code {

    ...

    await window.AddEventListener(ButilEvents.KeyDown, args => { ... });

    ...
}
```

### Keyboard
In Butil there is a special class to work with keyboard and short keys. you can use this class by inejcting the `Bit.Butil.Keyboard` class:

```razor
@inject Bit.Butil.Keyboard keyboard

...

@@code {
    ...

    await keyboard.Add("F10", args => { ... }, , ButilModifiers.Alt | ButilModifiers.Ctrl);

    ...
}
```