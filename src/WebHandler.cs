using System;
using System.Collections.Generic;
using System.Net;

namespace Miki1106.WebHandling
{
    public class WebHandler
    {
        public static bool debug = false;

        internal static readonly Dictionary<string, Func<HttpListenerContext, ListenerResponse>> listeners = new Dictionary<string, Func<HttpListenerContext, ListenerResponse>>();
        private Func<string, HttpListenerContext, ListenerResponse> fallbackListener = null;
        public readonly string prefix;

        public WebHandler(string prefix)
        {
            this.prefix = GetPath(prefix);
            Router.Register(this);
        }

        internal string GetPath(string path)
        {
            if (path.Length != 0)
                if (path[0] == '/')
                    path = path.Substring(1);

            if (path.Length != 0)
                if (path[path.Length - 1] == '/')
                    path = path.Remove(path.Length - 1, 1);
            return path;
        }

        internal ListenerResponse GetResponse(string path, HttpListenerContext context)
        {
            if (fallbackListener != null)
            {
                if (debug)
                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Found fallback listener for \"{path}\"");
                return fallbackListener.Invoke(path.Substring(prefix.Length + 1), context);
            }
            else
            {
                if (debug)
                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{path}\" does not exist.");

                return new ErrorPage(404, $"<br>Path \"{path}\" does not exist.");
            }
        }

        public void AddListener(string path, Func<HttpListenerContext, ListenerResponse> listener)
        {
            listeners.Add("/" + GetPath($"{prefix}/{GetPath(path)}"), listener);
        }

        public void SetFallback(Func<string, HttpListenerContext, ListenerResponse> listener)
        {
            fallbackListener = listener;
        }

        public void RemoveListener(string path)
        {
            if (listeners.ContainsKey("/" + GetPath($"{prefix}/{GetPath(path)}")))
                listeners.Remove("/" + GetPath($"{prefix}/{GetPath(path)}"));
        }
    }
}