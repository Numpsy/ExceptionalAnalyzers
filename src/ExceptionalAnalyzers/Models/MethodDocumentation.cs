using System.Collections.Generic;
using System.Xml.Serialization;

namespace Exceptional.Analyzers.Models
{
    [XmlRoot("member")]
    public class MethodDocumentation
    {
        private string _summary;

        [XmlElement("summary")]
        public string Summary
        {
            get { return _summary; }
            set { _summary = value.Trim(); }
        }

        [XmlElement("exception")]
        public List<ExceptionDocumentation> Exceptions { get; set; } = new List<ExceptionDocumentation>();
    }
}