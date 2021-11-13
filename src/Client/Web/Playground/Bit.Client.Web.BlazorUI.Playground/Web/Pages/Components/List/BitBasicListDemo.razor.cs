using System;
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

        private static string listCodeBehinde = $"@code {{ {Environment.NewLine}" +
             $"private readonly List<Person>[] People = new List<Person>[2];{Environment.NewLine}" +
             $"protected override void OnInitialized(){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"Person person = new();{Environment.NewLine}" +
             $"People[0] = person.GetPeople(8000);{Environment.NewLine}" +
             $"People[1] = person.GetPeople(100);{Environment.NewLine}" +
             $"base.OnInitialized();{Environment.NewLine}" +
             $"}} {Environment.NewLine}" +
             $"public class Person(){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"public int Id {{ get;  set; }}{Environment.NewLine}" +
             $"public int FirstName {{ get;  set; }}{Environment.NewLine}" +
             $"public int LastName {{ get;  set; }}{Environment.NewLine}" +
             $"public int Job {{ get;  set; }}{Environment.NewLine}" +
             $"}} {Environment.NewLine}" +
             $"public List<Person> GetPeople(int itemCount){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"List<Person> person = new(); {Environment.NewLine}" +
             $"for (int i = 0; i < itemCount; i++){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"people.Add(new Person {Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"Id = i + 1,{Environment.NewLine}" +
             $"FirstName = $'Person {{i + 1}}',{Environment.NewLine}" +
             $"LastName = $'Person Family {{ i + 1}}',{Environment.NewLine}" +
             $"Job = $'Programmer {{i + 1}}'{Environment.NewLine}" +
             $"}});{Environment.NewLine}" +
             $"}} {Environment.NewLine}" +
             $"}} {Environment.NewLine}" +
             $"}}";

        private readonly string basicListSampleCode = $"<BitBasicList Items='People[0]'{Environment.NewLine}" +
             $"Virtualize='true'{Environment.NewLine}" +
             $"Style='border: 1px #a19f9d solid; border-radius: 3px;'>{Environment.NewLine}" +
             $"<RowTemplate Context='person'>{Environment.NewLine}" +
             $"<div style='margin-left:3%; display: inline-block;'>{Environment.NewLine}" +
             $"<img src='https://picsum.photos/100/100?random=@(person.Id)'>{Environment.NewLine}" +
             $"<div style='margin-left:3%; display: inline-block;'>{Environment.NewLine}" +
             $"<p>Id: <strong>@person.Id</strong></p>{Environment.NewLine}" +
             $"<p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>{Environment.NewLine}" +
             $"<p>Job: <strong>@person.Job</strong></p>{Environment.NewLine}" +
             $"</div>{Environment.NewLine}" +
             $"</div>{Environment.NewLine}" +
             $"</RowTemplate>{Environment.NewLine}" +
             $"</BitBasicList>{Environment.NewLine}" + listCodeBehinde;

        private readonly string basicListWithoutVirtualizationSampleCode = $"<BitBasicList Items='People[1]'{Environment.NewLine}" +
            $"Virtualize='false'{Environment.NewLine}" +
            $"Style='border: 1px #a19f9d solid; border-radius: 3px;'>{Environment.NewLine}" +
            $"<RowTemplate Context='person'>{Environment.NewLine}" +
            $"<div style='margin-left:3%; display: inline-block;'>{Environment.NewLine}" +
            $"<img src='https://picsum.photos/100/100?random=@(person.Id)'>{Environment.NewLine}" +
            $"<div style='margin-left:3%; display: inline-block;'>{Environment.NewLine}" +
            $"<p>Id: <strong>@person.Id</strong></p>{Environment.NewLine}" +
            $"<p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>{Environment.NewLine}" +
            $"<p>Job: <strong>@person.Job</strong></p>{Environment.NewLine}" +
            $"</div>{Environment.NewLine}" +
            $"</div>{Environment.NewLine}" +
            $"</RowTemplate>{Environment.NewLine}" +
            $"</BitBasicList>{Environment.NewLine}" + listCodeBehinde;

        private readonly string basicListWithCustomRoleSampleCode = $"<BitBasicList Items='People[0]'{Environment.NewLine}" +
            $"Virtualize='true'{Environment.NewLine}" +
            $"Role='list'{Environment.NewLine}" +
            $"Class='lst-custom-style'{Environment.NewLine}" +
            $"Style='border: 1px #a19f9d solid; border-radius: 3px;'>{Environment.NewLine}" +
            $"<RowTemplate Context='person'>{Environment.NewLine}" +
            $"<div class='lst3-list-item'>{Environment.NewLine}" +
            $"<span>Id: <strong>@person.Id</strong></span>{Environment.NewLine}" +
            $"<span>Full Name: <strong>@person.FirstName @person.LastName</strong></span>{Environment.NewLine}" +
            $"<span>Job: <strong>@person.Job</strong></span>{Environment.NewLine}" +
            $"</div>{Environment.NewLine}" +
            $"</RowTemplate>{Environment.NewLine}" +
            $"</BitBasicList>{Environment.NewLine}" + listCodeBehinde +
            $"<style>{Environment.NewLine}" +
            $".lst-custom-style {{ {Environment.NewLine}" +
            $".lst3-list-item {{ {Environment.NewLine}" +
            $"padding :16px 20px;{Environment.NewLine}" +
            $"background-color : #f2f2f2;{Environment.NewLine}" +
            $"margin : 10px 10px;{Environment.NewLine}" +
            $"width : 20%;{Environment.NewLine}" +
            $"height : 143px;{Environment.NewLine}" +
            $"display : inline-grid;{Environment.NewLine}" +
            $"justify-content : center;{Environment.NewLine}" +
            $"align-items : center;{Environment.NewLine}" +
            $"}} {Environment.NewLine}" +
            $"}} {Environment.NewLine}" +
            $"</style>";

        private readonly string basicListWithoverscanSampleCode = $"<BitBasicList Items='People[0]'{Environment.NewLine}" +
            $"Virtualize='true'{Environment.NewLine}" +
            $"OverscanCount='5'{Environment.NewLine}" +
            $"ItemSize='300'{Environment.NewLine}" +
            $"Style='border: 1px #a19f9d solid; border-radius: 3px;'>{Environment.NewLine}" +
            $"<RowTemplate Context='person'>{Environment.NewLine}" +
            $"<div style='border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;'>{Environment.NewLine}" +
            $"<span>Id: <strong>@person.Id</strong></span>{Environment.NewLine}" +
            $"<span>Full Name: <strong>@person.FirstName @person.LastName</strong></span>{Environment.NewLine}" +
            $"<span>Job: <strong>@person.Job</strong></span>{Environment.NewLine}" +
            $"</div>{Environment.NewLine}" +
            $"</RowTemplate>{Environment.NewLine}" +
            $"</BitBasicList>{Environment.NewLine}" + listCodeBehinde;

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
