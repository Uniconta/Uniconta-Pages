using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SecuredClient.XmlSecurity
{
    public class XmlUtil
    {
        private XmlUtil() 
        {
        }

        public static XmlDocument ToDocument(string xml)
        {
            var document = new XmlDocument {PreserveWhitespace = true};
            document.LoadXml(xml);
            return document;
        }


        public static string PrettyXml(XmlDocument xml)
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = true
            };
            return Write(xml, settings);
        }

        private static string Write(XmlDocument xml, XmlWriterSettings settings)
        {
            var stringBuilder = new StringBuilder();

            var element = XElement.Parse(xml.OuterXml);


            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }

        public static string AsText(XmlDocument xml)
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                NewLineOnAttributes = true
            };
            return Write(xml, settings);
        }
    }
}
