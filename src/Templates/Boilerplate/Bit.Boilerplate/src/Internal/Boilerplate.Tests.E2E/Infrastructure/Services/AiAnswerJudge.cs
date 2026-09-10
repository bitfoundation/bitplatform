using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// Decides whether an answer the deployed chatbot wrote means what a test expects. A reply is prose, worded
/// differently every run and in whichever language the user wrote in, so substring assertions would pass on anything
/// or fail on everything; a second model grades it against a plain-English expectation instead, and its reasoning
/// goes into the assertion message.
/// </summary>
public static class AiAnswerJudge
{
    /// <summary>What the judge fills in. Reasoning first, so the verdict is written after it rather than before.</summary>
    private sealed class Verdict
    {
        /// <summary>One or two sentences on what the answer does and does not do, against the expectation.</summary>
        public string Reasoning { get; set; } = default!;

        public bool MeetsExpectation { get; set; }
    }

    private const string JudgeInstructions = """
        You are grading a single answer written by a customer support assistant that is embedded in an application.
        You are given what the user asked, what the answer must demonstrate, and the answer itself.

        Decide only whether the answer demonstrates what it must. Judge the substance, not the wording, the length,
        the formatting or the language it is written in - an answer in any language counts.

        Grade against the stated expectation and nothing else. You are not reviewing the answer's quality: a
        shortcoming the expectation does not mention is not yours to fail it for, however real, and neither is
        anything the user's question implies but the expectation left out. If the expectation is met, say so even
        when you can see something else you would have done differently.

        Be strict about the expectation itself. An answer that is empty, that says nothing at all, or that only asks
        a question back does not meet an expectation about what it should say.
        """;

    /// <summary>
    /// Fails the test unless <paramref name="answer"/> demonstrates <paramref name="expectation"/>. Inconclusive when
    /// no judge is configured, so a run without an <c>OpenAIChatApiKey</c> skips these rather than failing them.
    /// </summary>
    public static async Task AssertAnswer(string question, string expectation, string? answer, CancellationToken cancellationToken)
    {
        var judge = DeployedApiClientProvider.GetAnswerJudge();

        if (judge is null)
            Assert.Inconclusive("No 'OpenAIChatApiKey' in this project's user secrets or environment variables, so there is nothing to read the chatbot's answer with.");

        Assert.IsFalse(string.IsNullOrWhiteSpace(answer),
            $"The chatbot answered nothing at all to '{question}', so there is nothing to judge against: {expectation}");

        var prompt = $"""
            ## What the user asked
            {question}

            ## What the answer must demonstrate
            {expectation}

            ## The answer
            {answer}
            """;

        // No ChatOptions: gpt-5 rejects any temperature but its default, and the rest is the provider's business.
        var response = await judge!.GetResponseAsync<Verdict>(
            [new(ChatRole.System, JudgeInstructions), new(ChatRole.User, prompt)],
            cancellationToken: cancellationToken);

        Assert.IsTrue(response.TryGetResult(out var verdict) && verdict is not null,
            $"The judge answered with something that is not a verdict: {response.Text}");

        Assert.IsTrue(verdict!.MeetsExpectation,
            $"""
            The chatbot's answer does not demonstrate: {expectation}

            The judge's reasoning: {verdict.Reasoning}

            Asked: {question}

            Answered: {answer}
            """);
    }
}
