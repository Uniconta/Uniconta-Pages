using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Crypto
{
    public interface CryptoProvider
    {
        CertificateAndSignature SignRsaSha256(byte[] digest);

        byte[] DecryptRsaOaep(byte[] cipherText);

        IList<X509Certificate> TrustedCaCertificates { get; }
    }
}
