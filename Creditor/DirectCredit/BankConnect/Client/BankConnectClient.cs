using System.IO;
using System.Text;
using SecuredClient.Provider;

namespace SecuredClient.Client
{
    public class BankConnectClient : BaseClient
    {

        /// <summary>
        /// Construct the client
        /// </summary>
        /// <param name="serviceProvider">The service provider to use. Choose one of the available *ServiceProvider classes.</param>
        /// <param name="functionIdentifier">Functionidentifier - this id has been provided to you when ordering BankConnect.</param>
        /// <param name="keystoreFile">PKCS#12 file containing customer-certificate and key (created by ActivationClient)</param>
        /// <param name="keystorePassword">Password used to protect PKCS#12 file</param>
        public BankConnectClient(ServiceProvider serviceProvider, string functionIdentifier, FileInfo keystoreFile, string keystorePassword) : base(keystoreFile, keystorePassword, serviceProvider, functionIdentifier)
        {
        }

        public paymentResponse TransferPayment(string xmlPayload)
        {
            GetBankCertificateIfRequired();

            var request = new transferPayment
            {
                paymentMessage = new paymentMessage
                {
                    format = "ISO20022",
                    mimeType = "text/xml",
                    compressed = "0",
                    content = Encoding.UTF8.GetBytes(xmlPayload)
                }
            };

            var technicalAddress = new technicalAddress();

            var serviceHeader = BuildServiceHeader();
            var response = CreateClient().transferPayments(ref technicalAddress, ref serviceHeader, request);
            return response.paymentResponse;
        }

        public getStatusResponse GetStatus()
        {
            GetBankCertificateIfRequired();

            var request = new getStatus();
            var technicalAddress = new technicalAddress();
            var serviceHeader = BuildServiceHeader();
            var client = CreateClient();
            return client.getStatus(ref technicalAddress, ref serviceHeader, request);
        }

        public getCustomerStatementResponse GetCustomerStatement()
        {
            GetBankCertificateIfRequired();

            var request = new getCustomerStatement();
            var technicalAddress = new technicalAddress();
            var serviceHeader = BuildServiceHeader();
            var client = CreateClient();
            return client.getCustomerStatement(ref technicalAddress, ref serviceHeader, request);
        }

        public getCustomerAccountReportResponse GetCustomerAccountReport()
        {
            GetBankCertificateIfRequired();

            var request = new getCustomerAccountReport();
            var technicalAddress = new technicalAddress();
            var serviceHeader = BuildServiceHeader();
            var client = CreateClient();
            return client.getCustomerAccountReport(ref technicalAddress, ref serviceHeader, request);
        }

        public getAlternateResponse GetAlternate()
        {
            GetBankCertificateIfRequired();

            var request = new getAlternate();
            var technicalAddress = new technicalAddress();
            var serviceHeader = BuildServiceHeader();
            var client = CreateClient();
            return client.getAlternate(ref technicalAddress, ref serviceHeader, request);
        }

        public getDebitCreditNotificationResponse GetDebitCreditNotification()
        {
            GetBankCertificateIfRequired();

            var request = new getDebitCreditNotification();
            var technicalAddress = new technicalAddress();
            var serviceHeader = BuildServiceHeader();
            var client = CreateClient();
            return client.getDebitCreditNotification(ref technicalAddress, ref serviceHeader, request);
        }

    }
}
