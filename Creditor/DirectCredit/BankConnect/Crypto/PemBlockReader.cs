using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.OpenSsl;

namespace SecuredClient.Crypto
{
    public class PemBlockReader
    {
        private readonly string pemBlock;

        public PemBlockReader(string pemBlock)
        {
            this.pemBlock = pemBlock;
        }

        public IList<X509Certificate2> ReadCertificates()
        {
            var stringReader = new StringReader(pemBlock);

            var pemLines = new StringBuilder();
            string line;
            var certificates = new List<X509Certificate2>();
            while ((line = stringReader.ReadLine()) != null)
            {
                pemLines.Append(line).Append("\n");
                if (line != "-----END CERTIFICATE-----") continue;
                var pemCertificate = pemLines.ToString();
                var certReader = new StringReader(pemCertificate);
                var pemReader = new PemReader(certReader);
                var readCert = pemReader.ReadObject() as Org.BouncyCastle.X509.X509Certificate;
                certificates.Add(new X509Certificate2(readCert.GetEncoded()));
                pemLines.Clear();
            }

            return certificates;
        }
    }
}
