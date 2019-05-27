using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using Org.BouncyCastle.Security;
using SecuredClient.Crypto;
using SecuredClient.Provider;
using SecuredClient.Request;

namespace SecuredClient.Client
{
    public abstract class BaseClient
    {
        protected FileInfo KeystoreFile;
        protected ServiceProvider ServiceProvider;
        protected string FunctionIdentifier;
        protected string KeystorePassword;
        protected readonly SecureRandom SecureRandom = new SecureRandom();

        protected BaseClient(FileInfo keystoreFile, string keystorePassword, ServiceProvider provider, string functionIdentifier)
        {
            KeystoreFile = keystoreFile;
            KeystorePassword = keystorePassword;
            ServiceProvider = provider;
            FunctionIdentifier = functionIdentifier;
            CertificateStore.Instance.AddTrustedCaCertificate(ServiceProvider.RootCaCertificate);
            if (keystoreFile.Exists)
            {
                CertificateStore.Instance.ClientCertificate = new X509Certificate2(KeystoreFile.FullName, KeystorePassword, X509KeyStorageFlags.Exportable);
            }
        }

        protected SecuredCorporateServiceClient CreateClient()
        {
            var clientFactory = new SecuredCorporateServiceClientFactory();
            return clientFactory.CreateClient(ServiceProvider.Endpoint);
        }

        public void GetBankCertificateIfRequired()
        {
            if (CertificateStore.Instance.ServiceCertificate != null) return;

            var client = new SecuredCorporateServiceClient(new EndpointAddress(ServiceProvider.Endpoint));
            var technicalAddress = new technicalAddress();
            var activationHeader = BuildActivationHeader();
            var request = new getBankCertificate();

            var response = client.getBankCertificate(ref technicalAddress, ref activationHeader, request);

            var chain = ReadCertificateChain(response);
            new CertificateChainValidator(CertificateStore.Instance.TrustedCaCertificates).AssertValid(chain);
            CertificateStore.Instance.ServiceCertificate = chain[0];
        }

        private static IList<X509Certificate2> ReadCertificateChain(getBankCertificateResponse response)
        {
            var block = Encoding.UTF8.GetString(response.corporateMessage.content);
            var reader = new PemBlockReader(block);

            return reader.ReadCertificates();
        }

        protected ActivationHeader BuildActivationHeader()
        {
            var activationHeader = new ActivationHeader
            {
                createDateTime = DateTime.Now,
                endToEndMessageId = CreateMessageId(),
                functionIdentification = FunctionIdentifier,
                erpInformation = CreateErpInformation(),
                organisationIdentification = GetOrganisationIdentification()
            };

            return activationHeader;
        }

        private string CreateMessageId()
        {
            return Math.Abs(SecureRandom.NextLong()).ToString();
        }

        private static erpInformation CreateErpInformation()
        {
            return new erpInformation
            {
                erpsystem = "Client",
                erpversion = "1.0"
            };
        }

        private organisationIdentification GetOrganisationIdentification()
        {
            var organisationIdentification = new organisationIdentification
            {
                isoCountryCode = "DK",
                mainRegistrationNumber = ServiceProvider.RegistrationNumber
            };
            return organisationIdentification;
        }


        protected ServiceHeader BuildServiceHeader()
        {
            var serviceHeader = new ServiceHeader
            {
                functionIdentification = FunctionIdentifier,
                organisationIdentification = GetOrganisationIdentification(),
                createDateTime = DateTime.Now,
                endToEndMessageId = CreateMessageId(),
                erpInformation = CreateErpInformation(),
                format = "xml"
            };
            return serviceHeader;
        }

        protected static byte[] ToPkcs12Bytes(string pemBlock, CertificateRequestGenerator certificateRequestGenerator, string keystorePassword)
        {
            var reader = new PemBlockReader(pemBlock);
            var certificates = reader.ReadCertificates();
            new CertificateChainValidator(CertificateStore.Instance.TrustedCaCertificates).AssertValid(certificates);
            var pkcs12 = certificateRequestGenerator.CreatePkcs12(certificates[certificates.Count - 1], keystorePassword);
            return pkcs12;
        }
    }
}
