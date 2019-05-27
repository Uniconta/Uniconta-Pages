using System;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using SecuredClient.XmlSecurity;
using BcCert = Org.BouncyCastle.X509.X509Certificate;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace SecuredClient.Crypto
{
    public class CertificateValidator
    {
        private readonly Dictionary<string, BcCert> trustedRootsByDn = new Dictionary<string, BcCert>();

        public Func<long> NowFunction { get; set; }

        private CertificateValidator()
        {
            NowFunction = GetNowAsTicks;
        }

        public CertificateValidator(X509Certificate trustedRoot) : this()
        {
            if (trustedRoot == null)
            {
                throw new CertificateValidationException("Trusted root certificate is null");
            }
            AddTrustedRoot(trustedRoot);
        }

        public CertificateValidator(IEnumerable<X509Certificate> trustedCertificates) : this()
        {
            if (trustedCertificates == null)
            {
                throw new CertificateValidationException("Trusted root certificates is null");
            }
            foreach (var trustedCertificate in trustedCertificates)
            {
                AddTrustedRoot(trustedCertificate);
            }
        }

        public void AddTrustedRoot(X509Certificate trustedRoot)
        {
            var bcCertificate = ToBcCertificate(trustedRoot);
            var canonicalize = Canonicalize(bcCertificate.SubjectDN);

            if (!trustedRootsByDn.ContainsKey(canonicalize))
            {
                trustedRootsByDn.Add(canonicalize, bcCertificate);
            }

        }

        private static string Canonicalize(X509Name name)
        {
            return name.ToString(false, (IDictionary) X509Name.RFC2253Symbols);
        }

        private static BcCert ToBcCertificate(X509Certificate certificate)
        {
            return new X509CertificateParser().ReadCertificate(certificate.GetRawCertData());
        }

        public void AssertValid(X509Certificate subject)
        {
            var bcSubject = ToBcCertificate(subject);
            AssertNotAfter(bcSubject);
            AssertNotBefore(bcSubject);
            VerifySignature(bcSubject);
        }

        private void AssertNotAfter(BcCert bcSubject)
        {
            if (NowFunction.Invoke() > bcSubject.NotAfter.Ticks)
            {
                throw new CertificateValidationException(GetMessagePrefix(bcSubject) + "  expired at " + bcSubject.NotAfter);
            }
        }

        private static string GetMessagePrefix(BcCert bcSubject)
        {
            return "Certificate issued to " + bcSubject.SubjectDN;
        }

        private void AssertNotBefore(BcCert bcSubject)
        {
            if (NowFunction.Invoke() < bcSubject.NotBefore.Ticks)
            {
                throw new CertificateValidationException(GetMessagePrefix(bcSubject) + " is not valid until " + bcSubject.NotBefore);
            }
        }

        private void VerifySignature(BcCert bcSubject)
        {
            var canonicalIssuerName = Canonicalize(bcSubject.IssuerDN);

            if (!trustedRootsByDn.ContainsKey(canonicalIssuerName))
            {
                throw new CertificateValidationException(GetMessagePrefix(bcSubject) + " is not trusted, no trusted certificate registered named " + bcSubject.IssuerDN);
            }

            var trustedRoot = trustedRootsByDn[canonicalIssuerName];
            try
            {
                bcSubject.Verify(trustedRoot.GetPublicKey());
            }
            catch (Exception exception)
            {
                throw new CertificateValidationException(GetMessagePrefix(bcSubject) + ", could not be verified, is certificate trusted?", exception);
            }
        }

        private static long GetNowAsTicks()
        {
            var now = DateTime.Now.Ticks;
            return now;
        }
    }
}