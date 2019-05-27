using System;
using System.IO;
using System.Text;
using SecuredClient.Crypto;
using SecuredClient.Provider;
using SecuredClient.Request;

namespace SecuredClient.Client
{
    /// <summary>
    /// Class for doing the initial certificate issuance through ActivateServiceAgreement.
    /// </summary>
    public class ActivationClient : BaseClient
    {
        /// <summary>
        /// Construct the client
        /// </summary>
        /// <param name="serviceProvider">The service provider to use. Choose one of the available *ServiceProvider classes.</param>
        /// <param name="functionIdentifier">Functionidentifier - this id has been provided to you when ordering BankConnect.</param>
        /// <param name="keystoreFile">Where to save the issued PKCS#12 file (customer-certificate and key)</param>
        /// <param name="keystorePassword">Password used to protect PKCS#12 file</param>
        public ActivationClient(ServiceProvider serviceProvider, string functionIdentifier, FileInfo keystoreFile, string keystorePassword) : base(keystoreFile, keystorePassword, serviceProvider, functionIdentifier)
        {
        }

        public void ActivateServiceAgreement(string activationCode)
        {
            if (KeystoreFile.Exists)
            {
                throw new ArgumentException("Keystore file " + KeystoreFile.FullName + " already exists - you have already activated.");
            }

            GetBankCertificateIfRequired();

            var certificateRequestGenerator = new CertificateRequestGenerator(FunctionIdentifier, activationCode);
            var pkcs10Bytes = certificateRequestGenerator.GetPkcs10Bytes();

            var request = new activateServiceAgreement();
            var activationAgreement = new activationAgreement
            {
                activationCode = Encoding.UTF8.GetBytes(activationCode),
                certificateRequest = pkcs10Bytes
            };
            request.activationAgreement = activationAgreement;

            var technicalAddress = new technicalAddress();
            var activationHeader = BuildActivationHeader();

            var response = CreateClient().activateServiceAgreement(ref technicalAddress, ref activationHeader, request);

            var pemBlock = Encoding.UTF8.GetString(response.corporateMessage.content);

            ReceiveAndValidateCertificates(pemBlock, certificateRequestGenerator);
        }

        private void ReceiveAndValidateCertificates(string pemBlock, CertificateRequestGenerator certificateRequestGenerator)
        {
            var pkcs12 = ToPkcs12Bytes(pemBlock, certificateRequestGenerator, KeystorePassword);
            File.WriteAllBytes(KeystoreFile.FullName, pkcs12);
        }
    }
}
