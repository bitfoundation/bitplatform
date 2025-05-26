# Navigating the Blazor Maze: A Developer's Journey to Production-Ready Web Apps

The growing number of ASP.NET Blazor rendering modes—**Server**, **Auto**, **WebAssembly**, **Hybrid**, and **Static SSR**—coupled with crucial aspects like Progressive Web Applications (**PWA**) and **Pre-Rendering**, has clearly made it harder to build applications that are ready for real-world use.

We are going to discuss the following headlines:

1- Blazor Server, which runs component logic and maintains a virtual DOM server-side, using WebSocket for UI interactions and updates.

2- Blazor WebAssembly, which handles component logic and virtual DOM client-side via WebAssembly, communicating with the server via gRPC or HTTP.

3- Blazor Auto, which uses Blazor Server on a user's first visit and switches to WebAssembly for subsequent visits.

4- Blazor Hybrid, which runs component logic and maintains a virtual DOM in Android, iOS, Windows, or macOS apps, with full access to native platform features.

Writing code compatible across these modes is generally feasible, as seen in the `.NET MAUI Blazor Hybrid and Web App template`, enabling a single codebase for all modes.

Note: **You can always enable pre-rendering for SEO-friendliness and improved initial user experience in Blazor Server, Auto, or WebAssembly modes.**

Additionally, Blazor Static SSR pre-renders HTML on the first visit, relying on JavaScript or form posts to server endpoints that return HTML instead of JSON.

**The Blazor Server Development Trap: Initial Comfort, Future Hurdles**

I'm starting with Blazor Server. This mode offers a great experience for developers (DX), with easy Hot Reload and Debugging during development, but it's usually not the best option for running apps in production.

**Why is this the case?**

1. **Fundamental limitations in certain scenarios:** such as the development of Offline Web Applications.
2. **Scalability and cost inefficiencies:** For apps with complex pages or lots of users at the same time, Blazor Server can run into big problems. That’s because it keeps the UI state (like product pages, data, the Virtual DOM, and component states) in memory on the server for every connected user. This makes it harder to scale and can get expensive. The issue gets worse because memory leaks are common in frontend apps - not just in Blazor, but also in React, Vue, and Angular - often because developers miss or forget certain best practices. On the client side, a memory leak might just use a bit more RAM. But in Blazor Server, **these leaks pile up on the server**, which can quickly become a serious problem. Since tracking down memory leaks is tough, this makes Blazor Server a risky choice for apps with a lot of users.
3. **Features exclusive to Blazor Server:** Some coding practices that work in Blazor Server don’t work at all in other Blazor modes. A common example is using `DbContext` directly in Razor components or in services used by those components. In Blazor Server, this works because the code runs on the server and can access the database. But in client-side modes like Blazor WebAssembly, it will fail. That’s because code running in the browser can’t - and for important security and design reasons **shouldn’t** \- connect directly to a database server.
4. **Hidden costs for other Blazor modes:** Some design choices can lead to hidden problems when moving from Blazor Server to other Blazor modes. For example, using large UI libraries might seem fine in Blazor Server, since only the JavaScript and CSS files are sent to the browser. But if you later switch to something like Blazor WebAssembly or Hybrid, the app may become much heavier to load - often adding 5MB to 15MB - because the browser now has to download the .NET files for those UI tools.
5. **Latency:** Our tests show that opening a simple dropdown with various UI libraries can trigger around 30 WebSocket messages in Blazor Server. If the user's network is slow or has high latency, even basic actions like opening dropdowns or popups can feel delayed.

In my view, **Only focusing on Blazor Server and sticking to the coding styles and mastering a heavy UI Toolkit that only work well in its server-side setup, isn’t a strong enough reason to fully commit to learning Blazor.**. That’s because Blazor Server usually isn’t a good fit for building mobile or desktop apps (like with Blazor Hybrid), working offline, or handling websites with a lot of traffic. **Blazor Server can work well for internal business apps, but the skills and patterns you learn for it might not easily carry over to fully using everything Blazor has to offer.**. When used with the right approaches for its other hosting models, Blazor becomes a flexible framework that can be used to build almost any kind of app or website.

So, what is the **recommended approach**?

* Use a **lightweight UI toolkit**.
* Communicate with the backend server via gRPC or standard HTTP clients.
* Avoid an excessive number of NuGet packages in the client project. For example, in our projects, we handle JWT parsing on the client-side with just a few lines of code, whereas I've seen projects incorporating the heavy `System.IdentityModel.Tokens.Jwt` package for this. Similar to JavaScript frontend developers, **We need to build the habit of not carelessly adding large packages to the frontend.**.

This approach helps us create lightweight web apps - usually between 2.5MB and 4.5MB - that still have all the features we need and perform well, no matter how many users visit them, with full SEO requirements applied.

**In this development approach, it's totally fine to use Blazor Server while building the app because of its great developer experience, and then switch to Blazor WebAssembly for the final production version.**

With Blazor Auto, it’s important to remember that the required JS and CSS files still have to be downloaded by the browser, even during the first server-rendered phase.

**Every megabyte of compressed code (CSS, JS, or WASM) can take about one second to process on mobile devices with slower CPUs. This processing is independent of the initial download time and occurs each time the client-side application initializes**.

If your app is large, subsequent visits starting it up on the client side - especially on mobile devices - can be slow. Therefore, **do not rely on Blazor Auto as a justification for bloating your project with unnecessary packages.**

