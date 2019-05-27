using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Provider
{
    public class BECProductionServiceProvider : ProductionServiceProvider
    {
        public override X509Certificate2 RootCaCertificate =>
            FromPemBlock("MIIFezCCA2OgAwIBAgIBADANBgkqhkiG9w0BAQsFADBfMQswCQYDVQQGEwJESzEU" +
                         "MBIGA1UECxMLQkVDU1NMQ0VSVFMxJDAiBgNVBAoTG0JFQyAtIEJhbmtlcm5lcyBF" +
                         "REIgQ2VudHJhbDEUMBIGA1UEAxMLQkVDIFJPT1QgQ0EwHhcNMTUwNDI3MjIwMDAw" +
                         "WhcNNDUwNDI5MjIwMDAwWjBfMQswCQYDVQQGEwJESzEUMBIGA1UECxMLQkVDU1NM" +
                         "Q0VSVFMxJDAiBgNVBAoTG0JFQyAtIEJhbmtlcm5lcyBFREIgQ2VudHJhbDEUMBIG" +
                         "A1UEAxMLQkVDIFJPT1QgQ0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoIC" +
                         "AQC7lhZvl0AToynYU3S8Yj5qiad29hhfH5OtZoYLpmRP8HAt1kzmksAN2PuN03UA" +
                         "xAaz1Nlt3v8KaobHTnm9Fjy2aFjJxaOV+wNqqLHbEvD/ik9YlRa7IxgLuHwo6r9M" +
                         "njkL/EEYBLje1xOwGxxRuHGmd0bPz3C1Zc8PgsnO6c5Q8H5YqpfanAezIrX/ynJn" +
                         "A1jKyavk6cRrgk8z3npMXprVK3S6gnzs7vnd8sL3oGrkpL2FHexDLkMzDUAfsD1z" +
                         "EaQpNk9vQYi3nqKUjRToev50K/I9DPlRTN7ynxkuqDGkRLd3u2Wc8bQ9m/0tQLl5" +
                         "7lyZIb3rqpYKZ9XbWzfU92u0U6ssoT6L+Tcq1LjvDcnc94AV5r7l55eO+FP2Y0jP" +
                         "kAe04vKQS+zkIs+xmyC1le4RkgVlqKTtqgG1533tPc/NKTBvfUI7zC476n6ciGbk" +
                         "qSEWCdEYtJsR48rfSF5ZMRUU7bJgHsRJQ9UyIASqG1ghBw8nUgPzJYdfq6AD3yGP" +
                         "n2+xiMhTHDSPNg2eS1nfqdJBySdoS1RynznyxjvzmDFWoMEYL1B+HP0z2vW5Pv5h" +
                         "zcm8Gw8lfe6ylMw4u+uEdML+jtD++HDi8VRE1My5LOdNlR7JBLglbZDJIYTbXbcp" +
                         "cSu0eijmTVgQNomthppQFM+TYwyz1U5Q0/4HV+t9bH5z+QIDAQABo0IwQDAOBgNV" +
                         "HQ8BAf8EBAMCAgQwHQYDVR0OBBYEFGpAmicwduoHmXpLtfOSVUdNkWWkMA8GA1Ud" +
                         "EwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggIBAKi+9od1eXl1lm2VFoaLJF9O" +
                         "aSRYqtTyaucBbJakIgJ9dw+Lcc1pfqs0gl8HsCaVEb9w9TMUbeAAZvbEfz/83sc2" +
                         "hkhzWj4n3MBnClsgx1oOOb5JoRJPk3TUUQbvylm6juR4Yq9ZGCzrx1miAk55qrs4" +
                         "+JGDdgOgD7qGX501Y5nC//S2FkU+dwF8X5QvZfsWLeQ7dZ3n+Tf1txBQBFLL6Pnw" +
                         "uJ49m0NzBTXiw3mp2LbivQDlcSUi7ysyAPwWQdlNqos6ndzi6L49/cnxtABU9Xww" +
                         "txNQtuxQ+WFpSxozeUxUVwmtkb5F/bFnXF2ZPfUNaWHqUk8Y28+UtaajqGFdtocf" +
                         "ewhhAmEmfahCyEGvuCSSlZf6jfI2jNlmsV39ken/VbZdxfHIMnPw97wfx9e8RprW" +
                         "zPi38oNt7R7BodmyQWAzMbjLdfZQf5VTc2de7ziVol4SLzOfODQSwlwNTAWK4ry7" +
                         "RkKZtu8Ux7sX7mDvch/04hVbkGgttxINW6LGufrBlmMvkprEpEm0zWvGH4G23KdB" +
                         "fQzvi7X1tP55L+7Lhd7/00I/ODspDN99P3aarPnxOse9h/T3MH+4UTAlsNl996qV" +
                         "wN9cDz3fAX6gbZUF3jiMKidbUC4qMKcnm0YkirbFSBF95ifuMFdDwJTm2ZcposWg" +
                         "4BXJ82f6PnmuOUOzL7eG");

        public override string RegistrationNumber => "7570";
    }
}