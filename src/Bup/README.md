## bit Blazor Update Progress (bit Bup)

To use bit Bup, please follow these steps:

1. Install the `Bit.Bup` nuget package
```bat
dotnet add package Bit.Bup
```

2. In the default document (`index.html`, `_Host.cshtml`, or `_Layout.cshtml`), add an `autostart="false"` attribute and value to the script tag of the Blazor script.

```html

<script src="_framework/blazor.webassembly.js" autostart="false"></script>

```

3. Also In the default document (`index.html`, `_Host.cshtml`, or `_Layout.cshtml`), add the `Bit.Bup` reference after the script tag of the Blazor script.

```html

<script src="_content/Bit.Bup/bit-bup.js" log="verbose" handler="bitBupHandler"></script>

```

- `log`: The log level of the bit Bup logger. available options are: `info`, `verbose`, `debug`, and `error`. (not implemented yet)
- `handler`: The name of the handler function for the bit Bup events.

> You can remove any of these attributes, and use the default values mentioned above.

4. Add a handler function like the below code to handle multiple events of the bit Bup, or you can follow the full sample code which is provided in the Demo projects of this repo.

```js

function bitBupHandler(type, data) {
    switch (type)
    {
        case 'start':
            return console.log('downloading started.');
        case 'progress':
            return console.log('resource downloaded:', data);
        case 'end':
            return console.log('downloading ended.');
    }
}

```