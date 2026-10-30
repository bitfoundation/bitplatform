using Bit.Blow;

namespace Boilerplate.Workbench.Components.Pages.Showcase.Explainers;

public partial class ProjectStructurePage
{
    private BlowCanvas? canvas;
    private List<BlowNode> nodes = [];
    private List<BlowEdge> edges = [];
    private ProjectInfo? selected;
    private bool isLoading = true;
    private bool isFailed;
    private bool isNarrow;

    [AutoInject] private ProjectStructureService projectStructureService = default!;

    protected override Task OnInitializedAsync() => Load();

    private async Task Load()
    {
        isLoading = true;
        isFailed = false;

        try
        {
            BuildGraph(await projectStructureService.GetProjects(CurrentCancellationToken));
            isLoading = false;
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
            isLoading = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void BuildGraph(IReadOnlyList<ProjectInfo> projects)
    {
        var keys = projects.Select(p => p.Key).ToHashSet();

        nodes = [.. projects.Select(project => new BlowNode
        {
            Id = project.Key,
            Type = "project",
            Label = project.Name,
            Data = project,
            Width = 250,
            Height = 68
        })];

        edges = [.. projects.SelectMany(project => project.References.Where(keys.Contains).Select(reference => new BlowEdge
        {
            Id = $"{project.Key}-{reference}",
            Source = project.Key,
            Target = reference
        }))];

        BlowLayout.Layered(nodes, edges, new BlowLayeredLayoutOptions
        {
            Direction = isNarrow ? BlowLayoutDirection.LeftToRight : BlowLayoutDirection.TopToBottom,
            LayerGap = isNarrow ? 70 : 64,
            NodeGap = isNarrow ? 20 : 36
        });
    }

    private void OnNarrowChanged(bool narrow)
    {
        if (narrow == isNarrow)
            return;

        isNarrow = narrow;

        if (nodes.Count is 0)
            return;

        BuildGraph([.. nodes.Select(n => (ProjectInfo)n.Data!)]);

        canvas?.UpdateNodeInternals();
        canvas?.FitView();
    }

    private void Select(BlowNode node) => selected = node.Data as ProjectInfo;
}
