using System;
using System.IO;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Uniconta.Common;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace SecuredClient.Request
{
    public class CertificateRequestGenerator
    {
        private readonly string functionIdentifier;
        private readonly string activationCode;
        private RsaKeyPairGenerator keyPairGenerator;
        private AsymmetricCipherKeyPair asymmetricCipherKeyPair;
        private static readonly SecureRandom SecureRandom = new SecureRandom();

        public CertificateRequestGenerator(string functionIdentifier, string activationCode)
        {
            if (functionIdentifier == null)
            {
                throw new ArgumentNullException(nameof(functionIdentifier));
            }
            this.functionIdentifier = functionIdentifier;
            this.activationCode = activationCode;
        }

        public CertificateRequestGenerator(string functionIdentifier) : this(functionIdentifier, null)
        {
        }

        public byte[] GetPkcs10Bytes()
        {
            keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(SecureRandom, 2048));
            asymmetricCipherKeyPair = keyPairGenerator.GenerateKeyPair();
            var request = new Pkcs10CertificationRequest("SHA256WithRSA", GetSubject(), asymmetricCipherKeyPair.Public, new DerSet(), asymmetricCipherKeyPair.Private);
            return request.GetEncoded();
        }

        private X509Name GetSubject()
        {
            if (activationCode != null)
            {
                return new X509Name("CN=" + functionIdentifier + "@" + activationCode);
            }
            return new X509Name("CN=" + functionIdentifier);
        }

        public byte[] CreatePkcs12(X509Certificate certificate, string password)
        {
            var store = new Pkcs12Store();
            var entry = new X509CertificateEntry(new X509CertificateParser().ReadCertificate(certificate.GetRawCertData()));
            store.SetKeyEntry("erp", new AsymmetricKeyEntry(asymmetricCipherKeyPair.Private), new[] {entry});
            var memoryStream = UnistreamReuse.Create();
            store.Save(memoryStream, password.ToCharArray(), SecureRandom);
            return memoryStream.ToArrayAndRelease();
        }
    }
}