using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using SecuredClient.Crypto;

namespace SecuredClient.XmlSecurity
{
    public class Decryptor
    {
        private readonly XmlDocument document;
        private readonly NamespaceManager manager;
        public bool ValidateEncryptionCertificate { get; set; }

        public Decryptor(XmlDocument document)
        {
            this.document = document;
            manager = new NamespaceManager(document);
            ValidateEncryptionCertificate = true;
        }

        public void Decrypt()
        {
            var encryptedKey = GetEncryptedKey();
            var symmetricKey = DecryptKey(encryptedKey);
            DecryptData(symmetricKey);
        }

        private EncryptedKey GetEncryptedKey()
        {
            var encryptedKeyElement = document.SelectSingleNode("//xenc:EncryptedKey", manager) as XmlElement;
            if (encryptedKeyElement == null)
            {
                throw new SecurityException("No encrypted element key found");
            }
            var encryptedKey = new EncryptedKey();
            encryptedKey.LoadXml(encryptedKeyElement);
            var useOaep = (encryptedKey.EncryptionMethod != null && encryptedKey.EncryptionMethod.KeyAlgorithm == EncryptedXml.XmlEncRSAOAEPUrl);
            if (!useOaep)
            {
                throw new SecurityException("Illegal key encryption algorithm used");
            }
            if (encryptedKey.ReferenceList.Count != 1)
            {
                throw new SecurityException("Expected exactly one DataReference in EncryptedKey element, found " + encryptedKey.ReferenceList.Count);
            }
            return encryptedKey;
        }

        private byte[] DecryptKey(EncryptedType encryptedKey)
        {
            var cipherText = encryptedKey.CipherData.CipherValue;
            var symmetricKey = DecryptRsaOaep(cipherText);

            if (symmetricKey.Length != 32)
            {
                throw new SecurityException("Unexcepted length of decrypted symmetric key: " + symmetricKey.Length);
            }
            return symmetricKey;
        }

        private void DecryptData(byte[] symmetricKey)
        {
            using (Aes cipher = new AesCryptoServiceProvider())
            {
                var dataElement = (XmlElement)(document.GetElementsByTagName("EncryptedData", manager.LookupNamespace("xenc"))[0]);
                var encryptedData = new EncryptedData();
                encryptedData.LoadXml(dataElement);
                if (encryptedData.EncryptionMethod == null || !encryptedData.EncryptionMethod.KeyAlgorithm.Equals(EncryptedXml.XmlEncAES256Url))
                {
                    throw new SecurityException("Illegal symmetric encryption algorithm used");
                }

                cipher.Key = symmetricKey;
                var encryptedXml = new EncryptedXml();
                var decryptedData = encryptedXml.DecryptData(encryptedData, cipher);
                encryptedXml.ReplaceData(dataElement, decryptedData);
            }
        }

        public byte[] DecryptRsaOaep(byte[] cipherText)
        {
            return CertificateStore.Instance.PrivateKey.Decrypt(cipherText, true);
        }

    }
}
