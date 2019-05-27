using System.ServiceModel;
using System.ServiceModel.Channels;
using SecuredClient.MessageSecurity;

namespace SecuredClient.Client
{
    public class SecuredCorporateServiceClient : CorporateServiceClient
    {
        public SecuredCorporateServiceClient(EndpointAddress endpoint) : base(GetCustomBinding(endpoint), endpoint)
        {
            //var contextScope = new OperationContextScope(InnerChannel);
        }

        private static Binding GetCustomBinding(EndpointAddress endpoint)
        {
            var customBinding = new CustomBinding();
            customBinding.Elements.Add(new BankConnectMessageEncodingBindingElement());
            customBinding.Elements.Add(endpoint.Uri.Scheme == "http" ? new HttpTransportBindingElement() : new HttpsTransportBindingElement());

            return customBinding;
        }
    }
}