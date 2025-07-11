using System.IO;
using System.Net;

namespace Miki1106.WebHandling
{
    public class ListenerResponse
    {
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
