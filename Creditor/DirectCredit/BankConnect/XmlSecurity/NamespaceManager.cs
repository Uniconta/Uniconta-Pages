using System.Xml;

namespace SecuredClient.XmlSecurity
{
    public sealed class NamespaceManager : XmlNamespaceManager
    {
        public const string WsseNamespaceUri = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        public const string DsNamespaceUri = "http://www.w3.org/2000/09/xmldsig#";
        public const string WsuNamespaceUri = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        public const string SoapNamespaceUri = "http://schemas.xmlsoap.org/soap/envelope/";
        public const string BankConnectNamespaceUri = "http://bankconnect.dk/schema/2014";

        public NamespaceManager(XmlDocument document) : base(document.NameTable)
        {
            AddNamespace("ds", DsNamespaceUri);
            AddNamespace("xenc", "http://www.w3.org/2001/04/xmlenc#");
            AddNamespace("wsse", WsseNamespaceUri);
            AddNamespace("wsu", WsuNamespaceUri);
            AddNamespace("soap", SoapNamespaceUri);
            AddNamespace("bc", BankConnectNamespaceUri);
            AddNamespace("wsse11", "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd");
        }
    }
}