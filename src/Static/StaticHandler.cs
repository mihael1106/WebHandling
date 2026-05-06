using Miki1106.WebHandling.Core;
using Miki1106.WebHandling.Response;
using System.Net;

namespace Miki1106.WebHandling.Static
{
    public class StaticHandler
    {
        public virtual ListenerResponse OnForbidden(string path, HttpListenerContext context)
        {
            return new ErrorPage(403, "<br>Invalid request");
        }

        public virtual ListenerResponse OnFile(string path, HttpListenerContext context)
        {
            return new FileResponse(path, false);
        }

        public virtual ListenerResponse OnDirectory(string path, HttpListenerContext context)
        {
            return new FileListBuilder("static", StaticWebHandler.StaticPath, path).SetDefault();
        }

        public virtual ListenerResponse OnNotFound(string path, HttpListenerContext context)
        {
            return new ErrorPage(404, $"<br>Path \"{path}\" does not exist");
        }
    }
}
