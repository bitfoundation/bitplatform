using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bit.Client.Web.BlazorUI.Playground.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Client.Web.BlazorUI.Playground.Api.Controllers
{
    [ApiController]
    [Route("component-details")]
    public class ComponentDetailsController : ControllerBase
    {
        private static readonly Assembly _bitComponentAssembly = Assembly.GetAssembly(typeof(BitToggleButton));
       
        [HttpGet("properties")]
        public async Task<ActionResult<List<ComponentPropertyDetailsDto>>>
            GetProperties(string componentName)
        {
            if(string.IsNullOrWhiteSpace(componentName))
                return BadRequest("Component Name is empty!");

            var classType = GetComponentType(componentName);
            
            if (classType == null) 
                return NotFound("No component type found!");

            var result = new List<ComponentPropertyDetailsDto>();

            var xmlFile = await LoadXmlFileAsync();
            var instance = Activator.CreateInstance(classType);

            foreach (var item in classType.GetProperties())
            {
                string propertyAttribute = $"{classType.FullName}.{item.Name}";
                var xmlProperty = xmlFile?.Descendants().Attributes()
                    .Where(attr => attr.Value.Contains(propertyAttribute))
                    .FirstOrDefault();

                var componentDetail = new ComponentPropertyDetailsDto
                {
                    Name = item.Name,
                    Type = item.PropertyType.Name,
                    Summary = xmlProperty?.Parent.Element("summary")?.Value.Trim(),
                    DefaultValue = item.GetValue(instance)?.ToString(),
                };
                result.Add(componentDetail);
            }

            return Ok(result);
        }

        [HttpGet("methods")]
        public async Task<ActionResult<List<ComponentMethodDetailsDto>>>
            GetMethods(string componentName)
        {
            if (string.IsNullOrWhiteSpace(componentName))
                return BadRequest("Component Name is empty!");

            var classType = GetComponentType(componentName);

            if (classType == null)
                return NotFound("No component type found!");

            var result = new List<ComponentMethodDetailsDto>();
            
            var xmlFile = await LoadXmlFileAsync();

            var methodList = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Where(m => !m.IsSpecialName).ToList();

            foreach (var item in methodList)
            {
                string propertyAttribute = $"{classType.FullName}.{item.Name}";
                var xmlProperty = xmlFile?.Descendants().Attributes()
                    .Where(attr => attr.Value.Contains(propertyAttribute))
                    .FirstOrDefault();

                var parameters = item.GetParameters();
                
                var componentDetail = new ComponentMethodDetailsDto
                {
                    Name = item.Name,
                    Parameters = parameters.Length > 0 ?
                        parameters.Select(v=> v.ParameterType.FullName).Aggregate((a,b)=> $"{a},{b}") : null,
                    Output = item.ReturnType.ToString(),
                    Summary = xmlProperty?.Parent.Element("summary")?.Value.Trim(),
                };
                result.Add(componentDetail);
            }

            return Ok(result);
        }

        private static async Task<XDocument> LoadXmlFileAsync()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Bit.Client.Web.BlazorUI.xml");

            if (System.IO.File.Exists(path) == false)
                return null;

            var stream = System.IO.File.OpenRead(path);
            return await XDocument.LoadAsync(stream, LoadOptions.None, default);
        }

        private static Type GetComponentType(string name)
        {
            return _bitComponentAssembly.ExportedTypes
                .Where(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
        }
    }

}
