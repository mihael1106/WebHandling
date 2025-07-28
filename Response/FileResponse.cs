using Miki1106.WebHandling.Core;
using System;
using System.Data;
using System.IO;
using System.Net;

namespace Miki1106.WebHandling.Response
{
    public class FileResponse : ListenerResponse
    {
        readonly string filename;
        readonly Stream stream;
        readonly bool download;

        public FileResponse(Stream file, string filepath, bool download = true)
        {
            stream = file;
            filename = filepath;
            this.download = download;
        }

        public FileResponse(string filepath, bool download = true)
        {
            stream = File.OpenRead(filepath);
            filename = filepath;
            this.download = download;
        }

        public override Stream GetResponse(HttpListenerContext context)
        {
            if (stream == null)
                return null;

            string escaped = Uri.EscapeDataString(Path.GetFileName(filename));
            string contentDisposition = $"{(download ? "attachment" : "inline")}; filename=\"{Path.GetFileName(filename)}\"; filename*=UTF-8''{escaped}";
            context.Response.ContentType = MimeTypes.GetMimeType(Path.GetExtension(filename));
            context.Response.Headers["Content-Disposition"] = contentDisposition;

            string rangeHeader = context.Request.Headers["Range"];
            if (rangeHeader != null)
            {
                long fileLength = stream.Length;

                string[] range = rangeHeader.Substring(6).Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                startAt = long.Parse(range[0]);
                long end = range.Length == 2 ? long.Parse(range[1]) : fileLength - 1;

                sendCount = end - startAt + 1;
                context.Response.StatusCode = 206;
                context.Response.Headers["Content-Range"] = $"bytes {startAt}-{end}/{fileLength}";
            }
            bufferSize = 1048576;
            return stream;
        }
    }
}
