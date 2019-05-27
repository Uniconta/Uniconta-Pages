using System;

namespace SecuredClient.Provider
{
    public abstract class ProductionServiceProvider : ServiceProvider
    {
        public override Uri Endpoint => new Uri("https://www.bankconnectservices.dk/2015/06/25/services/CorporateService");
    }
}
