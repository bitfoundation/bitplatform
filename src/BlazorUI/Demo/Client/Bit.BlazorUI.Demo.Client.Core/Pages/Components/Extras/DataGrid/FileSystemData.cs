namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.DataGrid;

/// <summary>A node in a file-system style hierarchy used by the Tree View demo.</summary>
public class FileNode
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "Folder";
    public long Size { get; set; }
    public DateTime Modified { get; set; }
    public List<FileNode> Children { get; set; } = new();
}

public static class FileSystemData
{
    /// <summary>Builds a small, deterministic folder/file tree.</summary>
    public static List<FileNode> Build()
    {
        var id = 0;
        var baseDate = new DateTime(2025, 1, 1);

        FileNode Folder(string name, params FileNode[] children)
        {
            var node = new FileNode
            {
                Id = ++id,
                Name = name,
                Kind = "Folder",
                Modified = baseDate.AddDays(id),
                Children = children.ToList()
            };
            node.Size = node.Children.Sum(c => c.Size);
            return node;
        }

        FileNode File(string name, long size) => new()
        {
            Id = ++id,
            Name = name,
            Kind = "File",
            Size = size,
            Modified = baseDate.AddDays(id)
        };

        return new List<FileNode>
        {
            Folder("src",
                Folder("BitDataGrid",
                    File("BitDataGrid.razor", 24_500),
                    File("BitDataGrid.razor.cs", 41_200),
                    Folder("Models",
                        File("BitDataGridColumnAlign.cs", 320),
                        File("BitDataGridSortDescriptor.cs", 540),
                        File("BitDataGridFilterOperator.cs", 610)),
                    Folder("Infrastructure",
                        File("BitDataGridDataProcessor.cs", 8_900),
                        File("BitDataGridPropertyAccessor.cs", 3_400))),
                Folder("BitDataGrid.Demo",
                    File("Program.cs", 1_200),
                    Folder("Components",
                        File("App.razor", 760),
                        File("Routes.razor", 280)))),
            Folder("docs",
                File("README.md", 6_400),
                File("CHANGELOG.md", 2_100)),
            Folder("assets",
                File("logo.svg", 4_800),
                File("styles.css", 12_300),
                File("favicon.ico", 1_150)),
            File("LICENSE", 1_070),
            File(".gitignore", 410)
        };
    }
}
