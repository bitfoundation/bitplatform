using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;

        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private void HideMessageBar(MouseEventArgs args)
        {
            IsMessageBarHidden = true;
        }



        private List<Person> people = new List<Person>();
        protected override void OnInitialized()
        {
            base.OnInitialized();
            for (int i = 1; i < 8001; i++)
            {
                people.Add(new Person
                {
                    Id = i,
                    FirstName = $"Person {i.ToString()}",
                    LastName =$"Person Family {i.ToString()}",
                    Job = $"Programmer {i.ToString()}"
                });
            }
        }
    }
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Job { get; set; }
    }
}
