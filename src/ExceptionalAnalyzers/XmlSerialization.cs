using System.IO;
using System.Xml.Serialization;

namespace Exceptional.Analyzers
{
    public static class XmlSerialization
    {
        public static T Deserialize<T>(string xml)
            where T : new()
        {
            if (string.IsNullOrEmpty(xml))
                return new T();

            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
                return (T)serializer.Deserialize(reader);
        }
    }
}