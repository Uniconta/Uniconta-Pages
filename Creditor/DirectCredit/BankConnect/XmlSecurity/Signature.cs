using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SecuredClient.XmlSecurity
{
    public enum SignatureType
    {
        WsSecuritySignature,
        BusinessSignature
    }

    public class Signature
    {
        private readonly XmlElement signatureElement;
        private readonly XmlDocument document;
        private readonly NamespaceManager manager;

        private readonly ISet<string> signedElementIds = new HashSet<string>();

        public Signature(XmlElement signatureElement)
        {
            this.signatureElement = signatureElement;
            document = signatureElement.OwnerDocument;
            manager = new NamespaceManager(document);

            DetermineType();
        }

        public SignatureType SignatureType { get; private set; }

        public X509Certificate2 Signer { get; private set; }

        private void DetermineType()
        {
            var certificateNode = signatureElement.SelectSingleNode("ds:KeyInfo/ds:X509Data/ds:X509Certificate", manager);
            if (certificateNode != null)
            {
                SignatureType = SignatureType.BusinessSignature;
                Signer = new X509Certificate2(Convert.FromBase64String(certificateNode.InnerText));
                return;
            }

            var keyIdentifierNode = signatureElement.SelectSingleNode("ds:KeyInfo/wsse:SecurityTokenReference/wsse:KeyIdentifier", manager);
            if (keyIdentifierNode != null)
            {
                if (keyIdentifierNode.Attributes?["ValueType"] == null)
                {
                    throw new SignatureException("wsse:KeyIdentifier has no ValueType attribute");
                }

                var valueType = keyIdentifierNode.Attributes["ValueType"].Value;
                if (valueType != "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3")
                {
                    throw new SignatureException("Unsupported wsse:KeyIdentifier value type: " + valueType);
                }
                Signer = new X509Certificate2(Convert.FromBase64String(keyIdentifierNode.InnerText));
                SignatureType = SignatureType.WsSecuritySignature;
                return;
            } 

            var referenceUri = GetSecurityTokenReferenceUri();
            Signer = GetCertificateByReference(referenceUri);
            SignatureType = SignatureType.WsSecuritySignature;
        }

        private X509Certificate2 GetCertificateByReference(string uri)
        {
            var bstElements = document.SelectNodes("//wsse:BinarySecurityToken[@wsu:Id='" + uri.Substring(1) + "']", manager);
            if (bstElements == null || bstElements.Count == 0)
            {
                throw new SignatureException("Failed to locate BinarySecurityToken element for URI " + uri);
            }
            var base64 = bstElements[0].InnerText;
            return new X509Certificate2(Convert.FromBase64String(base64));
        }

        private string GetSecurityTokenReferenceUri()
        {
            var referenceNodes = signatureElement.SelectNodes("ds:KeyInfo/wsse:SecurityTokenReference/wsse:Reference", manager);

            if (referenceNodes == null || referenceNodes.Count == 0)
            {
                throw new SignatureException("Signature has no SecurityTokenReference");
            }
            var attributes = referenceNodes[0].Attributes;
            if (attributes == null)
            {
                throw new SignatureException("SecurityTokenReference does not have an URI attribute");
            }
            var referenceUri = attributes["URI"].Value;
            return referenceUri;
        }

        public void Validate()
        {
            VerifyXmlSignature();
            VerifyReferences();
        }

        private void VerifyReferences()
        {
            CollectReferences();
            VerifyReferencesAreCorrect();
        }

        private void CollectReferences()
        {
            signedElementIds.Clear();
            var referenceNodes = signatureElement.SelectNodes("ds:SignedInfo/ds:Reference", manager);
            if (referenceNodes == null)
            {
                throw new SignatureException("No references found in signature: " + signatureElement.OuterXml);
            }
            foreach (XmlElement referenceNode in referenceNodes)
            {
                var uriAttribute = referenceNode.Attributes["URI"];
                if (uriAttribute == null)
                {
                    throw new SignatureException("No URI attribute on Reference element");
                }
                var value = uriAttribute.Value;
                if (!value.StartsWith("#"))
                {
                    throw new SignatureException("Illegal URI reference, expected ID type ref");
                }

                var digestMethodElement = referenceNode.SelectSingleNode("ds:DigestMethod", manager);
                if (digestMethodElement?.Attributes == null)
                {
                    throw new SignatureException("No DigestMethod element in Reference " + referenceNode.OuterXml);
                }

                var digestMethod = digestMethodElement.Attributes["Algorithm"].Value;
                if (!"http://www.w3.org/2001/04/xmlenc#sha256".Equals(digestMethod))
                {
                    throw new SignatureException("Illegal DigestMethod used in Reference: " + digestMethod);
                }

                signedElementIds.Add(value.Substring(1));
            }
        }

        private void VerifyReferencesAreCorrect()
        {
            if (SignatureType == SignatureType.WsSecuritySignature)
            {
                VerifyWsSecuritySignatureReferences();
            }

            if (SignatureType == SignatureType.BusinessSignature)
            {
                VerifyBusinessSignatureReferences();
            }
        }

        private void VerifyBusinessSignatureReferences()
        {
            var withId = new SignedXmlWithId(document);
            if (signedElementIds.Count != 1)
            {
                throw new SignatureException("Wrong number of references found for " + SignatureType + ", found " + signedElementIds.Count + " expected 1.");
            }

            var signedElement = withId.GetIdElement(document, signedElementIds.First());
            if (signedElement == null)
            {
                throw new SignatureException("Could not locate signed element");
            }

            if (signedElement.NamespaceURI != manager.LookupNamespace("bc"))
            {
                throw new SignatureException("Wrong ns of signed element");
            }
            var name = signedElement.LocalName;
            if (name != "paymentMessage" && name != "paymentResponse" && name != "corporateMessage" && name != "corporateException")
            {
                throw new SignatureException("Wrong name of signed element");
            }
        }

        private void VerifyWsSecuritySignatureReferences()
        {
            var withId = new SignedXmlWithId(document);
            if (signedElementIds.Count != 2)
            {
                throw new SignatureException("Wrong number of references found for " + SignatureType + ", found " + signedElementIds.Count + " expected 2.");
            }

            var serviceHeaderSigned = false;
            var bodySigned = false;
            foreach (var signedElementId in signedElementIds)
            {
                var signedElement = withId.GetIdElement(document, signedElementId);
                if (signedElement == null)
                {
                    throw new SignatureException("Could not locate signed element");
                }

                if (signedElement.LocalName == "serviceHeader" && signedElement.NamespaceURI == manager.LookupNamespace("bc"))
                {
                    serviceHeaderSigned = true;
                }
                if (signedElement.LocalName == "Body" && signedElement.NamespaceURI == manager.LookupNamespace("soap"))
                {
                    bodySigned = true;
                }
            }

            if (!serviceHeaderSigned)
            {
                throw new SignatureException("WS-Security signature does not sign serviceHeader");
            }

            if (!bodySigned)
            {
                throw new SignatureException("WS-Security signature does not sign body");
            }
        }

        private void VerifyXmlSignature()
        {
            var signedXml = new SignedXmlWithId(document);
            signedXml.LoadXml(signatureElement);

            if (!signedXml.CheckSignature(Signer, true))
            {
                throw new SignatureException("Signature is invalid: " + signatureElement.InnerXml);
            }
        }

    }
}