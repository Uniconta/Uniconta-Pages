using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.DirectCredit.BankConnect
{
    public class BankConnectHelper
    {
        //public void TransferPayment()
    }

    public class ServiceProvider
    {
        public X509Certificate2 RootCaCertificate { get; }
        public Uri Endpoint { get; }
        public string RegistrationNumber { get; }

        public ServiceProvider()
        {
            RootCaCertificate = FromPemBlock("MIIFpzCCA4gAwIBAgIBADANBgkqhkiG9w0BAQsFADB1MScwJQYDVQQDEx5CRCBC" +
               "YW5rQ29ubmVjdCBQcmltYXJ5IENBIFRlc3QxHDAaBgNVBAcTE0Vycml0c29lIEZy" +
               "ZWRlcmljaWExDDAKBgNVBAsTA1ZCRjERMA8GA1UEChMIQmFua2RhdGExCzAJBgNV" +
               "BAYTAkRLMB4XDTE1MDMyNDIzMDAwMFoXDTQ1MDMyNDIzMDAwMFowdTEnMCUGA1UE" +
               "AxMeQkQgQmFua0Nvbm5lY3QgUHJpbWFyeSBDQSBUZXN0MRwwGgYDVQQHExNFcnJp" +
               "dHNvZSBGcmVkZXJpY2lhMQwwCgYDVQQLEwNWQkYxETAPBgNVBAoTCEJhbmtkYXRh" +
               "MQswCQYDVQQGEwJESzCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAM/B" +
               "flQlmp41ZJ0jyow2oDfoysnRfrKV+s+N/ff38PLHmlR+2FDgs0C/uZxKMLtL/LjW" +
               "sgCweXnq4a2LNpkqWEKru8dO3A+dbeN91gNxE+FSJ5/wPsoVSGOBFF6wB22O3jZ0" +
               "qhgV0AX+PxIO6rLjAn1e0xfcuUlzQbvKboqta013zzm3oYstIyXGj/yEloiHD2oZ" +
               "kZIROD3NwMZY3O3K8603ICIUM2e2t2uXzAndYhysr9DYeOelT7RfaM2kWtn+doe1" +
               "DlAB0qYebVBe5RzZV2v6Fi+5n99kkq5XAYNpfnuW40AW4IYwolK2uudbgHsu0dCa" +
               "HnnGZSwFkY1f1WwNyOKZwJZKso8vzh/nIS2RXawzyP17GsY5WCmvZJbL0e8EgzYF" +
               "qtOqoAJm2vcjEMdz5o7VWMv2Vtm5oo6RFcCgwqmXJlBHfPdHI9KcmEbsibYET0mx" +
               "o52hRaUhH1lilxIb5YMFS9W2eXNKqLbHvgbs0gfcAKQw4J/1ynR1sm91opG6+orW" +
               "HxKZskfoSLq+d8RMRqccIQOvex6wguNXf3OvIBFGzd4TYEduWpVm4nPZvUiWfK1x" +
               "ZvaRDAZ3A5CsiFhANjGd4cpXfA6wMrz1VO+Hm8+UBvwtPrMN0bjSQpt+8ZHO+Xrg" +
               "I3S4v7Ora2b/aT6PHTPH+Q5GsLu8UqD7+Efw80kzAgMBAAGjQjBAMB0GA1UdDgQW" +
               "BBSENhY7Jk/umFo7x/q0+3xTDwy2iTAOBgNVHQ8BAf8EBAMCAgQwDwYDVR0TAQH/" +
               "BAUwAwEB/zANBgkqhkiG9w0BAQsFAAOCAgEAju+XDJOOiUiDv298mub12Z7Ckrwb" +
               "kwfYjJwiQoswjFBr7BeH7UzmdtCMTFI5dDXQTl1YGbTtZGYKEmb2bQkcF5E1qYHp" +
               "ekC6d0owxTYpt8cZA9kFl21ZA+lpDNadIZ2xdnDeLsaU/G/QJP/nj2Y62xGfRHJw" +
               "dVn5aLqEUFPaUXM6i3MQH+PLU5IcholPJ6uXkTSyP37dQS00Etpr4lQiW3njCwxR" +
               "2ovCsRYFAxOY0BKhOzYh4h9Bcfk6poA7ayci1NDQkTlF2vHfLUeDBC1cJJ3aMwYy" +
               "B1ZxKxHNj7+vQvuG2o5DJVNzfzzdNUCiqw5m4D3PawB4JXYlqWw2LbghfdgVav3A" +
               "SA/1YtCtooP2H9/SlZF0dBKAjy55f58NGl6I/Ci0018tfpB+tsIwFLJA/pgrB90b" +
               "ujnfiIY2nXjRTsCxXP7dBfMQ32acc63Z6KVCCCuNXm2RcY3D/b59d85TzMIPUABC" +
               "JFS3ymqpQKYTVyqAA7XpvcUjLmIge8zQbHpj4FNWQ2u+isGQGOB1WTPbS4v86+rH" +
               "ovYf+bEHmE6KPNSKGHJCy6se6TG2LOhlmxHdVDo9/jg4L2JiGkpXRyqkUv6l1Ulq" +
               "wgYcNokZsatkkMmIKAIJ9VejdQgPUhS804xbTGbKpHL1nDxorDKezZm72sn99Ubj" +
               "bItiqY7KErr9qqc=");
            RegistrationNumber= "8079"; //Regnr for you bank. 8079 is Sydbank
        }

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
