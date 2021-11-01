using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.List
{
    public partial class BitBasicListDemo
    {
        private readonly List<Person>[] People = new List<Person>[2];

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "Items",
                Type = "ICollection<TItem>",
                DefaultValue = "",
                Description = "List of items want to render.",
            },
            new ComponentParameter()
            {
                Name = "ItemSize",
                Type = "int",
                DefaultValue = "50",
                Description = "The height of each item in pixels.",
            },
            new ComponentParameter()
            {
                Name = "OverscanCount",
                Type = "int",
                DefaultValue = "3",
                Description = "determines how many additional items are rendered before and after the visible region.",
            },
            new ComponentParameter()
            {
                Name = "Role",
                Type = "string",
                DefaultValue = "list",
                Description = "Role of the BasicList.",
            },
            new ComponentParameter()
            {
                Name = "RowTemplate",
                Type = "RenderFragment<TItem>",
                DefaultValue = "",
                Description = "content of each item. it should determin with context attribute.",
            },
            new ComponentParameter()
            {
                Name = "Virtualize",
                Type = "bool",
                DefaultValue = "false",
                Description = "virtualize rendering the list, UI rendering to just the parts that are currently visible",
            },
        };

        protected override void OnInitialized()
        {
            Person person = new();

            People[0] = person.GetPeople(8000);
            People[1] = person.GetPeople(100);

            base.OnInitialized();
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Job { get; set; }

        public List<Person> GetPeople(int itemCount)
        {
            List<Person> people = new();

            for (int i = 0; i < itemCount; i++)
            {
                people.Add(new Person
                {
                    Id = i + 1,
                    FirstName = $"Person {i + 1}",
                    LastName = $"Person Family {i + 1}",
                    Job = $"Programmer {i + 1}"
                });
            }

            return people;
        }
    }
}
