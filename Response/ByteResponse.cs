using System.IO;

namespace Miki1106.WebHandling.Response
{
    public class ByteResponse : ListenerResponse
    {
        private readonly byte[] data;

        public ByteResponse(byte[] data)
        {
            this.data = data;
        }

        protected override Stream GetResponse()
        {
            return new MemoryStream(data, false);
        }
    }
}
