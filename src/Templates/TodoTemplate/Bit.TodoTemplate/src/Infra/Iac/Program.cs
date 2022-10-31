using Pulumi;
using TodoTemplate.Iac;

public class Program
{
    static Task<int> Main() => Deployment.RunAsync<TdStack>();
}
