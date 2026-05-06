using System;
using System.IO;

namespace Miki1106.WebHandling.Static
{
    public static class StaticWebHandler
    {
        private static string _staticPath = "static";
        public static string StaticPath
        {
            get => _staticPath;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    throw new ArgumentException("The path contains invalid characters or is empty.", nameof(value));
                }

                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                _staticPath = value;
            }
        }
        private static StaticHandler _staticHandler = new StaticHandler();

        public static void SetHandler(StaticHandler staticHandler)
        {
            _staticHandler = staticHandler ?? throw new ArgumentNullException(nameof(staticHandler));
        }

        private static WebHandler handler;

        public static void StaticWeb()
        {
            if (handler == null)
            {
                handler = new WebHandler("static");
                handler.SetFallback((path, context) =>
                {
                    if (path.Length > 0)
                        if (path[0] == '/')
                            path = path.Substring(1);
                    string fullPath = Path.GetFullPath(Path.Combine(StaticPath, path));

                    if (!fullPath.StartsWith(Path.GetFullPath(StaticPath), StringComparison.OrdinalIgnoreCase))
                    {
                        return _staticHandler.OnForbidden(path, context);
                    }

                    if (File.Exists(fullPath))
                    {
                        return _staticHandler.OnFile(fullPath, context);
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        return _staticHandler.OnDirectory(path, context);
                    }
                    else
                    {
                        if(WebHandler.debug)
                            Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{path}\" does not exist.");
                        return _staticHandler.OnNotFound(path, context);
                    }
                });
            }
        }
    }
}
