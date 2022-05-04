using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Uniconta.Common.Utility;

namespace SecuredClient.Provider
{
    public abstract class ServiceProvider
    {
        public abstract X509Certificate2 RootCaCertificate { get; }
        public abstract Uri Endpoint { get; }
        public abstract string RegistrationNumber { get; }

        protected X509Certificate2 FromPemBlock(string pem)
        {
            var builder = StringBuilderReuse.Create();
            builder.Append(pem);
            builder.Remove('\n');
            builder.Remove('\r');
            builder.Remove("-----BEGIN CERTIFICATE-----");
            builder.Remove("-----END CERTIFICATE-----");

            return new X509Certificate2(Convert.FromBase64String(builder.ToStringAndRelease()));
        }
    }
}
