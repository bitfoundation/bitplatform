namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public partial class CreateProjectHelp
{
    private sealed record HelpContent(string Title,
                                      string? Lead,
                                      string? Advice,
                                      string? Later,
                                      IReadOnlyList<(string Text, string Url)> Demos,
                                      IReadOnlyList<(string Question, string Answer)> Faq);

    private int? openFaq;
    private CreateProjectHelpTopic? shownTopic;

    protected override Task OnParamsSetAsync()
    {
        ResetFaqWhenTopicChanges();

        return base.OnParamsSetAsync();
    }

    protected override bool ShouldRender()
    {
        ResetFaqWhenTopicChanges();

        return base.ShouldRender();
    }

    private void ResetFaqWhenTopicChanges()
    {
        if (State.HelpTopic == shownTopic)
            return;

        shownTopic = State.HelpTopic;
        openFaq = null;
    }

    private void ToggleFaq(int index) => openFaq = openFaq == index ? null : index;

    private void Close(bool isOpen)
    {
        if (isOpen is false)
        {
            State.OpenHelp(null);
        }
    }

    private static HelpContent? Content(CreateProjectHelpTopic? topic) => topic switch
    {
        CreateProjectHelpTopic.Name => new(
            "Your product's name",
            "It names your project's folder and its code, so it uses English letters and numbers and starts with a letter: Bluebird, AcmeCrm or Contoso.Shop. The name your users see is separate: it can be anything, in any language, and you can change it any time.",
            "Use the name you'd use today. It's easy to change.",
            "Rename the app to Bluebird Plus",
            [],
            [
                ("Can I rename my project later?", "Yes. Ask your AI to rename it, or run bit rename NewName in its folder. The folders and the code are renamed together."),
                ("Why only English letters?", "The name becomes part of your code, where only English letters and numbers work everywhere. The name people see in the app, the stores and the browser tab can be in any language."),
                ("Do you keep the name?", "No. This page never sends your project's name anywhere.")
            ]),

        CreateProjectHelpTopic.Goal => new(
            "A real product, or a quick try?",
            "A real product is set up the way it will run for your customers, with a full database (PostgreSQL) that runs on your computer while you build. A quick try skips that: your data sits in one file, and a few sample pages show how a screen, its data and the server fit together.",
            "Building something you want to launch? Pick real product. Only curious? A quick try is ready sooner and installs less.",
            "Make this project ready for production: move it to PostgreSQL and remove the sample pages",
            [("See the sample pages live", "https://todo.bitplatform.dev/todo")],
            [
                ("Can a quick try become my real product?", "Yes, it's the same project. Ask your AI to move it to PostgreSQL and remove the sample pages."),
                ("Why does a real product install more?", "Its database runs on your computer in a container, so the setup adds a free container engine to run it. It's the same database your server runs once you publish, so nothing changes then."),
                ("Does either one cost anything?", "No. Both use free tools on your computer. What costs money comes later: the AI you build with, and the server you publish to.")
            ]),

        CreateProjectHelpTopic.Start => new(
            "What it starts with",
            "Every project already has sign-up and sign-in with passkeys and two-step verification, profiles, roles and permissions, settings, 11 languages, and light and dark themes. On top of that, it can start with an admin panel or an online store.",
            "Building a tool businesses use? Start with the admin panel and turn its products into your own things. Selling products? Start with the store.",
            "Add the admin panel with its dashboard",
            [("See the admin panel live", "https://adminpanel.bitplatform.dev"), ("See the store live", "https://sales.bitplatform.dev")],
            [
                ("What's in the admin panel?", "A dashboard with charts, and pages to manage products and categories with search, sorting and paging. With the assistant and live updates on, the dashboard changes the moment the data does."),
                ("What's in the online store?", "A shop front with products and their pages, ready to make your own."),
                ("Can I have both?", "Not in one go today. Start with the one closest to your product and ask your AI to build the rest."),
                ("Is the code mine to change?", "All of it. Whatever you start with is your own code, in your own repository.")
            ]),

        CreateProjectHelpTopic.Customers => new(
            "Several customer companies (Multi-tenancy)",
            "With multi-tenancy, one app serves many companies. Each company has its own users and sees only its own data, and one person can belong to several companies. Most software sold to businesses (SaaS) works this way.",
            "Selling to businesses? Keep several customer companies. Building for one organization, like your own company? Pick one organization.",
            "Add multi-tenancy, so each customer company sees only its own data",
            [("Try it in the admin panel demo", "https://adminpanel.bitplatform.dev")],
            [
                ("How do I try it in the demo?", "Sign in to the admin panel demo as store-user@bitplatform.dev with the password 12345678, and switch between the companies that user belongs to."),
                ("How is each company's data kept apart?", "On the server: the app only reads and writes the data of the company the user is signed in to, and nothing in the browser can change that."),
                ("Can I change this later?", "Yes, either way, by asking your AI. If you think you'll sell to businesses, starting with it is simpler.")
            ]),

        CreateProjectHelpTopic.Features => new(
            "The AI assistant and notifications",
            "The AI assistant lives inside your app and answers your users' questions, and can look things up in your app's data. It comes with live updates: open screens change the moment the data does. Push notifications reach your users on their phones, computers and browsers.",
            "Keep both unless you're sure you won't need them. Taking something out later is easier than adding it.",
            "Add the AI assistant to the app",
            [],
            [
                ("Does the AI assistant cost anything?", "It needs a key from an AI provider such as OpenAI, which charges by use. Until you add a key, the assistant stays hidden."),
                ("Is it the AI I build my app with?", "No. It's an assistant for the people who use your app. You build the app with an AI agent of your own, such as Claude Code, which opens with your project at the end."),
                ("Do push notifications cost anything?", "No. Your server sends them straight to Apple, Google and the browsers, with no paid service in between.")
            ]),

        CreateProjectHelpTopic.Errors => new(
            "Errors and logs",
            "Every app meets errors sometimes: a payment that fails, a page that doesn't load. Your app writes down each one with what led to it, so you and your AI can find the cause and fix it. This decides where it writes them.",
            "Not sure? Keep them on your own server, without additional cost and with no other account. Pick Sentry if you want your Android and iOS apps' crashes caught too, or the service your team already uses.",
            "Send the app's errors to Sentry",
            [],
            [
                ("What does each one cost?", "On your own server, nothing more than the server you already pay for. Sentry is free for one person and up to 5,000 errors a month, and its team plan starts at about $26 a month. Application Insights charges for what it keeps: the first 5 GB a month are free, then about $2.30 for each GB. Their prices change, so check sentry.io and azure.microsoft.com before you pay."),
                ("Do I need an account for Sentry or Application Insights?", "Yes, in your own name: Sentry's at sentry.io, or an Azure account for Application Insights. Your project is ready for it, and your AI walks you through pasting its key."),
                ("Do you see my errors?", "No. They stay on your server or go to your own account at Sentry or Microsoft."),
                ("Can I change it later?", "Yes. Ask your AI to send them somewhere else, and it adds what that needs.")
            ]),

        CreateProjectHelpTopic.Devices => new(
            "Where you try it today",
            "Every project has all five apps: web, Android, iPhone and iPad, Windows and Mac. This only decides which ones get set up on this computer now. Each native app adds a few GB of tools and some minutes to the setup.",
            "In a hurry? Keep only the web. You can still open your app on your phone, in its browser, through a link your project gives you.",
            "Set up the Android app on this computer",
            [],
            [
                ("Can I see it on my phone without these?", "Yes. While your app runs, the project gives you a secure link and a QR code. Scan it with your phone, use the app in its browser, and add it to your home screen if you like."),
                ("Do I lose the mobile apps if I pick only the web?", "No. The code of all five apps is always in your project. This only sets up the tools to build them on this computer."),
                ("Can I build the iPhone app on Windows?", "With a Mac's help: Apple's tools only run on a Mac, so the iPhone app builds on one with Xcode, on your network or in the cloud. Without a Mac, your project still has the iPhone app, ready for when you have one."),
                ("What does Android need?", "About 8 GB of downloads, and 16 GB of memory for the phone emulator. You can also plug in your own Android phone instead.")
            ]),

        CreateProjectHelpTopic.Database => new(
            "Where your data lives",
            "PostgreSQL is free, popular and the safe choice for a real product. SQLite keeps everything in one file and installs nothing, which suits a quick try. SQL Server and MySQL are there if your company or your host already uses them.",
            "Not sure? Keep PostgreSQL.",
            "Move the database from SQLite to PostgreSQL",
            [],
            [
                ("Do I have to install a database?", "No. The setup runs it for you in a container while you build, and your server runs it once you publish."),
                ("Anything to know about SQL Server?", "On a Linux server only SQL Server Express is free, with up to 50 GB for each database and no scheduled backups."),
                ("Anything to know about MySQL?", MySqlLicense)
            ]),

        CreateProjectHelpTopic.GitHub => new(
            "A private GitHub repository",
            "GitHub keeps your code and its whole history safe in your own account, and later checks every change and publishes your app to your server. The setup creates a private repository and puts your code in it; you sign in to GitHub once, in your browser.",
            "Keep it on. A free GitHub account is enough, and you can make one when the setup asks you to sign in.",
            "Create a private GitHub repository for this project and push it",
            [],
            [
                ("Who can see my code?", "Only you and the people you invite. The repository is private, and it's in your account, not ours."),
                ("My team uses GitLab. What should I do?", "Turn this off and push the project yourself. Your AI can turn the project's GitHub workflows into GitLab pipelines.")
            ]),

        CreateProjectHelpTopic.Offline => new(
            "Working without internet",
            "Your app normally reads and saves its data on your server, so it needs internet. With this, it also keeps a copy of its data on each phone, computer and browser: people go on working offline, and what they change is sent to your server once they're back online.",
            "Keep it off unless people really must work without internet, like in a warehouse or out in the field. It makes the web app a few MB bigger and slower to start.",
            "Add the offline database",
            [("See an offline app live", "https://todo-offline.bitplatform.cc")],
            [
                ("How does the app catch up with the server?", "It keeps a small database on the device, in the browser too. What people change offline is sent to your server once they're back online. The sample to-do list shows how; your own data needs the same setup, which your AI can do."),
                ("How much heavier does it make the web app?", "About 2 MB more for the web app to download, and a slower first start."),
                ("Can I add it later?", "Yes. Ask your AI to add the offline database.")
            ]),

        CreateProjectHelpTopic.Line => new(
            "About the line you paste",
            "It runs the bit CLI, which the Getting started page installs. The bit CLI sets up the tools your project needs, then creates your project with the answers you gave. Everything happens on your computer, step by step, and every step says what it's doing.",
            null,
            null,
            [("How to check the bit CLI", "/templates/getting-started")],
            [
                ("Is it safe to paste a line from a website?", "Only from bitplatform.dev. Scammers copy pages like this one and change the line, so check that the address bar says bitplatform.dev before you copy. The bit CLI is open source, signed and built only by GitHub Actions, and you can read its code before you run it."),
                ("Will it change my computer?", "It adds the tools your project needs, each only if it's missing, and you can remove them like any other app. It changes nothing else."),
                ("Do I need an administrator password?", "Only when a tool needs it: Windows asks once to allow a change, such as turning on what the database container needs, and on a Mac or Linux it asks once for your password."),
                ("What if something fails?", "The other steps carry on. At the end you see what failed, with the exact line that fixes it. If you're stuck, write to us.")
            ]),

        CreateProjectHelpTopic.General => new(
            "Before you start",
            null,
            null,
            null,
            [],
            [
                ("Do I need to know how to code?", "No. You tell your AI what you want in plain words; it builds it, runs the tests and explains the change. You paste one line, once."),
                ("Do I need an account?", "No. Nothing on this page needs one."),
                ("What does it cost?", "Creating your project costs nothing. Building and running it does: an AI agent such as Claude Pro (about $20 a month), a server once you publish (about €5 a month), and the app stores if you publish there (Apple $99 a year, Google $25 once)."),
                ("Is it ready for real customers?", "Yes. Sign-in with passkeys and two-step verification, roles and permissions, each company's data kept apart on the server, and automatic tests and checks are there from the first minute."),
                ("What do you see of my project?", "When you copy the line, the options you picked are sent to us without your project's name, so we learn which ones people use. Never your code, your data or your prompts."),
                ("What if bitplatform disappears?", "Your app keeps working. Its code is in your repository and its data on your server; nothing of it runs on ours."),
                ("Which computer do I need?", "Windows 10 or 11, a Mac, or Linux, with at least 8 GB of memory (16 GB for Android) and about 20 GB of free space."),
                ("Can I do this on my phone?", "You can answer the questions here and send them to your computer. The project itself is created on a computer.")
            ]),

        _ => null
    };

    public const string MySqlLicense = "Your project reaches MySQL through Oracle's library, which is under the GPL: an app whose code you keep closed needs a commercial license from Oracle.";
}
