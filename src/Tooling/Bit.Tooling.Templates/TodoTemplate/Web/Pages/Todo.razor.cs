namespace TodoTemplate.App.Pages;

public partial class Todo
{
    List<TodoModel> todolist = new();
    protected override void OnInitialized()
    {
        for (int i = 0; i < 3; i++)
        {
            todolist.Add(new TodoModel
            {
                Date = "January 3, 2022, Monday",
                Title = "Project name"
            });
        }
    }
}
public class TodoModel
{
    public string? Title { get; set; }
    public string? Date { get; set; }
}
