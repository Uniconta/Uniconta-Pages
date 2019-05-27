using System.Security.Cryptography.X509Certificates;
using System.Xml;
using SecuredClient.Crypto;
using SecuredClient.XmlSecurity;

namespace SecuredClient.Client
{
    public class SecurityServiceImpl : SecurityService
    {
        public void WrapRequest(XmlDocument request, OperationPolicy policy)
        {
            if (policy.BusinessSignatureRequired)
            {
                var signer = new Signer(request);
                signer.BusinessSign();
            }

            if (policy.RequestEncryptionRequired)
            {
                var encryptor = new Encryptor(request, CertificateStore.Instance.ServiceCertificate);
                encryptor.EncryptBody();
            }

            if (policy.WsSignatureRequired)
            {
                var signer = new Signer(request);
                signer.WsSecuritySign();
            }
        }

        public void UnwrapResponse(XmlDocument response, OperationPolicy policy)
        {
            if (policy.WsSignatureRequired)
            {
                 ValidateWsSignature(response);
            }

            if (policy.ResponseEncryptionRequired)
            {
                var decryptor = new Decryptor(response) {ValidateEncryptionCertificate = true};
                decryptor.Decrypt();
            }

            //We don't check the policy here because all responses require business signatures
            ValidateBusinessSignature(response);

            RemoveSecurityHeaderIfPresent(response);
        }

        private static void RemoveSecurityHeaderIfPresent(XmlDocument response)
        {
            var security = response.SelectSingleNode("//wsse:Security", new NamespaceManager(response));
            security?.ParentNode.RemoveChild(security);
        }

        private static void ValidateBusinessSignature(XmlDocument serviceRequest)
        {
            var validator = new Validator(serviceRequest);
            var signingCertificate = validator.Validate();
            validator.AssertFound(XmlSecurity.SignatureType.BusinessSignature);
            ValidateSigner(signingCertificate);
        }

        private static void ValidateWsSignature(XmlDocument response)
        {
            var validator = new Validator(response) {RemoveValidSignatureElements = true};
            var signingCertificate = validator.Validate();
            validator.AssertFound(XmlSecurity.SignatureType.WsSecuritySignature);
            ValidateSigner(signingCertificate);
        }

        private static void ValidateSigner(X509Certificate signingCertificate)
        {
            new CertificateValidator(CertificateStore.Instance.TrustedCaCertificates).AssertValid(signingCertificate);
        }
    }
}