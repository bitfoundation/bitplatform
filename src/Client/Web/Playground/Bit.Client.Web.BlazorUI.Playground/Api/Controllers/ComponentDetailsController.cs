using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Bit.Client.Web.BlazorUI.Playground.Shared.ExtensionMethods;
using Bit.Client.Web.BlazorUI.Playground.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bit.Client.Web.BlazorUI.Playground.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ComponentDetailsController : ControllerBase
    {
        public ComponentDetailsController(IConfiguration configuration)
        {
            BitComponentAssembly = Assembly.GetAssembly(typeof(BitToggleButton));
        }

        protected Assembly BitComponentAssembly { get;}

        [HttpGet]
        public async Task<ActionResult<List<ComponentDetailsDto>>>
            Get(string componentName)
        {
            if(string.IsNullOrWhiteSpace(componentName))
                return BadRequest("Component Name is empty!");

            var result = new List<ComponentDetailsDto>();

            var classType = BitComponentAssembly.ExportedTypes
                .Where(c=> c.Name.ToLower() == componentName.ToLower())
                .FirstOrDefault();

            if (classType == null) return NotFound("No component type found!");

            string xml;
            using (var reader = new StreamReader(System.IO.File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Bit.Client.Web.BlazorUI.xml"))))
            {
                xml = await reader.ReadToEndAsync();
            }

            var xmlFile = XDocument.Parse(xml);

            foreach (var item in classType.GetProperties())
            {
                string propertyAttribute = $"{classType.FullName}.{item.Name}";
                var propertyXML = xmlFile.Descendants().Attributes()
                    .Where(attr => attr.Value.Contains(propertyAttribute))
                    .FirstOrDefault();

                var componentDetail = new ComponentDetailsDto
                {
                    Name = item.Name,
                    Type = item.PropertyType.Name,
                    Summary = propertyXML?.Parent.Element("summary")?.Value.Fix(),
                    DefaultValue = propertyXML?.Parent.Element("defaultValue")?.Value.Fix(),
                };
                result.Add(componentDetail);
            }

            return Ok(result);
        }
    }

}
