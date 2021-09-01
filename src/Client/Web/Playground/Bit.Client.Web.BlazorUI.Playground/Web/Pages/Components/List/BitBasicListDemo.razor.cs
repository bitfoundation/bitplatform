using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.List
{
    public partial class BitBasicListDemo
    {
        private readonly List<Person>[] People = new List<Person>[2];

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
