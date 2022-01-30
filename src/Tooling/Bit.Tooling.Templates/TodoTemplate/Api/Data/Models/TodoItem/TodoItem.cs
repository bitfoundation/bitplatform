
namespace TodoTemplate.Api.Data.Models.TodoItem
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public bool IsDone { get; set; }
    }
}
