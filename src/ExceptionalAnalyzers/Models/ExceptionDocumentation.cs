using System.Xml.Serialization;

namespace Exceptional.Analyzers.Models
{
    public class ExceptionDocumentation
    {
        private string _type;
        private string _description;

        [XmlAttribute("cref")]
        public string Type
        {
            get { return _type; }
            set { _type = value.Trim(); }
        }

        [XmlText]
        public string Description
        {
            get { return _description; }
            set { _description = value.Trim(); }
        }
    }
}