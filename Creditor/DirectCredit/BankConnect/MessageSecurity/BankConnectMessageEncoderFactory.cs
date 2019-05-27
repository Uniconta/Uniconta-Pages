using System;
using System.ServiceModel.Channels;

namespace SecuredClient.MessageSecurity
{
    internal class BankConnectMessageEncoderFactory : MessageEncoderFactory
    {
        readonly MessageEncoder encoder;

        public BankConnectMessageEncoderFactory(MessageEncoderFactory messageEncoderFactory)
        {
            if (messageEncoderFactory == null)
            {
                throw new ArgumentNullException(nameof(messageEncoderFactory));
            }
            encoder = new BankConnectMessageEncoder(messageEncoderFactory.Encoder);
        }

        public override MessageEncoder Encoder => encoder;

        public override MessageVersion MessageVersion => encoder.MessageVersion;
    }
}
