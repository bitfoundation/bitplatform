# Server sent events or Web Sockets?

There are several modern libraries to call rest api in client side code such as [fetch](https://developer.mozilla.org/en/docs/Web/API/Fetch_API), ".NET Http Client" etc.

We also have "OData $metadata" and "Swagger metadata" that allow us to generate our client side code we need to call our rest api very easily. For example [NSwag](https://github.com/RSuter/NSwag) can generate your codes.

These libraries are extensible, with caching, resiliency and security built-in.

Server side frameworks such as [Web API](https://www.asp.net/web-api) and [Service Stack](http://docs.servicestack.net/api-design) are also mature, powerful, extensible with lots of features you need to develop modern apps. They've routing, dependency injection, logging, security etc.

We also use web sockets to push some data from server to client. But Web socket introduces several problems as we begin to use that in our apps:

1- It opens one connection from server to client \(the handy useful one\), and one another from client to server. We almost have nothing to do with the second connection \(from client to server\).But it uses our valuable resources such as memory etc with no value provided in turn. If we start to use that connection instead of our modern http clients, we lose everything provided by ASP.NET Web API, Service Stack, Swagger, OData, fetch, Http client, service worker etc.

2- When you create [CORS policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Access_control_CORS), it won't be applied to web sockets, as that is HTTP based. \(Web Sockets and HTTP are two different protocols\)

3- When you create http compression policy, it won't be applied to your Web Sockets.

While we're developing [Bit Platform](https://github.com/bitfoundation/bitplatform/), we add every best practice we know in its "default" configuration. As Bit Platform supports HTTP 2.0, you'll use only one connection from client to server that works with your favorite modern http client, and at server side you've web api that you already know. To push from server to client you'll use [Server Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events) instead of web sockets by default. Server Sent Events \(SSE\) is a HTTP thing, it supports http CORS policies, respects http compression policies and uses less resources. By Using Bit Platform's architecture you can provide services to more users with the same hardware and budget.

If you're not using Bit, you can use SSE on you're own. For example, in Signalr, start hub by following code in client side:

```text
$.connection.hub.start({ transport: ["serverSentEvents", "webSockets"] })
```

By that code, signalr uses server sent events by default.

> **\[info\]Client Support:**
>
> Server sent events is supported across all C\# clients, including WPF, Xamarin for android and iOS etc. [95.34%](http://caniuse.com/#feat=eventsource) of browsers support SSE and for those with no support, you can use [EventSource polyfill](https://github.com/amvtek/EventSource)

## Conclusion:

Use Server Sent Events + HTTP 2.0 instead of Web Sockets, if you've nothing to do with client to server part of web sockets.

