using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SecuredClient.Provider
{
    public abstract class ServiceProvider
    {
        public abstract X509Certificate2 RootCaCertificate { get; }
        public abstract Uri Endpoint { get; }
        public abstract string RegistrationNumber { get; }

        protected X509Certificate2 FromPemBlock(string pem)
        {
            var builder = new StringBuilder(pem);
            builder.Replace("\n", "");
            builder.Replace("\r", "");
            builder.Replace("-----BEGIN CERTIFICATE-----", "");
            builder.Replace("-----END CERTIFICATE-----", "");

            return new X509Certificate2(Convert.FromBase64String(builder.ToString()));
        }
    }
}
