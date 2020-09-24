using Bit.Tooling.CodeGenerator.Implementations.CSharpClientProxyGenerator.Metadata;
using Bit.Tooling.CodeGenerator.Implementations.CSharpClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpClientProxyGenerator
{
    public class MetadataWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public class DefaultCSharpClientMetadataGenerator : ICSharpClientMetadataGenerator
    {
        readonly string escapeStr = new string(new[] { '\\', '"' });

        protected virtual string GetMetadata(IList<Dto> dtos, IList<Core.Model.EnumType> enumTypes, IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
        {
            var schemas = new List<Schema>();

            List<IGrouping<string, object>> groupedEnumsAndDtos =
                dtos.GroupBy(d => d.DtoSymbol.ContainingNamespace.ToDisplayString(), d => (object)d)
                .Union(enumTypes.GroupBy(e => e.EnumTypeSymbol.ContainingNamespace.ToDisplayString(), e => (object)e))
                .ToList();

            foreach (var g in groupedEnumsAndDtos)
            {
                var schema = new Schema
                {
                    Namespace = g.Key
                };

                foreach (var dto in g.OfType<Dto>())
                {
                    if (dto.DtoSymbol.IsComplexType())
                    {
                        schema.ComplexTypes.Add(new ComplexType
                        {
                            Name = dto.DtoSymbol.Name,
                            Properties = dto.Properties.Select(p => new Property
                            {
                                Name = p.Name,
                                Nullable = p.Type.IsNullable() ? null : "false",
                                Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection)
                            }).ToList()
                        });
                    }
                    else
                    {
                        var keys = dto.Properties.Where(p => p.IsKey()).ToArray();

                        schema.EntityTypes.Add(new EntityType
                        {
                            Name = dto.DtoSymbol.Name,
                            Key = new Key
                            {
                                PropertyRefs = keys.Select(k => new PropertyRef
                                {
                                    Name = k.Name
                                }).ToList(),
                            },
                            Properties = dto.Properties.Where(p => !p.Type.IsComplexType() && !p.Type.IsDto() && !p.Type.IsQueryableType() && !p.Type.IsCollectionType()).Select(p => new Property
                            {
                                Name = p.Name,
                                Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.NA),
                                Nullable = p.Type.IsNullable() ? null /*Default is true*/ : "false"
                            }).ToList(),
                            NavigationProperties = dto.Properties.Where(p => p.Type.IsComplexType() || p.Type.IsDto() || p.Type.IsQueryableType() || p.Type.IsCollectionType()).Select(p => new NavigationProperty
                            {
                                Name = p.Name,
                                Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection)
                            }).ToList()
                        });
                    }
                }

                foreach (var @enum in g.OfType<Core.Model.EnumType>())
                {
                    schema.EnumTypes.Add(new Metadata.EnumType
                    {
                        Name = @enum.EnumTypeSymbol.Name,
                        Members = @enum.Members.Select(m => new Member
                        {
                            Name = m.Name,
                            Value = m.Value.ToString(CultureInfo.InvariantCulture)
                        }).ToList()
                    });
                }

                schemas.Add(schema);
            }

            var defaultSchema = new Schema
            {
                Namespace = mapping.Namespace ?? "Default"
            };

            foreach (var operationWithController in controllers.SelectMany(c => c.Operations.Select(o => (Controller: c, Operation: o))))
            {
                if (operationWithController.Operation.Kind == ODataOperationKind.Action)
                {
                    var action = new Metadata.Action
                    {
                        Name = operationWithController.Operation.Method.Name
                    };

                    action.Parameters.Add(new Parameter { Name = "bindingParameter", Type = $"Collection({operationWithController.Controller.ModelSymbol.ToDisplayString()})" });

                    action.Parameters.AddRange(operationWithController.Operation.Parameters.Select(p => new Parameter
                    {
                        Name = p.Name,
                        Nullable = p.Type.IsNullable() ? null : "false",
                        Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection)
                    }));

                    if (!operationWithController.Operation.ReturnType.IsVoid())
                    {
                        action.ReturnType = new ReturnType
                        {
                            Nullable = operationWithController.Operation.ReturnType.IsNullable() ? null : "false",
                            Type = operationWithController.Operation.ReturnType.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection)
                        };
                    }

                    defaultSchema.Actions.Add(action);
                }
                else
                {
                    var function = new Function
                    {
                        Name = operationWithController.Operation.Method.Name
                    };

                    function.Parameters.Add(new Parameter { Name = "bindingParameter", Type = $"Collection({operationWithController.Controller.ModelSymbol.ToDisplayString()})" });

                    function.Parameters.AddRange(operationWithController.Operation.Parameters.Select(p => new Parameter
                    {
                        Name = p.Name,
                        Nullable = p.Type.IsNullable() ? null : "false",
                        Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection)
                    }));

                    function.ReturnType = new ReturnType
                    {
                        Nullable = operationWithController.Operation.ReturnType.IsNullable() ? null : "false",
                        Type = operationWithController.Operation.ReturnType.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection)
                    };

                    defaultSchema.Functions.Add(function);
                }
            }

            defaultSchema.EntityContainer = new EntityContainer
            {
                Name = $"{mapping.Route}Context",
                EntitySets = controllers.Select(c => new EntitySet
                {
                    EntityType = c.ModelSymbol.ToDisplayString(),
                    Name = c.Name
                }).ToList()
            };

            schemas.Add(defaultSchema);

            MetadataEdmx metadata = new MetadataEdmx
            {
                DataServices = new DataServices
                {
                    Schema = schemas
                }
            };

            using MetadataWriter metadataStringBuilder = new MetadataWriter { NewLine = string.Empty };
            new XmlSerializer(typeof(MetadataEdmx)).Serialize(metadataStringBuilder, metadata);

            string metadataString = metadataStringBuilder.ToString().Replace("\"", escapeStr, StringComparison.InvariantCulture);

            return metadataString;
        }

        public string GenerateMetadata(IList<Dto> dtos, IList<Core.Model.EnumType> enumTypes, IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos));

            if (enumTypes == null)
                throw new ArgumentNullException(nameof(enumTypes));

            CSharpMetadataGeneratorTemplate template = new CSharpMetadataGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Mapping", mapping },
                    { "BitToolingVersion", typeof(DefaultCSharpClientContextGenerator).Assembly.GetName().Version!.ToString() },
                    { "MetadataString", GetMetadata(dtos, enumTypes, controllers, mapping) }
                }
            };

            template.Initialize();

            return template.TransformText();
        }
    }
}
