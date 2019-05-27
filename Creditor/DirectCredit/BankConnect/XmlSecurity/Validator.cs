using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using SecuredClient.Crypto;

namespace SecuredClient.XmlSecurity
{
    public class Validator
    {
        private const string ExcC14NCanonicalization = "http://www.w3.org/2001/10/xml-exc-c14n#";

        private readonly XmlDocument document;
        private readonly Dictionary<string, X509Certificate> certificatesByThumbprint = new Dictionary<string, X509Certificate>();
        private readonly ISet<SignatureType> signatureTypes = new HashSet<SignatureType>();
        private readonly NamespaceManager manager;

        public X509Certificate SigningCertificate
        {
            get
            {
                if (certificatesByThumbprint.Count > 1)
                {
                    throw new SignatureException("At least two different certificates was used for signing, the same certificate must be used for all signatures.");
                }
                return certificatesByThumbprint.Values.FirstOrDefault();
            }
        }

        public bool RemoveValidSignatureElements { get; set; }

        public Validator(XmlDocument serviceRequest)
        {
            CryptoConfig.AddAlgorithm(typeof(RsaSha256SignatureDescription), @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");

            document = serviceRequest;
            manager = new NamespaceManager(document);
            RemoveValidSignatureElements = false;
        }

        public X509Certificate Validate()
        {
            var signatureElements = document.GetElementsByTagName("Signature", NamespaceManager.DsNamespaceUri);
            AssertOnlyLegalTransformsUsed();

            var toBeRemoved = new List<XmlElement>();
            foreach (XmlElement signatureElement in signatureElements)
            {
                VerifySignature(signatureElement);
                toBeRemoved.Add(signatureElement);
            }

            foreach (var element in toBeRemoved)
            {
                RemoveElementIfRequired(element);
            }
            return SigningCertificate;
        }

        public void AssertOnlyLegalTransformsUsed()
        {
            var transformNodes = document.SelectNodes("//ds:Transform", manager);
            if (transformNodes == null) return;
            foreach (XmlElement transformNode in transformNodes)
            {
                var algorithmAttribute = transformNode.Attributes["Algorithm"];
                if (algorithmAttribute == null)
                {
                    throw new SignatureException("No Algorithm attribute on transform element");
                }
                if (!algorithmAttribute.Value.Equals(ExcC14NCanonicalization))
                {
                    throw new SignatureException("Illegal transform found in reference, only " + ExcC14NCanonicalization + " allowed");
                }
            }
        }

        private void RemoveElementIfRequired(XmlNode signatureElement)
        {
            if (!RemoveValidSignatureElements) return;
            signatureElement.ParentNode?.RemoveChild(signatureElement);
        }

        private void VerifySignature(XmlElement signatureElement)
        {
            var signature = new Signature(signatureElement);
            signature.Validate();
            SaveCertificate(signature.Signer);
            signatureTypes.Add(signature.SignatureType);
        }

        private void SaveCertificate(X509Certificate2 certificate)
        {
            if (!certificatesByThumbprint.ContainsValue(certificate))
            {
                certificatesByThumbprint.Add(certificate.Thumbprint, certificate);
            }
        }

        public void AssertFound(SignatureType type)
        {
            if (!signatureTypes.Contains(type))
            {
                throw new SignatureException("Found no signature of type " + type + " in message");
            }
        }
    }
}
