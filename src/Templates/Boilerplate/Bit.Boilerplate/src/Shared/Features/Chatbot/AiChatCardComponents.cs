//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>The cards the server names (See <see cref="AiChatCard.ComponentType"/>); each is a component in AppAiChatPanelCards.</summary>
public static class AiChatCardComponents
{
    private const string Namespace = "Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards.";

    public const string HumanFollowUp = Namespace + "HumanFollowUpCard";

    public const string HumanApproval = Namespace + "HumanApprovalCard";

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    public const string Products = Namespace + "ProductsCard";
    //#endif
    //#endif
}