When looking at Blazor Static SSR (Static Server-Side Rendering), it's worth first asking: if you can already build a fast, SEO-friendly, and fully functional website using C# and Blazor WebAssembly (like [bitplatform.dev](https://bitplatform.dev) and [sales.bitplatform.dev](https://sales.bitplatform.dev/)), do you really need it? Why opt for Blazor Static SSR then?

In my view, Blazor Static SSR can lead to the following problems:

* A problematic mix of C# and JavaScript: In this model, C# code can't run in the browser, and there's no WebSocket connection to run C# on the server after the page is loaded, so you lose C# based interactivity.
* This means you have to rely heavily on Enhanced Forms or htmx-style solutions. These not only put more load on the server but also promote **coding styles and architecture** that don’t fit well with Blazor Hybrid apps (for Android, iOS, Windows, macOS) or offline web apps.
* Alternatively, for adding rich client-side interactivity in Static SSR, one might rely heavily on JavaScript interop. But this creates a **messy mix of C# and JavaScript**, which can make the project much harder to maintain.

**PWA:**

In my view, almost all web apps should include PWA features. If you disagree, it’s likely because you’ve run into a poorly implemented PWA. For example, many setups don’t work with pre-rendering, have issues in restricted environments like Firefox’s private mode, download all static files by default even when offline support isn’t needed, or struggle with updates. But these problems aren’t caused by PWA technology itself, they come from how it’s implemented. We’ve managed to solve these issues fairly easily in our own web apps.

What is the objection to an e-commerce site using both Pre-Rendering and Push Notifications (with user permission) to let users know when a product is back in stock or letting users install the app for a full-screen, easier-to-use experience?

**Pre-Rendering:**

When it comes to Pre-Rendering, it’s important to know that you can make your app SEO-friendly using Blazor Server, Auto, or WebAssembly.

* You can limit Pre-Rendering to the "User's first visit" (see demo: [todo.bitplatform.dev](https://todo.bitplatform.dev)). This reduces server load on subsequent visits, as the site can then leverage the client's cache storage, leading to a performance gain of 50ms to 300ms in load time.
* Alternatively, you can "Always" enable Pre-Rendering. Why would you do this? As mentioned, processing CSS/JS/WASM assets—even those already downloaded—requires CPU cycles every time the web app runs. On less powerful mobile devices, this can take, for instance, one to two seconds. If you have an e-commerce website, "Always Pre-Rendering" ensures that when a user clicks on a product link (even on return visits), they see the information immediately without waiting for the web app to become interactive. (See demo: [sales.bitplatform.dev](https://sales.bitplatform.dev/)).

However, if Pre-Rendering is unnecessary, for example, an admin panel where no specific information is accessible before sign-in, you can publish Blazor WebAssembly without pre-rendering (e.g., [adminpanel.bitplatform.dev](https://adminpanel.bitplatform.dev)). An even better approach is to use Blazor WebAssembly Standalone and deploy it on Azure Static Web Apps (which offers a generous free tier) (e.g., [adminpanel.bitplatform.cc](https://adminpanel.bitplatform.cc)). This ensures that even if the backend server is slow or down, the application itself will still load, even on the user's first visit. Users would then receive an appropriate error message upon interaction with the server, prompting them to wait or try again.

If you're wondering what extensive features a 4MB application can offer, or what capabilities a modern application should possess today, such as built-in 2-Factor Authentication, social sign-in (e.g., Google, Apple), comprehensive localization, an integrated AI Chatbot, dynamic theming, and robust feature/permission management, [this 15-minute video](https://youtu.be/-3viBEtJHLo) provides an excellent answer. All of these powerful features are available to you as **Free and Open Source** through our [bit Boilerplate Project Template](http://bitplatform.dev/templates).

Using the `bit Boilerplate`, you can develop with Blazor Server for an optimal developer experience and ultimately produce Blazor WebAssembly outputs (with or without Pre-Rendering). These outputs are PWA-enabled thanks to [bitplatform's PWA package for Blazor Apps](https://bitplatform.dev/bswup), and Pre-Rendering can be configured for the "First User Visit Only" or "Always". Furthermore, the same codebase can generate Blazor WebAssembly Standalone applications and Blazor Hybrid outputs for Android, iOS, Windows, and macOS, with full access to native OS capabilities.

You can install and experience these applications showcasing these features directly from the [Google Play and Apple Store](https://bitplatform.dev/demos#adminpanel).

To start utilizing this template and building your own advanced applications, head over to [Getting started](http://bitplatform.dev/templates).

Should you encounter any issues or have questions, please feel free to raise them on our [GitHub Repository](https://github.com/bitfoundation/bitplatform/).

A key enabler for achieving such lightweight yet full-featured applications, particularly when using the `bit Boilerplate`, is the choice of UI components. The [bit BlazorUI](https://blazorui.bitplatform.dev) library, which is integral to our template, provides access to over 80 full-featured components while maintaining an exceptionally small footprint. Indeed, according to [this benchmark](https://github.com/bitfoundation/blazor-benchmarks), it stands as one of the lightest yet most comprehensive UI libraries available, empowering you to build rich, modern user interfaces without incurring significant performance overhead or application size penalties.

Thank you for taking the time to read this. I welcome your feedback and am happy to answer any questions you may have.