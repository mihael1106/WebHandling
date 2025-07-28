using System;
using System.Collections.Generic;
using System.Net;

namespace Miki1106.WebHandling
{
    public static class Router
    {
        private static List<WebHandler> handlers = new List<WebHandler>();
        
        public static void Register(WebHandler handler)
        {
            handlers.Add(handler);
        }

        public static ListenerResponse FindRoute(string path, HttpListenerContext context)
        {
            if (path.EndsWith("/"))
                path = path.Remove(path.Length - 1, 1);
            if (path == "/throw" && WebHandler.debug)
            {
                throw new Exception("A debug exception has been thrown");
            }

            WebHandler bestHandler = null;
            int bestLength = -1;

            foreach (WebHandler handler in handlers)
            {
                if (path.StartsWith("/" + handler.prefix, StringComparison.OrdinalIgnoreCase))
                {
                    int len = handler.prefix.Length;
                    if (len > bestLength)
                    {
                        bestHandler = handler;
                        bestLength = len;
                    }
                }
            }
            
            if(bestHandler != null)
            {
                return bestHandler.GetResponse(path, context);
            }
            else
            {
                if (WebHandler.debug)
                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{path}\" does not exist.");
                return new ErrorPage(404, $"<br>Path \"{path}\" does not exist.");
            }
        }
    }
}
