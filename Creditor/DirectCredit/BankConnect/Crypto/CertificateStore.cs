using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Crypto
{
    public class CertificateStore
    {
        public static CertificateStore Instance { get; } = new CertificateStore();

        private CertificateStore()
        {
            TrustedCaCertificates = new List<X509Certificate>();
        }

        public X509Certificate2 ServiceCertificate { get; set; }
        public X509Certificate2 ClientCertificate { get; set; }
        public IList<X509Certificate> TrustedCaCertificates { get; private set; }

        public void AddTrustedCaCertificate(X509Certificate caCertificate)
        {
            if (!TrustedCaCertificates.Contains(caCertificate))
            {
                TrustedCaCertificates.Add(caCertificate);
            }
        }

        public RSACryptoServiceProvider PrivateKey
        {
            get
            {
                //Force use of Enhanced CSP since Basic does not support SHA256
                var privateKey = ClientCertificate.PrivateKey as RSACryptoServiceProvider;
                var newKey = new RSACryptoServiceProvider();
                var xmlString = privateKey.ToXmlString(true);
                newKey.FromXmlString(xmlString);
                return newKey;
            }
        }

    }
}