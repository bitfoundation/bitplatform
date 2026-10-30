namespace Boilerplate.Workbench.Components.Pages.Showcase.Explainers.Data;

public sealed record DeploymentStep(string Key, int Stage, string TitleKey, string DetailKey, string DescriptionKey, string IconName, string[] Next);
