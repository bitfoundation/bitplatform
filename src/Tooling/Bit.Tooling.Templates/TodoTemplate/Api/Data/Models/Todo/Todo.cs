using TodoTemplate.Shared.Enums;

namespace TodoTemplate.Api.Data.Models.Todo
{
    public class Todo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public TodoState State { get; set; }
    }
}
