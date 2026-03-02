using System.IO;

namespace Miki1106.WebHandling.Response
{
    public class StreamResponse : ListenerResponse
    {
        private readonly Stream stream;

        public StreamResponse(Stream stream)
        {
            this.stream = stream;
        }

        protected override Stream GetResponse()
        {
            return stream;
        }
    }
}
