using System;
using System.IO;
using System.Text;
using SecuredClient.Provider;
using SecuredClient.Request;

namespace SecuredClient.Client
{
    public class RenewalClient : BaseClient
    {
        /// <summary>
        /// Construct the client
        /// </summary>
        /// <param name="serviceProvider">The service provider to use. Choose one of the available *ServiceProvider classes.</param>
        /// <param name="functionIdentifier">Functionidentifier - this id has been provided to you when ordering BankConnect.</param>
        /// <param name="keystoreFile">PKCS#12 file containing the customer-certificate and key to be renewed</param>
        /// <param name="keystorePassword">Password used to protect PKCS#12 file</param>
        public RenewalClient(ServiceProvider serviceProvider, string functionIdentifier, FileInfo keystoreFile, string keystorePassword) : base(keystoreFile, keystorePassword, serviceProvider, functionIdentifier)
        {
        }

        /// <summary>
        /// Will renew the current customer certificate, and issue a new one.
        /// </summary>
        /// <param name="newKeystoreFile">The new PKCS#12 file will be saved here</param>
        /// <param name="newKeystorePassword">Password to protect new PKCS#12 file</param>
        public void RenewCustomerCertificate(FileInfo newKeystoreFile, string newKeystorePassword)
        {
            if (newKeystoreFile.Exists)
            {
                throw new ArgumentException("Keystore file " + newKeystoreFile.FullName + " already exists!");
            }

            GetBankCertificateIfRequired();

            var request = new renewCustomerCertificate();
            var certificateRequestGenerator = new CertificateRequestGenerator(FunctionIdentifier);
            var pkcs10Bytes = certificateRequestGenerator.GetPkcs10Bytes();

            request.certificateRequestMessage = new certificateRequestMessage {certificateRequest = pkcs10Bytes};

            var client = CreateClient();
            var technicalAddress = new technicalAddress();
            var serviceHeader = BuildServiceHeader();

            var response = client.renewCustomerCertificate(ref technicalAddress, ref serviceHeader, request);

            var pemBlock = Encoding.UTF8.GetString(response.corporateMessage.content);
            var pkcs12 = ToPkcs12Bytes(pemBlock, certificateRequestGenerator, newKeystorePassword);
            File.WriteAllBytes(newKeystoreFile.FullName, pkcs12);
        }
    }
}
