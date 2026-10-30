using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiDependencyInjectionTests
{
    [TestMethod]
    public void TheApprovalPolicyIsScopedSoOneUsersAlwaysAllowDoesNotApplyToAnother()
    {
        var services = new ServiceCollection();
        services.AddBitBlazorUIAIServices();

        var descriptor = Assert.ContainsSingle(d => d.ServiceType == typeof(IBitAiToolApprovalPolicy), services);
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.AreEqual(ServiceLifetime.Scoped,
            Assert.ContainsSingle(d => d.ServiceType == typeof(BitAiChatSessionFactory), services).Lifetime);
    }

    [TestMethod]
    public void AReplacementApprovalPolicyIsScopedSoItCanDependOnTheCurrentUser()
    {
        var services = new ServiceCollection();
        services.AddBitBlazorUIAIServices(ai => ai.UseApprovalPolicy<BitAiDefaultToolApprovalPolicy>());

        var descriptor = Assert.ContainsSingle(d => d.ServiceType == typeof(IBitAiToolApprovalPolicy), services);
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public async Task AlwaysApprovalsCanBeRecordedWhileCallsAreBeingChecked()
    {
        var policy = new BitAiDefaultToolApprovalPolicy();
        var invocation = new BitAiToolInvocation("c", "t", default);

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 5000; i++) policy.RememberAlwaysApproved($"tool_{i}");
        });

        while (!writer.IsCompleted)
        {
            await policy.RequiresApprovalAsync(new BitAiToolDefinition("tool_1") { Risk = BitAiToolRisk.Destructive }, invocation);
        }

        await writer;
        Assert.IsFalse(await policy.RequiresApprovalAsync(
            new BitAiToolDefinition("tool_4999") { Risk = BitAiToolRisk.Destructive }, invocation));
    }
}
