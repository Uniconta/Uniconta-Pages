using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using SecuredClient.XmlSecurity;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace SecuredClient.Crypto
{
    public class CertificateChainValidator
    {
        private readonly CertificateValidator certificateValidator;

        public CertificateChainValidator(IEnumerable<X509Certificate> trustedCaCertificates)
        {
            certificateValidator = new CertificateValidator(trustedCaCertificates);
        }

        public void AssertValid(IList<X509Certificate2> list)
        {
            if (list.Count == 1)
            {
                VerifySingleCertificate(list);
            }
            else if (list.Count == 2)
            {
                VerifyChain(list);
            }
            else
            {
                throw new CertificateValidationException("Unexpected length of certificate chain: " + list.Count);
            }
        }

        private void VerifySingleCertificate(IList<X509Certificate2> list)
        {
            //Certificate is issued under the root, verify that that is the case:
            certificateValidator.AssertValid(list[0]);
        }

        private void VerifyChain(IList<X509Certificate2> list)
        {
            //Certificate is issued under intermediate CA. 
            //The convention in BankConnect is, that intermediate certificates should be first in the chain
            var intermediate = list[0];
            var subject = list[1];
            //Verify, that intermediate CA is issued under root:
            certificateValidator.AssertValid(intermediate);
            CertificateStore.Instance.AddTrustedCaCertificate(intermediate);
            certificateValidator.AddTrustedRoot(intermediate);
            //... and that Bank certificate is issued under either root or intermediate:
            certificateValidator.AssertValid(subject);
        }
    }
}