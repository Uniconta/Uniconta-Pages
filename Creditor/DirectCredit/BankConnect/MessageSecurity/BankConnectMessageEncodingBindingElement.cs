using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;

namespace SecuredClient.MessageSecurity
{
    internal static class BankConnectMessageEncodingPolicyConstants
    {
        public const string BCEncodingName = "BankConnectEncoding";
        public const string BCEncodingNamespace = "http://bankconnect.dk/wcf/encoding";
        public const string BCEncodingPrefix = "bc";
    }

    public sealed class BankConnectMessageEncodingBindingElement : MessageEncodingBindingElement, IPolicyExportExtension
    {
        private MessageEncodingBindingElement innerBindingElement;

        public BankConnectMessageEncodingBindingElement()
            : this(new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8))
        {
        }

        public BankConnectMessageEncodingBindingElement(MessageEncodingBindingElement messageEncoderBindingElement)
        {
            innerBindingElement = messageEncoderBindingElement;
        }

        public MessageEncodingBindingElement InnerMessageEncodingBindingElement
        {
            get { return innerBindingElement; }
            set { innerBindingElement = value; }
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new BankConnectMessageEncoderFactory(innerBindingElement.CreateMessageEncoderFactory());
        }

        public override MessageVersion MessageVersion
        {
            get { return innerBindingElement.MessageVersion; }
            set { innerBindingElement.MessageVersion = value; }
        }

        public override BindingElement Clone()
        {
            return new BankConnectMessageEncodingBindingElement(this.innerBindingElement);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            return typeof (T) == typeof (XmlDictionaryReaderQuotas) ? innerBindingElement.GetProperty<T>(context) : base.GetProperty<T>(context);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        void IPolicyExportExtension.ExportPolicy(MetadataExporter exporter, PolicyConversionContext policyContext)
        {
            if (policyContext == null)
            {
                throw new ArgumentNullException(nameof(policyContext));
            }
            var document = new XmlDocument();
            policyContext.GetBindingAssertions().Add(document.CreateElement(
                BankConnectMessageEncodingPolicyConstants.BCEncodingPrefix,
                BankConnectMessageEncodingPolicyConstants.BCEncodingName,
                BankConnectMessageEncodingPolicyConstants.BCEncodingNamespace));
        }
    }

}