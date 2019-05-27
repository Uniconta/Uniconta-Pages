using System.Security.Cryptography.Xml;
using System.Xml;

namespace SecuredClient.XmlSecurity
{
    public class SignedXmlWithId : SignedXml
    {
        public SignedXmlWithId(XmlDocument xml) : base(xml)
        {
        }

        public SignedXmlWithId(XmlElement xmlElement) : base(xmlElement)
        {
        }


        public override XmlElement GetIdElement(XmlDocument doc, string id)
        {
            var idElem = base.GetIdElement(doc, id);

            if (idElem == null)
            {
                var nsManager = new XmlNamespaceManager(doc.NameTable);
                nsManager.AddNamespace("u", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");

                idElem = doc.SelectSingleNode("//*[@u:Id=\"" + id + "\"]", nsManager) as XmlElement;
            }

            return idElem;
        }
    }
}