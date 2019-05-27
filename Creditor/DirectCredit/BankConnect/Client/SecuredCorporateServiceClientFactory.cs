using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using SecuredClient.Crypto;
using SecuredClient.MessageSecurity;

namespace SecuredClient.Client
{
    public class SecuredCorporateServiceClientFactory
    {
        public SecuredCorporateServiceClientFactory()
        {
            CryptoConfig.AddAlgorithm(typeof (RsaSha256SignatureDescription), RsaSha256SignatureDescription.Name);
        }

        public SecuredCorporateServiceClient CreateClient(Uri endpoint)
        {
            var ea = CreateEndpointAddress(endpoint);
            var securedCorporateServiceClient = new SecuredCorporateServiceClient(ea);
            securedCorporateServiceClient.Endpoint.Behaviors.Add(new SecurityEndpointBehaviour());
            return securedCorporateServiceClient;
        }

        private static EndpointAddress CreateEndpointAddress(Uri endpoint)
        {
            var serviceCertificate = CertificateStore.Instance.ServiceCertificate;
            if (serviceCertificate == null)
            {
                throw new ArgumentException("Cannot create client without service certificate - invoke GetBankCertificateIfRequired() to retrieve this.");
            }
            var commonName = serviceCertificate.GetNameInfo(X509NameType.SimpleName, false);
            if (commonName == null)
            {
                throw new ArgumentException("Cannot initialize client using service certificate without common name: " + serviceCertificate.SubjectName);
            }
            var endpointIdentity = new DnsEndpointIdentity(commonName); //CN in the Bank certificate. This will cause encryption to use this certificate.
            var ea = new EndpointAddress(endpoint, endpointIdentity, new AddressHeaderCollection());
            return ea;
        }
    }
}