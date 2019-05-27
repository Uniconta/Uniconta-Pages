using System;

namespace SecuredClient.XmlSecurity
{
    public class OperationPolicy
    {
        public bool RequestEncryptionRequired { get; private set; }
        public bool ResponseEncryptionRequired { get; private set; }
        public bool WsSignatureRequired { get; private set; }
        public bool BusinessSignatureRequired { get; private set; }
        public bool Pkcs10RequestRequired { get; private set; }
        public string SoapAction { get; private set; }

        private OperationPolicy()
        {
            RequestEncryptionRequired = false;
            ResponseEncryptionRequired = false;
            Pkcs10RequestRequired = false;
            WsSignatureRequired = false;
            BusinessSignatureRequired = false;
            SoapAction = null;
        }

        public static OperationPolicy GetPolicyForAction(string fullAction)
        {
            if (fullAction == null)
            {
                throw new ArgumentException("soapAction is null - is SOAPAction header set in request?");
            }
            var action = fullAction.Replace("\"", "").Trim();

            var policy = new OperationPolicy { SoapAction = action };

            switch (action)
            {
                case "urn:CorporateService:activateServiceAgreement":
                    policy.RequestEncryptionRequired = true;
                    policy.Pkcs10RequestRequired = true;
                    return policy;
                case "urn:CorporateService:getBankCertificate":
                    return policy;
                case "urn:CorporateService:getCustomerStatement":
                case "urn:CorporateService:getAlternate":
                case "urn:CorporateService:getDebitCreditNotification":
                case "urn:CorporateService:getStatus":
                case "urn:CorporateService:getCustomerAccountReport":
                    policy.WsSignatureRequired = true;
                    policy.ResponseEncryptionRequired = true;
                    return policy;
                case "urn:CorporateService:renewCustomerCertificate":
                    policy.RequestEncryptionRequired = true;
                    policy.WsSignatureRequired = true;
                    policy.BusinessSignatureRequired = true;
                    policy.Pkcs10RequestRequired = true;
                    policy.ResponseEncryptionRequired = true;
                    return policy;
                case "urn:CorporateService:transferPayment":
                    policy.RequestEncryptionRequired = true;
                    policy.WsSignatureRequired = true;
                    policy.BusinessSignatureRequired = true;
                    policy.ResponseEncryptionRequired = true;
                    return policy;

                default:
                    throw new SecurityException("Failed to determine policy for action " + fullAction);
            }

        }
    }
}