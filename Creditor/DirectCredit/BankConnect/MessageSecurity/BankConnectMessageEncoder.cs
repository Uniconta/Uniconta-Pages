using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using SecuredClient.XmlSecurity;

namespace SecuredClient.MessageSecurity
{
    /// <summary>
    /// This WCF message encoder serves one purpose: Changing the mustUnderstand attribute value to "0", to 
    /// prevent the WCF stack from rejecting the messages (no WCF WS-Security processing is enabled).
    /// This is required to allow us to handle the security in (the simpler context of) SecurityInterceptor.
    /// </summary>
    public sealed class BankConnectMessageEncoder : MessageEncoder
    {
        private readonly MessageEncoder innerEncoder;

        /// <summary>
        /// Change the value if this const to true, to have all SOAP messages written to Console.
        /// </summary>
        private const bool WriteMessagesToConsole = false;

        internal BankConnectMessageEncoder(MessageEncoder messageEncoder)
        {
            if (messageEncoder == null)
            {
                throw new ArgumentNullException("messageEncoder");
            }
            innerEncoder = messageEncoder;
        }

        public override string ContentType => innerEncoder.ContentType;

        public override string MediaType => innerEncoder.MediaType;

        public override MessageVersion MessageVersion => innerEncoder.MessageVersion;

        public override bool IsContentTypeSupported(string contentType)
        {
            return contentType.StartsWith("text/xml");
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            var converter = new ArraySegmentTransformer(buffer, bufferManager);

            var document = converter.Document;
            var manager = new NamespaceManager(document);
            var securityElement = document.SelectSingleNode("//wsse:Security", manager) as XmlElement;
            securityElement?.SetAttribute("mustUnderstand", manager.LookupNamespace("soap"), "0");

            if (WriteMessagesToConsole)
            {
                Console.WriteLine("INCOMING MESSAGE: " + document.OuterXml);
            }

            var result = converter.DocumentAsArraySegment;
            return innerEncoder.ReadMessage(result, bufferManager, contentType);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            if (WriteMessagesToConsole)
            {
                var messageTransformer = new MessageTransformer(message);
                var xmlDocument = messageTransformer.GetXmlDocument();
                Console.WriteLine("OUTGOING MESSAGE: " + xmlDocument.OuterXml);
                return innerEncoder.WriteMessage(messageTransformer.ToMessage(xmlDocument), maxMessageSize, bufferManager, messageOffset);
            }
            return innerEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            return innerEncoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            innerEncoder.WriteMessage(message, stream);
        }
    }
}