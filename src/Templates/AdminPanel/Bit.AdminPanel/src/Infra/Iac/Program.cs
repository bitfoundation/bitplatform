using Pulumi;
using AdminPanel.Iac;

public class Program
{
    static Task<int> Main() => Deployment.RunAsync<AdStack>();
}
