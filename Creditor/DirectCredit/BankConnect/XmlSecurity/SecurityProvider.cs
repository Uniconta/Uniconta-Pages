using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using SecuredClient.MessageSecurity;

namespace SecuredClient.XmlSecurity
{
    public abstract class SecurityProvider
    {
        protected readonly XmlDocument Document;
        protected readonly NamespaceManager Manager;

        protected SecurityProvider(XmlDocument document)
        {
            Document = document;
            Manager = new NamespaceManager(document);
        }

        protected void CreateBinarySecurityToken(XmlElement securityElement, X509Certificate certificate, string id)
        {
            var binarySecurityToken = Document.CreateElement("wsse:BinarySecurityToken", Manager.LookupNamespace("wsse"));
            securityElement.AppendChild(binarySecurityToken);
            binarySecurityToken.SetAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
            binarySecurityToken.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            binarySecurityToken.SetAttribute("Id", Manager.LookupNamespace("wsu"), id);
            binarySecurityToken.InnerText = Convert.ToBase64String(certificate.GetRawCertData());
        }

        protected void CreateSoapHeaderIfMissing()
        {
            var bodyElements = SelectNodes("//soap:Body");
            if (bodyElements.Count != 1)
            {
                throw new SecurityException("Document must have exactly one soap:Body element, but " + bodyElements.Count + " was found");
            }
            var bodyElement = bodyElements[0];

            var headerElements = SelectNodes("//soap:Header");
            if (headerElements.Count > 1)
            {
                throw new SecurityException("Document with more than one soap:Header" + headerElements.Count);
            }
            if (headerElements.Count != 0) return;
            var headerElement = Document.CreateElement("soap:Header", Manager.LookupNamespace("soap"));
            Document.FirstChild.InsertBefore(headerElement, bodyElement);
        }

        protected XmlElement GetOrCreateSecurityElement()
        {
            var wssElements = SelectNodes("//wsse:Security");
            if (wssElements.Count > 1)
            {
                throw new SecurityException("Document has more than one wsse:Security element");
            }
            if (wssElements.Count == 1)
            {
                return wssElements[0] as XmlElement;
            }
            XmlElement securityElement = Document.CreateElement("wsse:Security", Manager.LookupNamespace("wsse"));
            securityElement.SetAttribute("xmlns:wsu", Manager.LookupNamespace("wsu"));
            securityElement.SetAttribute("mustUnderstand", Manager.LookupNamespace("soap"), "1");

            var header = SelectNodes("//soap:Header")[0];
            return (XmlElement) header.PrependChild(securityElement);
        }

        protected XmlNodeList SelectNodes(string xpath)
        {
            return Document.SelectNodes(xpath, Manager);
        }

        protected XmlNodeList SelectNodesNamed(string localName, string namespaceUri)
        {
            var prefix = Manager.LookupPrefix(namespaceUri);
            return SelectNodes("//" + prefix + ":" + localName);
        }

        protected static string CreateId()
        {
            return CreateIdWithPrefix("Id");
        }

        protected static string CreateIdWithPrefix(string prefix)
        {
            var id = prefix + "-" + Guid.NewGuid();
            return id;
        }

        protected XmlElement GetSingleElementByName(string localName, string namespaceUri)
        {
            var elements = SelectNodesNamed(localName, namespaceUri);
            if (elements.Count != 1)
            {
                throw new SecurityException("Expected exactly one element named {" + namespaceUri + "}" + localName + ", found " + elements.Count);
            }
            return elements[0] as XmlElement;
        }

        protected string CreateIdOnElement(XmlNode element)
        {
            var id = CreateId();
            var attribute = Document.CreateAttribute("id");
            attribute.Value = id;
            element.Attributes.Append(attribute);
            return id;
        }

        protected string CreateWsuIdOnElement(XmlNode element)
        {
            var id = CreateId();
            var attribute = Document.CreateAttribute("wsu", "Id", Manager.LookupNamespace("wsu"));
            attribute.Value = id;
            element.Attributes.Append(attribute);
            return id;
        }

        protected XmlElement CreateSecurityTokenReference(string referenceId, XmlElement parentElement)
        {
            var keyInfo = Document.CreateElement("ds:KeyInfo", Manager.LookupNamespace("ds"));
            var securityTokenReference = Document.CreateElement("wsse:SecurityTokenReference", Manager.LookupNamespace("wsse"));
            CreateWsuIdOnElement(securityTokenReference);
            keyInfo.AppendChild(securityTokenReference);
            var reference = CreateReference(referenceId);
            securityTokenReference.AppendChild(reference);
            parentElement?.AppendChild(keyInfo);
            return keyInfo;
        }

        private XmlElement CreateReference(string referenceId)
        {
            var reference = Document.CreateElement("wsse:Reference", Manager.LookupNamespace("wsse"));
            reference.SetAttribute("URI", "#" + referenceId);
            reference.SetAttribute("ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            return reference;
        }

        protected XmlElement GetFirstChildInBody()
        {
            var body = Document.SelectSingleNode("//soap:Body/*[1]", Manager);
            if (body == null)
            {
                throw new SecurityException("No child in soap:Body element found");
            }
            return body as XmlElement;
        }

        protected void SetDsPrefix()
        {
            foreach (XmlNode node in Document.SelectNodes("descendant-or-self::*[namespace-uri()='http://www.w3.org/2000/09/xmldsig#']"))
            {
                node.Prefix = "ds";
            }
        }
    }
}
