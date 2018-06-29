using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace MailTest
{


    [XmlType(TypeName = "error")]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class SMTPCode
    {
        [XmlAttribute("code")]
        public string Code { get; set; }
        [XmlElement("message")]
        public string Message { get; set; }
        [XmlElement("desc")]
        public string Description { get; set; }
        
        public static List<SMTPCode> GetErrorList()
        {
            List<SMTPCode> errors;
            using (var reader = new StreamReader("SMTPErrors.xml"))
            {
                var deserializer = new XmlSerializer(typeof(List<SMTPCode>),
                    new XmlRootAttribute("errors"));
                errors = (List<SMTPCode>) deserializer.Deserialize(reader);
            }

            return errors;
        }
    }

}
