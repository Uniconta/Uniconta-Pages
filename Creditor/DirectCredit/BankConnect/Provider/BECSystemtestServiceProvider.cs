using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Provider
{
    public class BECSystemtestServiceProvider : TestServiceProvider
    {
        public override X509Certificate2 RootCaCertificate =>
            FromPemBlock("MIIFiTCCA3GgAwIBAgIBADANBgkqhkiG9w0BAQsFADBmMQswCQYDVQQGEwJESzEUMBIGA1UECxML" +
                         "QkVDU1NMQ0VSVFMxJDAiBgNVBAoTG0JFQyAtIEJhbmtlcm5lcyBFREIgQ2VudHJhbDEbMBkGA1UE" +
                         "AxMSQkVDIFJvb3QgQ0EgLSBUZXN0MB4XDTE1MDQwNzIyMDAwMFoXDTQ1MDQwNzIyMDAwMFowZjEL" +
                         "MAkGA1UEBhMCREsxFDASBgNVBAsTC0JFQ1NTTENFUlRTMSQwIgYDVQQKExtCRUMgLSBCYW5rZXJu" +
                         "ZXMgRURCIENlbnRyYWwxGzAZBgNVBAMTEkJFQyBSb290IENBIC0gVGVzdDCCAiIwDQYJKoZIhvcN" +
                         "AQEBBQADggIPADCCAgoCggIBAOq5qorQZ995QAuH9CF9JFWERfYPrBt2dy/gB0jxgS4fqg+aF0mI" +
                         "gjhhGtx0dcC93dCeDELCSg8Kt4osYaghiDH+KTZwgVdlqmQNQYUHY1sTQzKWH3e1OzYgUMsAjIo8" +
                         "H7E264ZvcR2dbxWvucXDVPaVjU9EBmCVgvlo3351IVkOVJKr2klcj1JCLSlXhxPKRccyJWMAcc37" +
                         "CwwoBNAyixEH3PDolwBnNuM+Hbn+xTfTYAO7iozGVLouimtvKnLpOKcf6Zv9y378woO208mBb51B" +
                         "9AAsYOTW5hQZseQjGDoKAj4AwXOfxdvzTNrFy1d19H0NWW/nW4pqt0sZhG6mY24QGjCouhw5+xQp" +
                         "wNNZC9wIIIG9tRJv3VKaikriAhXPJ9SBHpn47Z0v9kisQOhIyDLYPzRp8PyZ9MTeonwIBb4PFqbU" +
                         "xIRoanLMDNnmwYVd75aksjPaB+BHXQqfZwzfEitfSISD3btPP/tW5XfJr5vVbzkB/2ng2BqXJ2bB" +
                         "Vabac0tUtSLDD2j62o45Eq/nKCE6QKw0mujTY8rsBIy9rgII2HoahNS/ZJNxp9L7E9NnyIigTReD" +
                         "Yt0wuu7yJBozq7fjGDyerioDVM4iVo7zMQA9X6t9QNxTBr8/Vf7vPhcg3a/0B27cywdNEh1Vnq5S" +
                         "3KHDavx1AmAhgu/aLUCB7/VfAgMBAAGjQjBAMB0GA1UdDgQWBBQI5vgbtshDL6OHsz2rmgmJmG0H" +
                         "DzAPBgNVHRMBAf8EBTADAQH/MA4GA1UdDwEB/wQEAwICBDANBgkqhkiG9w0BAQsFAAOCAgEAcBBM" +
                         "XTMxZHrHZPx3mDg2QaEn/V0xVmH/giYl4VmTJjVILG+XoY32ZZOyuJuafXHf22Qo0kpqypeynBwI" +
                         "t1d0GoSfym2r6ZuQiLK7EdvmRXyqcCywePoOQLuVyW5cpGe4rtGhcrRtnL2dUeoBGEYsCxGDsMLg" +
                         "AntEjdjYBeUBlCd1keBkCJCf5d46fqJG+RDtf5pG4mWn1LtqrhnM7wHrN2/QfNgI6n60M7GXKt0B" +
                         "g3SzJ082YH+RCEmnQzQvgnNNdUhyNbNI2+DysdDfgFDunTZNZGWCMCwC8UrRewOjsMUIIdXMahfG" +
                         "DMsmbSv3NM13IzF0S5ohAcwuyoG/p7XS+SARzjoLny9EiNVVohO8imtyjdhEbZafzUAY8wyV6xv8" +
                         "xbh2FqWdbd7/8PQ5GjjxahRq3Aq3UzguqICjGI0Huu63S7QOli+zkfSzyjHJYuOEk25pjwKo2rBz" +
                         "oMZsVKnHiOz1iGbauvaOov7KC5VTy85vIPYA9/EHUvlQJ1EdnIgZTZAIjZjc26cnNddVUD7KSE8p" +
                         "Zavbqj4/fBoQx1TTjawlAYyfW5mv8re71bPB0uOfzESfNN5yGS1QtU/iwhdFSxTdKj6YTKV4/DnP" +
                         "2Dlnw0W+d1a4UDRer+ebFlF0ABqJ5iXSHW6lKnVABJa2RDnGwPhPMprK/ADK9YGBu8ZoJ5M=");

        public override string RegistrationNumber => "7570";
    }
}