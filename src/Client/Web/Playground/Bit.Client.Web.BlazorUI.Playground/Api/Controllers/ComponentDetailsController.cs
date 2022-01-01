using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bit.Client.Web.BlazorUI.Playground.Shared.ExtensionMethods;
using Bit.Client.Web.BlazorUI.Playground.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bit.Client.Web.BlazorUI.Playground.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ComponentDetailsController : ControllerBase
    {
        public ComponentDetailsController()
        {
            BitComponentAssembly = Assembly.GetAssembly(typeof(BitToggleButton));
        }

        protected Assembly BitComponentAssembly { get;}

        [HttpGet("GetProperties")]
        public async Task<ActionResult<List<ComponentPropertyDetailsDto>>>
            GetProperties(string componentName)
        {
            if(string.IsNullOrWhiteSpace(componentName))
                return BadRequest("Component Name is empty!");

            var result = new List<ComponentPropertyDetailsDto>();

            var classType = BitComponentAssembly.ExportedTypes
                .Where(c=> c.Name.ToLower() == componentName.ToLower())
                .FirstOrDefault();

            if (classType == null) 
                return NotFound("No component type found!");

            string xml;
            using (var reader = new StreamReader(System.IO.File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Bit.Client.Web.BlazorUI.xml"))))
            {
                xml = await reader.ReadToEndAsync();
            }

            var xmlFile = XDocument.Parse(xml);

            foreach (var item in classType.GetProperties())
            {
                string propertyAttribute = $"{classType.FullName}.{item.Name}";
                var xmlProperty = xmlFile.Descendants().Attributes()
                    .Where(attr => attr.Value.Contains(propertyAttribute))
                    .FirstOrDefault();

                var componentDetail = new ComponentPropertyDetailsDto
                {
                    Name = item.Name,
                    Type = item.PropertyType.Name,
                    Summary = xmlProperty?.Parent.Element("summary")?.Value.Fix(),
                    DefaultValue = xmlProperty?.Parent.Element("defaultValue")?.Value.Fix(),
                };
                result.Add(componentDetail);
            }

            return Ok(result);
        }

        [HttpGet("GetMethods")]
        public async Task<ActionResult<List<ComponentMethodDetailsDto>>>
            GetMethods(string componentName)
        {
            if (string.IsNullOrWhiteSpace(componentName))
                return BadRequest("Component Name is empty!");

            var result = new List<ComponentMethodDetailsDto>();

            var classType = BitComponentAssembly.ExportedTypes
                .Where(c => c.Name.ToLower() == componentName.ToLower())
                .FirstOrDefault();

            if (classType == null)
                return NotFound("No component type found!");

            string xml;
            using (var reader = new StreamReader(System.IO.File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Bit.Client.Web.BlazorUI.xml"))))
            {
                xml = await reader.ReadToEndAsync();
            }

            var xmlFile = XDocument.Parse(xml);

            var methodList = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Where(m => !m.IsSpecialName).ToList();
            foreach (var item in methodList)
            {
                string propertyAttribute = $"{classType.FullName}.{item.Name}";
                var xmlProperty = xmlFile.Descendants().Attributes()
                    .Where(attr => attr.Value.Contains(propertyAttribute))
                    .FirstOrDefault();

                var signatures = item.GetParameters();
                
                var componentDetail = new ComponentMethodDetailsDto
                {
                    Name = item.Name,
                    Signatures = signatures.Length > 0 ? signatures.Select(v=> v.ParameterType.FullName).Aggregate((a,b)=> $"{a},{b}") : null,
                    Output = item.ReturnType.ToString(),
                    Summary = xmlProperty?.Parent.Element("summary")?.Value.Fix(),
                };
                result.Add(componentDetail);
            }

            return Ok(result);
        }
    }

}
