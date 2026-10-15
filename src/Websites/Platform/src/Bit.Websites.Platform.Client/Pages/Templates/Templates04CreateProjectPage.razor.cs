using System.Text.RegularExpressions;
using Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates04CreateProjectPage
{
    private const int QuestionCount = 17;

    private static readonly ProjectSetup pageDefaults = new() { SignalR = true, Captcha = "reCaptcha" };
    private static readonly ProjectSetup realProduct = WithPageDefaults(ProjectSetup.RealProduct());
    private static readonly ProjectSetup testDrive = WithPageDefaults(new ProjectSetup { Sample = true });

    private static readonly Faq[] finalFaqs =
    [
        new("What does bit new install on my machine?",
            "It checks this machine and installs what's missing, such as the .NET SDK, Node.js, Git, Docker when Aspire needs it and VS Code. Then it creates the project, sets up git, builds it, adds the first database migration, installs the recommended editor extensions and opens the project. A step that fails doesn't stop the others."),
        new("How do I run my new project?",
            "If you use an AI assistant, just ask it to run the project. Otherwise, follow the [Run project](/templates/run-project) page.")
    ];

    private static readonly ProjectOption[] technicalExtras =
    [
        ProjectOptionCatalog.Get("aspire"),
        ProjectOptionCatalog.Get("cloudflare"),
        ProjectOptionCatalog.Get("brouter")
    ];

    private static readonly BitAccordionClassStyles summaryRowClasses = new()
    {
        Root = "summary-accordion",
        Header = "summary-header",
        Title = "summary-title",
        Content = "summary-content"
    };

    private ProjectSetup setup = pageDefaults.Clone();
    private Step[] steps = [];
    private int stepIndex;
    private bool focusQuestion;
    private ElementReference questionHeading;
    private string? purpose;
    private readonly HashSet<string> touched = [];
    private readonly List<string> purposeKeys = [];
    private Faq? openFaq;
    private int? openSummaryRow;

    private Step CurrentStep => steps[stepIndex];

    private bool IsFinalStep => stepIndex >= QuestionCount;

    private bool CanSkip => IsFinalStep is false && stepIndex > 0 && IsAnswered(CurrentStep) is false;

    private ProjectSetup Baseline => purpose is "real" ? realProduct : pageDefaults;

    private string ProgressText => IsFinalStep ? "All done" : $"Question {stepIndex + 1} of {QuestionCount}";

    private double ProgressPercent => (stepIndex + 1) * 100.0 / (QuestionCount + 1);

    private string NextText => stepIndex == QuestionCount - 1 ? "Show my command" : "Next";

    private string? NameError
    {
        get
        {
            var name = setup.Name;

            if (string.IsNullOrWhiteSpace(name))
                return "Your project needs a name.";

            if (name.Length > 50)
                return "Keep it under 50 characters.";

            if (name.Split('.').All(part => part.Length > 0 && (char.IsLetter(part[0]) || part[0] is '_') && part.All(c => char.IsLetterOrDigit(c) || c is '_')) is false)
                return "Use letters, numbers and dots, and start each part with a letter, for example MyShop or Contoso.Shop.";

            return null;
        }
    }

    protected override Task OnInitAsync()
    {
        steps = CreateSteps();

        return base.OnInitAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (focusQuestion is false)
            return;

        focusQuestion = false;
        await questionHeading.FocusAsync();
    }

    private void GoTo(int index)
    {
        stepIndex = Math.Clamp(index, 0, QuestionCount);
        openFaq = null;
        openSummaryRow = null;
        focusQuestion = true;
    }

    private void GoNext() => GoTo(stepIndex + 1);

    private void GoBack() => GoTo(stepIndex - 1);

    private void ShowCommand() => GoTo(QuestionCount);

    private void StartOver()
    {
        setup.ApplyPreset(pageDefaults);
        touched.Clear();
        purposeKeys.Clear();
        purpose = null;
        GoTo(0);
    }

    private void Choose(ChoiceGroup group, Answer answer)
    {
        answer.Choose();

        foreach (var key in group.Keys)
        {
            touched.Add(key);
        }
    }

    private void MarkTouched(string key) => touched.Add(key);

    private void ChoosePurpose(string value)
    {
        if (purpose == value)
            return;

        ResetPurpose();

        purpose = value;
        var preset = value is "real" ? realProduct : testDrive;

        foreach (var option in ProjectOptionCatalog.All)
        {
            if (SameValue(option, preset, pageDefaults) || touched.Contains(option.Key))
                continue;

            CopyValue(option, preset, setup);
            purposeKeys.Add(option.Key);
        }
    }

    private void ResetPurpose()
    {
        foreach (var key in purposeKeys.Where(k => touched.Contains(k) is false))
        {
            CopyValue(ProjectOptionCatalog.Get(key), pageDefaults, setup);
        }

        purposeKeys.Clear();
        purpose = null;
    }

    private void ToggleFaq(Faq faq) => openFaq = openFaq == faq ? null : faq;

    private void ToggleSummaryRow(int index, bool expanded) => openSummaryRow = expanded ? index : openSummaryRow == index ? null : openSummaryRow;

    private static IEnumerable<AnswerPart> AnswerParts(string answer)
    {
        foreach (Match match in AnswerPartRegex().Matches(answer))
        {
            var text = match.Value;

            if (text.StartsWith('`'))
            {
                yield return new(text[1..^1], null, true);
            }
            else if (text.StartsWith('[') && text.EndsWith(')'))
            {
                var separator = text.IndexOf("](", StringComparison.Ordinal);
                yield return new(text[1..separator], text[(separator + 2)..^1], false);
            }
            else
            {
                yield return new(text, null, false);
            }
        }
    }

    [GeneratedRegex(@"`[^`]+`|\[[^\]]+\]\([^)]+\)|[^`\[]+|[`\[]")]
    private static partial Regex AnswerPartRegex();

    private static ProjectSetup WithPageDefaults(ProjectSetup preset)
    {
        preset.SignalR = pageDefaults.SignalR;
        preset.Captcha = pageDefaults.Captcha;
        return preset;
    }

    private static bool SameValue(ProjectOption option, ProjectSetup first, ProjectSetup second) => option.IsChoice
        ? option.GetChoice!(first) == option.GetChoice!(second)
        : option.GetBool!(first) == option.GetBool!(second);

    private static void CopyValue(ProjectOption option, ProjectSetup from, ProjectSetup to)
    {
        if (option.IsChoice)
        {
            option.SetChoice!(to, option.GetChoice!(from));
        }
        else
        {
            option.SetBool!(to, option.GetBool!(from));
        }
    }

    private bool IsAnswered(Step step) => step.Key is "purpose"
        ? purpose is not null
        : step.Groups.SelectMany(g => g.Keys).Any(touched.Contains);

    private List<EffectPart> EffectFor(ChoiceGroup group) => group.Effect?.Invoke() ?? EffectOf(group.Keys);

    private List<EffectPart> StepEffect(Step step) => [.. step.Groups.SelectMany(EffectFor)];

    private List<EffectPart> EffectOf(IEnumerable<string> keys)
    {
        List<string> added = [];
        List<string> dropped = [];

        foreach (var option in keys.Select(ProjectOptionCatalog.Get))
        {
            if (option.Applies(setup) is false || option.IsModified(setup) is false)
                continue;

            if (option.Key is "githubRepo")
            {
                dropped.Add(option.Flag!);
            }
            else if (option.IsChoice)
            {
                added.Add($"{option.Flag} {option.GetChoice!(setup)}");
            }
            else
            {
                added.Add(option.GetBool!(setup) ? option.Flag! : $"{option.Flag} false");
            }
        }

        List<EffectPart> parts = [];

        if (added.Count > 0)
        {
            parts.Add(new("adds", string.Join(' ', added)));
        }

        if (dropped.Count > 0)
        {
            parts.Add(new("drops", string.Join(' ', dropped)));
        }

        return parts;
    }

    private List<EffectPart> PlatformsEffect()
    {
        List<string> platforms = ["web"];

        if (setup.Android) platforms.Add("android");
        if (setup.Ios) platforms.Add("ios");
        if (setup.MacOS) platforms.Add("macos");
        if (setup.Windows) platforms.Add("windows");

        if (platforms.Count is 1)
            return [];

        return [new("adds", $"--platforms {string.Join(',', platforms)}")];
    }

    private string Describe(Step step)
    {
        if (step.Key is "name")
            return setup.Name;

        var answers = step.Groups
            .SelectMany(g => g.Answers.Where(a => a.IsChosen()))
            .Select(a => a.Summary ?? a.Text)
            .ToArray();

        return answers.Length is 0 ? "Not answered" : string.Join(", ", answers);
    }

    private Answer Pick(string key, string value, string text, string hint, string? summary = null, bool none = false)
    {
        var option = ProjectOptionCatalog.Get(key);

        return new()
        {
            Text = text,
            Hint = hint,
            Summary = summary,
            IsNone = none,
            IsChosen = () => option.GetChoice!(setup) == value,
            Choose = () => option.SetChoice!(setup, value),
            IsRecommended = () => option.GetChoice!(Baseline) == value
        };
    }

    private Answer Switch(string key, bool value, string text, string hint, string? summary = null)
    {
        var option = ProjectOptionCatalog.Get(key);

        return new()
        {
            Text = text,
            Hint = hint,
            Summary = summary,
            IsNone = value is false,
            IsChosen = () => option.GetBool!(setup) == value,
            Choose = () => option.SetBool!(setup, value),
            IsRecommended = () => option.GetBool!(Baseline) == value
        };
    }

    private Answer Monitoring(bool sentry, bool appInsights, string text, string hint, string? summary = null) => new()
    {
        Text = text,
        Hint = hint,
        Summary = summary,
        IsNone = sentry is false && appInsights is false,
        IsChosen = () => setup.Sentry == sentry && setup.AppInsights == appInsights,
        Choose = () =>
        {
            setup.Sentry = sentry;
            setup.AppInsights = appInsights;
        },
        IsRecommended = () => Baseline.Sentry == sentry && Baseline.AppInsights == appInsights
    };

    private Answer Delivery(string pipeline, bool githubRepo, string text, string hint, string summary) => new()
    {
        Text = text,
        Hint = hint,
        Summary = summary,
        IsNone = pipeline is "None",
        IsChosen = () => setup.Pipeline == pipeline && (pipeline is not "GitHub" || setup.GitHubRepo == githubRepo),
        Choose = () =>
        {
            setup.Pipeline = pipeline;
            setup.GitHubRepo = githubRepo;
        },
        IsRecommended = () => Baseline.Pipeline == pipeline && (pipeline is not "GitHub" || Baseline.GitHubRepo == githubRepo)
    };

    private static Answer Platform(string text, string hint, Func<bool> get, Action<bool> set) => new()
    {
        Text = text,
        Hint = hint,
        IsChosen = get,
        Choose = () => set(get() is false)
    };

    private Step[] CreateSteps() =>
    [
        new()
        {
            Key = "name",
            Label = "Project name",
            Question = "What's your project called?",
            Help = "Use letters and numbers, starting with a letter, like MyShop or Contoso.Shop. It names the project's folder and its code.",
            Faqs =
            [
                new("Can I rename the project later?",
                    "Yes. Commit your changes, then run `bit rename NewName` in the project folder. It closes whatever is using the project's files, then renames the folders, projects and code.")
            ]
        },
        new()
        {
            Key = "purpose",
            Label = "Real product or test drive",
            Question = "Is this your real product or a test drive?",
            Help = "Not sure? Skip it and nothing changes.",
            Groups =
            [
                new()
                {
                    Effect = () => EffectOf(purposeKeys.Where(k => touched.Contains(k) is false)),
                    Answers =
                    [
                        new()
                        {
                            Text = "My real product",
                            Hint = "Sets it up for the long run: a full database, cloud file storage, room to grow, live updates and quick restarts. Answers you already gave stay as they are.",
                            Summary = "Real product",
                            IsChosen = () => purpose is "real",
                            Choose = () => ChoosePurpose("real")
                        },
                        new()
                        {
                            Text = "Just a test drive",
                            Hint = "Adds sample pages that show how a screen, its data and the server fit together.",
                            Summary = "Test drive",
                            IsChosen = () => purpose is "test",
                            Choose = () => ChoosePurpose("test")
                        }
                    ]
                }
            ],
            Links = [new("See the sample pages live", "https://todo.bitplatform.dev/todo")],
            Faqs =
            [
                new("Can a test drive become my real product later?",
                    "Yes, it's the same project. Ask your AI assistant for what a real product needs, for example `/ai-dlc remove the sample todo pages` or `/ai-dlc add Redis`.")
            ]
        },
        new()
        {
            Key = "module",
            Label = "Starting point",
            Question = "Should it start from something ready-made?",
            Help = "Whatever you pick, it's all your code to change.",
            Groups =
            [
                new()
                {
                    Keys = ["module"],
                    Answers =
                    [
                        Pick("module", "None", "A blank app", "Sign in, profile and settings already work. The rest is yours to build.", none: true),
                        Pick("module", "Admin", "An admin panel", "A dashboard with products and categories, ready to make your own."),
                        Pick("module", "Sales", "An online store", "A shop front with products, ready to make your own.")
                    ]
                }
            ],
            Links = [new("See the admin panel", "https://adminpanel.bitplatform.dev"), new("See the online store", "https://sales.bitplatform.dev")],
            Faqs =
            [
                new("What does the admin panel include?",
                    "A dashboard with charts, a categories page, a products page with search, and a page to add or edit a product with a rich text description and a picture. It starts with a sample car catalog. With the AI chatbot on, the dashboard updates live and AI checks uploaded product pictures."),
                new("What does the online store include?",
                    "A storefront: a product carousel and an endless product grid on the home page, and a page for every product with similar ones. It has no pages to manage products, which come from sample data, and it can't be combined with the admin panel. With the AI chatbot on and PostgreSQL or SQL Server, the chatbot can search and recommend products."),
                new("Can I add the admin panel or the store to a blank app later?",
                    "Yes. Write a prompt like `/ai-dlc add the admin panel module` or `/ai-dlc add the online store module`.")
            ]
        },
        new()
        {
            Key = "multitenant",
            Label = "Many organizations (multi-tenancy)",
            Question = "Will several companies or teams use it, each seeing only their own data?",
            Help = "This is known as multi-tenancy. Try it in the admin panel demo: sign in as store-user@bitplatform.dev with password 123456.",
            Groups =
            [
                new()
                {
                    Keys = ["multitenant"],
                    Answers =
                    [
                        Switch("multitenant", true, "Yes, each sees only their own data", "One app serves many companies or teams, each with its own users.", "Many organizations"),
                        Switch("multitenant", false, "No, it's for one organization", "One set of users and one set of data.", "One organization")
                    ]
                }
            ],
            Links = [new("Open the admin panel demo", "https://adminpanel.bitplatform.dev")],
            Faqs =
            [
                new("How is each organization's data kept apart?",
                    "All organizations share one database. Products, categories and chatbot prompts carry the organization they belong to, and a database filter shows each organization only its own rows. Every organization has its own admin and member roles, and one user can belong to several organizations and switch between them."),
                new("Can I turn it on later?",
                    "Yes. Write this prompt: `/ai-dlc add multitenancy`.")
            ]
        },
        new()
        {
            Key = "database",
            Label = "Data",
            Question = "Where should it keep its data?",
            Help = "While you develop, PostgreSQL, SQL Server and MySQL run in a container for you. bit new offers to install Docker if it's missing.",
            Groups =
            [
                new()
                {
                    Keys = ["database"],
                    Answers =
                    [
                        Pick("database", "Sqlite", "SQLite", "Nothing to install. Great for getting started."),
                        Pick("database", "PostgreSQL", "PostgreSQL", "Free and popular. A safe choice for a real product."),
                        Pick("database", "SqlServer", "SQL Server", "Microsoft's database, common in companies."),
                        Pick("database", "MySql", "MySQL", "Free and offered by most hosting companies."),
                        Pick("database", "Other", "Something else", "Any other database. You connect it yourself.", "Another database")
                    ]
                }
            ],
            Faqs =
            [
                new("Can I move from SQLite to PostgreSQL later?",
                    "Yes. Write this prompt: `/ai-dlc switch the database from SQLite to PostgreSQL`.")
            ]
        },
        new()
        {
            Key = "files",
            Label = "Uploaded files",
            Question = "Where should uploaded files go?",
            Help = "Things like profile pictures.",
            Groups =
            [
                new()
                {
                    Keys = ["filesStorage"],
                    Answers =
                    [
                        Pick("filesStorage", "Local", "On the server's own disk", "Simplest. Fine while one server is enough.", "Server's own disk"),
                        Pick("filesStorage", "S3", "S3 compatible storage", "Amazon S3 or lookalikes such as Cloudflare R2, Hetzner or your own RustFS server."),
                        Pick("filesStorage", "AzureBlobStorage", "Azure Blob Storage", "Microsoft's cloud storage."),
                        Pick("filesStorage", "Other", "Somewhere else", "FTP, SFTP, Google Cloud Storage and [many more](https://github.com/robinrodricks/FluentStorage#storage-providers).", "Other storage")
                    ]
                }
            ],
            Faqs =
            [
                new("Which S3 compatible services work?",
                    "Any of them: Amazon S3, Cloudflare R2, Hetzner, DigitalOcean Spaces, Contabo, RustFS on your own server and more. The API's `appsettings.json` has sample connection strings for them, and while you develop, Aspire runs RustFS on your machine."),
                new("Which other storages can I use?",
                    "Uploaded files go through FluentStorage, so the app works the same on FTP, SFTP, Google Cloud Storage and [many more storages](https://github.com/robinrodricks/FluentStorage#storage-providers). Only the storage setup changes: pick \"Somewhere else\", then ask your AI assistant, for example `/ai-dlc store uploaded files on our FTP server`."),
                new("Can I move uploaded files from the server's disk to S3 later?",
                    "Yes. Write this prompt: `/ai-dlc switch file storage from the server's disk to S3`. Uploaded files keep their paths, so the ones you already have move over as they are.")
            ]
        },
        new()
        {
            Key = "offline",
            Label = "Works offline",
            Question = "Must it keep working without internet?",
            Warning = "Pick this only if people really must work offline. It puts a database on every device, so the web app downloads a few MB more and starts noticeably slower.",
            Groups =
            [
                new()
                {
                    Keys = ["offlineDb"],
                    Answers =
                    [
                        Switch("offlineDb", false, "No, it can count on the internet", "A smaller app that starts faster.", "Needs internet"),
                        Switch("offlineDb", true, "Yes, it must work offline", "Keeps data on the device. The app gets heavier: a bigger download and a slower start.", "Works offline")
                    ]
                }
            ],
            Links = [new("See an offline app live", "https://todo-offline.bitplatform.cc")],
            Faqs =
            [
                new("How does the app sync with the server?",
                    "It keeps a SQLite database on the device, in the browser too, with bit Besql. The offline Todo sample syncs it with CommunityToolkit.Datasync: what people change offline is sent to the server once they're back online. Your own data needs the same setup."),
                new("How much heavier does it make the web app?",
                    "About 2 MB more for the web app to download."),
                new("Can I add it later?",
                    "Yes. Write this prompt: `/ai-dlc add the offline database`.")
            ]
        },
        new()
        {
            Key = "scale",
            Label = "Many users at once",
            Question = "Will many people use it at the same time, maybe on more than one server?",
            Groups =
            [
                new()
                {
                    Keys = ["redis"],
                    Answers =
                    [
                        Switch("redis", false, "No, one server is enough", "Right for most new apps.", "One server"),
                        Switch("redis", true, "Yes, lots of people, maybe several servers", "Adds shared caching and fast background jobs, so it can run on more than one server.", "Lots of people, several servers")
                    ]
                }
            ],
            Faqs =
            [
                new("What does Redis do for my app?",
                    "It gives the app a shared cache, faster background jobs, locks and live updates that work across servers. Without it the cache stays in memory, background jobs are kept in the database, which is slower, and locks only work on one server. One server runs fine without it."),
                new("How do I run it on more than one server?",
                    "Turn on Redis, keep uploaded files in S3 or Azure Blob Storage instead of the server's disk, point every server at the same database and give them the same app certificate. With Redis on, the servers don't need sticky sessions.")
            ]
        },
        new()
        {
            Key = "chatbot",
            Label = "AI chatbot",
            Question = "Do you want the AI chatbot in your app?",
            Help = "An AI assistant inside your app that answers your users' questions and can look things up in your app's data. It works with OpenAI or any OpenAI compatible provider.",
            Groups =
            [
                new()
                {
                    Keys = ["signalR"],
                    Answers =
                    [
                        Switch("signalR", true, "Yes, include the AI chatbot", "Your users get an AI assistant in the app. Open screens also update live when something changes.", "AI chatbot"),
                        Switch("signalR", false, "No", "Leaves out the AI chatbot and live updates.", "No AI chatbot")
                    ]
                }
            ],
            Faqs =
            [
                new("What can the AI chatbot do for my users?",
                    "It answers their questions, even before they sign in, and acts in the app: it opens pages, signs people in, changes the language or theme and hands off to a human. People can attach pictures, dictate, listen to answers and make voice calls once you add the matching AI keys. Admins edit its instructions from the app."),
                new("Which AI providers does it work with?",
                    "OpenAI, or any OpenAI compatible service such as Azure AI Foundry, Google AI Studio, OpenRouter or xAI. Chat, speech and embeddings each get their own model and key in the API's `appsettings.json`, and a feature without a key stays off. Voice calls need OpenAI."),
                new("Can it answer from my app's own data?",
                    "Through tools, yes. It knows the app's pages and the last error the user ran into, and with the online store on PostgreSQL or SQL Server it searches the products. For any other data you add a tool, since it doesn't read your database on its own."),
                new("Can I add it later?",
                    "Yes. Write this prompt: `/ai-dlc add the AI chatbot`.")
            ]
        },
        new()
        {
            Key = "notifications",
            Label = "Push notifications",
            Question = "Should it send push notifications?",
            Groups =
            [
                new()
                {
                    Keys = ["notification"],
                    Answers =
                    [
                        Switch("notification", true, "Yes", "On Android, iOS, macOS and in web browsers.", "Push notifications"),
                        Switch("notification", false, "No", "Leaves them out.", "No push notifications")
                    ]
                }
            ],
            Faqs =
            [
                new("Do push notifications cost anything?",
                    "No. Your server sends them straight to Firebase, Apple and the browsers' own push services, which are free, so there's no paid notification service in between.")
            ]
        },
        new()
        {
            Key = "crashes",
            Label = "Crash reports",
            Question = "How do you want to hear about crashes?",
            Groups =
            [
                new()
                {
                    Keys = ["sentry", "appInsights"],
                    Answers =
                    [
                        Monitoring(false, false, "Not now", "No outside service for now.", "No crash reports"),
                        Monitoring(true, false, "Sentry", "Errors and crashes from the server and every app, native Android and iOS crashes included, collected in Sentry."),
                        Monitoring(false, true, "Application Insights", "Logs and errors from the server and the web app, collected in Microsoft Azure."),
                        Monitoring(true, true, "Both", "Sentry and Application Insights together.", "Sentry and Application Insights")
                    ]
                }
            ],
            Faqs =
            [
                new("Sentry or Application Insights?",
                    "Sentry collects errors and crashes from the server and every app, including the Windows app and native Android and iOS crashes. Application Insights runs on Azure and collects the server's logs and requests, plus page views and errors from the web app. Pick Sentry if crashes in the apps matter most."),
                new("Can I use both?",
                    "Yes. Pick \"Both\", then ask your AI assistant to split the work between them, for example `/ai-dlc use Application Insights for the server and the web app, and Sentry for the Android and iOS apps`.")
            ]
        },
        new()
        {
            Key = "code",
            Label = "Code and deployment",
            Question = "Where should the code live and deploy from?",
            Groups =
            [
                new()
                {
                    Keys = ["pipeline", "githubRepo"],
                    Answers =
                    [
                        Delivery("GitHub", true, "GitHub, create a private repo for me", "Creates the repo, pushes the code and sets up build and deploy. Asks you to sign in to GitHub if needed.", "GitHub, repo created for you"),
                        Delivery("GitHub", false, "GitHub, I'll create the repo myself", "Sets up build and deploy for GitHub. You create and push the repo.", "GitHub, your own repo"),
                        Delivery("Azure", true, "Azure DevOps", "Sets up build and deploy on Azure DevOps.", "Azure DevOps"),
                        Delivery("None", true, "Nowhere yet", "No build and deploy setup for now.", "Nowhere yet")
                    ]
                }
            ],
            Faqs =
            [
                new("What do the GitHub pipelines do?",
                    "On every pull request, CI checks the code style, builds everything and runs the tests. On every push to `main` or `test`, CD deploys the web app, and the API when it runs on its own server, to Azure App Service, and builds a signed Windows installer and Android and iOS packages for you to download. Store uploads are up to you."),
                new("Where can I deploy my project?",
                    "The pipelines deploy to Azure App Service by default, but the server is a regular ASP.NET Core app: it runs on AWS, Google Cloud, your own Linux servers, Windows with IIS, Docker or Kubernetes."),
                new("My code is on GitLab. What should I pick?",
                    "Pick \"GitHub, I'll create the repo myself\" and use its workflows in `.github/workflows` as a starting point. Most of their steps are plain command line commands, so your AI assistant can turn them into GitLab, Bitbucket or Jenkins pipelines, for example `/ai-dlc create GitLab pipelines that do what the GitHub workflows do`.")
            ]
        },
        new()
        {
            Key = "api",
            Label = "Website and API publishing",
            Question = "Should the website and the API be published together or separately?",
            Groups =
            [
                new()
                {
                    Keys = ["api"],
                    Answers =
                    [
                        Pick("api", "Integrated", "Together", "Easier. One publish sends out both the website and the API.", "Published together"),
                        Pick("api", "Standalone", "Separately", "More advanced. The website and the API are published on their own, so each can be updated and grown without the other.", "Published separately")
                    ]
                }
            ]
        },
        new()
        {
            Key = "look",
            Label = "Look and editor",
            Question = "Which look, and which editor should open it?",
            Help = "Every look comes with light and dark modes.",
            Groups =
            [
                new()
                {
                    Prompt = "Which look?",
                    Keys = ["theme"],
                    Answers =
                    [
                        Pick("theme", "Fluent2", "Fluent 2", "Microsoft's current style.", "Fluent 2 look"),
                        Pick("theme", "Fluent", "Fluent", "Microsoft's earlier style.", "Fluent look"),
                        Pick("theme", "Cupertino", "Cupertino", "Apple's style. Still experimental.", "Cupertino look"),
                        Pick("theme", "Material", "Material", "Google's style. Still experimental.", "Material look")
                    ]
                },
                new()
                {
                    Prompt = "Which editor should open it?",
                    Keys = ["ide"],
                    Answers =
                    [
                        Pick("ide", "code", "VS Code", "Free. Installed for you if it's missing, with the extensions the project recommends.", "opens in VS Code"),
                        Pick("ide", "vs", "Visual Studio", "Microsoft's full editor for Windows.", "opens in Visual Studio"),
                        Pick("ide", "rider", "Rider", "JetBrains' editor for Windows, macOS and Linux.", "opens in Rider"),
                        Pick("ide", "none", "Don't open it", "Just create the project.", "not opened in an editor")
                    ]
                }
            ],
            Faqs =
            [
                new("Can I use my own brand colors?",
                    "Yes. Give the project's rebrand skill whatever reference you have, for example `/rebrand Make the app look like https://contoso.com`, `/rebrand Use our Figma design: <link to the file>`, `/rebrand Follow our brand book in docs/brand-book.pdf` or `/rebrand Calm and modern, our brand color is #0F766E`."),
                new("Can I switch to another theme later?",
                    "Yes. Write this prompt: `/rebrand Switch the app to the Material theme`.")
            ]
        },
        new()
        {
            Key = "captcha",
            Label = "Bot protection",
            Question = "Should sign-up be protected from bots?",
            Groups =
            [
                new()
                {
                    Keys = ["captcha"],
                    Answers =
                    [
                        Pick("captcha", "reCaptcha", "Yes, with Google reCAPTCHA", "Checks for bots when people sign up.", "Google reCAPTCHA"),
                        Pick("captcha", "None", "No", "Keeps signing up as quick as possible.", "No bot check", none: true)
                    ]
                }
            ],
            Links = [new("Try a protected sign-up page", "https://todo.bitplatform.dev/sign-up")]
        },
        new()
        {
            Key = "ads",
            Label = "Rewarded ads",
            Question = "Will you show rewarded ads?",
            Groups =
            [
                new()
                {
                    Keys = ["ads"],
                    Answers =
                    [
                        Switch("ads", false, "No", "No ads in the app.", "No ads"),
                        Switch("ads", true, "Yes, Google rewarded ads", "People watch an ad in return for something you give them, such as a premium plan you build.", "Reward-based Google ads")
                    ]
                }
            ],
            Links = [new("See it live", "https://adminpanel.bitplatform.dev/settings/UpgradeAccount")],
            Faqs =
            [
                new("How do the rewarded ads work?",
                    "They use Google Ad Manager's rewarded ads on the Upgrade account page in settings: once people agree to ads, they watch one and see a thank you message. What they get for it is yours to build, since the template doesn't grant or save anything. Replace Google's sample ad unit in `appsettings.json` with your own.")
            ]
        },
        new()
        {
            Key = "platforms",
            Label = "Apps on this machine",
            Question = "Which apps should be ready on this machine today?",
            Help = "Every project includes the web, Android, iOS, macOS and Windows apps. This only decides what gets installed and built here now. Each one adds several GB of tools and makes the first build minutes longer.",
            Groups =
            [
                new()
                {
                    Multiple = true,
                    Keys = ["android", "ios", "macOS", "windows"],
                    Effect = PlatformsEffect,
                    Answers =
                    [
                        new()
                        {
                            Text = "Just the web app",
                            Hint = "Quickest to set up.",
                            IsNone = true,
                            IsChosen = () => (setup.Android || setup.Ios || setup.MacOS || setup.Windows) is false,
                            Choose = () =>
                            {
                                setup.Android = false;
                                setup.Ios = false;
                                setup.MacOS = false;
                                setup.Windows = false;
                            }
                        },
                        Platform("Android", "Builds the Android app now.", () => setup.Android, v => setup.Android = v),
                        Platform("iOS", "Builds the iOS app now.", () => setup.Ios, v => setup.Ios = v),
                        Platform("macOS", "Builds the macOS app now.", () => setup.MacOS, v => setup.MacOS = v),
                        Platform("Windows", "Builds the Windows app now.", () => setup.Windows, v => setup.Windows = v)
                    ]
                }
            ],
            Faqs =
            [
                new("Can I build the iOS app on Windows?",
                    "Yes, it builds on Windows. To run it from Windows, Visual Studio pairs with a Mac on your network that has Xcode. The macOS app runs on a Mac."),
                new("What does the Android app need on this machine?",
                    "bit installs what's missing: the .NET MAUI tools, the Android SDK and Java, and on Windows it turns on what the Android emulator needs. Expect several GB of downloads. bit doesn't create an emulator for you.")
            ]
        }
    ];

    private sealed record EffectPart(string Verb, string Flags);

    private sealed record Faq(string Question, string Answer);

    private sealed record AnswerPart(string Text, string? Url, bool IsCode);

    private sealed record DemoLink(string Text, string Url);

    private sealed class Answer
    {
        private static int count;

        public string HintId { get; } = $"answer-hint-{Interlocked.Increment(ref count)}";
        public required string Text { get; init; }
        public string? Hint { get; init; }
        public string? Summary { get; init; }
        public bool IsNone { get; init; }
        public required Func<bool> IsChosen { get; init; }
        public required Action Choose { get; init; }
        public Func<bool> IsRecommended { get; init; } = () => false;
        public bool ShowsRecommended => IsNone is false && IsRecommended();
    }

    private sealed class ChoiceGroup
    {
        public string? Prompt { get; init; }
        public bool Multiple { get; init; }
        public string[] Keys { get; init; } = [];
        public Func<List<EffectPart>>? Effect { get; init; }
        public required Answer[] Answers { get; init; }
    }

    private sealed class Step
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public required string Question { get; init; }
        public string? Help { get; init; }
        public string? Warning { get; init; }
        public Faq[] Faqs { get; init; } = [];
        public ChoiceGroup[] Groups { get; init; } = [];
        public DemoLink[] Links { get; init; } = [];
    }
}
