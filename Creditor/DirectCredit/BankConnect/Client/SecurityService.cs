using System.Xml;
using SecuredClient.XmlSecurity;

namespace SecuredClient.Client
{
    public interface SecurityService
    {
        void WrapRequest(XmlDocument request, OperationPolicy operationPolicy);

        void UnwrapResponse(XmlDocument response, OperationPolicy operationPolicy);
    }
}
