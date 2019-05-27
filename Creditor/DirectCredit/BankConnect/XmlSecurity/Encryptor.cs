using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SecuredClient.XmlSecurity
{
    public class Encryptor : SecurityProvider
    {
        private readonly X509Certificate certificate;

        public Encryptor(XmlDocument document, X509Certificate certificate) : base(document)
        {
            this.certificate = certificate;
        }

        public void EncryptBody()
        {
            var toEncrypt = GetFirstChildInBody();
            EncryptElement(toEncrypt);
            SetDsPrefix();
            var xml = Document.OuterXml; //Serialize and deserialize to render ns prefixes correctly
            Document.LoadXml(xml);
        }

        private void EncryptElement(XmlElement toEncrypt)
        {
            CreateSoapHeaderIfMissing();
            var securityElement = GetOrCreateSecurityElement();
            var tokenId = CreateIdWithPrefix("X509");
            CreateBinarySecurityToken(securityElement, certificate, tokenId);

            var aes = new AesManaged();
            var dataId = CreateIdWithPrefix("ED");
            var keyId = CreateIdWithPrefix("EK");
            CreateAndEmbedEncryptedKey(aes, tokenId, securityElement, dataId, keyId);

            CreateAndEmbedEncryptedData(dataId, keyId, aes, toEncrypt);

            SetXencPrefix();
        }

        private void CreateAndEmbedEncryptedKey(SymmetricAlgorithm symmetricAlgorithm, string tokenId, XmlElement securityElement, string dataId, string keyId)
        {
            var encryptedKey = CreatedEncryptedKey(symmetricAlgorithm, dataId, keyId);
            var keyInfoElement = CreateSecurityTokenReference(tokenId, null);
            var keyInfo = new KeyInfo();
            keyInfo.LoadXml(keyInfoElement);
            encryptedKey.KeyInfo = keyInfo;
            var encryptedKeyElement = Document.ImportNode(encryptedKey.GetXml(), true);
            securityElement.AppendChild(encryptedKeyElement);
        }

        private EncryptedKey CreatedEncryptedKey(SymmetricAlgorithm aes, string dataReferenceId, string keyId)
        {
            var encryptedKey = new EncryptedKey();
            encryptedKey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSAOAEPUrl);
            encryptedKey.Id = keyId;
            var cert = new X509Certificate2(certificate);
            encryptedKey.CipherData.CipherValue = EncryptedXml.EncryptKey(aes.Key, cert.PublicKey.Key as RSA, true);
            var dataReference = new DataReference("#" + dataReferenceId);
            encryptedKey.AddReference(dataReference);
            return encryptedKey;
        }

        private void CreateAndEmbedEncryptedData(string dataId, string keyId, AesManaged aes, XmlElement toEncrypt)
        {
            var encryptedData = CreateEncryptedData(dataId, keyId);
            EncryptData(aes, toEncrypt, encryptedData);
        }

        private EncryptedData CreateEncryptedData(string dataId, string keyId)
        {
            var encryptedData = new EncryptedData();
            encryptedData.Type = EncryptedXml.XmlEncElementContentUrl;
            encryptedData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encryptedData.Id = dataId;
            var tokenReference = Document.CreateElement("wsse:SecurityTokenReference", Manager.LookupNamespace("wsse"));
            tokenReference.SetAttribute("TokenType", Manager.LookupNamespace("wsse11"), "http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey");
            var reference = Document.CreateElement("wsse:Reference", Manager.LookupNamespace("wsse"));
            reference.SetAttribute("URI", "#" + keyId);
            tokenReference.AppendChild(reference);
            var infoNode = new KeyInfoNode(tokenReference);
            encryptedData.KeyInfo.AddClause(infoNode);
            return encryptedData;
        }

        private static void EncryptData(SymmetricAlgorithm symmetricAlgorithm, XmlElement toEncrypt, EncryptedData encryptedData)
        {
            var encryptedXml = new EncryptedXml();
            encryptedData.CipherData.CipherValue = encryptedXml.EncryptData(toEncrypt, symmetricAlgorithm, false);
            EncryptedXml.ReplaceElement(toEncrypt, encryptedData, false);
        }

        private void SetXencPrefix()
        {
            foreach (XmlNode node in Document.SelectNodes("descendant-or-self::*[namespace-uri()='http://www.w3.org/2001/04/xmlenc#']"))
            {
                node.Prefix = "xenc";
            }
        }

    }
}