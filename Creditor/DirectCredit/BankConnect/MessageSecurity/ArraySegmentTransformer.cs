using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using Uniconta.Common;

namespace SecuredClient.MessageSecurity
{
    public class ArraySegmentTransformer
    {
        private readonly ArraySegment<byte> buffer;
        private readonly BufferManager bufferManager;
        private readonly Stream messageStream;

        public ArraySegmentTransformer(ArraySegment<byte> buffer, BufferManager bufferManager)
        {
            this.buffer = buffer;
            this.bufferManager = bufferManager;
            CreateDocument();
        }

        public ArraySegmentTransformer(Stream messageStream)
        {
            this.messageStream = messageStream;
            CreateDocument();
        }

        private void CreateDocument()
        {
            var documentStream = messageStream ?? new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);

            Document = new XmlDocument {PreserveWhitespace = true, XmlResolver = null};
            Document.Load(documentStream);
        }

        public XmlDocument Document { get; private set; }

        public ArraySegment<byte> DocumentAsArraySegment => ToArraySegment();

        public byte[] DocumentAsBytes => ToBytes();


        private ArraySegment<byte> ToArraySegment()
        {
            var documentBytes = ToBytes();

            return ToArraySegment(documentBytes);
        }

        private ArraySegment<byte> ToArraySegment(byte[] documentBytes)
        {
            var bufferManagerBuffer = bufferManager.TakeBuffer(documentBytes.Length + buffer.Offset);
            Array.Copy(buffer.Array, 0, bufferManagerBuffer, 0, buffer.Offset);
            Array.Copy(documentBytes, 0, bufferManagerBuffer, buffer.Offset, documentBytes.Length);

            var byteArray = new ArraySegment<byte>(bufferManagerBuffer, buffer.Offset, documentBytes.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            return byteArray;
        }

        private byte[] ToBytes()
        {
            var memoryStream = UnistreamReuse.Create();
            var xmlWriter = XmlWriter.Create(memoryStream);

            Document.WriteTo(xmlWriter);
            xmlWriter.Flush();
            xmlWriter.Close();

            var documentBytes = memoryStream.ToArray();
            memoryStream.Release();
            return documentBytes;
        }
    }
}