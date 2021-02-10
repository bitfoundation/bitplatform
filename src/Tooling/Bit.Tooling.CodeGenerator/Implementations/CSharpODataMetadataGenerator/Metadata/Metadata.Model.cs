using System.Collections.Generic;
using System.Xml.Serialization;

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpODataMetadataGenerator.Metadata
{
    [XmlRoot(ElementName = "Property", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Property
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "Nullable")]
        public string? Nullable { get; set; }
    }

    [XmlRoot(ElementName = "ComplexType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class ComplexType
    {
        [XmlElement(ElementName = "Property", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Property> Properties { get; set; } = new List<Property> { };

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "PropertyRef", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class PropertyRef
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "Key", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Key
    {
        [XmlElement(ElementName = "PropertyRef", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<PropertyRef> PropertyRefs { get; set; } = new List<PropertyRef> { };
    }

    [XmlRoot(ElementName = "NavigationProperty", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class NavigationProperty
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "EntityType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class EntityType
    {
        [XmlElement(ElementName = "Key", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public Key? Key { get; set; }

        [XmlElement(ElementName = "Property", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Property> Properties { get; set; } = new List<Property> { };

        [XmlElement(ElementName = "NavigationProperty", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<NavigationProperty> NavigationProperties { get; set; } = new List<NavigationProperty> { };

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "BaseType")]
        public string? BaseType { get; set; }
    }

    [XmlRoot(ElementName = "Schema", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Schema
    {
        [XmlElement(ElementName = "ComplexType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<ComplexType> ComplexTypes { get; set; } = new List<ComplexType>();

        [XmlElement(ElementName = "EntityType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<EntityType> EntityTypes { get; set; } = new List<EntityType>();

        [XmlAttribute(AttributeName = "Namespace")]
        public string Namespace { get; set; } = "Default";

        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; } = "http://docs.oasis-open.org/odata/ns/edm";

        [XmlElement(ElementName = "EnumType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<EnumType> EnumTypes { get; set; } = new List<EnumType> { };

        [XmlElement(ElementName = "Action", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Action> Actions { get; set; } = new List<Action> { };

        [XmlElement(ElementName = "Function", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Function> Functions { get; set; } = new List<Function> { };

        [XmlElement(ElementName = "EntityContainer", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public EntityContainer EntityContainer { get; set; }
    }

    [XmlRoot(ElementName = "Member", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Member
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "EnumType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class EnumType
    {
        [XmlElement(ElementName = "Member", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Member> Members { get; set; } = new List<Member> { };

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "Parameter", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Parameter
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "Nullable")]
        public string? Nullable { get; set; }
    }

    [XmlRoot(ElementName = "Action", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Action
    {
        [XmlElement(ElementName = "Parameter", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter> { };

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "IsBound")]
        public string IsBound { get; set; } = "true";

        [XmlElement(ElementName = "ReturnType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public ReturnType ReturnType { get; set; }
    }

    [XmlRoot(ElementName = "ReturnType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class ReturnType
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "Nullable")]
        public string? Nullable { get; set; }
    }

    [XmlRoot(ElementName = "Function", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class Function
    {
        [XmlElement(ElementName = "Parameter", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Parameter> Parameters { get; set; } = new List<Parameter> { };

        [XmlElement(ElementName = "ReturnType", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public ReturnType ReturnType { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "IsBound")]
        public string IsBound { get; set; } = "true";
    }

    [XmlRoot(ElementName = "NavigationPropertyBinding", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class NavigationPropertyBinding
    {
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }

        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }
    }

    [XmlRoot(ElementName = "EntitySet", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class EntitySet
    {
        [XmlElement(ElementName = "NavigationPropertyBinding", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<NavigationPropertyBinding> NavigationPropertyBindings { get; set; } = new List<NavigationPropertyBinding> { };

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "EntityType")]
        public string EntityType { get; set; }
    }

    [XmlRoot(ElementName = "EntityContainer", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
    public class EntityContainer
    {
        [XmlElement(ElementName = "EntitySet", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<EntitySet> EntitySets { get; set; } = new List<EntitySet> { };

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "DataServices", Namespace = "http://docs.oasis-open.org/odata/ns/edmx")]
    public class DataServices
    {
        [XmlElement(ElementName = "Schema", Namespace = "http://docs.oasis-open.org/odata/ns/edm")]
        public List<Schema> Schema { get; set; } = new List<Schema> { };
    }

    [XmlRoot(ElementName = "Edmx", Namespace = "http://docs.oasis-open.org/odata/ns/edmx")]
    public class MetadataEdmx
    {
        [XmlElement(ElementName = "DataServices", Namespace = "http://docs.oasis-open.org/odata/ns/edmx")]
        public DataServices DataServices { get; set; }

        [XmlAttribute(AttributeName = "Version")]
        public string Version { get; set; } = "4.0";

        [XmlAttribute(AttributeName = "edmx", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Edmx { get; set; } = "http://docs.oasis-open.org/odata/ns/edmx";
    }
}
