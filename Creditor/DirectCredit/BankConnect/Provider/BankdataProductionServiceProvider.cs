using System.Security.Cryptography.X509Certificates;

namespace SecuredClient.Provider
{
    public class BankdataProductionServiceProvider : ProductionServiceProvider
    {
        public override X509Certificate2 RootCaCertificate =>
            FromPemBlock("MIIFnTCCA4WgAwIBAgIBADANBgkqhkiG9w0BAQsFADBwMSIwIAYDVQQDExlCRCBC" +
                         "YW5rQ29ubmVjdCBQcmltYXJ5IENBMRwwGgYDVQQHExNFcnJpdHNvZSBGcmVkZXJp" +
                         "Y2lhMQwwCgYDVQQLEwNTSUsxETAPBgNVBAoTCEJhbmtkYXRhMQswCQYDVQQGEwJE" +
                         "SzAeFw0xNTA1MDUyMjAwMDBaFw00NTA1MDUyMjAwMDBaMHAxIjAgBgNVBAMTGUJE" +
                         "IEJhbmtDb25uZWN0IFByaW1hcnkgQ0ExHDAaBgNVBAcTE0Vycml0c29lIEZyZWRl" +
                         "cmljaWExDDAKBgNVBAsTA1NJSzERMA8GA1UEChMIQmFua2RhdGExCzAJBgNVBAYT" +
                         "AkRLMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAwi4CfFY0XGqIyhg4" +
                         "80terThMc8mJ6beeHG2QfYHYys5dc+vC74byrdHy2g06iG955whojFMUqRKZgX2i" +
                         "wr2U2qwhGdNTkalCtlHfOsuoHCYBsi9pyumzO33iFNsqKfXYegj2Adth1X8sushF" +
                         "odX8OtV0RPNe6aEHOOzv0SaJu2+Pz01xl9wdVOq84mAmlatkpGLBeReqYqq2TeBd" +
                         "vSv4vc//VVfS9iCJI3x8jXyu7hMH0uA5mXyRCj/n3rPiNmebXVttkoXU31EJ4lWT" +
                         "cqT8YhrHn6+bZWtEm5I59FnvyI4uoEJLlKmwMaxRwqfWWBHIT+eY0WwxAr73AWPy" +
                         "XfBWnWAyT7QVYZ3MZ/yi/tBbUOh97EBcESHeiVFnQmU5573VRSpIi9ICwD/OYMXl" +
                         "TGqdhz6rSKUjJRuKp/rOJ14tPbi6DqBZCqYqtc/HZxj7va3LM8lHKeb89MaEb5la" +
                         "9T76Z6FWRCmLuLNlWD2DvrM/vzVrsVrwgQSQnkVfIZ5fDOQXuDB3pkXvlqgzwgAQ" +
                         "urHYhMs+tHedy/0hvWgGzU1ruvyRhQOzfs5C2lvXnL4IcMYVpt4zRBRUjtgN8gPI" +
                         "2a68n3oj/8jQDwQNctp09RysKZ/0A8CRibGr5m9Zkqpla4+iVN79zjC9STdtIkW+" +
                         "5fFRE5G+GAHdDMnEy051iSYKckUCAwEAAaNCMEAwHQYDVR0OBBYEFB10DmUNN6HV" +
                         "dka9to/jCDpSmHUuMA4GA1UdDwEB/wQEAwICBDAPBgNVHRMBAf8EBTADAQH/MA0G" +
                         "CSqGSIb3DQEBCwUAA4ICAQBzWY6cZu4wIirku1a3i/U76WUInfOfR4n929oO9av4" +
                         "FUZ8rVRcExEGROioaQs3qW2Js6AZJDMlG/W8nuD40YJS89tGsdKx4iLn6S8HEmSK" +
                         "1MoUiw5YBBSqrzLm5sr54PnZOeIrYnWOSseMmfwZe/BwKpt2YQlqwn+etIWTnNlP" +
                         "JFwvJ1YUsDFNiwaY7XCvZT56GAbeYOESrs74Tu/0koAPjP4IjCgQ2U955xHFHPHG" +
                         "/oBpl+D9AWtVWGVBPSRacljbrKWa4R+p0qx0D7qUGBVYlCiXodEaEXtT0jgyAYmL" +
                         "uYb8WQ6GbHnQR7sEM90HlpnH0Cqa+Zq1ZDUIeeYj1Lj/rr8DHYUFcGxw1sfPL6I8" +
                         "pELqLrBJt7EyKd1CwRJf7BNcxQi3KXzmyg1a3HtvnR/fMFuwu5ICHj9/esZAH4XF" +
                         "vTerWwHsO32WAyKE16FllQonAeBkqe2v24nPmJa3B22DKeHKAJGtnqmXTvlYXZJV" +
                         "tJ6s5n6nNMzPNxrI4YdYnFRJe8TWE9+gfENcxmr7QljCz56ZG7/hw3yMvKHaCcwe" +
                         "nMw8dvCM+Lo/TOHW4BJPFDAnNzEnyBaMx4xQ1f7xYYvFUTvY/pOuwguzflJgDw3i" +
                         "rbJml664aN6UGP2eunLqxJnwbPwsXxspnvL9m1cF2prnz1PoKIVrVQtR+oFGXMPI" +
                         "aQ==");

        public override string RegistrationNumber => "8079";
    }
}