using System;

namespace SecuredClient.XmlSecurity
{
    public class SecurityException : Exception 
    {
        public SecurityException(string message) : base(message)
        {
        }

        public SecurityException(string message, Exception exception) : base(message, exception)
        {
        }
    }

    public class CertificateValidationException : SecurityException
    {
        public CertificateValidationException(string message) : base(message)
        {
        }

        public CertificateValidationException(string message, Exception exception) : base(message, exception)
        {
        }
    }

    public class SignatureException : SecurityException
    {
        public SignatureException(string message) : base(message)
        {
        }

        public SignatureException(string message, Exception exception) : base(message, exception)
        {
        }
    }

}
