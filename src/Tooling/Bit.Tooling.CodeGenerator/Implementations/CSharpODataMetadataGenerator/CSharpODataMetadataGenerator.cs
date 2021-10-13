using Bit.Tooling.CodeGenerator.Implementations.CSharpODataMetadataGenerator.Metadata;
using Bit.Tooling.CodeGenerator.Implementations.CSharpODataMetadataGenerator.Templates;
using Bit.Tooling.CodeGenerator.Implementations.CSharpSimpleODataClientProxyGenerator;
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

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpODataMetadataGenerator
{
    public class CSharpSimpleODataClientMetadataGenerator : ICSharpClientMetadataGenerator
    {
        readonly string escapeStr = new string(new[] { '\\', '"' });

        static bool ShouldGetsIgnored(IPropertySymbol p)
        {
            return p.GetAttributes().Any(att => att.AttributeClass.Name == "NotMappedAttribute")
                || p.IsOverride;
        }

        static bool IsNavProp(ITypeSymbol t)
        {
            if (t.IsCollectionType() || t.IsQueryableType())
                t = t.GetElementType();

            return t.IsDto();
        }

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
                            Properties = dto.Properties.Where(p => !ShouldGetsIgnored(p)).Select(p => new Property
                            {
                                Name = p.Name,
                                Nullable = p.Type.IsNullable() ? "true" : "false",
                                Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None)
                            }).ToList()
                        });
                    }
                    else
                    {
                        var keys = dto.Properties.Where(p => p.IsKey()).ToArray();

                        schema.EntityTypes.Add(new EntityType
                        {
                            Name = dto.DtoSymbol.Name,
                            BaseType = dto.BaseDtoSymbol?.ToDisplayString(topLevelNullability: NullableFlowState.None),
                            Key = keys.Any() ? new Key
                            {
                                PropertyRefs = keys.Select(k => new PropertyRef
                                {
                                    Name = k.Name
                                }).ToList(),
                            } : null /* Inheritance: Key is in base type for example */,
                            Properties = dto.Properties.Where(p => !ShouldGetsIgnored(p) && !IsNavProp(p.Type)).Select(p => new Property
                            {
                                Name = p.Name,
                                Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None),
                                Nullable = p.Type.IsNullable() ? "true" : "false"
                            }).ToList(),
                            NavigationProperties = dto.Properties.Where(p => !ShouldGetsIgnored(p) && IsNavProp(p.Type)).Select(p => new NavigationProperty
                            {
                                Name = p.Name,
                                Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None)
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

                    action.Parameters.Add(new Parameter { Name = "bindingParameter", Type = $"Collection({operationWithController.Controller.ModelSymbol.ToDisplayString(topLevelNullability: NullableFlowState.None)})" });

                    action.Parameters.AddRange(operationWithController.Operation.Parameters.Select(p => new Parameter
                    {
                        Name = p.Name,
                        Nullable = p.Type.IsNullable() ? "true" : "false",
                        Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None)
                    }));

                    if (!operationWithController.Operation.ReturnType.IsVoid())
                    {
                        action.ReturnType = new ReturnType
                        {
                            Nullable = operationWithController.Operation.ReturnType.IsNullable() ? "true" : "false",
                            Type = operationWithController.Operation.ReturnType.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None)
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

                    function.Parameters.Add(new Parameter { Name = "bindingParameter", Type = $"Collection({operationWithController.Controller.ModelSymbol.ToDisplayString(topLevelNullability: NullableFlowState.None)})" });

                    function.Parameters.AddRange(operationWithController.Operation.Parameters.Select(p => new Parameter
                    {
                        Name = p.Name,
                        Nullable = p.Type.IsNullable() ? "true" : "false",
                        Type = p.Type.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None)
                    }));

                    function.ReturnType = new ReturnType
                    {
                        Nullable = operationWithController.Operation.ReturnType.IsNullable() ? "true" : "false",
                        Type = operationWithController.Operation.ReturnType.GetEdmTypeName(TypeToEdmTypeCollectionBehavior.UseODataCollection, topLevelNullability: NullableFlowState.None)
                    };

                    defaultSchema.Functions.Add(function);
                }
            }

            defaultSchema.EntityContainer = new EntityContainer
            {
                Name = $"{mapping.Route}Context",
                EntitySets = controllers.Select(c => new EntitySet
                {
                    EntityType = c.ModelSymbol.ToDisplayString(topLevelNullability: NullableFlowState.None),
                    Name = c.Name,
                    NavigationPropertyBindings = c.ModelSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !ShouldGetsIgnored(p) && IsNavProp(p.Type) && controllers.Any(_c => SymbolEqualityComparer.Default.Equals(_c.ModelSymbol, p.Type.IsCollectionType() ? p.Type.GetElementType() : p.Type))).Select(p => new NavigationPropertyBinding
                    {
                        Path = p.Name,
                        Target = controllers.First(_c => SymbolEqualityComparer.Default.Equals(_c.ModelSymbol, p.Type.IsCollectionType() ? p.Type.GetElementType() : p.Type)).Name
                    }).ToList()
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

            using StringWriter metadataStringBuilder = new StringWriter { NewLine = string.Empty };
            new XmlSerializer(typeof(MetadataEdmx)).Serialize(metadataStringBuilder, metadata);

            string metadataString = metadataStringBuilder.ToString().Replace("\"", escapeStr, StringComparison.InvariantCulture).Replace(Environment.NewLine, string.Empty);

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

            CSharpODataMetadataGeneratorTemplate template = new CSharpODataMetadataGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Mapping", mapping },
                    { "BitToolingVersion", typeof(CSharpSimpleODataClientContextGenerator).Assembly.GetName().Version!.ToString() },
                    { "MetadataString", GetMetadata(dtos, enumTypes, controllers, mapping) }
                }
            };

            template.Initialize();

            return template.TransformText();
        }
    }
}
