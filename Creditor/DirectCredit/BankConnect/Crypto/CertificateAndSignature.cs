using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Crypto
{
    public class CertificateAndSignature
    {
        public X509Certificate Certificate { get; set; }
        public byte[] Signature { get; set; }
    }
}
