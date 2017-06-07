using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    public class DefaultXmlContentFormatter : IContentFormatter
    {
        public virtual string Serialize<T>(T obj)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof (T));
            using (StringWriter stringWriter = new StringWriter())
            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
            {
                xmlSerializer.Serialize(xmlWriter, obj);
                return stringWriter.ToString();
            }
        }

        public virtual T DeSerialize<T>(string objAsStr)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof (T));
            using (StringReader stringWriter = new StringReader(objAsStr))
                return (T) xmlSerializer.Deserialize(stringWriter);
        }
    }
}