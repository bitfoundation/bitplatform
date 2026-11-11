namespace Bit.BlazorUI;

/// <summary>
/// An agent run has begun.
/// </summary>
public sealed record BitAiAgentRunStartedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="runId">Identifier for the run.</param>
    public BitAiAgentRunStartedEvent(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        RunId = runId;
    }

    /// <summary>Identifier for the run.</summary>
    public string RunId { get; }

    /// <summary>The name of the agent, shown in the run header.</summary>
    public string? AgentName { get; init; }

    /// <summary>The goal the run is pursuing, shown under the name.</summary>
    public string? Goal { get; init; }

    /// <inheritdoc />
    public override string Type => "agent.run.started";
}

/// <summary>
/// The agent published or revised its plan.
/// </summary>
/// <remarks>
/// Replaces the whole plan rather than patching it, because agents commonly rewrite the tail of a
/// plan as they learn. Steps that keep their identifier keep their state.
/// </remarks>
public sealed record BitAiAgentPlanEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="runId">The run being planned.</param>
    /// <param name="steps">The planned steps, in execution order.</param>
    public BitAiAgentPlanEvent(string runId, IReadOnlyList<BitAiAgentPlanStep> steps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(steps);
        RunId = runId;
        Steps = steps;
    }

    /// <summary>The run being planned.</summary>
    public string RunId { get; }

    /// <summary>The planned steps, in execution order.</summary>
    public IReadOnlyList<BitAiAgentPlanStep> Steps { get; }

    /// <inheritdoc />
    public override string Type => "agent.plan";
}

/// <summary>
/// One entry in an agent plan.
/// </summary>
/// <param name="StepId">Stable identifier, preserved across plan revisions.</param>
/// <param name="Title">What the step does.</param>
/// <param name="Detail">Optional elaboration shown when the step is expanded.</param>
public readonly record struct BitAiAgentPlanStep(string StepId, string Title, string? Detail = null);

/// <summary>
/// A step started executing.
/// </summary>
public sealed record BitAiAgentStepStartedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="runId">The run the step belongs to.</param>
    /// <param name="stepId">The step being started.</param>
    /// <param name="title">What the step does. Optional when the step came from a plan.</param>
    public BitAiAgentStepStartedEvent(string runId, string stepId, string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        RunId = runId;
        StepId = stepId;
        Title = title;
    }

    /// <summary>The run the step belongs to.</summary>
    public string RunId { get; }

    /// <summary>The step being started.</summary>
    public string StepId { get; }

    /// <summary>What the step does.</summary>
    public string? Title { get; }

    /// <inheritdoc />
    public override string Type => "agent.step.started";
}

/// <summary>
/// A step finished.
/// </summary>
public sealed record BitAiAgentStepCompletedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="runId">The run the step belongs to.</param>
    /// <param name="stepId">The step that finished.</param>
    /// <param name="status">How it finished. Defaults to completed.</param>
    public BitAiAgentStepCompletedEvent(string runId, string stepId, BitAiAgentStepStatus status = BitAiAgentStepStatus.Completed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        RunId = runId;
        StepId = stepId;
        Status = status;
    }

    /// <summary>The run the step belongs to.</summary>
    public string RunId { get; }

    /// <summary>The step that finished.</summary>
    public string StepId { get; }

    /// <summary>How it finished.</summary>
    public BitAiAgentStepStatus Status { get; }

    /// <summary>A short account of what the step produced.</summary>
    public string? Summary { get; init; }

    /// <summary>The failure, when the step failed.</summary>
    public BitAiError? Error { get; init; }

    /// <inheritdoc />
    public override string Type => "agent.step.completed";
}

/// <summary>
/// The run ended.
/// </summary>
public sealed record BitAiAgentRunCompletedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="runId">The run that ended.</param>
    /// <param name="status">How it ended. Defaults to completed.</param>
    public BitAiAgentRunCompletedEvent(string runId, BitAiAgentRunStatus status = BitAiAgentRunStatus.Completed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        RunId = runId;
        Status = status;
    }

    /// <summary>The run that ended.</summary>
    public string RunId { get; }

    /// <summary>How it ended.</summary>
    public BitAiAgentRunStatus Status { get; }

    /// <inheritdoc />
    public override string Type => "agent.run.completed";
}

/// <summary>
/// The run is blocked until the user answers.
/// </summary>
public sealed record BitAiAgentInputRequestedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="runId">The blocked run.</param>
    /// <param name="prompt">What the agent needs from the user.</param>
    public BitAiAgentInputRequestedEvent(string runId, string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(prompt);
        RunId = runId;
        Prompt = prompt;
    }

    /// <summary>The blocked run.</summary>
    public string RunId { get; }

    /// <summary>What the agent needs from the user.</summary>
    public string Prompt { get; }

    /// <summary>Suggested answers, rendered as one-tap choices.</summary>
    public IReadOnlyList<string>? Suggestions { get; init; }

    /// <inheritdoc />
    public override string Type => "agent.input_requested";
}
