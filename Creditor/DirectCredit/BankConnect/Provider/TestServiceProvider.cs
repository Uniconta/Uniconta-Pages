using System;

namespace SecuredClient.Provider
{
    public abstract class TestServiceProvider : ServiceProvider
    {
        public override Uri Endpoint => new Uri("https://stest.bankconnect.dk/2015/06/25/services/CorporateService/");
    }
}