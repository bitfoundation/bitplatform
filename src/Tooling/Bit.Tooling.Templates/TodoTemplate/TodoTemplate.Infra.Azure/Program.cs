using Pulumi;
using TodoTemplate.Infra.Azure;

public class Program
{
    static Task<int> Main() => Deployment.RunAsync<StackSelector>();
}

public class StackSelector : Stack
{
    public StackSelector()
    {
        string stackName = Deployment.Instance.StackName;

        if (stackName == "cd")
            new CdStack(); // azure devops agent
        else
            new TdStack(); // test / prod
    }
}
