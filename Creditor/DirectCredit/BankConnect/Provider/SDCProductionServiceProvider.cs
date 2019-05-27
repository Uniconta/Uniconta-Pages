using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Provider
{
    public class SDCProductionServiceProvider : ProductionServiceProvider
    {
        public override X509Certificate2 RootCaCertificate
            => FromPemBlock(
                "MIIFtTCCA52gAwIBAgIBADANBgkqhkiG9w0BAQsFADB8MQswCQYDVQQGEwJESzER" +
                "MA8GA1UEBwwIQmFsbGVydXAxJDAiBgNVBAoMG1NrYW5kaW5hdmlzayBEYXRhIENl" +
                "bnRlciBBUzEOMAwGA1UECwwFZUJhbmsxJDAiBgNVBAMMG1NEQyBCYW5rIENvbm5l" +
                "Y3QgUHJpbWFyeSBDQTAeFw0xNTA1MTMxMzQyNTBaFw00NTA1MTMxMzQyNTBaMHwx" +
                "CzAJBgNVBAYTAkRLMREwDwYDVQQHDAhCYWxsZXJ1cDEkMCIGA1UECgwbU2thbmRp" +
                "bmF2aXNrIERhdGEgQ2VudGVyIEFTMQ4wDAYDVQQLDAVlQmFuazEkMCIGA1UEAwwb" +
                "U0RDIEJhbmsgQ29ubmVjdCBQcmltYXJ5IENBMIICIjANBgkqhkiG9w0BAQEFAAOC" +
                "Ag8AMIICCgKCAgEAxMH3Ws/4vaKGlHKKFpCLJT1H2oLR3k2l2mulR8qyAuUy4rDT" +
                "U35o/ex1r959xe+HVvG8x1Phxr6ER1CM8DxWR4uK7qZYjld+deWmpaCXoL/Ar+n1" +
                "mX5WRB8yyvXTfpImyBDZ499PXLZVmUIE7EoaBiq2W39wXOzhfP5Oe2MGmb1zfZR0" +
                "WgRteQ/yapss61wLr+WP4K0yf3BVPY0iKAgvQYvg1+4Wd+PtMTESGiCPoeu0GexR" +
                "pTPq/Dd/cG7TZPHwYUJq97X6jZiapJQGJ1iEkzAZFo2EwTqZBtnev01rUuRX+gC2" +
                "JlsdbX4GdEX0lk7LeoitteXYVJyqWWzimXKyAbM5AAghyl1my05/301G2EyRiEmq" +
                "FqJFu+dpIt3ivyw7YSBvUccv3ey9826d/6+YrzGTvsypXT0ANJ14fSyQ1SJJTYPn" +
                "flgknQ8dIqQs6iiKBeM96n9sRXBnafrrBh2z4YJ/fgjQhRcMm4snUuPmFj9Zc69C" +
                "4XLpIMtivLmoUnVB6BnpEz/Cj2jNaoDyMdMLcGEGey3FiBryghnHeUZD5AEMttmc" +
                "eiVyL849WizfvOUhTTKTXmgRuesJXXYMrlCxjQH6CDoDN1UDIyxxdNHr/uEK4ray" +
                "W2IvB1Xbu2SFczr7TnWReWt9uNVjF4RHSXRVw24bzvN8FOL22jILyz8RgX0CAwEA" +
                "AaNCMEAwHQYDVR0OBBYEFJ68l4CT4jkAbqiDgHmdFR8MsfDFMA8GA1UdEwEB/wQF" +
                "MAMBAf8wDgYDVR0PAQH/BAQDAgIEMA0GCSqGSIb3DQEBCwUAA4ICAQCiDDEz0lTY" +
                "Eusj/M6zfJ0tcU1RnT0TkC3V/tlBXXma/Q7RzkK0oRbKNiwBACYT4RJ6J+lV1pWg" +
                "VXHpjcIrxX1UPN9vqx9DNZzvrWKXTFqnOtemwVxu5eUxpG5nWneZKNtuLVkyA3Ja" +
                "XesuCkdq6AFFo6kGytfG6T2rpA3fl3b966WgEVum6wr9Yksr1qBCwVmp5wU2PlB2" +
                "tw0hB3qN9PPsKcgFwE9d7H21PrTG/G5o8qv5QqbiA9Z8VHxKrY9c2MHYR0NNrrSK" +
                "BH44Wme1+RCgmvbZdqdNryzeocpj53/wVjD6Oc8PoGEu51ddF6gWg+0BrbSGuSxO" +
                "xQYnAWZYEUUIzIen60FSe9RCmPDhyPMdFlFou65VjeFyUL4sBVru5RK9HDdPpqcq" +
                "Oc70FXeJu1uVV4Mbn0Rbot3IE4h9HejxmTwt8VezEnmsIoHocEHnFj3LXGnK8CpJ" +
                "3gEi2ZHWXAEfqy8bRlNiM4sjpY16wxE/Q42yXfXMnMVkjt8iNhiPJ2MHLlF8bm/e" +
                "51x3Km8ul8spnHZYhEkiSjgNwLihB1V2P721pepd/gLRcXvEF+ZUON2ULw0m5tj7" +
                "tpCiRjBBzFbz5Xc0mnZ35hvtCLZhs+GTuIJRWFN3mm0Z7vHrPZGUU/DH/ktW3+u8" +
                "q+vGKMnlb5e71IxcGKT+rDr6Plne8AdcOQ=="
                );

        public override string RegistrationNumber => "1671";
    }
}