using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

/// <summary>
/// The AI chat panel against the deployed apps: a real model, its real tools and the deployment's real database. The
/// answers are prose, so they are read with <see cref="AiAnswerJudge"/> rather than by string matching.
/// <para>
/// The Web, Windows and Android classes deriving from this decide where the app runs; an app with no build on the
/// platform under test reports inconclusive (See <see cref="AppTestBase.OpenApp"/>).
/// </para>
/// </summary>
public abstract class AiChatbotTestsBase : AppTestBase
{
    /// <summary>Nothing about this app, and nothing the assistant's tools could answer either.</summary>
    private const string OffTopicQuestion = "What is the distance between Amsterdam and Berlin?";

    private const string ProductQuestion = "I'm searching for a Benz SUV electric car with a 75K budget!";

    /// <summary>The seeded manufacturer the product question names; see CategoryConfiguration.</summary>
    private const string BenzCategory = "Benz";

    private const decimal ProductQuestionBudget = 75_000;

    /// <summary>Opens <paramref name="app"/>, waits for it to be interactive and opens its chat panel.</summary>
    protected async Task<AiChatPanel> OpenChatPanel(App app)
    {
        var page = await OpenApp(app);

        await WaitUntilInteractive(page);

        return await AiChatPanel.Open(page);
    }

    /// <summary>
    /// The system prompt's Relevance rule: never answer an off-topic question, and never leave the user without a
    /// reply either (See <c>SystemPromptConfiguration.GetInitialSystemPromptMarkdown</c>). Both halves are asserted,
    /// since each fails on its own - an assistant that answers has no scope, one that says nothing leaves an empty
    /// bubble.
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    public virtual async Task Assistant_Should_SayAQuestionIsNotItsBusiness_WhenItIsNotAboutTheApp(App app)
    {
        var panel = await OpenChatPanel(app);

        var answer = await panel.Ask(OffTopicQuestion);

        await AiAnswerJudge.AssertAnswer(OffTopicQuestion,
            "The assistant tells the user, politely, that this question is outside what it helps with - and it does " +
            "not answer the question. Naming a distance, a duration, a route or anything else that tells the user " +
            "how far apart the two cities are fails, however hedged or approximate it is, and so does offering to " +
            "look it up. Saying nothing at all fails too: the user has to be told.",
            answer, TestContext.CancellationToken);
    }

    /// <summary>
    /// <c>GetProductRecommendations</c> searches the deployment's own products, so what comes back has to be cars in
    /// that database at the prices it holds - one written out of the model's own knowledge of Mercedes-Benz would
    /// read just as well and be worth nothing. The cars are shown as cards (See <c>ShowProducts</c>), which the answer
    /// only talks about. Sales only, as the module with products.
    /// </summary>
    [TestMethod]
    public virtual async Task Assistant_Should_RecommendProductsThatAreInTheDatabase()
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        // Unfiltered: the products belong to the deployment's store tenant, and this host carries no tenant of its own.
        var benzCars = await dbContext.Products.IgnoreQueryFilters()
            .Where(product => product.Category!.Name == BenzCategory)
            .Select(product => new { Name = product.Name!, product.Price })
            .ToArrayAsync(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, benzCars.Length,
            $"The deployment's database holds no '{BenzCategory}' products, so there is nothing the assistant could correctly recommend.");

        var panel = await OpenChatPanel(App.Sales);

        var answer = await panel.Ask(ProductQuestion);

        var shown = await panel.ReadProductCards();

        var recommended = benzCars.Where(car => shown.Contains(car.Name, StringComparer.OrdinalIgnoreCase)).ToArray();

        Assert.IsGreaterThan(0, recommended.Length,
            $"""
            The product cards show none of the {benzCars.Length} {BenzCategory} cars in the deployment's database, so
            whatever was recommended did not come from there.

            Shown: {string.Join(", ", shown)}
            Answered: {answer}
            """);

        Assert.Contains(car => car.Price <= ProductQuestionBudget, recommended,
            $"Every {BenzCategory} car on the cards is above the {ProductQuestionBudget:N0} budget the question set: " +
            $"{string.Join(", ", recommended.Select(car => $"{car.Name} at {car.Price:N0}"))}");

        // The cards come from the database by id, so they can't invent a car; the answer around them still can.
        await AiAnswerJudge.AssertAnswer(ProductQuestion,
            $"""
            The assistant recommends cars for this request rather than declining it. The cars are shown to the user as
            cards, listed after the answer below, and the answer talks about them. Every car the answer or the cards name
            - with any price given for it - appears in this catalogue:
            {string.Join(Environment.NewLine, benzCars.Select(car => $"- {car.Name}, {car.Price:N0}"))}
            A car that is not in the catalogue, or one given a price the catalogue does not hold for it, fails. So does
            one recommended above the {ProductQuestionBudget:N0} budget the question set - naming a car to rule it out as
            too expensive is fine, recommending it is not. Cars in the catalogue that are left out are fine, and so is
            any advice around them.
            """,
            $"{answer}{Environment.NewLine}{Environment.NewLine}Cards shown: {string.Join("; ", shown)}", TestContext.CancellationToken);
    }
}
