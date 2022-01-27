using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTemplate.Shared.Enums;

namespace TodoTemplate.Shared.Dtos.Todo
{
    public class TodoDto
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public TodoState State{ get; set; }
    }
}
