## Bit Blazor Update Progress (Bit Bup)

To use BitBup, please follow these steps:

1. Install the `Bit.Bup` nuget package

2. In the default document (`_Host.cshtml`, `index.html` or `_Layout.cshtml`), add an `autostart="false"` attribute and value to the <script> tag for the Blazor script.

```html

<script src="_framework/blazor.webassembly.js" autostart="false"></script>

```

3. In the default document (`_Host.cshtml`, `index.html` or `_Layout.cshtml`), add the `Bit.Bup` reference after the <script> tag for the Blazor script.
```html

<script src="_content/Bit.Bup/bit-bup.js" log="verbose" handler="bitBupHandler"></script>

```

- log: The log level for log provider. log options: `info`, `verbose`, `debug`, `error`
- handler: The name of handler for the service worker events
> You can not specify the values of the attributes, and use the default values which are equal to the above values. 

4. Add a handler in the simplest way possible, like the below code. or you can add a handler with a progress process bar like the bitBupHandler on the sample in the index.html file of the demo project in this repo.

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