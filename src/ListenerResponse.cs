using System.IO;
using System.Net;

namespace Miki1106.WebHandling
{
    public class ListenerResponse
    {
        public long startAt = 0;
        public long sendCount = -1;
        public int bufferSize = 131072;

        protected virtual Stream GetResponse()
        {
            return null;
        }

        public virtual Stream GetResponse(HttpListenerContext context)
        {
            return GetResponse();
        }
    }
}
