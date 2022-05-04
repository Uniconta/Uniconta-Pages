using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using SecuredClient.Crypto;
using Uniconta.Common;

namespace SecuredClient.XmlSecurity
{
    public class Signer : SecurityProvider
    {
        public Signer(XmlDocument document) : base(document)
        {
            CryptoConfig.AddAlgorithm(typeof(RsaSha256SignatureDescription), @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");

            StripWhiteSpace();
        }

        private void StripWhiteSpace()
        {
            Document.PreserveWhitespace = false;
            var xml = Document.InnerXml;
            Document.LoadXml(xml);
        }

        public void WsSecuritySign()
        {
            var elements = new List<XmlNode>
            {
                GetSingleElementByName("Body", Manager.LookupNamespace("soap")),
                GetSingleElementByName("serviceHeader", Manager.LookupNamespace("bc")) 
            };
            SignElements(elements);
        }

        private void SignElements(IEnumerable<XmlNode> elementsToSign)
        {
            CreateSoapHeaderIfMissing();
            var securityElement = GetOrCreateSecurityElement();
            var signedXml = CreateSignedXml(securityElement);

            foreach (var elementToSign in elementsToSign)
            {
                var id = CreateWsuIdOnElement(elementToSign);
                CreateIdReference(id, signedXml);
            }
            ComputeSignature(signedXml);
            var signatureElement = AddSignatureElement(signedXml, securityElement, true);
            SetDsPrefix();

            var certificate = Resign(signatureElement);
            var tokenId = "X509" + Guid.NewGuid();
            CreateBinarySecurityToken(securityElement, certificate, tokenId);
            CreateSecurityTokenReference(tokenId, signatureElement);
        }

        private static SignedXmlWithId CreateSignedXml(XmlElement securityElement)
        {
            var signedXml = new SignedXmlWithId(securityElement);
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            return signedXml;
        }

        private XmlElement AddSignatureElement(SignedXml signedXml, XmlNode targetNode, bool prependElement)
        {
            var signatureElement = signedXml.GetXml();
            AddId(signatureElement);
            if (prependElement)
            {
                targetNode.PrependChild(signatureElement);
            }
            else
            {
                targetNode.AppendChild(signatureElement);
            }
            return signatureElement;
        }

        private static void CreateIdReference(string id, SignedXml signedXml)
        {
            var reference = new Reference("#" + id);
            reference.AddTransform(new XmlDsigExcC14NTransform());
            reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
            signedXml.AddReference(reference);
        }

        private void ComputeSignature(SignedXml signedXml)
        {
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signedXml.SigningKey = new RSACryptoServiceProvider();
            signedXml.ComputeSignature(); //With default RSA Key
        }

        private void AddId(XmlNode signatureElement)
        {
            var signedInfoId = "DS-" + Guid.NewGuid();
            var signedInfoIdAttribute = Document.CreateAttribute("Id");
            signedInfoIdAttribute.Value = signedInfoId;
            signatureElement.Attributes.Append(signedInfoIdAttribute);
        }

        private X509Certificate Resign(XmlElement signatureElement)
        {
            var signedBytes = CanonicalizeSignedInfo(signatureElement);
            var digest = new SHA256Managed();
            var hash = digest.ComputeHash(signedBytes);

            var signature = SignRsaSha256(hash);
            var signatureEncoded = Convert.ToBase64String(signature);

            var signatureValueNode = signatureElement.SelectSingleNode("ds:SignatureValue", Manager);
            signatureValueNode.InnerText = signatureEncoded;
            
            return CertificateStore.Instance.ClientCertificate;
        }

        private static byte[] SignRsaSha256(byte[] digest)
        {
            var oid = CryptoConfig.MapNameToOID("SHA256");
            return CertificateStore.Instance.PrivateKey.SignHash(digest, oid);
        }

        private byte[] CanonicalizeSignedInfo(XmlNode signatureElement)
        {
            var signedInfoNode = signatureElement.SelectSingleNode("ds:SignedInfo", Manager);
            var reader = new XmlNodeReader(signedInfoNode);
            var stream = UnistreamReuse.Create();
            var writer = new XmlTextWriter(stream, Encoding.UTF8);
            writer.WriteNode(reader, false);
            writer.Flush();
            stream.Position = 0;
            var transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(stream);
            stream.Release();
            stream = null;

            var signedInfoStream = (Stream) transform.GetOutput();
            var signedBytes = new byte[signedInfoStream.Length];
            signedInfoStream.Read(signedBytes, 0, signedBytes.Length);
            return signedBytes;
        }

        public void BusinessSign()
        {
            var elementToSign = GetBusinessElement();
            if (elementToSign == null) throw new SecurityException("Business signing requested, but no business element is present in the document.");

            var signedXml = CreateSignedXml(elementToSign);
            var id = CreateIdOnElement(elementToSign);
            CreateIdReference(id, signedXml);
            ComputeSignature(signedXml);

            var signatureElement = AddSignatureElement(signedXml, elementToSign.ParentNode, false);
            SetDsPrefix();
            var certificate = Resign(signatureElement);
            AddCertificateToSignatureElement(certificate, signatureElement);
        }

        private XmlElement GetBusinessElement()
        {
            var nodeToSign = Document.SelectSingleNode("//bc:paymentMessage | //bc:corporateMessage | //bc:certificateRequestMessage", Manager);
            var elementToSign = nodeToSign as XmlElement;
            return elementToSign;
        }

        private void AddCertificateToSignatureElement(X509Certificate certificate, XmlElement signatureElement)
        {
            var keyInfo = Document.CreateElement("ds:KeyInfo", Manager.LookupNamespace("ds"));
            var x509Data = Document.CreateElement("ds:X509Data", Manager.LookupNamespace("ds"));
            var x509Certificate = Document.CreateElement("ds:X509Certificate", Manager.LookupNamespace("ds"));
            x509Certificate.InnerText = Convert.ToBase64String(certificate.GetRawCertData());

            x509Data.AppendChild(x509Certificate);
            keyInfo.AppendChild(x509Data);
            signatureElement.AppendChild(keyInfo);
        }
    }
}
