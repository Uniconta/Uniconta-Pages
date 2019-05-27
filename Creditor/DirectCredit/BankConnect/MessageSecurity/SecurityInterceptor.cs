using System;
using System.Net.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.Xml;
using SecuredClient.Client;
using SecuredClient.XmlSecurity;

namespace SecuredClient.MessageSecurity
{
    public class SecurityInterceptor : IClientMessageInspector
    {
        private bool debug = true;
        private readonly SecurityService securityService = new SecurityServiceImpl();
        private readonly ThreadLocal<string> currentAction = new ThreadLocal<string>();

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var action = request.Headers.Action;
            currentAction.Value = action;

            var policy = OperationPolicy.GetPolicyForAction(action);
            var transformer = new MessageTransformer(request);
            var requestDocument = transformer.GetXmlDocument();

            if (debug)
            {
                Console.WriteLine("### Request not wrapped: " + XmlUtil.PrettyXml(requestDocument));
            }
            securityService.WrapRequest(requestDocument, policy);

            if (debug)
            {
                Console.WriteLine("### Request wrapped: " + XmlUtil.AsText(requestDocument));
            }

            request = transformer.ToMessage(requestDocument);
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var action = currentAction.Value;
            var policy = OperationPolicy.GetPolicyForAction(action);
            var transformer = new MessageTransformer(reply);
            var responseDocument = transformer.GetXmlDocument();

            if (!IsFault(responseDocument))
            {
                if (debug) Console.WriteLine("### Response wrapped: " + XmlUtil.AsText(responseDocument));
                securityService.UnwrapResponse(responseDocument, policy);
            }
            if (debug) Console.WriteLine("### Response unwrapped: " + XmlUtil.PrettyXml(responseDocument));

            currentAction.Value = null;
            reply = transformer.ToMessage(responseDocument);
        }

        private static bool IsFault(XmlDocument reply)
        {
            var faultElement = reply.SelectSingleNode("//soap:Envelope/soap:Body/soap:Fault", new NamespaceManager(reply));
            return faultElement != null;
        }
    }
}