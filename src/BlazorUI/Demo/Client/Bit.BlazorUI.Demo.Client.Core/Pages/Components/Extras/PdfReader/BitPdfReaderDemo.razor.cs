namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfReader;

public partial class BitPdfReaderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "Items",
            Type = "IQueryable<TGridItem>?",
            DefaultValue = "null",
            Description = @"A queryable source of data for the grid.
                            This could be in-memory data converted to queryable using the
                            System.Linq.Queryable.AsQueryable(System.Collections.IEnumerable) extension method,
                            or an EntityFramework DataSet or an IQueryable derived from it.
                            You should supply either Items or ItemsProvider, but not both.",
         }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "BitDataGridColumnBase",
            Title = "BitDataGridColumnBase",
            Parameters=
            [
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Title text for the column. This is rendered automatically if HeaderTemplate is not used.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "BitDataGridAlign",
            Name = "BitDataGridAlign",
            Description = "Describes alignment for a BitDataGrid<TGridItem> column.",
            Items =
            [
                new()
                {
                     Name = "Left",
                     Value = "0",
                     Description = "Justifies the content against the start of the container."
                },
            ]
        }
    ];



    private readonly string example1RazorCode = @"
";
    private readonly string example1CsharpCode = @"
";
}
