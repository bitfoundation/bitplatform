using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI.Tests.E2E.TestHost.AI;

/// <summary>
/// How both hosts set the AI package up - <c>AddBitBlazorUIAIServices(E2EAiSetup.Configure)</c> on the server and in the
/// browser alike, so an AI page runs unchanged in either render mode: the scripted <see cref="E2EAiTransport"/>, the
/// <see cref="E2EAiTools"/>, the <see cref="E2EAiModels"/>, an in-memory conversation store, a renderer turning a "form"
/// schema into a <c>BitAiForm</c>, and <see cref="E2EAiCard"/> as the one component generative UI may render.
/// </summary>
public static class E2EAiSetup
{
    /// <summary>The structured-data schema the host renders as a BitAiForm (see <see cref="E2EAiScripts.Form"/>).</summary>
    public const string FormSchema = "form";

    /// <summary>The name <see cref="E2EAiCard"/> is registered under for generative UI.</summary>
    public const string CardComponent = "e2e_card";

    public static void Configure(BitAiBuilder ai)
    {
        ai.Services.TryAddScoped<E2EAiScriptSelection>();
        ai.UseTransport<E2EAiTransport>();

        foreach (var tool in E2EAiTools.Create()) ai.AddTool(tool);

        ai.AddModels(E2EAiModels.Models, E2EAiModels.Providers, E2EAiModels.DefaultModelId);
        ai.UseConversationStore<BitAiInMemoryConversationStore>();

        ai.ConfigureRenderers(renderers => renderers.RegisterStructured(FormSchema, context => builder =>
        {
            if (context.Part.Value is not { } schema) return;

            builder.OpenComponent<BitAiForm>(0);
            builder.AddComponentParameter(1, nameof(BitAiForm.Definition), BitAiFormDefinition.FromJsonSchema(schema));
            builder.CloseComponent();
        }));

        ai.ConfigureGenerativeUi(RegisterGenerativeComponents);
    }

    // Registering reads the component's parameters by reflection, which the registry marks as unsafe to trim. It is safe
    // here: Register<TComponent> keeps the type whole through its annotation, and the parameters it binds are a string
    // and an integer, which have no members of their own for a trimmer to take away.
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "E2EAiCard is kept whole by the annotation of Register, and its bindable parameters are a string and an integer.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "E2EAiCard's bindable parameters are a string and an integer, which need no code generated at run time.")]
    private static void RegisterGenerativeComponents(BitAiGenerativeUiRegistry registry)
    {
        registry.Register<E2EAiCard>(CardComponent, "A card showing one number under a title.", nameof(E2EAiCard.Title));
    }
}
