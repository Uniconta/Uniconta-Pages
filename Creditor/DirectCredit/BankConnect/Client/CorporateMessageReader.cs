using System.IO;
using System.IO.Compression;
using System.Xml;

namespace SecuredClient.Client
{
    public class CorporateMessageReader
    {
        public CorporateMessageReader(corporateMessage message)
        {
            Content = new XmlDocument();
            if (message.compressed == "0")
            {
                Content.Load(new MemoryStream(message.content));
            }
            else
            {
                var stream = new GZipStream(new MemoryStream(message.content), CompressionMode.Decompress);
                Content.Load(stream);
            }
        }

        public XmlDocument Content { get; }
    }
}
