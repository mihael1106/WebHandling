using Miki1106.WebHandling.Core;
using Miki1106.WebHandling.Response;
using System;
using System.IO;

namespace Miki1106.WebHandling
{
    public static class StaticHandler
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
        private static WebHandler handler;

        internal static void StaticWeb()
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
                        return new ErrorPage(403, "<br>Invalid request");
                    }

                    if (File.Exists(fullPath))
                    {
                        string mimeType = MimeTypes.GetMimeType(Path.GetExtension(fullPath));
                        if (WebHandler.debug)
                            Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Found mime type: {mimeType}");

                        return new FileResponse(fullPath, false);
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        return new FileListBuilder("static", StaticPath, path).SetDefault();
                    }
                    else
                    {
                        Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{path}\" does not exist.");
                        context.Response.StatusCode = 404;
                        return new ErrorPage(404, $"<br>Path \"{path}\" does not exist");
                    }
                });
            }
        }
    }
}
