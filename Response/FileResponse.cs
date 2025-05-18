using Miki1106.WebHandling.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Mime;

namespace Miki1106.WebHandling.Response
{
    public class FileResponse : ListenerResponse
    {
        readonly string filename;
        readonly Stream stream;

        public FileResponse(Stream file, string filepath)
        {
            stream = file;
            filename = filepath;
        }

        public FileResponse(string filepath)
        {
            stream = File.OpenRead(filepath);
            filename = filepath;
        }

        public override Stream GetResponse(HttpListenerContext context)
        {
            if (stream == null)
                return null;
            FileInfo fileInfo = new FileInfo(filename);
            ContentDisposition cd = new ContentDisposition
            {
                FileName = Path.GetFileName(filename),
                DispositionType = DispositionTypeNames.Attachment,
                Inline = false,
                Size = stream.Length,
                ReadDate = fileInfo.LastAccessTimeUtc,
                ModificationDate = fileInfo.LastWriteTimeUtc,
                CreationDate = fileInfo.CreationTimeUtc
            };
            context.Response.ContentType = MimeTypes.GetMimeType(Path.GetExtension(filename));
            context.Response.AddHeader("Content-Disposition", cd.ToString());
            Console.WriteLine(cd.ToString());
            return stream;
        }
    }
}
