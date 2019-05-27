using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Provider
{
    public class SDCSystemtestServiceProvider : TestServiceProvider
    {
        public override X509Certificate2 RootCaCertificate
            => FromPemBlock("MIIFwTCCA6mgAwIBAgIBADANBgkqhkiG9w0BAQsFADCBgTELMAkGA1UEBhMCREsx" +
                            "ETAPBgNVBAcMCEJhbGxlcnVwMSQwIgYDVQQKDBtTa2FuZGluYXZpc2sgRGF0YSBD" +
                            "ZW50ZXIgQVMxDjAMBgNVBAsMBWVCYW5rMSkwJwYDVQQDDCBTREMgQmFuayBDb25u" +
                            "ZWN0IFByaW1hcnkgQ0EgVGVzdDAeFw0xNTA0MTMwNzQzMzdaFw00NTA0MTMwNzQz" +
                            "MzdaMIGBMQswCQYDVQQGEwJESzERMA8GA1UEBwwIQmFsbGVydXAxJDAiBgNVBAoM" +
                            "G1NrYW5kaW5hdmlzayBEYXRhIENlbnRlciBBUzEOMAwGA1UECwwFZUJhbmsxKTAn" +
                            "BgNVBAMMIFNEQyBCYW5rIENvbm5lY3QgUHJpbWFyeSBDQSBUZXN0MIICIjANBgkq" +
                            "hkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAme8s9oo/cU6hhoRt6x8AzMt1b6IHXmvC" +
                            "cNLKqtjgpniDYXsl3TMVn4l2PgDzd8gTsXwti/S1A6dFc7gR3mjFRlCyNhLB1JHu" +
                            "TzLTRFfNKlqW/eZumPq/blrex6hrL4Ig44dfQbvTM5n/k7J2OCB68TE7APsqJaXO" +
                            "I3shzAHb0H3iShziiTEJORxy+htCLhxkrdUdiZbSeik3dPZ26YPtROBKAOaQ5e2S" +
                            "dsJ8F2YIuJk/xDnCpiuHaAmspsAAiWDqhg3OJSkaub/n+aji/EpHwuvo+imFvBFP" +
                            "qVR16/JK74GwCngHMonNjKBOoEB0XXkooNreRcyVa4FeZ/Za/fZGn89w218o+4jh" +
                            "gDUD4nI3DWHw9ETDoasKWtlRRjPqKaSZoiQIXZfrvpXRvqJn3I+GNjnZjwlXTPIc" +
                            "um/b9W4+p14jQYX3sMBNwW5ZK9HbEqZgDX1YIKhvaG5U1e1nkxp3VkOaL9hcR6/K" +
                            "0BjSXhcSsAaBrGOwQ2ZYgSAkfchw3htCdvYJZhPhrqEML4r7k3omjVtpISJ8znwu" +
                            "flpr9VLj9u5I8NRXI3Vk8qIP2XtxDzHdzbOuUjejpDhe5hpN+Lvhv/DeWatWG87d" +
                            "DLeeEMkK9SfpbEO+yrHP/mUKpvNlKOw3X5nE9GJYLMEgZB9rO07K7pHWyHECugf+" +
                            "ALFYMP+g3T0CAwEAAaNCMEAwHQYDVR0OBBYEFHfQywsRrYKBkiiv550YBkn9mzKW" +
                            "MA8GA1UdEwEB/wQFMAMBAf8wDgYDVR0PAQH/BAQDAgIEMA0GCSqGSIb3DQEBCwUA" +
                            "A4ICAQA/htebI2s0tIMX/8kvy1QL55fqiBzrMMkdFq+MSCdLKY4rxWvdPvDfe3mv" +
                            "IUrY4rlTx/fR7kXIWrj1Bd6VffgYrRuAyx3fHFz8b38Vj7tmrqlYpYcnSPlzyXhD" +
                            "kmBpj0M5XDukQLvzCU7uzB+f79h0hCWxm0zAokVcE+EZTyVxFGE5jOaV+Evz0fpM" +
                            "liuGCVtRNYe2qiiFYDRUovXglv9JuFSY7bnWFLoHzWxeEMFq9zWcQ43h0GTWyP7f" +
                            "xqxqtlpSEB/3WOZPwl+M+HpNKDptoHDR7Idt5V8eTMtQfeEXPHYwRNOjRo/vhEuX" +
                            "EzE55AwCTdp2dE04+tVSbLFT9MGzrZJaqQAEakau3ocmW6oZT4xnevY3Sifo6GAO" +
                            "LSuqw0GwRrIsDcvg9lt8e+w6QKrx3KBK010C9Pz7Vm6tuZZRisP+b3hYA3ci2D0F" +
                            "aDKo6UY7tL07aAWmmEVPhTs+e9UV7Mz+8Wx0FS884PAhyHjpzw2Ay1y8l2l8Whtg" +
                            "XLhfw8illdMOaGCoIjlrY9VJmNARyzi8M2CH+RhXrchd8sDLmzP4t/CJJOaepVhq" +
                            "noDLiamD8YBZONnHwt3zirG9NFffgIo0715ResKs/Ks0/AfMBdSy6VkwPzD2Kfcr" +
                            "1bh+cvWjce1A+8BnhNPJFonBsav+0U5oVSYO8XCdVlhgb79BFA==");

        public override string RegistrationNumber => "1671";
    }
}